using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;
using UnityEditorInternal;
using VRC.SDKBase.Editor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Editor;
using VRC.SDKBase;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation;
using VRC.SDKBase.Validation.Performance.Stats;
using VRCStation = VRC.SDK3.Avatars.Components.VRCStation;
using VRC.SDK3.Validation;
using VRC.Core;
using VRCSDK2;
using VRCQuestifyer;

public class VRC_Questifyer : EditorWindow
{
    VRCAvatarDescriptor sourceVrcAvatarDescriptor;
    List<Action> actions = new List<Action>();
    bool isCreateFormVisible = false;
    int selectedTypeDropdownIdx = 0;
    string createFormFieldValue1 = "";
    string createFormFieldValue2 = "";
    bool isDryRun = false;

    enum Types {
        SwitchToMaterial,
        RemoveGameObject
    }

    List<Action> actionsToPerform = new List<Action>();
    List<System.Exception> errors = new List<System.Exception>();

    [MenuItem("PeanutTools/VRC Questifyer _%#T")]
    public static void ShowWindow()
    {
        var window = GetWindow<VRC_Questifyer>();
        window.titleContent = new GUIContent("VRC Questifyer");
        window.minSize = new Vector2(250, 50);
    }

    void Awake() {
        LoadActions();
    }

    void OnFocus() {
        LoadActions();
    }

