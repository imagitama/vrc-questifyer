using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using PeanutTools_VRC_Questifyer;
using VRC.SDK3.Avatars.Components;

[CustomEditor(typeof(VRC_Questify))]
public class VRC_Questify_Editor : Editor {
    SerializedProperty actions;
    ReorderableList actionsList;
    
    ReorderableList[] materialLists = new ReorderableList[0];
    ReorderableList[] componentLists = new ReorderableList[0];

    int defaultPadding = 5;

    private void OnEnable() {
        PrepareLists();
    }

    private void PrepareLists() {
        actions = serializedObject.FindProperty("actions");
        actionsList = new ReorderableList(serializedObject, actions) {
            displayAdd = true,
            displayRemove = true,
            draggable = true,
            drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Actions");
            },
            drawElementCallback = DrawActionsListItem,
            elementHeightCallback = (index) => {
                SerializedProperty element = actionsList.serializedProperty.GetArrayElementAtIndex(index);
                
                VRC_Questify.ActionType type = (VRC_Questify.ActionType)element.FindPropertyRelative("type").enumValueIndex;

                switch (type) {
                    case VRC_Questify.ActionType.SwitchToMaterial:
                    case VRC_Questify.ActionType.RemoveComponent:
                        var items = element.FindPropertyRelative(type == VRC_Questify.ActionType.SwitchToMaterial ? "materials" : "components");
                        var count = 0;

                        if (items != null) {
                            count = items.arraySize;
                        }

                        return EditorGUIUtility.singleLineHeight + 30 + (EditorGUIUtility.singleLineHeight * count) + (defaultPadding * 2);
                    case VRC_Questify.ActionType.RemoveGameObject:
                        break;
                    default:
                        throw new System.Exception($"Unknown type {type}");
                }

                return EditorGUIUtility.singleLineHeight + (defaultPadding * 2);
            }
        };
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        CustomGUI.LargeLabel("VRC Questifyer");
        CustomGUI.ItalicLabel("Non-destructively performs actions on your avatar for Quest upload.");

        var gameObject = ((VRC_Questify)target).gameObject;

        CustomGUI.LineGap();

        if (gameObject.GetComponent<VRCAvatarDescriptor>() != null) {
            CustomGUI.LineGap();

            if (CustomGUI.StandardButton("Questify Avatar")) {
                QuestifyGameObjectAndChildren();
                serializedObject.Update();
                EditorUtility.SetDirty(target);
            }
            CustomGUI.ItalicLabel("Automatically adds VRC Questify components to this VRC avatar and any child object that should have one.");
        } else {
            if (CustomGUI.StandardButton("Questify")) {
                QuestifyGameObject();
                serializedObject.Update();
                EditorUtility.SetDirty(target);
            }
            CustomGUI.ItalicLabel("Automatically adds VRC Questify components to this object.");
        }
        
        CustomGUI.LineGap();

        CustomGUI.MediumLabel("Actions");

        CustomGUI.SmallLineGap();

        actionsList.DoLayoutList();

        CustomGUI.MediumLabel("Children");
        
        CustomGUI.SmallLineGap();

