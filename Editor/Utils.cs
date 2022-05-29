using UnityEngine;
using UnityEditor;
using System.IO;

namespace VRCQuestifyer {
    public class Utils {
        public static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        public static string GetRelativeGameObjectPath(GameObject obj, GameObject relativeTo) {
            string absolutePath = GetGameObjectPath(obj);
            string relativePath = absolutePath.Replace("/" + relativeTo.name + "/", "");
            return relativePath;
        }

        public static string GetPathRelativeToAssets(string path) {
            return Path.GetFullPath(path).Replace(Path.GetFullPath(Application.dataPath), "Assets");
        }

        public static string GetDirectoryPathRelativeToAssets(string path) {
            return GetPathRelativeToAssets(Directory.GetParent(path).FullName);
        }

        public static int StringToInt(string val) {
            return System.Int32.Parse(val);
        }
    }
}