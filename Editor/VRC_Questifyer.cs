#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;
using VRC.SDKBase.Editor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Editor;
using VRC.SDKBase;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation;
using VRC.SDKBase.Validation.Performance.Stats;
using VRCStation = VRC.SDK3.Avatars.Components.VRCStation;
using VRC.SDK3.Validation;
using VRC.Core;
using VRCSDK2;

public class VRC_Questifyer : EditorWindow
{
    class Action {
    }

    class SwitchToQuestMaterialsAction : Action {
        public string pathToMesh;
    }

    class RemoveGameObjectAction : Action {
        public string pathToGameObject;
    }

    VRCAvatarDescriptor sourceVrcAvatarDescriptor;

    List<Action> actions = new List<Action>() {
    //   new SwitchToQuestMaterialsAction() {
    //     pathToMesh = "Body"
    //   },
    //   new SwitchToQuestMaterialsAction() {
    //     pathToMesh = "CanisJockStrapWrapper/CanisJockStrap/CanisJockStrap"
    //   },
      // not care about
      new RemoveGameObjectAction() {
          pathToGameObject = "Snoot"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "dynamic_penetrator_caninePeen"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "Armature/Hips/DynamicButthole"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "Armature/Hips/Spine/Chest/Right shoulder/Right arm/Right elbow/Right wrist/DynamicHandRight"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "Armature/Hips/Spine/Chest/ChestUp/Neck/Head/DynamicMouth"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "dynamicBone_caninePeen/flop1"
      }, 
      new RemoveGameObjectAction() {
          pathToGameObject = "dynamicBone_caninePeen/flop2"
      }, 
      new RemoveGameObjectAction() {
          pathToGameObject = "dynamicBone_caninePeen/flop3"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "CropTop"
      }, 
      new RemoveGameObjectAction() {
          pathToGameObject = "Armature/Hips/armature_caninePeen/root/peen root/peen_1/peen_2/peen_3/pre"
      }, 
      // physbones
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/Bandana"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/LeftButtCheek"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/RightButtCheek"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/JockstrapOnHand"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/WhiskersLeft"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/WhiskersRight"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/Pre"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/ToeLeftIndex"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/ToeLeftMid"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/ToeLeftRing"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/ToeLeftPinky"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/ToeRightIndex"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/ToeRightMid"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/ToeRightRing"
      },
      new RemoveGameObjectAction() {
          pathToGameObject = "PhysBones/ToeRightPinky"
      },
    };

    [MenuItem("PeanutTools/VRC Questifyer _%#T")]
    public static void ShowWindow()
    {
        var window = GetWindow<VRC_Questifyer>();
        window.titleContent = new GUIContent("VRC Questifyer");
        window.minSize = new Vector2(250, 50);
    }

    void OnGUI()
    {
        sourceVrcAvatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", sourceVrcAvatarDescriptor, typeof(VRCAvatarDescriptor));

        if (sourceVrcAvatarDescriptor != null) {
            if (GUILayout.Button("Questify", GUILayout.Width(100), GUILayout.Height(50)))
            {
                Questify();
            }
        }

        GUILayout.Label("Download new versions: https://github.com/imagitama");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("https://twitter.com/@HiPeanutBuddha");
        GUILayout.Label("Peanut#1756");
    }

    void Questify() {
        Debug.Log("Found " + actions.Count + " actions");

        GameObject avatar = CreateQuestAvatar(sourceVrcAvatarDescriptor);

        foreach (Action action in actions) {
            if (action is SwitchToQuestMaterialsAction) {
                // SwitchMeshToQuestMaterialsForAvatar(avatar, (action as SwitchToQuestMaterialsAction).pathToMesh);
            } else if (action is RemoveGameObjectAction) {
                RemoveGameObjectForAvatar(avatar, (action as RemoveGameObjectAction).pathToGameObject);
            } else {
                throw new Exception("Unknown action!");
            }

            // if (action.GetType() == typeof(SwitchToQuestMaterialsAction)) {
            //     SwitchMeshToQuestMaterials(avatar, (action as SwitchToQuestMaterialsAction).pathToMesh);
            // } else {
            //     throw new Exception("Unknown action!");
            // }
        }
        
        SwitchAllMaterialsToQuestForAvatar(avatar);
    }

    GameObject CreateQuestAvatar(VRCAvatarDescriptor sourceAvatar) {
        string questAvatarName = sourceAvatar.gameObject.name + " [Quest]";

        GameObject existingObject = GameObject.Find("/" + questAvatarName);

        if (existingObject != null) {
            return existingObject;
        }

        GameObject clone = Instantiate(sourceAvatar.gameObject, new Vector3(-3, 0, 0), new Quaternion(0, 0, 0, 1));
        clone.name = questAvatarName;

        sourceAvatar.gameObject.SetActive(false);

        return clone;
    }

    void SwitchAllMaterialsToQuestForAvatar(GameObject avatar) {
        Debug.Log("Switching all materials to Quest...");

        Renderer[] allRenderers = avatar.GetComponentsInChildren<Renderer>(true);

        Debug.Log("Found " + allRenderers.Length + " renderers");

        foreach (Renderer renderer in allRenderers) {
            SwitchMeshToQuestMaterialsForAvatar(avatar, renderer);
        }
    }

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

    void SwitchMeshToQuestMaterialsForAvatar(GameObject avatar, Renderer renderer) {
        Debug.Log("Switching all materials for mesh " + GetGameObjectPath(renderer.gameObject) + "...");

        Material[] materials = renderer.sharedMaterials;

        Debug.Log("Found " + materials.Length + " materials");

        Material[] newMaterials = new Material[materials.Length];

        int idx = -1;

        foreach (Material material in materials) {
            idx++;

            string pathToMaterial = AssetDatabase.GetAssetPath(material);

            if (pathToMaterial == "" || pathToMaterial.Contains("Quest")) {
                Debug.Log("Material is empty or already Quest, skipping...");
                newMaterials[idx] = material;
                return;
            }
            
            Debug.Log("Switching material " + pathToMaterial + "...");

            string pathToQuestMaterial = pathToMaterial.Replace(".mat", " Quest.mat");

            Material questMaterial = (Material)AssetDatabase.LoadAssetAtPath(pathToQuestMaterial, typeof(Material));

            if (questMaterial != null) {
                newMaterials[idx] = questMaterial;
            } else {
                string pathToQuestMaterialParent = Directory.GetParent(pathToMaterial).FullName.Replace(Path.GetFullPath(Application.dataPath), "Assets");

                string pathToQuestMaterialInQuestFolder = Path.Combine(pathToQuestMaterialParent, "Quest", Path.GetFileName(pathToMaterial).Replace(".mat", " Quest.mat"));

                Debug.Log("here" + pathToQuestMaterialInQuestFolder);

                Material questMaterialInFolder = (Material)AssetDatabase.LoadAssetAtPath(pathToQuestMaterialInQuestFolder, typeof(Material));

                if (questMaterialInFolder == null) {
                    throw new Exception("Could not find either a quest version for material " + pathToMaterial);
                }

                newMaterials[idx] = questMaterialInFolder;
            }
        }

        renderer.sharedMaterials = newMaterials;
    }

    void SwitchMeshByPathToQuestMaterialsForAvatar(GameObject avatar, string pathToMesh) {
        Debug.Log("Switching all materials for mesh " + pathToMesh + "...");

        Transform meshTransform = avatar.transform.Find(pathToMesh);

        if (meshTransform == null) {
            throw new Exception("Game object not found at path " + pathToMesh);
        }

        Renderer renderer = meshTransform.GetComponent<Renderer>();

        if (renderer == null) {
            throw new Exception("Renderer not found at path " + pathToMesh);
        }

        SwitchMeshToQuestMaterialsForAvatar(avatar, renderer);
    }

    void RemoveGameObjectForAvatar(GameObject avatar, string pathToGameObject) {
        Debug.Log("Removing game object " + pathToGameObject + "...");

        Transform foundTransform = avatar.transform.Find(pathToGameObject);

        if (foundTransform == null) {
            Debug.Log("Game object not found at path " + pathToGameObject);
            return;
        }

        DestroyImmediate(foundTransform.gameObject);
    }
}

#endif