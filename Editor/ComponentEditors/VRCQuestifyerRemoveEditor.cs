using UnityEditor;
using UnityEngine;
using PeanutTools_VRCQuestifyer;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;

[CustomEditor(typeof(VRCQuestifyerRemove))]
public class VRCQuestifyerRemoveEditor : VRCQuestifyerBaseEditor {
    public override string title { get; set; } = "Remove";

    public void OnEnable() {
        base.OnEnable();

        if (target == null) {
            return;
        }
    }

    public override void RenderGUI() {
        RenderMainGUI();
    }

    public override void RenderMainGUI() {
        var component = target as VRCQuestifyerRemove;
        var transformToUse = component.overrideTarget != null ? component.overrideTarget : component.transform;

        CustomGUI.RenderSuccessMessage($"Object '{transformToUse.name}' will be removed");
    }
}