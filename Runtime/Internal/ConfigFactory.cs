using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Byrniee.UnityConfiguration.Internal
{
    /// <summary>
    /// Factory class for creating the config classes.
    /// </summary>
    public class ConfigFactory : IConfigFactory, IConfigDiagnostics
    {
        private static readonly JsonMergeSettings MergeSettings = new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
            MergeNullValueHandling = MergeNullValueHandling.Merge,
        };

        private static readonly JsonLoadSettings LoadSettings = new JsonLoadSettings
        {
            CommentHandling = CommentHandling.Ignore,
        };

        private readonly JObject configFile;
        private readonly HashSet<string> requestedSections = new HashSet<string>(StringComparer.Ordinal);
        private readonly object requestedSectionsLock = new object();

        /// <summary>
        /// Initialises a new instance of the <see cref="ConfigFactory"/> class, reading from
        /// <see cref="ConfigEnvironment.DefaultConfigDirectory"/>.
        /// </summary>
        public ConfigFactory()
            : this(ConfigEnvironment.DefaultConfigDirectory)
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="ConfigFactory"/> class, reading from
        /// <paramref name="configDirectory"/>.
        /// </summary>
        /// <param name="configDirectory">The directory containing the config and .env files.</param>
        public ConfigFactory(string configDirectory)
            : this(configDirectory, Environment.GetEnvironmentVariable, GetProcessEnvironmentVariables())
        {
        }

        internal ConfigFactory(string configDirectory, Func<string, string> getEnvironmentVariable)
            : this(configDirectory, getEnvironmentVariable, Enumerable.Empty<KeyValuePair<string, string>>())
        {
        }

        internal ConfigFactory(
            string configDirectory,
            Func<string, string> getEnvironmentVariable,
            IEnumerable<KeyValuePair<string, string>> environmentVariables)
        {
            IReadOnlyList<KeyValuePair<string, string>> environmentFileEntries =
                ConfigEnvironment.ReadEnvironmentFile(Path.Combine(configDirectory, ConfigEnvironment.EnvironmentFilename));
            EnvironmentName = ConfigEnvironment.ResolveEnvironmentName(getEnvironmentVariable, environmentFileEntries);

            configFile = new JObject();
            List<string> loadedFiles = new List<string>();
            foreach (string fileName in ConfigEnvironment.GetLayerFileNames(EnvironmentName))
            {
                string filePath = Path.GetFullPath(Path.Combine(configDirectory, fileName));
                if (!File.Exists(filePath))
                {
                    continue;
                }

                configFile.Merge(ReadConfigFile(filePath), MergeSettings);
                loadedFiles.Add(filePath);
            }

            LoadedFiles = loadedFiles.AsReadOnly();

            // Overrides are applied after every layer file, .env entries first so process environment
            // variables win.
            List<string> overriddenKeys = new List<string>();
            ApplyOverrides(environmentFileEntries, overriddenKeys);
            ApplyOverrides(environmentVariables, overriddenKeys);
            OverriddenKeys = overriddenKeys.AsReadOnly();
        }

        /// <inheritdoc />
        public string EnvironmentName { get; }

        /// <inheritdoc />
        public IReadOnlyList<string> LoadedFiles { get; }

        /// <inheritdoc />
        public IReadOnlyList<string> OverriddenKeys { get; }

        /// <inheritdoc />
        public IConfig<T> Create<T>(string index)
        {
            lock (requestedSectionsLock)
            {
                requestedSections.Add(index);
            }

            Config<T> config = new Config<T>();

            JToken section = configFile[index];
            if (section != null && section.Type != JTokenType.Null)
            {
                config.Value = section.ToObject<T>();
            }
            else
            {
                // Create new or default.
                config.Value = JsonConvert.DeserializeObject<T>("{}");
            }

            return config;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<string> GetUnrequestedSections()
        {
            lock (requestedSectionsLock)
            {
                return configFile.Properties()
                    .Select(x => x.Name)
                    .Where(x => !requestedSections.Contains(x))
                    .ToList()
                    .AsReadOnly();
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> GetProcessEnvironmentVariables()
        {
            return Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .Select(x => new KeyValuePair<string, string>((string)x.Key, (string)x.Value))
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .ToList();
        }

        private static JProperty FindProperty(JObject container, string name)
        {
            return container.Property(name)
                ?? container.Properties().FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryParseIndex(string segment, out int index)
        {
            return int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out index);
        }

        private static JToken GetChild(JToken container, string segment)
        {
            if (container is JObject obj)
            {
                return FindProperty(obj, segment)?.Value;
            }

            JArray array = (JArray)container;
            return TryParseIndex(segment, out int index) && index < array.Count ? array[index] : null;
        }

        private static void SetChild(JToken container, string segment, JToken value)
        {
            if (container is JObject obj)
            {
                JProperty property = FindProperty(obj, segment);
                if (property != null)
                {
                    property.Value = value;
                }
                else
                {
                    obj.Add(segment, value);
                }

                return;
            }

            // Callers only pass an array with an index segment.
            JArray array = (JArray)container;
            TryParseIndex(segment, out int index);
            while (array.Count <= index)
            {
                array.Add(JValue.CreateNull());
            }

            array[index] = value;
        }

        private void ApplyOverrides(IEnumerable<KeyValuePair<string, string>> variables, List<string> overriddenKeys)
        {
            foreach (KeyValuePair<string, string> variable in variables)
            {
                string[] path = ConfigEnvironment.GetOverridePath(variable.Key);
                if (path == null)
                {
                    continue;
                }

                SetOverride(path, variable.Value);
                if (!overriddenKeys.Contains(variable.Key, StringComparer.OrdinalIgnoreCase))
                {
                    overriddenKeys.Add(variable.Key);
                }
            }
        }

        private void SetOverride(string[] path, string value)
        {
            JToken current = configFile;
            for (int i = 0; i < path.Length - 1; i++)
            {
                // Walk into an existing object, or an array when the next segment is an index. Anything
                // else in the way is replaced with an object, so the override always lands.
                JToken next = GetChild(current, path[i]);
                bool canWalkInto = next is JObject || (next is JArray && TryParseIndex(path[i + 1], out _));
                if (!canWalkInto)
                {
                    next = new JObject();
                    SetChild(current, path[i], next);
                }

                current = next;
            }

            SetChild(current, path[path.Length - 1], new JValue(value));
        }

        private static JObject ReadConfigFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new JObject();
            }

            try
            {
                // Comments and unquoted property names are tolerated by the reader. Dates are left as
                // strings so they reach the target type exactly as written.
                using (JsonTextReader reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
                {
                    return JObject.Load(reader, LoadSettings);
                }
            }
            catch (JsonReaderException e)
            {
                throw new JsonReaderException($"Failed to read config file '{filePath}': {e.Message}", e);
            }
        }
    }
}
