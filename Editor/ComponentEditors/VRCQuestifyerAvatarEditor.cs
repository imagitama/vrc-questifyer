using UnityEditor;
using UnityEngine;
using PeanutTools_VRCQuestifyer;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(VRCQuestifyerAvatar))]
public class VRCQuestifyerAvatarEditor : VRCQuestifyerBaseEditor {
    public override string title { get; set; } = "Avatar";
    public override bool canOverrideTarget { get; set; } = false;
    SerializedProperty replaceExistingQuestAvatarsProp;
    SerializedProperty hideOriginalAvatarProp;
    SerializedProperty zoomToClonedAvatarProp;
    SerializedProperty removeComponentsProp;
    SerializedProperty moveAvatarBackMetersProp;

    public void OnEnable() {
        base.OnEnable();
        
        if (target == null) {
            return;
        }
        
        replaceExistingQuestAvatarsProp = serializedObject.FindProperty("replaceExistingQuestAvatars");
        hideOriginalAvatarProp = serializedObject.FindProperty("hideOriginalAvatar");
        zoomToClonedAvatarProp = serializedObject.FindProperty("zoomToClonedAvatar");
        removeComponentsProp = serializedObject.FindProperty("removeComponents");
        moveAvatarBackMetersProp = serializedObject.FindProperty("moveAvatarBackMeters");
    }

