using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.Rendering;
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
    enum Types {
        SwitchToMaterial,
        RemoveGameObject
    }

    VRCAvatarDescriptor sourceVrcAvatarDescriptor;
    List<Action> actions = new List<Action>();
    bool isCreateFormVisible = false;
    int selectedTypeDropdownIdx = 0;
    string createFormFieldValue1 = "";
    string createFormFieldValue2 = "";
    int createFormFieldValue3 = 0;
    bool isDryRun = false;
    bool isRunningOnExistingQuestAvatar = false;
    List<Action> actionsToPerform = new List<Action>();
    List<System.Exception> errors = new List<System.Exception>();
    Dictionary<string, Material> knownMaterials = new Dictionary<string, Material>();
    bool shouldPerformAtEnd = false;

    // user settings
    bool autoCreateQuestMaterials = false;
    bool placeQuestMaterialsInOwnDirectory = true;
    bool useToonShader = false;

    [MenuItem("PeanutTools/VRC Questifyer")]
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
        GUILayout.Label("Step 1: Select your avatar", EditorStyles.boldLabel);

        sourceVrcAvatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", sourceVrcAvatarDescriptor, typeof(VRCAvatarDescriptor));
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Step 2: Configure your actions", EditorStyles.boldLabel);
        
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
        
        GUILayout.Label("Step 3: Configure settings", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        
        float originalValue = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 500;

        autoCreateQuestMaterials = EditorGUILayout.Toggle("Create missing Quest materials", autoCreateQuestMaterials);

        EditorGUILayout.Space();

        placeQuestMaterialsInOwnDirectory = EditorGUILayout.Toggle("Place materials inside \"Quest\" folder (recommended)", placeQuestMaterialsInOwnDirectory);

        EditorGUILayout.Space();

        useToonShader = EditorGUILayout.Toggle("Use toon shader", useToonShader);

        EditorGUIUtility.labelWidth = originalValue;

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Step 4: Questify!", EditorStyles.boldLabel);
        
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
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontStyle = FontStyle.Italic;
        GUILayout.Label("Dry run note: a Quest avatar is always created!", labelStyle);

        RenderActionsToPerform();

        RenderErrors();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Download new versions and get support: https://discord.gg/R6Scz6ccdn");

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
            if (exception is MaterialNotFoundException) {
                GUILayout.Label("Could not find either a quest version for material " + (exception as MaterialNotFoundException).pathToMaterial);
            } else if (exception is GameObjectNotFoundException) {
                GUILayout.Label("Could not find the game object at path " + (exception as GameObjectNotFoundException).pathToGameObject);
            } else {
                GUILayout.Label(exception.Message);
            }
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
            AddAction(selectedType, createFormFieldValue1, createFormFieldValue2, createFormFieldValue3);
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
                        createFormFieldValue1 = Utils.GetRelativeGameObjectPath(gameObjectToUse, sourceVrcAvatarDescriptor.gameObject);
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

                    createFormFieldValue3 = EditorGUILayout.IntField("Material index (default 0):", createFormFieldValue3);

                    RenderPerformAtEndToggle();
                    RenderAddButton(selectedType, createFormFieldValue1 != "" && createFormFieldValue2 != "");
                    break;

                case Types.RemoveGameObject:
                    GUILayout.Label("Path to game object:");
                    createFormFieldValue1 = EditorGUILayout.TextField(createFormFieldValue1);

                    gameObjectToUse = (GameObject)EditorGUILayout.ObjectField("Search:", gameObjectToUse, typeof(GameObject));

                    if (gameObjectToUse != null) { 
                        createFormFieldValue1 = Utils.GetRelativeGameObjectPath(gameObjectToUse, sourceVrcAvatarDescriptor.gameObject);
                    }

                    RenderPerformAtEndToggle();
                    RenderAddButton(selectedType, createFormFieldValue1 != "");
                    break;
                
                default:
                    throw new System.Exception("Unknown type for dropdown!");
                    break;
            }
        }
    }

    void RenderPerformAtEndToggle() {
        shouldPerformAtEnd = EditorGUILayout.Toggle("Perform at end", shouldPerformAtEnd);
    }

    // TODO: Better way of passing arbitrary fields around
    void AddAction(Types type, string fieldValue1, string fieldValue2, int fieldValue3) {
        Debug.Log("Adding action...");

        Action action;

        switch (type) {
            case Types.SwitchToMaterial:
                action = new SwitchToMaterialAction() {
                    pathToRenderer = fieldValue1,
                    pathToMaterial = fieldValue2,
                    materialIndex = fieldValue3
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

        action.performAtEnd = shouldPerformAtEnd;

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
        RenderTypeForAction(action);

        GUILayout.BeginHorizontal();

        RenderDataForAction(action);

        GUILayout.EndHorizontal();
    }

    void RenderTypeForAction(Action action) {
        string label;

        if (action is SwitchToMaterialAction) {
            label = "Switch Material";
        } else if (action is RemoveGameObjectAction) {
            label = "Remove Object";
        } else {
            throw new System.Exception("Unknown action!");
        }

        GUILayout.Label(label + (action.performAtEnd ? " (end)" : ""));
    }

    void RenderDataForAction(Action action) {
        GUIStyle guiStyle = new GUIStyle() {
            fontSize = 10
        };
        if (action is SwitchToMaterialAction) {
            GUILayout.Label((action as SwitchToMaterialAction).pathToRenderer, guiStyle);
            string label = (action as SwitchToMaterialAction).pathToMaterial;
            string materialIndexStr = (action as SwitchToMaterialAction).materialIndex.ToString();
            GUILayout.Label(label + " (" + materialIndexStr + ")", guiStyle);
        } else if (action is RemoveGameObjectAction) {
            GUILayout.Label((action as RemoveGameObjectAction).pathToGameObject, guiStyle);
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
        LoadActions();
        errors = new List<System.Exception>();

        Debug.Log("Found " + actions.Count + " from filesystem");

        actionsToPerform = actions.ToList();

        GameObject avatar = CreateQuestAvatar(sourceVrcAvatarDescriptor);

        AddActionsToSwitchAllMaterialsToQuestForAvatar(avatar);

        Debug.Log("Added " + (actionsToPerform.Count - actions.Count) + " new actions");
        
        Debug.Log("Performing " + actionsToPerform.Count + " actions...");

        List<Action> sortedActions = SortActions(actionsToPerform);

        if (isDryRun == false) {
            foreach (Action actionToPerform in actionsToPerform) {
                PerformAction(actionToPerform, avatar);
            }
        }
    }

    List<Action> SortActions(List<Action> actionsToSort) {
        actionsToSort.Sort((actionA, actionB) => {
            if (actionA.performAtEnd == true && actionB.performAtEnd != true) {
                return 1;
            }
            if (actionA.performAtEnd != true && actionB.performAtEnd == true) {
                return -1;
            }
            // both same
            // if (actionA.performAtEnd == true && actionB.performAtEnd == true) {
            //     return 0;
            // }
            return 0;
        });
        return actionsToSort;
    }

    void PerformAction(Action action, GameObject avatar) {
        if (action is SwitchToMaterialAction) {
            string pathToRenderer = (action as SwitchToMaterialAction).pathToRenderer;
            string pathToMaterial = (action as SwitchToMaterialAction).pathToMaterial;
            int materialIndex = (action as SwitchToMaterialAction).materialIndex;
            SwitchGameObjectMaterialForAvatar(avatar, pathToRenderer, pathToMaterial, materialIndex);
        } else if (action is RemoveGameObjectAction) {
            try {
                string pathToGameObject = (action as RemoveGameObjectAction).pathToGameObject;
                RemoveGameObjectForAvatar(avatar, pathToGameObject);
            } catch (GameObjectNotFoundException exception) {
                errors.Add(exception);
            }
        } else {
            throw new System.Exception("Cannot perform action - unknown action type: " + nameof(action));
        }
    }

    void SwitchGameObjectMaterialForAvatar(GameObject avatar, string pathToRenderer, string pathToMaterial, int materialIndex = 0) {
        Transform rendererTransform = avatar.transform.Find(pathToRenderer);

        if (rendererTransform == null) {
            throw new System.Exception("Failed to switch game object material - game object not found! Path: " + pathToRenderer);
        }

        Renderer renderer = rendererTransform.GetComponent<Renderer>();

        if (renderer == null) {
            throw new System.Exception("Failed to switch game object material - game object does not have a renderer! Path: " + pathToRenderer);
        }

        Debug.Log("Switching renderer (" + pathToRenderer + ") material to " + pathToMaterial + " (index " + materialIndex.ToString() + ")");

        Material materialToSwitchTo = GetMaterialAtPath(pathToMaterial);

        Material[] existingMaterials = renderer.sharedMaterials;

        existingMaterials[materialIndex] = materialToSwitchTo;

        renderer.sharedMaterials = existingMaterials;
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

    // TODO: Add as methods to Action class
    ActionJson ActionToJson(Action action) {
        ActionJson actionJson;

        if (action is SwitchToMaterialAction) {
            actionJson = new ActionJson() {
                type = "SwitchToMaterial",
                data = new StringStringDictionary() {
                    { "pathToRenderer", (action as SwitchToMaterialAction).pathToRenderer },
                    { "pathToMaterial", (action as SwitchToMaterialAction).pathToMaterial },
                    { "materialIndex", (action as SwitchToMaterialAction).materialIndex.ToString() },
                }
            };
        } else if (action is RemoveGameObjectAction) {
            actionJson = new ActionJson() {
                type = "RemoveGameObject",
                data = new StringStringDictionary() {
                    { "pathToGameObject", (action as RemoveGameObjectAction).pathToGameObject }
                }
            };
        } else {
            throw new System.Exception("Cannot convert action to JSON: unknown type " + nameof(action));
        }

        actionJson.performAtEnd = action.performAtEnd;

        return actionJson;
    }

    Action ActionJsonToAction(ActionJson actionJson) {
        Action action;

        switch (actionJson.type) {
            case "SwitchToMaterial":
                string pathToRenderer;
                actionJson.data.TryGetValue("pathToRenderer", out pathToRenderer);
                string pathToMaterial;
                actionJson.data.TryGetValue("pathToMaterial", out pathToMaterial);
                string materialIndexStr;
                actionJson.data.TryGetValue("materialIndex", out materialIndexStr);

                int materialIndex = materialIndexStr != null ? Utils.StringToInt(materialIndexStr) : 0;

                action = new SwitchToMaterialAction() {
                    pathToRenderer = pathToRenderer,
                    pathToMaterial = pathToMaterial,
                    materialIndex = materialIndex
                };
                break;
            case "RemoveGameObject":
                action = new RemoveGameObjectAction() {
                    pathToGameObject = actionJson.data["pathToGameObject"]
                };
                break;
            default:
                throw new System.Exception("Cannot convert action JSON to action: unknown type " + actionJson.type);
                break;
        }

        action.performAtEnd = actionJson.performAtEnd;

        return action;
    }

    GameObject CreateQuestAvatar(VRCAvatarDescriptor sourceAvatar) {
        string questAvatarName = sourceAvatar.gameObject.name + " [Quest]";

        GameObject existingObject = GameObject.Find("/" + questAvatarName);

        if (existingObject != null) {
            Debug.Log("Quest avatar already exists, using it...");
            isRunningOnExistingQuestAvatar = true;
            return existingObject;
        }

        GameObject clone = Instantiate(sourceAvatar.gameObject);
        clone.name = questAvatarName;
        clone.SetActive(true);

        sourceAvatar.transform.position = new Vector3(sourceAvatar.transform.position.x - 3, sourceAvatar.transform.position.y, sourceAvatar.transform.position.z);
        sourceAvatar.gameObject.SetActive(false);

        return clone;
    }

    void AddActionsToSwitchAllMaterialsToQuestForAvatar(GameObject avatar) {
        Debug.Log("Switching all materials to Quest...");

        Renderer[] allRenderers = avatar.GetComponentsInChildren<Renderer>(true);

        Debug.Log("Found " + allRenderers.Length + " renderers");

        foreach (Renderer renderer in allRenderers) {
            AddActionsForSwitchingMeshToQuestMaterialsForAvatar(avatar, renderer);
        }
    }

    Material GetMaterialAtPath(string pathToMaterial, bool ignoreNotFound = false) {
        if (knownMaterials.ContainsKey(pathToMaterial)) {
            return knownMaterials[pathToMaterial];
        }

        Material loadedMaterial = (Material)AssetDatabase.LoadAssetAtPath(pathToMaterial, typeof(Material));

        if (loadedMaterial == null) {
            if (ignoreNotFound) {
                return null;
            }
            throw new System.Exception("Failed to load material at path: " + pathToMaterial);
        }

        knownMaterials[pathToMaterial] = loadedMaterial;

        return loadedMaterial;
    }

    Material LooselyGetMaterialAtPath(string pathToMaterial) {
        return GetMaterialAtPath(pathToMaterial, true);
    }

    void AddActionsForSwitchingMeshToQuestMaterialsForAvatar(GameObject avatar, Renderer renderer) {
        string relativePathToRenderer = Utils.GetRelativeGameObjectPath(renderer.gameObject, avatar);

        Debug.Log("Switching all materials for renderer " + relativePathToRenderer + "...");

        Material[] materials = renderer.sharedMaterials;

        Debug.Log("Found " + materials.Length + " materials");

        int idx = -1;

        foreach (Material material in materials) {
            idx++;

            try {
                string pathToMaterial = AssetDatabase.GetAssetPath(material);

                if (pathToMaterial == "" || pathToMaterial.Contains("Quest")) {
                    Debug.Log("Material is empty or already Quest, skipping...");
                    continue;
                }
                
                Debug.Log("Switching material " + pathToMaterial + "...");

                string pathToQuestMaterial = pathToMaterial.Replace(".mat", " Quest.mat");

                Material questMaterial = LooselyGetMaterialAtPath(pathToQuestMaterial);

                if (questMaterial == null) {
                    string pathToQuestMaterialParent = Utils.GetDirectoryPathRelativeToAssets(pathToMaterial);

                    string pathToQuestMaterialInQuestFolder = Path.Combine(pathToQuestMaterialParent, "Quest", Path.GetFileName(pathToMaterial).Replace(".mat", " Quest.mat"));

                    Debug.Log("Looking for a quest folder version: " + pathToQuestMaterialInQuestFolder);

                    Material questMaterialInFolder = LooselyGetMaterialAtPath(pathToQuestMaterialInQuestFolder);

                    if (questMaterialInFolder == null) {
                        if (autoCreateQuestMaterials) {
                            if (isDryRun == false) {
                                questMaterialInFolder = CreateMissingQuestMaterialForRenderer(pathToMaterial, material);
                            }
                        } else {
                            throw new MaterialNotFoundException() {
                                pathToMaterial = pathToMaterial
                            };
                        }
                    }

                    pathToQuestMaterial = pathToQuestMaterialInQuestFolder;
                }

                actionsToPerform.Add(new SwitchToMaterialAction() {
                    pathToRenderer = relativePathToRenderer,
                    pathToMaterial = pathToQuestMaterial,
                    materialIndex = idx,
                });
            } catch (System.Exception exception) {
                errors.Add(exception);
                throw exception;
            }
        }
    }

    Material CreateMissingQuestMaterialForRenderer(string originalMaterialPath, Material originalMaterial) {
        Debug.Log("Creating missing Quest material for renderer for material: " + originalMaterialPath);

        string pathToMaterialTemplate = "Assets/PeanutTools/VRC_Questifyer/Materials/QuestTemplate" + (useToonShader ? "Toon" : "Standard") + ".mat";
        Material materialTemplate = GetMaterialAtPath(pathToMaterialTemplate);

        string pathToParentDir = Utils.GetDirectoryPathRelativeToAssets(originalMaterialPath);

        string pathToNewParentDir = pathToParentDir + "/" + (placeQuestMaterialsInOwnDirectory ? "Quest/" : "");

        string pathToDest = pathToNewParentDir + Path.GetFileName(originalMaterialPath).Replace(".mat", " Quest.mat");

        if (placeQuestMaterialsInOwnDirectory) {
            bool exists = Directory.Exists(pathToNewParentDir);

            if(!exists) {
                Directory.CreateDirectory(pathToNewParentDir);
            }
        }

        bool result = AssetDatabase.CopyAsset(pathToMaterialTemplate, pathToDest);

        if (result == false) {
            throw new System.Exception("Failed to copy Quest material template!");
        }

        Material createdMaterial = GetMaterialAtPath(pathToDest);

        try {
            createdMaterial.CopyPropertiesFromMaterial(originalMaterial);
        } catch (System.Exception err) {
            // if props don't exist then it throws errors
            // ignore them
        }

        if (originalMaterial.GetTexture("_EmissionMap") != null) {
            // note this does not check the checkbox in the UI
            createdMaterial.SetInt("_EnableEmission", 1);
        }

        return createdMaterial;
    }

    void RemoveGameObjectForAvatar(GameObject avatar, string pathToGameObject) {
        Debug.Log("Removing game object " + pathToGameObject + "...");

        Transform foundTransform = avatar.transform.Find(pathToGameObject);

        if (foundTransform == null) {
            if (isRunningOnExistingQuestAvatar) {
                return;
            } else {
                throw new GameObjectNotFoundException() {
                    pathToGameObject = pathToGameObject
                };
            }
        }

        DestroyImmediate(foundTransform.gameObject);
    }
}
