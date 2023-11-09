using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using PeanutTools_VRC_Questifyer;
using VRC.SDK3.Avatars.Components;

public class VRC_Questifyer : EditorWindow {
    static GitHub_Update_Checker githubUpdateChecker;
    Vector2 scrollPosition;

    [MenuItem("PeanutTools/VRC Questifyer")]
    public static void ShowWindow() {
        var window = GetWindow<VRC_Questifyer>();
        window.titleContent = new GUIContent("VRC Questifyer");
        window.minSize = new Vector2(400, 200);
        SetupAutoUpdate();
    }

    static void SetupAutoUpdate() {
        if (githubUpdateChecker == null) {
            githubUpdateChecker = new GitHub_Update_Checker() {
                githubOwner = "imagitama",
                githubRepo = "vrc-questifyer",
                currentVersion = File.ReadAllText("Assets/PeanutTools/VRC_Questifyer/VERSION.txt", System.Text.Encoding.UTF8)
            };
        }
    }

    void Awake() {
        SetupAutoUpdate();
    }

    void OnFocus() {
        SetupAutoUpdate();
    }

    void OnGUI() {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        CustomGUI.LargeLabel("VRC Questifyer");
        CustomGUI.ItalicLabel("Non-destructively performs actions on your avatar for Quest upload.");

        CustomGUI.LineGap();
        
        CustomGUI.LargeLabel("Avatars In Scene");

        RenderAllAvatarsAndComponentsInScene();

        CustomGUI.LineGap();

        CustomGUI.LargeLabel("Danger Zone");
        
        CustomGUI.SmallLineGap();

        if (GUILayout.Button("Remove all VRC Questify components", GUILayout.Width(300), GUILayout.Height(25))) {
            if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to remove all VRC Questify components from all objects in the scene?", "Yes", "No")) {
                RemoveAllVrcQuestifyerComponentsInScene();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void RemoveAllVrcQuestifyerComponentsInScene() {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (var rootObject in rootObjects) {
            Utils.RemoveAllVrcQuestifyerComponents(rootObject);
        }
    }

    void QuestifyAvatar(GameObject gameObject) {
        Debug.Log($"VRC_Questifyer :: Questifying avatar \"{gameObject.name}\"...");
        Utils.CreateMissingQuestMaterials(gameObject, true);
        Utils.AddActionsToRemoveNonWhitelistedComponents(gameObject, true);
    }

    VRC_Questify[] FindAllComponentsInScene() {
        return FindObjectsOfType<VRC_Questify>();
    }

    void RenderAllAvatarsAndComponentsInScene() {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var hasRenderedAtLeastOne = false;

        foreach (var rootObject in rootObjects) {
            VRCAvatarDescriptor vrcAvatarDescriptor = rootObject.GetComponent<VRCAvatarDescriptor>();

            if (vrcAvatarDescriptor != null) {
                if (hasRenderedAtLeastOne) {
                    CustomGUI.LineGap();
                } else {
                    hasRenderedAtLeastOne = true;
                }

                CustomGUI.MediumLabel($"{rootObject.name}");

                CustomGUI.FocusableLabel("Go To Avatar", rootObject);

                CustomGUI.SmallLineGap();

                if (CustomGUI.StandardButton("Questify Avatar")) {
                    QuestifyAvatar(rootObject);
                }
                CustomGUI.ItalicLabel("Automatically adds VRC Questify components to any child object that should have one.");

                CustomGUI.SmallLineGap();

                CustomGUI.MediumLabel("Children");

                Utils.RenderChildrenOfInterestList(rootObject, 0);
            }
        }
    }
}