    public override void RenderGUI() {
        serializedObject.Update();

        var component = target as VRCQuestifyerAvatar;

        var vrcAvatarDescriptor = Utils.FindComponentInAncestor<VRCAvatarDescriptor>(component.transform);

        EditorGUI.BeginDisabledGroup(!canOverrideTarget);
        EditorGUILayout.ObjectField("Target", vrcAvatarDescriptor, typeof(VRCAvatarDescriptor));
        CustomGUI.ItalicLabel("Leave blank to use this object");
        EditorGUI.EndDisabledGroup();

        CustomGUI.LineGap();

        CustomGUI.MediumLabel("Avatar");
        
        CustomGUI.LineGap();

        if (vrcAvatarDescriptor == null) {
            CustomGUI.RenderErrorMessage("No VRChat avatar detected on this object or any ancestor");
            return;
        }

        RenderIssues();
        
        CustomGUI.LineGap();

        CustomGUI.MediumLabel("Actions");
        
        CustomGUI.LineGap();

        if (CustomGUI.PrimaryButton("Perform Actions")) {
            Questifyer.CloneAndQuestifyAvatar(vrcAvatarDescriptor, replaceExistingQuestAvatarsProp.boolValue);
        }

        CustomGUI.LineGap();

        CustomGUI.Label(@"    1. Copies this avatar (disconnecting any prefabs)
        
    2. Performs all actions listed
    
    3. Done - upload!");

        CustomGUI.LineGap();
        
        CustomGUI.MediumLabel("Bulk Controls");

        CustomGUI.LineGap();

        if (CustomGUI.StandardButtonWide("Add Existing Materials")) {
            AddExistingMaterials();
        }
        CustomGUI.ItalicLabel("Uses whatever settings are in each component");
        
        CustomGUI.LineGap();

        if (CustomGUI.StandardButtonWide("Create Missing Materials")) {
            CreateMissingMaterials();
        }
        CustomGUI.ItalicLabel("Uses whatever settings are in each component");

        CustomGUI.LineGap();

        if (CustomGUI.StandardButtonWide("Clear Materials")) {
            ClearMaterials();
        }
        CustomGUI.ItalicLabel("Clears any Quest material overrides");

        CustomGUI.LineGap();

        RenderActions();

        CustomGUI.LineGap();

        CustomGUI.MediumLabel("Settings");
        
        CustomGUI.LineGap();

        replaceExistingQuestAvatarsProp.boolValue = CustomGUI.Checkbox("Replace existing Quest avatars", replaceExistingQuestAvatarsProp.boolValue);
        hideOriginalAvatarProp.boolValue = CustomGUI.Checkbox("Hide original avatar", hideOriginalAvatarProp.boolValue);
        zoomToClonedAvatarProp.boolValue = CustomGUI.Checkbox("Focus on copy", zoomToClonedAvatarProp.boolValue);
        removeComponentsProp.boolValue = CustomGUI.Checkbox("Remove Questifyer components", removeComponentsProp.boolValue);
        moveAvatarBackMetersProp.floatValue = CustomGUI.FloatInput("Move original avatar back on Z axis (meters)", moveAvatarBackMetersProp.floatValue);

        serializedObject.ApplyModifiedProperties();
    }

    void RenderIssues() {
        var component = target as VRCQuestifyerAvatar;
        var vrcAvatarDescriptor = Utils.FindComponentInAncestor<VRCAvatarDescriptor>(component.transform);

        List<VRCQuestifyerBase> components = Questifyer.GetComponents(vrcAvatarDescriptor.transform);
        var invalidComponents = new List<VRCQuestifyerBase>();

        foreach (var componentToCheck in components) {
            if (!componentToCheck.GetIsValid() && !componentToCheck.GetIsBeingDeletedByVrcFury()) {
                invalidComponents.Add(componentToCheck);
            }
        }

        if (invalidComponents.Count == 0) {
            CustomGUI.RenderSuccessMessage("All actions are valid and can be performed");
            return;
        }
        
        CustomGUI.MediumLabel("Issues");

        CustomGUI.LineGap();

        foreach (var invalidComponent in invalidComponents) {
            EditorGUILayout.BeginHorizontal();

            if (CustomGUI.TinyButton("Ping")) {
                Utils.Ping(invalidComponent.gameObject);
            }

            if (CustomGUI.TinyButton("View")) {
                Utils.Inspect(invalidComponent.gameObject);
            }

            CustomGUI.Label($"{invalidComponent} is invalid");

            EditorGUILayout.EndHorizontal();
        }

        List<Component> oldUnityConstraints = Utils.GetAllConstraintComponentsInChildren(vrcAvatarDescriptor.transform);

        foreach (var oldUnityConstraint in oldUnityConstraints) {
            EditorGUILayout.BeginHorizontal();

            if (CustomGUI.TinyButton("Ping")) {
                Utils.Ping(oldUnityConstraint.gameObject);
            }

            if (CustomGUI.TinyButton("View")) {
                Utils.Inspect(oldUnityConstraint.gameObject);
            }

            CustomGUI.Label($"{oldUnityConstraint} needs conversion to VRChat constraint (use SDK)");

            EditorGUILayout.EndHorizontal();
        }

        List<VRCPhysBone> physBones = Utils.FindAllComponents<VRCPhysBone>(vrcAvatarDescriptor.transform);

        List<VRCQuestifyerRemoveComponents> rccs = Utils.FindAllComponents<VRCQuestifyerRemoveComponents>(vrcAvatarDescriptor.transform);

        List<VRCPhysBone> physBonesQuestifyWillRemove = new List<VRCPhysBone>();

        foreach (var rcc in rccs) {
            var transformToUse = rcc.overrideTarget != null ? rcc.overrideTarget : rcc.transform;

            foreach (var physBone in physBones) {
                if (physBone.transform == transformToUse) {
                    var physBonesOnTransform = transformToUse.GetComponents<VRCPhysBone>().ToList();

                    for (var i = 0; i < physBonesOnTransform.Count; i++) {
                        var physBoneOnTransform = physBonesOnTransform[i];

                        foreach (var componentDeletion in rcc.componentDeletions) {
                            if (componentDeletion.index == i) {
                                physBonesQuestifyWillRemove.Add(physBoneOnTransform);
                            }
                        }
                    }
                }
            }
        }

        List<VRCPhysBone> physBonesAfterRemoval = physBones
            .Where(bone => !physBonesQuestifyWillRemove.Contains(bone))
            .ToList();

        var maxPhysBoneCount = 8;

        if (physBonesAfterRemoval.Count <= maxPhysBoneCount) {
            return;
        }

        CustomGUI.Label($"There are too many PhysBones on this avatar ({physBones.Count} total, {physBonesAfterRemoval.Count} after questify, max {maxPhysBoneCount}):");

        foreach (var physBone in physBonesAfterRemoval) {
            EditorGUILayout.BeginHorizontal();

            if (CustomGUI.TinyButton("Ping")) {
                Utils.Ping(physBone.gameObject);
            }

            if (CustomGUI.TinyButton("View")) {
                Utils.Inspect(physBone.gameObject);
            }

            if (CustomGUI.TinyButton("Add")) {
                var removeComponentsComponent = physBone.gameObject.GetComponent<VRCQuestifyerRemoveComponents>();
                if (removeComponentsComponent == null) {
                    removeComponentsComponent = physBone.gameObject.AddComponent<VRCQuestifyerRemoveComponents>();
                }
                removeComponentsComponent.AddDeletionForComponent(physBone);
            }

            CustomGUI.Label($"{physBone.transform.name}");
        
            EditorGUILayout.EndHorizontal();
        }
    }

    void RenderActions() {
        var component = target as VRCQuestifyerAvatar;
        var vrcAvatarDescriptor = Utils.FindComponentInAncestor<VRCAvatarDescriptor>(component.transform);

        List<VRCQuestifyerBase> components = Questifyer.GetComponents(vrcAvatarDescriptor.transform);

        foreach (var componentToAction in components) {
            if (componentToAction is VRCQuestifyerAvatar) {
                continue;
            }

            RenderAction(componentToAction);
        }
    }

    void RenderAction(VRCQuestifyerBase component) {
        VRCQuestifyerBaseEditor customEditor = (VRCQuestifyerBaseEditor)Editor.CreateEditor(component);

        EditorGUILayout.BeginHorizontal();

        var transformToUse = component.overrideTarget != null ? component.overrideTarget : component.transform;

        CustomGUI.MediumClickableLabel($"Action - {customEditor.GetTitle()} - \"{transformToUse.gameObject.name}\"", transformToUse.gameObject);
        
        if (CustomGUI.TinyButton("Ping")) {
            Utils.Ping(component.gameObject);
        }

        if (CustomGUI.TinyButton("View")) {
            Utils.Inspect(component.gameObject);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(true);
        customEditor.RenderMainGUI();
        customEditor.RenderExtraGUI();
        EditorGUI.EndDisabledGroup();
    }

    // special actions

    void ClearMaterials() {
        Debug.Log("VRCQuestifyer :: Avatar editor - clear materials");

        var component = target as VRCQuestifyerAvatar;
        var vrcAvatarDescriptor = Utils.FindComponentInAncestor<VRCAvatarDescriptor>(component.transform);
        var switchMaterialsComponents = Utils.FindAllComponents<VRCQuestifyerSwitchMaterials>(vrcAvatarDescriptor.transform);
        
        Debug.Log($"VRCQuestifyer :: Found {switchMaterialsComponents.Count} switch material components");

        foreach (var switchMaterialsComponent in switchMaterialsComponents) {
            VRCQuestifyerSwitchMaterialsEditor customEditor = (VRCQuestifyerSwitchMaterialsEditor)Editor.CreateEditor(switchMaterialsComponent);
            customEditor.ClearMaterials();
        }
        
        Debug.Log("VRCQuestifyer :: Avatar editor - clear materials done");   
    }

    void AddExistingMaterials() {
        Debug.Log("VRCQuestifyer :: Avatar editor - add existing materials");

        var component = target as VRCQuestifyerAvatar;
        var vrcAvatarDescriptor = Utils.FindComponentInAncestor<VRCAvatarDescriptor>(component.transform);
        var switchMaterialsComponents = Utils.FindAllComponents<VRCQuestifyerSwitchMaterials>(vrcAvatarDescriptor.transform);
        
        Debug.Log($"VRCQuestifyer :: Found {switchMaterialsComponents.Count} switch material components");

        foreach (var switchMaterialsComponent in switchMaterialsComponents) {
            VRCQuestifyerSwitchMaterialsEditor customEditor = (VRCQuestifyerSwitchMaterialsEditor)Editor.CreateEditor(switchMaterialsComponent);
            customEditor.AddExistingMaterials();
        }
        
        Debug.Log("VRCQuestifyer :: Avatar editor - add existing materials done");
    }

    void CreateMissingMaterials() {
        Debug.Log("VRCQuestifyer :: Avatar editor - create missing materials");

        var component = target as VRCQuestifyerAvatar;
        var vrcAvatarDescriptor = Utils.FindComponentInAncestor<VRCAvatarDescriptor>(component.transform);
        var switchMaterialsComponents = Utils.FindAllComponents<VRCQuestifyerSwitchMaterials>(vrcAvatarDescriptor.transform);
        
        Debug.Log($"VRCQuestifyer :: Found {switchMaterialsComponents.Count} switch material components");

        var recentlyCreatedMaterialPaths = new List<string>();

        foreach (var switchMaterialsComponent in switchMaterialsComponents) {
            VRCQuestifyerSwitchMaterialsEditor customEditor = (VRCQuestifyerSwitchMaterialsEditor)Editor.CreateEditor(switchMaterialsComponent);
            customEditor.CreateMissingMaterials(recentlyCreatedMaterialPaths);
        }
        
        Debug.Log("VRCQuestifyer :: Avatar editor - create missing materials done");
    }
}