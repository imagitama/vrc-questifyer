using UnityEditor;
using UnityEngine;
using PeanutTools_VRCQuestifyer;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;
using VRC.SDKBase.Validation;

[CustomEditor(typeof(VRCQuestifyerRemoveBlacklistedComponents))]
public class VRCQuestifyerRemoveBlacklistedComponentsEditor : VRCQuestifyerBaseEditor {
    public override string title { get; set; } = "Remove Blacklisted Components";

    public override void RenderGUI() {
        CustomGUI.RenderSuccessMessage("These components will be removed:");
        
        CustomGUI.LineGap();

        RenderMainGUI();   
    }

    public override void RenderMainGUI() {
        var component = target as VRCQuestifyerRemoveBlacklistedComponents;
        var transformToUse = component.overrideTarget != null ? component.overrideTarget : component.transform;

        var componentsToRemove = component.GetComponentsToRemove(transformToUse);

        if (componentsToRemove.Count == 0) {
            CustomGUI.ItalicLabel("No blacklisted components found");
            return;
        }

        foreach (var componentToRemove in componentsToRemove) {
            var gameObject = componentToRemove.gameObject;

            GUILayout.BeginHorizontal();
 
            var icon = EditorGUIUtility.ObjectContent(null, componentToRemove.GetType()).image;
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
            GUILayout.Label(componentToRemove.GetType().Name, GUILayout.Width(200));

            if (GUILayout.Button(gameObject.name, GUILayout.Width(200))) {
                Utils.Ping(gameObject);
            }

            GUILayout.EndHorizontal();
        }
    }
}