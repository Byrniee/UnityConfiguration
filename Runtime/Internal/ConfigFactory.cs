using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Byrniee.UnityConfiguration.Internal
{
    /// <summary>
    /// Factory class for creating the config classes.
    /// </summary>
    public class ConfigFactory : IConfigFactory
    {
        private const string ConfigFilename = "config.json";
        private const string EnvironmentFilename = ".env";
        private readonly Dictionary<string, object> configFile;

        /// <summary>
        /// Initialises a new instance of the <see cref="ConfigFactory"/> class.
        /// </summary>
        public ConfigFactory()
        {
            configFile = ReadConfigFile();
        }

        /// <inheritdoc />
        public IConfig<T> Create<T>(string index)
        {
            Config<T> config = new Config<T>();

            if (configFile.ContainsKey(index))
            {
                string json = configFile[index].ToString();
                config.Value = JsonConvert.DeserializeObject<T>(json);
            }
            else
            {
                // Create new or default.
                config.Value = JsonConvert.DeserializeObject<T>("{}");
            }

            return config;
        }

        private Dictionary<string, object> ReadConfigFile()
        {
            // Next to the .exe.
            string filePath = Path.Combine(Application.dataPath, "../", ConfigFilename);

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "{\r\n    // Add settings here\r\n}");
            }

            string json = File.ReadAllText(filePath);
            json = InjectEnvironmentVariables(json);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }

        private string InjectEnvironmentVariables(string json)
        {
            // Next to the .exe and config file.
            string filePath = Path.Combine(Application.dataPath, "../", EnvironmentFilename);
            if (!File.Exists(filePath))
            {
                return json;
            }

            foreach (string line in File.ReadAllLines(filePath))
            {
                string[] parts = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    continue;
                }

                json = json.Replace($"${{{parts[0]}}}", parts[1]);
            }

            return json;
        }
    }
}
