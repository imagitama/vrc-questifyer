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
using VRC.SDK3.Dynamics.PhysBone.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class VRC_Questifyer : EditorWindow
{
    enum Types {
        SwitchToMaterial,
        RemoveGameObject,
        RemovePhysBone
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
    Vector2 scrollPosition;

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

    void HorizontalRule() {
       Rect rect = EditorGUILayout.GetControlRect(false, 1);
       rect.height = 1;
       EditorGUI.DrawRect(rect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
    }

    void OnGUI()
    {
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUIStyle italicStyle = new GUIStyle(GUI.skin.label);
        italicStyle.fontStyle = FontStyle.Italic;

        GUILayout.Label("VRC Questifyer", EditorStyles.boldLabel);
        GUILayout.Label("Automatically make your avatar Quest compatible", italicStyle);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        HorizontalRule();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Step 1: Select your avatar", EditorStyles.boldLabel);

        sourceVrcAvatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", sourceVrcAvatarDescriptor, typeof(VRCAvatarDescriptor));
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        HorizontalRule();

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        GUILayout.Label("Step 2: Configure settings", EditorStyles.boldLabel);

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

        HorizontalRule();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Step 3: Configure manual actions", EditorStyles.boldLabel);
        GUILayout.Label("Any additional actions you usually do by hand", italicStyle);
        
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

        HorizontalRule();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Step 4: Questify", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(sourceVrcAvatarDescriptor == null);

        if (GUILayout.Button("Dry Run", GUILayout.Width(100), GUILayout.Height(50)))
        {
            DryRun();
        }

        GUILayout.Label("Creates the avatar but doesn't perform any actions", italicStyle);
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Questify!", GUILayout.Width(100), GUILayout.Height(50)))
        {
            Questify();
        }

        EditorGUI.EndDisabledGroup();

        RenderErrors();

        RenderActionsToPerform();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Download new versions and get support: https://discord.gg/R6Scz6ccdn");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("https://twitter.com/@HiPeanutBuddha");
        GUILayout.Label("Peanut#1756");

        isDryRun = false;

        EditorGUILayout.EndScrollView();
    }

    void RenderErrors() {
        if (errors.Count == 0) {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Errors:");

        GUIStyle guiStyle = new GUIStyle() {
            // fontSize = 10
        };
        guiStyle.normal.textColor = Color.red;

        foreach (System.Exception exception in errors) {
            string message = "";

            if (exception is FailedToSwitchMaterialException) {
                message = message + "Failed to switch material: " + exception.Message + "\nMaterial: " + (exception as FailedToSwitchMaterialException).pathToMaterial;
            } else if (exception is FailedToRemoveGameObjectException) {
                message = message + "Failed to remove game object: " + exception.Message + "\nGame object: " + (exception as FailedToRemoveGameObjectException).pathToGameObject;
            } else if (exception is FailedToRemovePhysBoneException) {
                message = message + "Failed to remove PhysBone: " + exception.Message + "\nPhysBone game object: " + (exception as FailedToRemovePhysBoneException).pathToGameObject + " (index " + (exception as FailedToRemovePhysBoneException).physBoneIndex + ")";
            }

            GUILayout.Label(message, guiStyle);
        }
    }

    void RenderActionsToPerform() {
        if (actionsToPerform.Count == 0) {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        HorizontalRule();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Final actions:");

        foreach (Action action in actionsToPerform) {
            RenderAction(action);
        }
    }

    void DryRun() {
        // disabled at end of GUI
        isDryRun = true;

        Questify();
    }

    void SelectFileInProjectWindow(string pathToFile) {
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(pathToFile);
    }

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
        string[] typesAsStrings = System.Enum.GetNames(typeof(Types));
        string[] dropdownOptions = typesAsStrings.Prepend("Select an action:").ToArray();

        selectedTypeDropdownIdx = EditorGUILayout.Popup("Type", selectedTypeDropdownIdx, dropdownOptions);

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

                case Types.RemovePhysBone:
                    GUILayout.Label("Path to PhysBone game object:");
                    createFormFieldValue1 = EditorGUILayout.TextField(createFormFieldValue1);

                    gameObjectToUse = (GameObject)EditorGUILayout.ObjectField("Search:", gameObjectToUse, typeof(GameObject));

                    if (gameObjectToUse != null) { 
                        createFormFieldValue1 = Utils.GetRelativeGameObjectPath(gameObjectToUse, sourceVrcAvatarDescriptor.gameObject);
                    }

                    createFormFieldValue3 = EditorGUILayout.IntField("Component index (default 0):", createFormFieldValue3);

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

            case Types.RemovePhysBone:
                action = new RemovePhysBoneAction() {
                    pathToGameObject = fieldValue1,
                    physBoneIndex = fieldValue3
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
        } else if (action is RemovePhysBoneAction) {
            label = "Remove PhysBone";
        } else {
            throw new System.Exception("Unknown action!");
        }

        GUILayout.Label(label + (action.performAtEnd ? " (end)" : ""));
    }

    void RenderDataForAction(Action action) {
        GUIStyle guiStyle = new GUIStyle() {
            fontSize = 10
        };
        guiStyle.normal.textColor = Color.white;

        if (action is SwitchToMaterialAction) {
            string pathToRenderer = (action as SwitchToMaterialAction).pathToRenderer;
            GUILayout.Label(pathToRenderer, guiStyle);

            string pathToMaterial = (action as SwitchToMaterialAction).pathToMaterial;
            string materialIndexStr = (action as SwitchToMaterialAction).materialIndex.ToString();
            GUILayout.Label(pathToMaterial + " (" + materialIndexStr + ")", guiStyle);
            
            if (GUILayout.Button("Show", GUILayout.Width(50), GUILayout.Height(15))) {
                SelectFileInProjectWindow(pathToMaterial);
            }
        } else if (action is RemoveGameObjectAction) {
            GUILayout.Label((action as RemoveGameObjectAction).pathToGameObject, guiStyle);
        } else if (action is RemovePhysBoneAction) {
            string pathToGameObject = (action as RemovePhysBoneAction).pathToGameObject;
            string physBoneIndexStr = (action as RemovePhysBoneAction).physBoneIndex.ToString();
            GUILayout.Label(pathToGameObject + " (" + physBoneIndexStr + ")", guiStyle);
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
        ClearErrors();

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
            } catch (FailedToRemoveGameObjectException exception) {
                errors.Add(exception);
            }
        } else if (action is RemovePhysBoneAction) {
            try {
                string pathToGameObject = (action as RemovePhysBoneAction).pathToGameObject;
                int physBoneIndex = (action as RemovePhysBoneAction).physBoneIndex;
                RemovePhysBoneForAvatar(avatar, pathToGameObject, physBoneIndex);
            } catch (FailedToRemovePhysBoneException exception) {
                errors.Add(exception);
            }
        } else {
            throw new System.Exception("Cannot perform action - unknown action type: " + nameof(action));
        }
    }

    void RemovePhysBoneForAvatar(GameObject avatar, string pathToGameObject, int physBoneIndex = 0) {
        Debug.Log("Removing physbones at " + pathToGameObject + " (" + physBoneIndex.ToString() + ")...");

        if (physBoneIndex < 0) {
            throw new FailedToRemovePhysBoneException("Index is less than 0") {
                pathToGameObject = pathToGameObject,
                physBoneIndex = physBoneIndex
            };
        } 

        Transform gameObjectTransform = Utils.FindChild(avatar.transform, pathToGameObject);

        if (gameObjectTransform == null) {
            throw new FailedToRemovePhysBoneException("Game object not found") {
                pathToGameObject = pathToGameObject,
                physBoneIndex = physBoneIndex
            };
        }

        VRCPhysBone[] physBones = gameObjectTransform.gameObject.GetComponents<VRCPhysBone>();

        if (physBones.Length == 0) {
            throw new FailedToRemovePhysBoneException("No PhysBones found on the game object") {
                pathToGameObject = pathToGameObject,
                physBoneIndex = physBoneIndex
            };
        }

        if (physBones.Length - 1 < physBoneIndex) {
            throw new FailedToRemovePhysBoneException("Index is too big") {
                pathToGameObject = pathToGameObject,
                physBoneIndex = physBoneIndex
            };
        }

        VRCPhysBone physBoneToRemove = physBones[physBoneIndex];

        DestroyImmediate(physBoneToRemove);
    }

    void SwitchGameObjectMaterialForAvatar(GameObject avatar, string pathToRenderer, string pathToMaterial, int materialIndex = 0) {
        Transform rendererTransform = Utils.FindChild(avatar.transform, pathToRenderer);

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

        ActionsJson actionsData = JsonConvert.DeserializeObject<ActionsJson>(json);

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

        string json = JsonConvert.SerializeObject(actionsForJson, Newtonsoft.Json.Formatting.Indented);

        File.WriteAllText(pathToJsonFile, json);

        ClearErrors();
    }

    void ClearErrors() {
        errors = new List<System.Exception>();
    }

    // TODO: Add as methods to Action class
    ActionJson ActionToJson(Action action) {
        ActionJson actionJson;

        if (action is SwitchToMaterialAction) {
            actionJson = new ActionJson() {
                type = Types.SwitchToMaterial.ToString(),
                data = JObject.FromObject(new {
                    pathToRenderer = (action as SwitchToMaterialAction).pathToRenderer,
                    pathToMaterial = (action as SwitchToMaterialAction).pathToMaterial,
                    materialIndex = (action as SwitchToMaterialAction).materialIndex.ToString()
                })
            };
        } else if (action is RemoveGameObjectAction) {
            actionJson = new ActionJson() {
                type = Types.RemoveGameObject.ToString(),
                data = JObject.FromObject(new {
                    pathToGameObject = (action as RemoveGameObjectAction).pathToGameObject
                })
            };
        } else if (action is RemovePhysBoneAction) {
            actionJson = new ActionJson() {
                type = Types.RemovePhysBone.ToString(),
                data = JObject.FromObject(new {
                    pathToGameObject = (action as RemovePhysBoneAction).pathToGameObject,
                    physBoneIndex = (action as RemovePhysBoneAction).physBoneIndex.ToString()
                })
            };
        } else {
            throw new System.Exception("Cannot convert action to JSON: unknown type " + nameof(action));
        }

        actionJson.performAtEnd = action.performAtEnd;

        return actionJson;
    }

    Action ActionJsonToAction(ActionJson actionJson) {
        Action action;

        Types type = (Types)Types.Parse(typeof(Types), actionJson.type);
        JObject jsonObject = actionJson.data;

        switch (type) {
            case Types.SwitchToMaterial:
                action = new SwitchToMaterialAction() {
                    pathToRenderer = (string)jsonObject["pathToRenderer"],
                    pathToMaterial = (string)jsonObject["pathToMaterial"],
                    materialIndex = (int)jsonObject["materialIndex"]
                };
                break;
            case Types.RemoveGameObject:
                action = new RemoveGameObjectAction() {
                    pathToGameObject = (string)jsonObject["pathToGameObject"]
                };
                break;
            case Types.RemovePhysBone:
                action = new RemovePhysBoneAction() {
                    pathToGameObject = (string)jsonObject["pathToGameObject"],
                    physBoneIndex = (int)jsonObject["physBoneIndex"]
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
                            throw new FailedToSwitchMaterialException("Quest material not found (auto-create disabled)") {
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

        Transform foundTransform = Utils.FindChild(avatar.transform, pathToGameObject);

        if (foundTransform == null) {
            if (isRunningOnExistingQuestAvatar) {
                return;
            } else {
                throw new FailedToRemoveGameObjectException("Game object not found") {
                    pathToGameObject = pathToGameObject
                };
            }
        }

        DestroyImmediate(foundTransform.gameObject);
    }
}
