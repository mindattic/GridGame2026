using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{


    public static class FolderHelper
    {
        public static class Folder
        {
            /*
            Application.persistentDataPath resolves to:

            Windows     | C:\Users\<User>\AppData\LocalLow\<CompanyName>\<ProductName>
            macOS       | ~/Library/Application Support/<CompanyName>/<ProductName>
            Linux       | ~/.config/unity3d/<CompanyName>/<ProductName>
            Android     | /storage/emulated/0/Android/data/<package-name>/files
            iOS         | /var/mobile/Containers/Data/Application/<guid>/Documents
            WebGL       | IndexedDB (no filesystem access)
            */

            public static string Profiles
            {
                get
                {
#if UNITY_WEBGL
                Debug.LogError("File system operations are not supported on WebGL.");
                return string.Empty;
#else
                    string path = Path.Combine(Application.persistentDataPath, "Profiles");
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    return path;
#endif
                }
            }
        }

        /// <summary>
        /// Creates a folder at the specified path if it doesn't exist.
        /// </summary>
        public static string Create(string basePath, string folderName)
        {
#if UNITY_WEBGL
        Debug.LogError("Folder creation is not supported on WebGL.");
        return string.Empty;
#else
            var path = Path.Combine(basePath, folderName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
#endif
        }

        /// <summary>
        /// Returns a list of directories within the specified base path.
        /// </summary>
        public static List<string> Get(string basePath)
        {
#if UNITY_WEBGL
        Debug.LogError("Directory listing is not supported on WebGL.");
        return new List<string>();
#else
            return Directory.GetDirectories(basePath).ToList();
#endif
        }
    }

}
