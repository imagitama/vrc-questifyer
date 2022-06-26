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

        // does NOT start with slash
        public static string GetRelativeGameObjectPath(GameObject objToFind, GameObject rootObj) {
            return GetGameObjectPath(objToFind).Replace(GetGameObjectPath(rootObj), "");
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

        public static Transform FindChild(Transform source, string pathToChild) {
            if (pathToChild.Length == 0) {
                return null;
            }

            if (pathToChild.Substring(0, 1) == "/") {
                if (pathToChild.Length == 1) {
                    return source;
                }

                pathToChild = pathToChild.Substring(1);
            }

            return source.Find(pathToChild);
        }
    }
}