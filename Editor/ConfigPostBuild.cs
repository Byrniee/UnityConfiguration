using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Byrniee.UnityConfiguration.Editor
{
    /// <summary>
    /// Post build script for coping the config files to the
    /// build directory.
    /// </summary>
    public class ConfigPostBuild : MonoBehaviour
    {
        private const string ConfigFilenamePrefix = "config";
        private const string ConfigFilenamePostfix = ".json";
        
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            string directoryPath = Path.Combine(Application.dataPath, "../");
            IList<string> configFiles = Directory.GetFiles(directoryPath)
                .Select(x => x.Replace(directoryPath, string.Empty))
                .Where(x => x.StartsWith(ConfigFilenamePrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            string projectFileName = Path.GetFileName(pathToBuiltProject);
            string buildFolder = pathToBuiltProject.Replace(projectFileName, string.Empty);
            foreach (string configFile in configFiles)
            {
                File.Copy(Path.Combine(directoryPath, configFile), Path.Combine(buildFolder, configFile), true);
            }
        }
    }
}