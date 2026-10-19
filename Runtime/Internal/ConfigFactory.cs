using System;
using System.Collections.Generic;
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
            : this(configDirectory, Environment.GetEnvironmentVariable)
        {
        }

        internal ConfigFactory(string configDirectory, Func<string, string> getEnvironmentVariable)
        {
            EnvironmentName = ConfigEnvironment.ResolveEnvironmentName(configDirectory, getEnvironmentVariable);

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
        }

        /// <inheritdoc />
        public string EnvironmentName { get; }

        /// <inheritdoc />
        public IReadOnlyList<string> LoadedFiles { get; }

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