    void OnGUI()
    {
        GUILayout.Label("Step 1: Select your avatar");

        sourceVrcAvatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", sourceVrcAvatarDescriptor, typeof(VRCAvatarDescriptor));
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Step 2: Configure your actions");
        
        EditorGUILayout.Space();

        RenderActions();

        EditorGUILayout.Space();

        if (isCreateFormVisible) {
            RenderCreateActionForm();

            if (GUILayout.Button("Cancel", GUILayout.Width(75), GUILayout.Height(25))) {
                isCreateFormVisible = false;
            }
        } else {
            if (GUILayout.Button("Add Action", GUILayout.Width(75), GUILayout.Height(25))) {
                isCreateFormVisible = true;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        GUILayout.Label("Step 3: Questify!");
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(sourceVrcAvatarDescriptor == null);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Questify", GUILayout.Width(100), GUILayout.Height(50)))
        {
            Questify();
        }

        if (GUILayout.Button("Dry Run", GUILayout.Width(100), GUILayout.Height(50)))
        {
            DryRun();
        }
            
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
        
        GUILayout.Label("Note that a Quest avatar is always created if missing");

        RenderActionsToPerform();

        RenderErrors();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Download new versions: https://github.com/imagitama");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("https://twitter.com/@HiPeanutBuddha");
        GUILayout.Label("Peanut#1756");

        isDryRun = false;
    }

    void RenderErrors() {
        if (errors.Count == 0) {
            return;
        }

        GUILayout.Label("Errors:");

        foreach (System.Exception exception in errors) {
            GUILayout.Label(exception.Message);
        }
    }

    void RenderActionsToPerform() {
        if (actionsToPerform.Count == 0) {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Actions that have been performed (or to perform if dry):");

        foreach (Action action in actionsToPerform) {
            RenderAction(action);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    void DryRun() {
        // disabled at end of GUI
        isDryRun = true;

        Questify();
    }

    string[] typeOptions = new string[] {
        "Select an action",
        Types.SwitchToMaterial.ToString(),
        Types.RemoveGameObject.ToString()
    };

    void RenderAddButton(Types selectedType, bool isEnabled = true) {
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(isEnabled != true);

        if (GUILayout.Button("Add", GUILayout.Width(50), GUILayout.Height(25))) {
            AddAction(selectedType, createFormFieldValue1, createFormFieldValue2);
            isCreateFormVisible = false;
        }
        
        EditorGUI.EndDisabledGroup();
    }

    void RenderCreateActionForm() {
        selectedTypeDropdownIdx = EditorGUILayout.Popup("Type", selectedTypeDropdownIdx, typeOptions);

        if (selectedTypeDropdownIdx != 0) {
            int enumIndex = selectedTypeDropdownIdx - 1;
            Types selectedType = (Types)enumIndex;

            GameObject gameObjectToUse = null;

            switch (selectedType) {
                case Types.SwitchToMaterial:
                    GUILayout.Label("Path to mesh game object:");
                    createFormFieldValue1 = EditorGUILayout.TextField(createFormFieldValue1);

                    gameObjectToUse = (GameObject)EditorGUILayout.ObjectField("Search:", gameObjectToUse, typeof(GameObject));

                    if (gameObjectToUse != null) { 
                        createFormFieldValue1 = Utils.GetGameObjectPath(gameObjectToUse);
                    }

                    GUILayout.Label("Path to material file:");
                    createFormFieldValue2 = EditorGUILayout.TextField(createFormFieldValue2);

                    if (GUILayout.Button("Select File", GUILayout.Width(75), GUILayout.Height(25))) {
                        string path = EditorUtility.OpenFilePanel("Select a material", Application.dataPath, "mat");

                        if (path != "") {
                            string pathToUse = Utils.GetPathRelativeToAssets(path);
                            createFormFieldValue2 = pathToUse;
                        }
                    }

                    RenderAddButton(selectedType, createFormFieldValue1 != "" && createFormFieldValue2 != "");
                    break;

                case Types.RemoveGameObject:
                    GUILayout.Label("Path to game object:");
                    createFormFieldValue1 = EditorGUILayout.TextField(createFormFieldValue1);

                    gameObjectToUse = (GameObject)EditorGUILayout.ObjectField("Search:", gameObjectToUse, typeof(GameObject));

                    if (gameObjectToUse != null) { 
                        createFormFieldValue1 = Utils.GetGameObjectPath(gameObjectToUse);
                    }

                    RenderAddButton(selectedType, createFormFieldValue1 != "");
                    break;
                
                default:
                    throw new System.Exception("Unknown type for dropdown!");
                    break;
            }
        }
    }

    void AddAction(Types type, string fieldValue1, string fieldValue2) {
        Debug.Log("Adding action...");

        Action action;

        switch (type) {
            case Types.SwitchToMaterial:
                action = new SwitchToMaterialAction() {
                    pathToRenderer = fieldValue1,
                    pathToMaterial = fieldValue2
                };
                break;

            case Types.RemoveGameObject:
                action = new RemoveGameObjectAction() {
                    pathToGameObject = fieldValue1
                };
                break;

            default:
                throw new System.Exception("Unknown type to add!");
                break;
        }

        List<Action> newActions = actions.ToList();
        newActions.Add(action);
        actions = newActions;

        SaveActions();
    }

    void RenderActions() {
        if (actions.Count == 0) {
            GUILayout.Label("No actions configured");
            return;
        }

        foreach (Action action in actions) {
            GUILayout.BeginHorizontal();

            RenderTypeForAction(action);
            RenderDataForAction(action);

            int idx = actions.IndexOf(action);

            if (GUILayout.Button("Delete", GUILayout.Width(50), GUILayout.Height(25))) {
                DeleteAction(action);
            }

            EditorGUI.BeginDisabledGroup(idx == 0);
            if (GUILayout.Button("^", GUILayout.Width(20), GUILayout.Height(25))) {
                MoveActionUp(action);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(idx == actions.Count - 1);
            if (GUILayout.Button("v", GUILayout.Width(20), GUILayout.Height(25))) {
                MoveActionDown(action);
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }
    }

    void RenderAction(Action action) {
        GUILayout.BeginHorizontal();

        RenderTypeForAction(action);
        RenderDataForAction(action);

        GUILayout.EndHorizontal();
    }

    void RenderTypeForAction(Action action) {
        if (action is SwitchToMaterialAction) {
            GUILayout.Label("Switch Material");
        } else if (action is RemoveGameObjectAction) {
            GUILayout.Label("Remove Object");
        } else {
            throw new System.Exception("Unknown action!");
        }
    }

    void RenderDataForAction(Action action) {
        if (action is SwitchToMaterialAction) {
            GUILayout.Label((action as SwitchToMaterialAction).pathToRenderer);
            GUILayout.Label((action as SwitchToMaterialAction).pathToMaterial);
        } else if (action is RemoveGameObjectAction) {
            GUILayout.Label((action as RemoveGameObjectAction).pathToGameObject);
        } else {
            throw new System.Exception("Unknown action!");
        }
    }

    void DeleteAction(Action action) {
        Debug.Log("Deleting action...");

        List<Action> newActions = actions.ToList();
        newActions.Remove(action);
        actions = newActions;

        SaveActions();
    }

    void MoveActionUp(Action action) {
        Debug.Log("Moving action up...");

        List<Action> newActions = actions.ToList();
        int idx = newActions.IndexOf(action);
        newActions.Remove(action);
        newActions.Insert(idx - 1, action);
        actions = newActions;

        SaveActions();
    }

    void MoveActionDown(Action action) {
        Debug.Log("Moving action down...");

        List<Action> newActions = actions.ToList();
        int idx = newActions.IndexOf(action);
        newActions.Remove(action);
        newActions.Insert(idx + 1, action);
        actions = newActions;

        SaveActions();
    }

    void Questify() {
        Debug.Log("Found " + actions.Count + " actions");

        LoadActions();
        errors = new List<System.Exception>();

        actionsToPerform = actions.ToList();

        GameObject avatar = CreateQuestAvatar(sourceVrcAvatarDescriptor);

        SwitchAllMaterialsToQuestForAvatar(avatar);
    }

    void CreateActionsFileIfNoExist() {
        try {
            string pathToJsonFile = Application.dataPath + "/VRC_Questifyer_Data.json";
            File.ReadAllText(pathToJsonFile);
        } catch (FileNotFoundException exception) {
            Debug.Log("JSON file does not exist, creating...");
            SaveActions();
        }
    }

    void LoadActions() {
        CreateActionsFileIfNoExist();

        string pathToJsonFile = Application.dataPath + "/VRC_Questifyer_Data.json";
        string json = File.ReadAllText(pathToJsonFile);

        ActionsJson actionsData = JsonUtility.FromJson<ActionsJson>(json);

        if (actionsData.actions == null) {
            throw new System.Exception("Actions in JSON is missing!");
        }

        actions = actionsData.actions.Select(actionData => ActionJsonToAction(actionData)).ToList();
    }

    void SaveActions() {
        string pathToJsonFile = Application.dataPath + "/VRC_Questifyer_Data.json";

        ActionsJson actionsForJson = new ActionsJson() {
            actions = actions.Select(action => ActionToJson(action)).ToArray()
        };

        // uses Unity's serializer so make sure they have the [Serializable] attribute
        string json = JsonUtility.ToJson(actionsForJson, true);

        File.WriteAllText(pathToJsonFile, json);
    }

    ActionJson ActionToJson(Action action) {
        if (action is SwitchToMaterialAction) {
            return new ActionJson() {
                type = "SwitchToMaterial",
                data = new StringStringDictionary() {
                    { "pathToRenderer", (action as SwitchToMaterialAction).pathToRenderer },
                    { "pathToMaterial", (action as SwitchToMaterialAction).pathToMaterial }
                }
            };
        } else if (action is RemoveGameObjectAction) {
            return new ActionJson() {
                type = "RemoveGameObject",
                data = new StringStringDictionary() {
                    { "pathToGameObject", (action as RemoveGameObjectAction).pathToGameObject }
                }
            };
        } else {
            throw new System.Exception("Cannot convert action to JSON: unknown type " + nameof(action));
        }
    }

    Action ActionJsonToAction(ActionJson actionJson) {
        switch (actionJson.type) {
            case "SwitchToMaterial":
                return new SwitchToMaterialAction() {
                    pathToRenderer = actionJson.data["pathToRenderer"],
                    pathToMaterial = actionJson.data["pathToMaterial"],
                };
            case "RemoveGameObject":
                return new RemoveGameObjectAction() {
                    pathToGameObject = actionJson.data["pathToGameObject"]
                };
            default:
                throw new System.Exception("Cannot convert action JSON to action: unknown type " + actionJson.type);
                break;
        }
    }

    GameObject CreateQuestAvatar(VRCAvatarDescriptor sourceAvatar) {
        string questAvatarName = sourceAvatar.gameObject.name + " [Quest]";

        GameObject existingObject = GameObject.Find("/" + questAvatarName);

        if (existingObject != null) {
            return existingObject;
        }

        GameObject clone = Instantiate(sourceAvatar.gameObject, new Vector3(-3, 0, 0), new Quaternion(0, 0, 0, 1));
        clone.name = questAvatarName;

        sourceAvatar.gameObject.SetActive(false);

        return clone;
    }

    void SwitchAllMaterialsToQuestForAvatar(GameObject avatar) {
        Debug.Log("Switching all materials to Quest...");

        Renderer[] allRenderers = avatar.GetComponentsInChildren<Renderer>(true);

        Debug.Log("Found " + allRenderers.Length + " renderers");

        foreach (Renderer renderer in allRenderers) {
            SwitchMeshToQuestMaterialsForAvatar(avatar, renderer);
        }
    }

    void SwitchMeshToQuestMaterialsForAvatar(GameObject avatar, Renderer renderer) {
        string pathToGameObject = Utils.GetGameObjectPath(renderer.gameObject);

        Debug.Log("Switching all materials for mesh " + pathToGameObject + "...");

        Material[] materials = renderer.sharedMaterials;

        Debug.Log("Found " + materials.Length + " materials");

        Material[] newMaterials = new Material[materials.Length];

        int idx = -1;

        foreach (Material material in materials) {
            idx++;

            try {
                string pathToMaterial = AssetDatabase.GetAssetPath(material);

                if (pathToMaterial == "" || pathToMaterial.Contains("Quest")) {
                    Debug.Log("Material is empty or already Quest, skipping...");
                    newMaterials[idx] = material;
                    return;
                }
                
                Debug.Log("Switching material " + pathToMaterial + "...");

                string pathToQuestMaterial = pathToMaterial.Replace(".mat", " Quest.mat");

                Material questMaterial = (Material)AssetDatabase.LoadAssetAtPath(pathToQuestMaterial, typeof(Material));

                if (questMaterial != null) {
                    newMaterials[idx] = questMaterial;
                } else {
                    string pathToQuestMaterialParent = Directory.GetParent(pathToMaterial).FullName.Replace(Path.GetFullPath(Application.dataPath), "Assets");

                    string pathToQuestMaterialInQuestFolder = Path.Combine(pathToQuestMaterialParent, "Quest", Path.GetFileName(pathToMaterial).Replace(".mat", " Quest.mat"));

                    Material questMaterialInFolder = (Material)AssetDatabase.LoadAssetAtPath(pathToQuestMaterialInQuestFolder, typeof(Material));

                    if (questMaterialInFolder == null) {
                        throw new System.Exception("Could not find either a quest version for material " + pathToMaterial);
                    }

                    pathToQuestMaterial = pathToQuestMaterialInQuestFolder;

                    newMaterials[idx] = questMaterialInFolder;
                }

                actionsToPerform.Add(new SwitchToMaterialAction() {
                    pathToRenderer = pathToGameObject,
                    pathToMaterial = pathToQuestMaterial
                });
            } catch (System.Exception exception) {
                errors.Add(exception);
            }
        }

        if (isDryRun == false) {
            renderer.sharedMaterials = newMaterials;
        }
    }

    void SwitchMeshByPathToQuestMaterialsForAvatar(GameObject avatar, string pathToMesh) {
        Debug.Log("Switching all materials for mesh " + pathToMesh + "...");

        Transform meshTransform = avatar.transform.Find(pathToMesh);

        if (meshTransform == null) {
            throw new System.Exception("Game object not found at path " + pathToMesh);
        }

        Renderer renderer = meshTransform.GetComponent<Renderer>();

        if (renderer == null) {
            throw new System.Exception("Renderer not found at path " + pathToMesh);
        }

        SwitchMeshToQuestMaterialsForAvatar(avatar, renderer);
    }

    void RemoveGameObjectForAvatar(GameObject avatar, string pathToGameObject) {
        Debug.Log("Removing game object " + pathToGameObject + "...");

        Transform foundTransform = avatar.transform.Find(pathToGameObject);

        if (foundTransform == null) {
            Debug.Log("Game object not found at path " + pathToGameObject);
            return;
        }

        DestroyImmediate(foundTransform.gameObject);
    }
}
