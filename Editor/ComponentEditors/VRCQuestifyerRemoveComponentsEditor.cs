using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using PeanutTools_VRCQuestifyer;
using VRC.SDK3.Avatars.Components;
using System.Linq;
using System.Collections.Generic;
using VRC.SDKBase;

[CustomEditor(typeof(VRCQuestifyerRemoveComponents))]
public class VRCQuestifyerRemoveComponentsEditor : VRCQuestifyerBaseEditor {
    public override string title { get; set; } = "Remove Components";
    SerializedProperty componentDeletionsProperty;
    ReorderableList reorderableList;

    public void OnEnable() {
        base.OnEnable();

        if (target == null) {
            return;
        }

        componentDeletionsProperty = serializedObject.FindProperty("componentDeletions");

        Hydrate();
    }

    public override void Hydrate() {
        var component = target as VRCQuestifyerRemoveComponents;
        var transformToUse = component.overrideTarget != null ? component.overrideTarget : component.transform;

        var removableComponents = transformToUse.GetComponents<Component>()
            .Where(component =>
                component != null &&
                component.GetType() != typeof(Transform) &&
                !(component is IEditorOnly)
            )
            .ToList();

        reorderableList = new ReorderableList(removableComponents, typeof(Component), false, true, false, false);

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            float columnWidth = rect.width / 3;

            var component = removableComponents[index];
            var typeName = component.GetType().Name;
            var icon = EditorGUIUtility.ObjectContent(null, component.GetType()).image;

            var componentsOfType = removableComponents.Where(c => c.GetType() == component.GetType()).ToList();
            var componentIndex = componentsOfType.IndexOf(component);

            if (icon != null) {
                Rect iconRect = new Rect(rect.x, rect.y, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            EditorGUI.LabelField(new Rect(rect.x, rect.y, columnWidth - 5, EditorGUIUtility.singleLineHeight), $"      {typeName}{(componentsOfType.Count > 0 ? $" ({componentIndex})" : "")}");

            bool existsInDeletions = false;

            for (int i = 0; i < componentDeletionsProperty.arraySize; i++)
            {
                SerializedProperty deletionItem = componentDeletionsProperty.GetArrayElementAtIndex(i);
                var deletionTypeName = deletionItem.FindPropertyRelative("typeName").stringValue;
                var deletionIndex = deletionItem.FindPropertyRelative("index").intValue;

                if (deletionTypeName == typeName && deletionIndex == componentIndex)
                {
                    existsInDeletions = true;
                    break;
                }
            }

            bool newValue = EditorGUI.Toggle(new Rect(rect.x + columnWidth, rect.y, columnWidth - 5, EditorGUIUtility.singleLineHeight), existsInDeletions);

            if (newValue != existsInDeletions) 
            {
                if (newValue) 
                {
                    componentDeletionsProperty.arraySize++;
                    SerializedProperty newElement = componentDeletionsProperty.GetArrayElementAtIndex(componentDeletionsProperty.arraySize - 1);
                    newElement.FindPropertyRelative("typeName").stringValue = typeName;
                    newElement.FindPropertyRelative("index").intValue = componentIndex;
                } 
                else 
                {
                    for (int i = 0; i < componentDeletionsProperty.arraySize; i++) 
                    {
                        SerializedProperty deletionItem = componentDeletionsProperty.GetArrayElementAtIndex(i);
                        if (deletionItem.FindPropertyRelative("typeName").stringValue == typeName && 
                            deletionItem.FindPropertyRelative("index").intValue == componentIndex) 
                        {
                            componentDeletionsProperty.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }
            }

            EditorGUI.LabelField(new Rect(rect.x + 2 * columnWidth, rect.y, columnWidth - 5, EditorGUIUtility.singleLineHeight), newValue ? "Remove" : "Skip");
        };

        reorderableList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width / 3 - 5, rect.height), "Type");
            EditorGUI.LabelField(new Rect(rect.x + rect.width / 3, rect.y, rect.width / 3 - 5, rect.height), "Remove");
            EditorGUI.LabelField(new Rect(rect.x + 2 * rect.width / 3, rect.y, rect.width / 3 - 5, rect.height), "Result");
        };
    }

    public override void RenderGUI() {
        serializedObject.Update();

        CustomGUI.MediumLabel("Remove Components");
        CustomGUI.ItalicLabel("Warning: Does not check for dependencies (like Cloth depends on SkinnedMeshRenderer)");

        var component = target as VRCQuestifyerRemoveComponents;
        var transformToUse = component.overrideTarget != null ? component.overrideTarget : component.transform;

        CustomGUI.LineGap();

        CustomGUI.RenderSuccessMessage("These components will be removed:");
        
        CustomGUI.LineGap();

        RenderMainGUI();
        
        serializedObject.ApplyModifiedProperties();
    }

    public override void RenderMainGUI() {
        reorderableList.DoLayoutList();
    }
}