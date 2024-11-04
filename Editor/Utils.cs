using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PeanutTools_VRCQuestifyer {
    public static class Utils {
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

        public static GameObject FindGameObjectByPath(string pathToGameObject) {
            GameObject[] rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (GameObject rootGameObject in rootGameObjects) {
                if (GetGameObjectPath(rootGameObject) == pathToGameObject) {
                    return rootGameObject;
                }

                Transform[] transforms = rootGameObject.GetComponentsInChildren<Transform>(true);
                
                foreach (Transform transform in transforms) {
                    if (GetGameObjectPath(transform.gameObject) == pathToGameObject) {
                        return transform.gameObject;
                    }
                }
            }

            return null;
        }

        public static void FitTransformInViewport(Transform transform) {
            if (SceneView.lastActiveSceneView != null) {
                Bounds bounds = CalculateBounds(transform);
                SceneView.lastActiveSceneView.Frame(bounds, true);
            }
        }

        public static void Focus(Transform transform) {
            Debug.Log($"VRCQuestifyer :: Focus '{transform.gameObject.name}'");
            FitTransformInViewport(transform);
            Ping(transform);
        }

        public static void Ping(Object obj) {
            Debug.Log($"VRCQuestifyer :: Ping '{obj}'");

            if (AssetDatabase.Contains(obj)) {
                EditorApplication.ExecuteMenuItem("Window/General/Project");
            }

            EditorGUIUtility.PingObject(obj);
        }

        public static void Inspect(GameObject obj) {
            Debug.Log($"VRCQuestifyer :: Inspect '{obj}'");

            Selection.activeGameObject = obj;

            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
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

        // includes itself
        public static T FindComponentInAncestor<T>(Transform current) where T : Component
        {
            while (current != null) {
                T component = current.GetComponent<T>();
                if (component != null) {
                    return component;
                }
                current = current.parent;
            }
            return null;
        }

        // includes itself
        public static T FindComponentInDescendant<T>(Transform current) where T : Component {
            T component = current.GetComponent<T>();
            if (component != null) {
                return component;
            }

            foreach (Transform child in current) {
                component = FindComponentInDescendant<T>(child);
                if (component != null) {
                    return component;
                }
            }

            return null;
        }

        // includes inactive
        public static void RemoveAllComponents<T>(Transform transform) {
            T[] components = transform.GetComponentsInChildren<T>(true);

            foreach (T component in components) {
                Object.DestroyImmediate(component as Object);
            }
        }

        public static List<T> FindAllComponents<T>(Transform transform) {
            T[] components = transform.GetComponentsInChildren<T>(true);
            return components.ToList();
        }

        public static Bounds CalculateBounds(Transform root) {
            Bounds bounds = new Bounds(root.position, Vector3.zero);

            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(root);

            while (stack.Count > 0) {
                Transform current = stack.Pop();
                
                Renderer renderer = current.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (bounds.size == Vector3.zero) {
                        bounds = renderer.bounds;
                    } else {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                foreach (Transform child in current) {
                    stack.Push(child);
                }
            }

            return bounds;
        }

        public static string FormatBytes(long byteCount) {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = System.Math.Abs(byteCount);
            int place = System.Convert.ToInt32(System.Math.Floor(System.Math.Log(bytes, 1024)));
            double num = System.Math.Round(bytes / System.Math.Pow(1024, place), 1);
            return $"{System.Math.Sign(byteCount) * num} {suf[place]}";
        }

        // public static bool GetIsComponentQuestCompatible(Component component) {
        //     return component != null &&
        //         component.GetType() != typeof(Transform)
        //         & component is IEditorOnly;
        // }
    }
}