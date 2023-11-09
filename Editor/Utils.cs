using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;

namespace PeanutTools_VRC_Questifyer {
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

        public static Material CreateQuestMaterial(string originalMaterialPath, Material originalMaterial, bool useToonShader = false, bool placeQuestMaterialsInOwnDirectory = true) {
            Debug.Log($"Create quest material {originalMaterialPath}...");

            string pathToMaterialTemplate = "Assets/PeanutTools/VRC_Questifyer/Materials/QuestTemplate" + (useToonShader ? "Toon" : "Standard") + ".mat";
            Material materialTemplate = GetMaterialAtPath(pathToMaterialTemplate);

            string pathToParentDir = Utils.GetDirectoryPathRelativeToAssets(originalMaterialPath);

            string pathToNewParentDir = pathToParentDir + "/" + (placeQuestMaterialsInOwnDirectory ? "Quest/" : "");

            string pathToDest = pathToNewParentDir + Path.GetFileName(originalMaterialPath).Replace(".mat", " Quest.mat");

            Material existingMaterial = LooselyGetMaterialAtPath(pathToDest);

            if (existingMaterial != null) {
                Debug.Log($"Existing material found, using...");
                return existingMaterial;
            }

            Debug.Log("Creating material...");

            if (placeQuestMaterialsInOwnDirectory) {
                bool exists = Directory.Exists(pathToNewParentDir);

                if(!exists) {
                    Debug.Log($"Creating directory for materials at {pathToNewParentDir}...");
                    Directory.CreateDirectory(pathToNewParentDir);
                }
            }

            bool result = AssetDatabase.CopyAsset(pathToMaterialTemplate, pathToDest);

            if (result == false) {
                throw new System.Exception("Failed to copy Quest material template!");
            }

            Material createdMaterial = GetMaterialAtPath(pathToDest);

            try {
                createdMaterial.CopyPropertiesFromMaterial(originalMaterial);
            } catch (System.Exception err) {
                // if props don't exist then it throws errors
                // ignore them
                Debug.Log(err);
            }

            return createdMaterial;
        }

            
        static Material GetMaterialAtPath(string pathToMaterial, bool ignoreNotFound = false) {
            Material loadedMaterial = (Material)AssetDatabase.LoadAssetAtPath(pathToMaterial, typeof(Material));

            if (loadedMaterial == null) {
                if (ignoreNotFound) {
                    return null;
                }
                throw new System.Exception("Failed to load material at path: " + pathToMaterial);
            }

            return loadedMaterial;
        }

        static Material LooselyGetMaterialAtPath(string pathToMaterial) {
            return GetMaterialAtPath(pathToMaterial, true);
        }

        public static void FocusGameObject(GameObject obj) {
            EditorGUIUtility.PingObject(obj);
        }

        public static void CreateMissingQuestMaterials(GameObject gameObject, bool includeChildren = false) {
            Renderer renderer = gameObject.GetComponent<Renderer>();

            if (renderer != null) {
                Debug.Log($"VRC_Questifyer :: Object \"{gameObject.name}\" has a renderer, creating any missing Quest materials...");

                var allMaterials = new List<Material>();

                foreach (Material material in renderer.sharedMaterials) {
                    string pathToMaterial = AssetDatabase.GetAssetPath(material);

                    if (pathToMaterial == "") {
                        Debug.Log(material);
                        throw new System.Exception("VRC_Questifyer - failed to get material path");
                    }

                    if (pathToMaterial.Contains("Quest")) {
                        Debug.Log($"VRC_Questifyer :: Material \"{pathToMaterial}\" is already named with Quest");
                        allMaterials.Add(material);
                        continue;
                    }

                    Material questMaterial = Utils.CreateQuestMaterial(pathToMaterial, material);

                    allMaterials.Add(questMaterial);
                }

                Debug.Log($"VRC_Questifyer :: We now have {allMaterials.Count} Quest materials");

                VRC_Questify vrcQuestify = gameObject.GetComponent<VRC_Questify>();

                if (vrcQuestify != null) {
                    Debug.Log($"VRC_Questifyer :: Found existing Questify component");
                    var found = false;

                    for (var i = 0; i < vrcQuestify.actions.Length; i++) {
                        var action = vrcQuestify.actions[i];

                        if (action.type == VRC_Questify.ActionType.SwitchToMaterial) {
                            Debug.Log("VRC_Questifyer :: Found existing action, overriding...");
                            found = true;
                            action.materials = allMaterials;
                        }
                    }

                    if (!found) {
                        Debug.Log("VRC_Questifyer :: No existing action found, adding...");

                        vrcQuestify.AddAction(new VRC_Questify.Action() {
                            type = VRC_Questify.ActionType.SwitchToMaterial,
                            materials = allMaterials
                        });
                    }
                } else {
                    Debug.Log($"VRC_Questifyer :: Creating Questify component...");

                    var newVrcQuestify = gameObject.AddComponent<VRC_Questify>();
                    newVrcQuestify.AddAction(
                        new VRC_Questify.Action() {
                            type = VRC_Questify.ActionType.SwitchToMaterial,
                            materials = allMaterials
                        }
                    );
                }
            }

            if (includeChildren) {
                for (int i = 0; i < gameObject.transform.childCount; i++) {
                    CreateMissingQuestMaterials(gameObject.transform.GetChild(i).gameObject, true);
                }
            }
        }

