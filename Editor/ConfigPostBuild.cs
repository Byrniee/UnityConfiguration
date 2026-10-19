using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Byrniee.UnityConfiguration.Editor
{
    /// <summary>
    /// Post build script for coping the config files for the current environment
    /// to the build directory. Define <c>BYRNIEE_UNITYCONFIGURATION_DISABLE_POSTBUILD_COPY</c>
    /// in the scripting define symbols to skip the copy.
    /// </summary>
    public class ConfigPostBuild : MonoBehaviour
    {
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
#if BYRNIEE_UNITYCONFIGURATION_DISABLE_POSTBUILD_COPY
            return;
#else
            string sourceDirectory = ConfigEnvironment.DefaultConfigDirectory;
            string environmentName = ConfigEnvironment.ResolveEnvironmentName(sourceDirectory);
            string buildFolder = Path.GetDirectoryName(pathToBuiltProject);

            List<string> copiedFiles = new List<string>();
            foreach (string configFile in ConfigEnvironment.GetLayerFileNames(environmentName))
            {
                string sourcePath = Path.Combine(sourceDirectory, configFile);
                if (!File.Exists(sourcePath))
                {
                    continue;
                }

                File.Copy(sourcePath, Path.Combine(buildFolder, configFile), true);
                copiedFiles.Add(configFile);
            }

            Debug.Log($"UnityConfiguration: copied [{string.Join(", ", copiedFiles)}] for environment '{environmentName ?? "(none)"}' to {buildFolder}");
#endif
        }
    }
}
