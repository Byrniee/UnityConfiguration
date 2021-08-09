using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Byrniee.UnityConfiguration.Editor
{
    /// <summary>
    /// Post build script for coping the config file to the
    /// build directory.
    /// </summary>
    public class ConfigPostBuild : MonoBehaviour
    {
        private const string ConfigFilename = "config.json";
        
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            string filePath = Path.Combine(Application.dataPath, "../", ConfigFilename);
            if (File.Exists(filePath))
            {
                string projectFileName = Path.GetFileName(pathToBuiltProject);
                string buildFolder = pathToBuiltProject.Replace(projectFileName, string.Empty);
                File.Copy(filePath, Path.Combine(buildFolder, ConfigFilename));
            }
        }
    }
}