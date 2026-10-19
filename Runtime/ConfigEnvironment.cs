using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Byrniee.UnityConfiguration
{
    /// <summary>
    /// Resolves the current environment and the config layer files it maps to.
    /// </summary>
    public static class ConfigEnvironment
    {
        /// <summary>
        /// The name of the environment variable (and <c>.env</c> key) that selects the environment.
        /// </summary>
        public const string VariableName = "UNITY_ENVIRONMENT";

        /// <summary>
        /// The name of the file the environment is read from when the environment variable is not set.
        /// </summary>
        public const string EnvironmentFilename = ".env";

        private const string ConfigFilenamePrefix = "config";
        private const string ConfigFilenamePostfix = ".json";

        /// <summary>
        /// Gets the default directory the config files are read from: the project root in the
        /// editor, or the folder containing the executable in a build.
        /// </summary>
        public static string DefaultConfigDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        /// <summary>
        /// Resolves the environment name. The <c>UNITY_ENVIRONMENT</c> process environment variable
        /// takes precedence; otherwise the <c>UNITY_ENVIRONMENT</c> entry in the <c>.env</c> file in
        /// <paramref name="configDirectory"/> is used.
        /// </summary>
        /// <param name="configDirectory">The directory containing the config files.</param>
        /// <returns>The environment name, or null if none is set.</returns>
        public static string ResolveEnvironmentName(string configDirectory)
        {
            return ResolveEnvironmentName(configDirectory, Environment.GetEnvironmentVariable);
        }

        /// <summary>
        /// Gets the config file names to load, in order, for an environment. For
        /// <c>Manchester.Strongman</c> this is <c>config.json</c>, <c>config.Manchester.json</c>,
        /// <c>config.Manchester.Strongman.json</c>.
        /// </summary>
        /// <param name="environmentName">The environment name, or null for the base file only.</param>
        /// <returns>The ordered file names.</returns>
        public static IReadOnlyList<string> GetLayerFileNames(string environmentName)
        {
            List<string> fileNames = new List<string> { $"{ConfigFilenamePrefix}{ConfigFilenamePostfix}" };
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                return fileNames;
            }

            string[] segments = environmentName
                .Split('.')
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray();

            for (int i = 1; i <= segments.Length; i++)
            {
                string layerName = string.Join(".", segments, 0, i);
                fileNames.Add($"{ConfigFilenamePrefix}.{layerName}{ConfigFilenamePostfix}");
            }

            return fileNames;
        }

        internal static string ResolveEnvironmentName(string configDirectory, Func<string, string> getEnvironmentVariable)
        {
            string fromVariable = getEnvironmentVariable(VariableName);
            if (!string.IsNullOrWhiteSpace(fromVariable))
            {
                return fromVariable.Trim();
            }

            string fromFile = ReadEnvironmentFile(Path.Combine(configDirectory, EnvironmentFilename));
            return string.IsNullOrWhiteSpace(fromFile) ? null : fromFile;
        }

        private static string ReadEnvironmentFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            foreach (string rawLine in File.ReadAllLines(filePath))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0 || line.Substring(0, separatorIndex).Trim() != VariableName)
                {
                    continue;
                }

                string value = line.Substring(separatorIndex + 1).Trim();
                if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[value.Length - 1] == value[0])
                {
                    value = value.Substring(1, value.Length - 2).Trim();
                }

                return value;
            }

            return null;
        }
    }
}
