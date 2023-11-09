using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using VRC.SDK3A.Editor;
using VRC.SDKBase.Editor.BuildPipeline;
using System.Reflection;
using PeanutTools_VRC_Questifyer;

class VRC_Questifyer_VRCSDK_Extension {
    public static List<string> originalWhitelist = new List<string>();

    static List<string> GetShaderNamesFromGameObject(GameObject gameObject) {
        return GetShaderNamesRecursively(gameObject.transform);
    }

    static List<string> GetShaderNamesRecursively(Transform currentTransform) {
        var shaderNames = new List<string>();

        Renderer renderer = currentTransform.GetComponent<Renderer>();

        if (renderer != null) {
            foreach (Material material in renderer.sharedMaterials) {
                string shaderName = material.shader.name;
                shaderNames.Add(shaderName);
            }
        }

        for (int i = 0; i < currentTransform.childCount; i++) {
            shaderNames = shaderNames.Concat(GetShaderNamesRecursively(currentTransform.GetChild(i))).ToList();
        }

        return shaderNames;
    }

    static List<string> GetEveryComponentType() {
        var types = new List<string>();

        // Find all active GameObjects with components
        GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject go in allGameObjects)
        {
            // Get all components attached to the GameObject
            Component[] components = go.GetComponents<Component>();

            foreach (var component in components) {
                var type = component.GetType().ToString();
                if (!types.Contains(type)) {
                    types.Add(type);
                }
            }
        }

        return types;
    }

    static void ForceAllowedShaderNames() {
        GameObject[] rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var shaderNames = new List<string>();

        foreach (GameObject gameObject in rootGameObjects) {
            var newShaderNames = GetShaderNamesFromGameObject(gameObject);

            shaderNames = shaderNames.Concat(newShaderNames).ToList();
        }

        Debug.Log($"VRC_Questifyer :: Found {shaderNames.Count} shaders to allow");

        var field = typeof(VRC.SDKBase.Validation.AvatarValidation).GetField("ShaderWhiteList", BindingFlags.Static | BindingFlags.Public);

        var originalShaderNames = VRC.SDKBase.Validation.AvatarValidation.ShaderWhiteList;

        field.SetValue(null, shaderNames.Concat(originalShaderNames).ToArray());
    }

    static void ForceAllowedComponents() {
        originalWhitelist = new List<string>(VRC.SDKBase.Validation.AvatarValidation.ComponentTypeWhiteListCommon);
        originalWhitelist.AddRange(VRC.SDKBase.Validation.AvatarValidation.ComponentTypeWhiteListSdk3);
        
        Debug.Log($"VRC_Questifyer :: Found {originalWhitelist.Count} in original whitelist");

        var newComponentWhitelist = GetEveryComponentType();

        Debug.Log($"VRC_Questifyer :: Found {newComponentWhitelist.Count} components to allow");

        var field = typeof(VRC.SDKBase.Validation.AvatarValidation).GetField("ComponentTypeWhiteListCommon", BindingFlags.Static | BindingFlags.Public);

        field.SetValue(null, originalWhitelist.Concat(newComponentWhitelist).ToArray());
    }

    [InitializeOnLoadMethod]
    public static void RegisterSDKCallback() {
        Debug.Log("VRC_Questifyer :: RegisterSDKCallback");
        VRCSdkControlPanel.OnSdkPanelEnable += AddBuildHook;

        ForceAllowedShaderNames();
        ForceAllowedComponents();
    }

    private static void AddBuildHook(object sender, EventArgs e) {
        if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder)) {
            builder.OnSdkBuildStart += OnBuildStarted;
        }
    }

    private static void OnBuildStarted(object sender, object target) {
        var name = ((GameObject)target).name;
        Debug.Log($"VRC_Questifyer :: SDK is now building {name}");
    }

    [InitializeOnLoad]
    public class PreuploadHook : IVRCSDKPreprocessAvatarCallback {
        // This has to be before -1024 when VRCSDK deletes our components
        public int callbackOrder => -90000;

        void ApplyQuestifyer(GameObject clonedAvatarGameObject) {
            VRC_Questify[] components = clonedAvatarGameObject.GetComponents<VRC_Questify>();

            foreach (VRC_Questify vrcQuestifyComponent in components) {
                ApplyQuestifyComponent(vrcQuestifyComponent);
            }

            for (int i = 0; i < clonedAvatarGameObject.transform.childCount; i++) {
                ApplyQuestifyer(clonedAvatarGameObject.transform.GetChild(i).gameObject);
            }
        }

        void ApplyQuestifyComponent(VRC_Questify vrcQuestifyComponent) {
            foreach (VRC_Questify.Action action in vrcQuestifyComponent.actions) {
                ApplyQuestifyAction(vrcQuestifyComponent, action);
            }
        }

        void ApplyQuestifyAction(VRC_Questify vrcQuestifyComponent, VRC_Questify.Action action) {
            Debug.Log($"VRC_Questifyer :: Applying action {action.type} to gameobject {vrcQuestifyComponent.gameObject.name}...");

            switch (action.type) {
                case VRC_Questify.ActionType.SwitchToMaterial:
                    Debug.Log($"VRC_Questifyer :: Switching {action.materials.Count} materials...");

                    foreach (var material in action.materials) {
                        Debug.Log(material.name);
                    }

                    Renderer renderer = vrcQuestifyComponent.gameObject.GetComponent<Renderer>();
                    renderer.sharedMaterials = action.materials.ToArray();
                    break;
                case VRC_Questify.ActionType.RemoveGameObject:
                    break;
                case VRC_Questify.ActionType.RemoveComponent:
                    Debug.Log($"VRC_Questifyer :: Removing {action.components.Count} components...");

                    foreach (Component componentToRemove in action.components) {
                        // check if it exists to resolve exception about destroying a component that doesnt exist
                        if (vrcQuestifyComponent.gameObject.GetComponent(componentToRemove.GetType()) != null) {
                            Debug.Log($"VRC_Questifyer :: Removing {componentToRemove.name}...");
                            UnityEngine.Object.DestroyImmediate(componentToRemove);
                        }
                    }
                    break;
                default:
                    throw new System.Exception($"Unknown type {action.type}");
            }
        }

        public bool OnPreprocessAvatar(GameObject clonedAvatarGameObject) {
            Debug.Log($"VRC_Questifyer :: OnPreprocessAvatar :: {clonedAvatarGameObject.name}");

#if UNITY_ANDROID
                Debug.Log($"VRC_Questifyer :: Platform is Android, applying Questifyer...");
                ApplyQuestifyer(clonedAvatarGameObject);
#else
                Debug.Log($"VRC_Questifyer :: Platform is not Android, skipping...");
#endif

            Utils.RemoveAllVrcQuestifyerComponents(clonedAvatarGameObject);
            return true;
        }
    }
}