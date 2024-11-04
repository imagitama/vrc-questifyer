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
using PeanutTools_VRCQuestifyer;

class VRCQuestifyer_VRCSDK_Extension {
    [InitializeOnLoadMethod]
    public static void RegisterSDKCallback() {
        Debug.Log("VRCQuestifyer :: RegisterSDKCallback");
    }

    private static void AddBuildHook(object sender, EventArgs e) {
        if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder)) {
            builder.OnSdkBuildStart += OnBuildStarted;
        }
    }

    private static void OnBuildStarted(object sender, object target) {
        var name = ((GameObject)target).name;
        Debug.Log($"VRCQuestifyer :: SDK is now building '{name}'");
    }

    [InitializeOnLoad]
    public class PreuploadHook : IVRCSDKPreprocessAvatarCallback {
        // This has to be before -1024 when VRCSDK deletes our components
        public int callbackOrder => -90000;

        public bool OnPreprocessAvatar(GameObject clonedAvatarGameObject) {
            Debug.Log($"VRCQuestifyer :: OnPreprocessAvatar :: Processing '{clonedAvatarGameObject.name}'...");

            Questifyer.RemoveAllQuestifyerComponents(clonedAvatarGameObject.transform);
            return true;
        }
    }
}