        foreach (Transform child in gameObject.transform) {
            Utils.RenderChildrenOfInterestList(child.gameObject, 0);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void QuestifyGameObjectAndChildren() {
        var gameObject = ((VRC_Questify)target).gameObject;
        Debug.Log($"VRC_Questifyer :: Questifying object \"{gameObject.name}\" and children...");
        Utils.CreateMissingQuestMaterials(gameObject, true);
        Utils.AddActionsToRemoveNonWhitelistedComponents(gameObject, true);
    }

    void QuestifyGameObject() {
        var gameObject = ((VRC_Questify)target).gameObject;
        Debug.Log($"VRC_Questifyer :: Questifying only object \"{gameObject.name}\"...");

        Utils.CreateMissingQuestMaterials(gameObject);
        Utils.AddActionsToRemoveNonWhitelistedComponents(gameObject);
    }

    void DrawActionsListItem(Rect rect, int index, bool isActive, bool isFocused) {
        SerializedProperty actionElement = actionsList.serializedProperty.GetArrayElementAtIndex(index);

        var margin = 40;
        
        var firstColumnPerc = 0.4f;
        var firstColumnWidth = (rect.width * firstColumnPerc) - margin;
        
        SerializedProperty typeProperty = actionElement.FindPropertyRelative("type");
        var enumValueIndex = typeProperty.enumValueIndex;

        EditorGUI.PropertyField(
            new Rect(margin, rect.y + defaultPadding, firstColumnWidth, EditorGUIUtility.singleLineHeight),
            typeProperty,
            GUIContent.none
        );

        var secondColumnPerc = 0.6f;
        var secondColumnWidth = rect.width * secondColumnPerc;

        VRC_Questify.ActionType type = (VRC_Questify.ActionType)typeProperty.enumValueIndex;

        switch (type) {
            case VRC_Questify.ActionType.SwitchToMaterial:
                SerializedProperty materials = actionElement.FindPropertyRelative("materials");

                if (materialLists.Length != actions.arraySize) {
                    materialLists = new ReorderableList[actions.arraySize];
                }

                if (materialLists[index] == null) {
                    materialLists[index] = new ReorderableList(serializedObject, materials) {
                        displayAdd = true,
                        displayRemove = true,
                        draggable = true,
                        drawHeaderCallback = (Rect subRect) => {
                            EditorGUI.LabelField(subRect, "Materials");
                        },
                        drawElementCallback = (Rect subRect, int subIndex, bool subIsActive, bool subIsFocused) => {
                            SerializedProperty materialElement = materials.GetArrayElementAtIndex(subIndex);

                            EditorGUI.PropertyField(
                                new Rect(subRect.x, subRect.y, subRect.width, subRect.height),
                                materialElement,
                                GUIContent.none
                            );
                        }
                    };
                }

                materialLists[index].DoList(
                    new Rect(margin + firstColumnWidth, rect.y + defaultPadding, secondColumnWidth, EditorGUIUtility.singleLineHeight * 2)
                );
                break;
            case VRC_Questify.ActionType.RemoveGameObject:
                break;
            case VRC_Questify.ActionType.RemoveComponent:
                SerializedProperty components = actionElement.FindPropertyRelative("components");

                if (componentLists.Length != actions.arraySize) {
                    componentLists = new ReorderableList[actions.arraySize];
                }

                if (componentLists[index] == null) {
                    componentLists[index] = new ReorderableList(serializedObject, components) {
                        displayAdd = true,
                        displayRemove = true,
                        draggable = false,
                        drawHeaderCallback = (Rect subRect) => {
                            EditorGUI.LabelField(subRect, "Components");
                        },
                        drawElementCallback = (Rect subRect, int subIndex, bool subIsActive, bool subIsFocused) => {
                            SerializedProperty componentElement = components.GetArrayElementAtIndex(subIndex);

                            EditorGUI.PropertyField(
                                new Rect(subRect.x, subRect.y, subRect.width, subRect.height),
                                componentElement,
                                GUIContent.none
                            );
                        }
                    };
                }

                componentLists[index].DoList(
                    new Rect(margin + firstColumnWidth, rect.y + defaultPadding, secondColumnWidth, EditorGUIUtility.singleLineHeight * 2)
                );
                break;
            default:
                throw new System.Exception($"VRC_Questifyer - unknown type {type}");
        }
    }

    public void CreateMissingQuestMaterials() {
        var gameObject = ((VRC_Questify)target).gameObject;
        var renderer = gameObject.GetComponent<Renderer>();
        var allMaterials = new List<Material>();

        foreach (Material material in renderer.sharedMaterials) {
            string pathToMaterial = AssetDatabase.GetAssetPath(material);

            if (pathToMaterial == "") {
                throw new System.Exception("VRC_Questifyer - failed to find material at path " + pathToMaterial);
            }

            if (pathToMaterial.Contains("Quest")) {
                Debug.Log($"VRC_Questifyer :: Material \"{pathToMaterial}\" is already named with Quest");
                allMaterials.Add(material);
                continue;
            }

            Material questMaterial = Utils.CreateQuestMaterial(pathToMaterial, material);

            allMaterials.Add(questMaterial);
        }

        var found = false;

        for (var i = 0; i < actions.arraySize; i++) {
            var actionElement = actions.GetArrayElementAtIndex(i);

            if ((VRC_Questify.ActionType)actionElement.FindPropertyRelative("type").enumValueIndex == VRC_Questify.ActionType.SwitchToMaterial) {
                found = true;
                var materialsElement = actionElement.FindPropertyRelative("materials");

                materialsElement.arraySize = allMaterials.Count;

                for (var m = 0; m < allMaterials.Count; m++) {
                    var materialElement = materialsElement.GetArrayElementAtIndex(m);
                    materialElement.objectReferenceValue = allMaterials[m];
                }
            }
        }

        if (!found) {
            actions.arraySize++;
            serializedObject.ApplyModifiedProperties(); // Apply the change

            SerializedProperty actionElement = actions.GetArrayElementAtIndex(actions.arraySize - 1);

            var materialsElement = actionElement.FindPropertyRelative("materials");

            materialsElement.arraySize = allMaterials.Count;

            for (var m = 0; m < allMaterials.Count; m++) {
                var materialElement = materialsElement.GetArrayElementAtIndex(m);
                materialElement.objectReferenceValue = allMaterials[m];
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}