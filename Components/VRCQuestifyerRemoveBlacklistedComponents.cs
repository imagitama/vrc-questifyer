using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDKBase.Validation;
using PeanutTools_VRCQuestifyer;

[AddComponentMenu("VRC Questify Remove Blacklisted Components")]
public class VRCQuestifyerRemoveBlacklistedComponents : VRCQuestifyerBase {
    public List<Component> GetComponentsToRemove(Transform transformToUse) {
        var whitelistCommon = AvatarValidation.ComponentTypeWhiteListCommon;
        var whitelistSdk3 = AvatarValidation.ComponentTypeWhiteListSdk3;
        var whitelistedComponentNames = whitelistCommon.ToList().Concat(whitelistSdk3).ToList();

        var allComponentsInDescendants = transformToUse.GetComponentsInChildren<Component>();

        var components = new List<Component>();

        foreach (var componentToCheck in allComponentsInDescendants) {
            if (componentToCheck == null) {
                continue;
            }

            var type = componentToCheck.GetType();
            var componentName = componentToCheck.GetType().FullName;

            if (componentToCheck is IEditorOnly) {
                continue;
            }

            if (whitelistedComponentNames.Any(keyword => componentName.Contains(keyword)) && Common.disallowedQuestComponentNames.Any(disallowedName => componentName == disallowedName) == false) {
                continue;
            }

            components.Add(componentToCheck);
        }

        return components;
    }

    public override void Apply() {
        Debug.Log($"VRCQuestifyer :: Remove blacklisted components - apply");

        var transformToUse = this.overrideTarget != null ? this.overrideTarget : this.transform;

        var componentsToRemove = GetComponentsToRemove(transformToUse);

        Debug.Log($"VRCQuestifyer :: Remove {componentsToRemove.Count} blacklisted components");

        foreach (var component in componentsToRemove) {
            Debug.Log($"VRCQuestifyer :: Remove blacklisted component '{component}'");
            DestroyImmediate(component);
        }
        
        Debug.Log($"VRCQuestifyer :: Remove blacklisted components - apply done");
    }
}