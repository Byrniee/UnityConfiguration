using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Byrniee.UnityConfiguration.Internal
{
    /// <summary>
    /// Factory class for creating the config classes.
    /// </summary>
    public class ConfigFactory : IConfigFactory
    {
        private const string ConfigFilenamePrefix = "config";
        private const string ConfigFilenamePostfix = ".json";
        private const string EnvironmentFilename = ".env";
        private const string UnityEnvironment = "UNITY_ENVIRONMENT";
        
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

            if (configFile.TryGetValue(index, out object value))
            {
                string json = value.ToString();
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
            // Read the base config file.
            string baseConfigFilename = $"{ConfigFilenamePrefix}{ConfigFilenamePostfix}";
            string baseConfigFilePath = Path.Combine(Application.dataPath, "../", baseConfigFilename);
            Dictionary<string, object> baseConfig = ReadConfigFile(baseConfigFilePath);
            
            // Check for an environment.
            string environmentFilePath = Path.Combine(Application.dataPath, "../", EnvironmentFilename);
            if (!TryGetEnvironmentName(environmentFilePath, out string environmentName))
            {
                return baseConfig;
            }
            
            // Read the environment config
            string environmentConfigFilename = $"{ConfigFilenamePrefix}.{environmentName}{ConfigFilenamePostfix}";
            string environmentConfigFilePath = Path.Combine(Application.dataPath, "../", environmentConfigFilename);
            Dictionary<string, object> environmentConfig = ReadConfigFile(environmentConfigFilePath);
            
            // Apply the environment config to the base config
            foreach (string key in environmentConfig.Keys)
            {
                // This will add any new keys, and overwrite any existing base config with the environment config. 
                baseConfig[key] = environmentConfig[key];
            }

            return baseConfig;
        }

        private Dictionary<string, object> ReadConfigFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, object>();
            }
            
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }

        private bool TryGetEnvironmentName(string filePath, out string environmentName)
        {
            if (!File.Exists(filePath))
            {
                environmentName = string.Empty;
                return false;
            }

            string[] lines = File.ReadAllLines(filePath);
            environmentName = lines
                .Select(SplitEnvLine)
                .FirstOrDefault(x => x.Key == UnityEnvironment)
                .Value;
            
            return !string.IsNullOrEmpty(environmentName);
        }

        private (string Key, string Value) SplitEnvLine(string line)
        {
            string[] parts = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return (string.Empty, string.Empty);
            }

            return (parts[0], parts[1]);
        }
    }
}
