using System.IO;
using UnityEditor;
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
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (File.Exists("config.json"))
            {
                File.Copy("config.json", Path.Combine(pathToBuiltProject, "config.json"));
            }
        }
    }
}