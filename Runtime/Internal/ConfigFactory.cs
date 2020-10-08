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
        private readonly Dictionary<string, object> configFile;

        /// <summary>
        /// Initalises a new instance of the <see cref="ConfigFactory"/> class.
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

            return config;
        }

        private Dictionary<string, object> ReadConfigFile()
        {
            // Next to the .exe.
            string filePath = Path.Combine(Application.dataPath, "../", "config.json");

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "{\r\n    // Add settings here\r\n}");
            }

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }
    }
}
