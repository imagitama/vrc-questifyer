using UnityEditor;
using UnityEngine;
using PeanutTools_VRCQuestifyer;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;

[CustomEditor(typeof(VRCQuestifyerRename))]
public class VRCQuestifyerRenameEditor : VRCQuestifyerBaseEditor {
    public override string title { get; set; } = "Rename";
    SerializedProperty newNameProp;
    SerializedProperty modeProp;

    public void OnEnable() {
        base.OnEnable();

        if (target == null) {
            return;
        }

        newNameProp = serializedObject.FindProperty("newName");
        modeProp = serializedObject.FindProperty("mode");
    }

    public override void RenderGUI() {
        RenderMainGUI();
    }

    public override void RenderMainGUI() {
        var component = target as VRCQuestifyerRename;
        var transformToUse = component.overrideTarget != null ? component.overrideTarget : component.transform;

        var originalName = transformToUse.gameObject.name;

        var finalName = component.GetFinalName();

        CustomGUI.RenderSuccessMessage($"Object will be renamed from '{originalName}' to '{finalName}'");
    }
}