        static bool GetIfGameObjectIsOfInterest(GameObject gameObject) {
            if (gameObject.GetComponent<Renderer>()) {
                return true;
            }
            
            foreach (var component in gameObject.GetComponents(typeof(Component))) {
                var type = component.GetType().ToString();

                if (!VRC_Questifyer_VRCSDK_Extension.originalWhitelist.Contains(type)) {
                    return true;
                }
            }

            return false;
        }

        public static void RenderChildrenOfInterestList(GameObject gameObject, int indentLevel) {
            if (GetIfGameObjectIsOfInterest(gameObject)) {
                var vrcQuestify = gameObject.GetComponent<VRC_Questify>();

                string questifyLabel = vrcQuestify != null ? $"({vrcQuestify.actions.Length})" : "[Not Questified]";
                string label = $"{gameObject.name} {questifyLabel}";

                EditorGUILayout.LabelField(
                    new GUIContent(
                        new string(' ', (indentLevel + 1) * 2) + label
                    ),
                    EditorStyles.label
                );

                if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) {
                    Utils.FocusGameObject(gameObject);
                }
            }

            foreach (Transform child in gameObject.transform) {
                RenderChildrenOfInterestList(child.gameObject, indentLevel + 1);
            }
        }

        // TODO: Allow user to add their own to support more plugins
        // TODO: Maybe do a wildcard thing here to futureproof
        static List<string> myWhitelist = new List<string>() {
            "VF.Model.VRCFury",
            "VRC_Questify"
        };

        public static void AddActionsToRemoveNonWhitelistedComponents(GameObject gameObject, bool includeChildren = false) {
            List<string> whitelistToUse = VRC_Questifyer_VRCSDK_Extension.originalWhitelist;
            whitelistToUse.AddRange(myWhitelist);

            var componentsToRemove = new List<Component>();

            foreach (var component in gameObject.GetComponents(typeof(Component))) {
                var type = component.GetType().ToString();
                if (!whitelistToUse.Contains(type)) {
                    Debug.Log($"VRC_Questifyer :: Component \"{type}\" on object \"{gameObject.name}\" is not whitelisted, adding for removal...");
                    componentsToRemove.Add(component);
                }
            }

            if (componentsToRemove.Count > 0) {
                VRC_Questify vrcQuestify = gameObject.GetComponent<VRC_Questify>();

                if (vrcQuestify == null) {
                    vrcQuestify = gameObject.AddComponent<VRC_Questify>();
                }

                vrcQuestify.AddAction(new VRC_Questify.Action() {
                    type = VRC_Questify.ActionType.RemoveComponent,
                    components = componentsToRemove
                });
            }

            if (includeChildren) {
                foreach (Transform child in gameObject.transform) {
                    AddActionsToRemoveNonWhitelistedComponents(child.gameObject);
                }
            }
        }

        public static void RemoveAllVrcQuestifyerComponents(GameObject gameObject) {
            VRC_Questify[] components = gameObject.GetComponents<VRC_Questify>();

            foreach (var component in components) {
                Debug.Log($"VRC_Questifyer :: Removing VRC_Questify component from gameobject {gameObject.name}...");
                UnityEngine.Object.DestroyImmediate(component);
            }

            for (int i = 0; i < gameObject.transform.childCount; i++) {
                RemoveAllVrcQuestifyerComponents(gameObject.transform.GetChild(i).gameObject);
            }
        }
    }
}