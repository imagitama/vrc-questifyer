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
using PeanutTools_VRCQuestifyer;

[AddComponentMenu("VRC Questify Remove Components")]
public class VRCQuestifyerRemoveComponents : VRCQuestifyerBase {
    [System.Serializable]
    public struct ComponentDeletion {
        public string typeName;
        public int index; // relative to other components of same type
    }

    public ComponentDeletion[] componentDeletions = new ComponentDeletion[0];

    public override void Apply() {
        Debug.Log($"VRCQuestifyer :: Remove components - apply");
        
        Debug.Log($"VRCQuestifyer :: Remove {componentDeletions.Length} components");

        var transformToUse = this.overrideTarget != null ? this.overrideTarget : this.transform;
        
        var removableComponents = transformToUse.GetComponents<Component>()
            .Where(component => 
                component != null &&
                component.GetType() != typeof(Transform) &&
                !(component is IEditorOnly))
            .ToList();

        foreach (var componentDeletion in componentDeletions) {
            string typeName = componentDeletion.typeName;
            int index = componentDeletion.index;
            
            Debug.Log($"VRCQuestifyer :: Remove '{typeName}' #{index}");

            var matchingComponents = removableComponents.Where(c => c.GetType().Name == typeName).ToList();

            if (index >= 0 && index < matchingComponents.Count) {
                Component matchingComponent = matchingComponents[index];

                // TODO: Also delete any components that depend on it

                Debug.Log($"VRCQuestifyer :: Delete component '{matchingComponent}'");

                DestroyImmediate(matchingComponent);
            } else {
                Debug.Log($"VRCQuestifyer :: Remove ignored - not found (found {matchingComponents.Count} of same type) ");
            }
        }
        
        Debug.Log($"VRCQuestifyer :: Remove components - apply done");
    }

    public void AddDeletionForComponent(Component component) {
        Debug.Log($"VRCQuestifyer :: Add component deletion '{component}' to '{this.transform.name}'");

        if (component == null) {
            throw new System.Exception("Component is null");
        }

        var transformToUse = this.overrideTarget != null ? this.overrideTarget : this.transform;

        string typeName = component.GetType().Name;

        Component[] componentsOfSameType = transformToUse.gameObject.GetComponents(component.GetType());
        
        Debug.Log($"VRCQuestifyer :: Found {componentsOfSameType.Length} of type '{component.GetType().Name}'");

        int index = -1;
        for (int i = 0; i < componentsOfSameType.Length; i++) {
            if (componentsOfSameType[i] == component) {
                index = i;
                break;
            }
        }

        if (index == -1) {
            throw new System.Exception("Index is -1");
        }

        for (int i = 0; i < componentDeletions.Length; i++) {
            if (componentDeletions[i].typeName == typeName && componentDeletions[i].index == index) {
                Debug.Log($"VRCQuestifyer :: Found existing component deletion - skip");
                return;
            }
        }

        ComponentDeletion deletion = new ComponentDeletion {
            typeName = typeName,
            index = index,
        };

        Array.Resize(ref componentDeletions, componentDeletions.Length + 1);

        componentDeletions[componentDeletions.Length - 1] = deletion;
        
        Debug.Log($"VRCQuestifyer :: Add component deletion done");
    }
}