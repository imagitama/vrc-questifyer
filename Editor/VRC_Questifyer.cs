using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

using UnityEditor;
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
using VRC.SDK3.Dynamics.PhysBone.Components;

using PeanutTools_VRC_Questifyer;

public class VRC_Questifyer : EditorWindow
{
    static GitHub_Update_Checker githubUpdateChecker;

    SuccessStates successState;

    enum SuccessStates {
        Unknown,
        Success,
        Failed
    }

    enum Types {
        SwitchToMaterial,
        RemoveGameObject,
        RemovePhysBone,
        RemoveAllPhysBones
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
    bool isImportedAssetsShown = false;

    // physbones
    bool isPhysBonesVisible = false;
    bool[] isToDeleteEachPhysBone = new bool[0];
    
    // build
    bool isBuilderShown = false;
    long buildFileSize;
    List<AssetInsideBundle> thingsInsideBundle = new List<AssetInsideBundle>();

    // user settings
    bool autoCreateQuestMaterials = false;
    bool placeQuestMaterialsInOwnDirectory = true;
    bool useToonShader = false;

    [MenuItem("PeanutTools/VRC Questifyer")]
    public static void ShowWindow()
    {
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

        CustomGUI.BoldLabel("VRC Questifyer");
        CustomGUI.ItalicLabel("Automatically make your avatar Quest compatible");

        CustomGUI.LineGap();

        CustomGUI.HorizontalRule();
        
        CustomGUI.LineGap();

        CustomGUI.BoldLabel("Step 1: Select your avatar");
        
        CustomGUI.SmallLineGap();

        sourceVrcAvatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", sourceVrcAvatarDescriptor, typeof(VRCAvatarDescriptor));
        
        CustomGUI.LineGap();

        CustomGUI.HorizontalRule();

        CustomGUI.LineGap();
        
        CustomGUI.BoldLabel("Step 2: Configure settings");

        CustomGUI.SmallLineGap();
        
        float originalValue = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 500;

        autoCreateQuestMaterials = EditorGUILayout.Toggle("Create missing Quest materials", autoCreateQuestMaterials);

        CustomGUI.SmallLineGap();

        placeQuestMaterialsInOwnDirectory = EditorGUILayout.Toggle("Place materials inside \"Quest\" folder (recommended)", placeQuestMaterialsInOwnDirectory);

        CustomGUI.SmallLineGap();

        useToonShader = EditorGUILayout.Toggle("Use toon shader", useToonShader);

        EditorGUIUtility.labelWidth = originalValue;

        CustomGUI.LineGap();

        CustomGUI.HorizontalRule();

        CustomGUI.LineGap();

        if (isCreateFormVisible) {
            RenderCreateActionForm();
            
            CustomGUI.SmallLineGap();

            if (CustomGUI.StandardButton("Cancel")) {
                isCreateFormVisible = false;
            }
        } else {
            if (CustomGUI.StandardButton("Add Action")) {
                isCreateFormVisible = true;
            }
        }

        CustomGUI.LineGap();

        CustomGUI.HorizontalRule();

        CustomGUI.LineGap();

        CustomGUI.BoldLabel("Step 3: Remove PhysBones");
        CustomGUI.ItalicLabel("Delete PhysBones to reach the limit (8)");

        CustomGUI.LineGap();
        
        VRCPhysBone[] physBones = sourceVrcAvatarDescriptor != null ? GetPhysBonesInTransform(sourceVrcAvatarDescriptor.transform) : new VRCPhysBone[0];

        PopulatePhysBones(physBones);

        int remainingPhysBonesCount = GetRemainingNumberOfPhysBones();
        
        if (physBones.Length > 0) {
            if (remainingPhysBonesCount > 8) {
                CustomGUI.RenderErrorMessage("Number of PhysBones (" + remainingPhysBonesCount + ") is greater than the limit (8)");
            } else {
                CustomGUI.RenderSuccessMessage("Number of PhysBones (" + remainingPhysBonesCount + ") is within the limit (8)");
            }

            CustomGUI.LineGap();
        }

        EditorGUI.BeginDisabledGroup(sourceVrcAvatarDescriptor == null);

        if (CustomGUI.ToggleButton("PhysBones", isPhysBonesVisible)) {
            isPhysBonesVisible = !isPhysBonesVisible;
        }

        if (isPhysBonesVisible) {
            CustomGUI.LineGap();

            RenderPhysBonesEditor(physBones);
        }

        EditorGUI.EndDisabledGroup();

        CustomGUI.LineGap();
        
        CustomGUI.HorizontalRule();
        
        CustomGUI.LineGap();

        CustomGUI.BoldLabel("Step 4: Questify");
        
        CustomGUI.LineGap();

        EditorGUI.BeginDisabledGroup(sourceVrcAvatarDescriptor == null);

        if (CustomGUI.PrimaryButton("Dry Run")) {
            DryRun();
        }

        CustomGUI.ItalicLabel("Preview the actions the tool will perform");
        
        CustomGUI.LineGap();

        if (CustomGUI.PrimaryButton("Questify!")) {
            Questify();
        }

        EditorGUI.EndDisabledGroup();

        if (successState == SuccessStates.Success) {
            CustomGUI.LineGap();
            CustomGUI.RenderSuccessMessage("Avatar has been questified successfully");
        }

        RenderErrors();

        RenderActionsToPerform();
        
        CustomGUI.LineGap();
        
        CustomGUI.HorizontalRule();

        CustomGUI.LineGap();

        EditorGUI.BeginDisabledGroup(sourceVrcAvatarDescriptor == null);
        if (CustomGUI.ToggleButton("Inspect Textures", isImportedAssetsShown))
        {
            isImportedAssetsShown = !isImportedAssetsShown;
        }
        EditorGUI.EndDisabledGroup();

        if (isImportedAssetsShown && sourceVrcAvatarDescriptor != null) {
            CustomGUI.LineGap();  

            RenderImportedAssets();
        }

        CustomGUI.LineGap();

        CustomGUI.HorizontalRule();

        CustomGUI.LineGap();  

        EditorGUI.BeginDisabledGroup(sourceVrcAvatarDescriptor == null);

        if (CustomGUI.ToggleButton("Inspect Avatar", isBuilderShown))
        {
            isBuilderShown = !isBuilderShown;
        }

        EditorGUI.EndDisabledGroup();

        if (isBuilderShown && sourceVrcAvatarDescriptor != null) {
            CustomGUI.LineGap();  

            RenderBuilder();
        }

        CustomGUI.LineGap();
        
        CustomGUI.HorizontalRule();

        CustomGUI.LineGap();
        
        CustomGUI.MyLinks("vrc-questifyer");
        
        CustomGUI.LineGap();
        
        CustomGUI.HorizontalRule();

        CustomGUI.LineGap();

        if (githubUpdateChecker != null) {
            githubUpdateChecker.Render();
            
            CustomGUI.LineGap();
        }

        isDryRun = false;

        EditorGUILayout.EndScrollView();
    }

    int GetRemainingNumberOfPhysBones() {
        return isToDeleteEachPhysBone.ToList().Where(x => x == false).ToArray().Length;
    }

    VRCPhysBone[] GetPhysBonesInTransform(Transform root) {
        return root.gameObject.GetComponentsInChildren<VRCPhysBone>(true);
    }

    void PopulatePhysBones(VRCPhysBone[] physBones) {
        if (isToDeleteEachPhysBone.Length != physBones.Length) {
            isToDeleteEachPhysBone = new bool[physBones.Length];

            for (int i = 0; i < physBones.Length; i++) {
                isToDeleteEachPhysBone[i] = false;
            }
        }
    }

    void RenderPhysBonesEditor(VRCPhysBone[] physBones) {
        if (sourceVrcAvatarDescriptor == null) {
            return;
        }

        PopulatePhysBones(physBones);

        int idx = 0;
        Dictionary<Transform, int> componentIdxPerTransform = new Dictionary<Transform, int>();

        foreach (VRCPhysBone physBone in physBones) {
            int componentIndex;

            if (componentIdxPerTransform.ContainsKey(physBone.transform)) {
                componentIdxPerTransform[physBone.transform]++;
                componentIndex = componentIdxPerTransform[physBone.transform];
            } else {
                componentIdxPerTransform[physBone.transform] = 0;
                componentIndex = 0;
            }

            Transform physBoneRoot = physBone.rootTransform ? physBone.rootTransform : physBone.transform;
            string pathToTransform = Utils.GetGameObjectPath(physBone.transform.gameObject);
            string pathToPhysBoneRoot = Utils.GetGameObjectPath(physBoneRoot.gameObject);

            string nameOfRootTransform = physBoneRoot.gameObject.name;
            
            GUIStyle guiStyle = new GUIStyle(GUI.skin.label);

            if (isToDeleteEachPhysBone[idx] == true) {
                guiStyle = new GUIStyle(GUI.skin.label);
                guiStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            }

            EditorGUILayout.BeginHorizontal();

            // for some reason this does not immediately refresh the UI
            if (GUILayout.Button(nameOfRootTransform, guiStyle, GUILayout.ExpandWidth(false))) {
                isToDeleteEachPhysBone[idx] = !isToDeleteEachPhysBone[idx];
            }

            isToDeleteEachPhysBone[idx] = !EditorGUILayout.Toggle("", !isToDeleteEachPhysBone[idx], GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();

            guiStyle.fontSize = 10;
            
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(GetShortenedPathToTransform(pathToTransform) + " (" + componentIndex + ")", guiStyle, GUILayout.ExpandWidth(false));

            if (CustomGUI.TinyButton("View")) {
                SelectPhysBoneComponent(physBone);
            }
            
            EditorGUILayout.EndHorizontal();

            if (pathToTransform != pathToPhysBoneRoot) {
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(" => " + GetShortenedPathToTransform(pathToPhysBoneRoot), guiStyle, GUILayout.ExpandWidth(false));
               
                if (CustomGUI.TinyButton("View")) {
                    SelectTransform(physBoneRoot);
                }
                
                EditorGUILayout.EndHorizontal();
            }

            CustomGUI.SmallLineGap();
            
            idx++;
        }
    }

    string GetShortenedPathToTransform(string pathToTransform) {
        string avatarName = sourceVrcAvatarDescriptor.gameObject.name;

        if (pathToTransform == "/" + avatarName) {
            return "(root)";
        }

        return pathToTransform.Replace(avatarName, "").Substring(1);
    }

    void RenderBuilder() {
        CustomGUI.RenderWarningMessage("This tool uses the same build process as VRChat however the sizes are an estimate ONLY");

        CustomGUI.SmallLineGap();

        if (CustomGUI.StandardButton("Build Avatar")) {
            BuildAvatarAndInspect();
        }

        if (CustomGUI.StandardButton("Inspect Last Build")) {
            InspectLastBuild();
        }

        RenderLastBuild();
    }

    public class AssetInsideBundle {
        public List<string> pathsInHierarchy;
        public string pathToAsset;
        public long fileSizeB;
    }

    void RenderLastBuild() {
        if (buildFileSize == null) {
            return;
        }

        CustomGUI.LineGap();
        
        GUILayout.Label("Total avatar size: " + FormatBytes(buildFileSize) + " (estimate)");
        
        CustomGUI.LineGap();

        GUILayout.Label("Detected meshes:");

        if (thingsInsideBundle.Count == 0) {
            CustomGUI.LineGap();    
    
            CustomGUI.ItalicLabel("(none)");
        }

        long totalSize = 0;

        foreach (AssetInsideBundle assetInsideBundle in thingsInsideBundle) {
            CustomGUI.LineGap();

            RenderAssetInsideBundle(assetInsideBundle);

            totalSize += assetInsideBundle.fileSizeB;
        }

        CustomGUI.LineGap();

        GUILayout.Label("Total asset size: " + FormatBytes(totalSize) + " (estimate)");
    }

    void BuildAvatar() {
        Debug.Log("Building avatar...");

        string pathToDirInsideAssets = "VRC_Questifyer_Temp";

        Directory.CreateDirectory(Application.dataPath + "/" + pathToDirInsideAssets);

        string pathToPrefabInsideAssets = "Assets/" + pathToDirInsideAssets + "/avatar.prefab";

        PrefabUtility.SaveAsPrefabAsset(sourceVrcAvatarDescriptor.gameObject, pathToPrefabInsideAssets);

        AssetImporter prefabAssetImporter = AssetImporter.GetAtPath(pathToPrefabInsideAssets);
        prefabAssetImporter.SetAssetBundleNameAndVariant("vrc_questifyer", "");

        AssetBundleBuild assetBundleBuild = new AssetBundleBuild() {
            assetNames = new string[1] { pathToPrefabInsideAssets },
            assetBundleName = "vrc_questifyer"
        };

        string assetBundleOutputPath = Application.dataPath + "/" + pathToDirInsideAssets;

        UnityEditor.BuildPipeline.BuildAssetBundles(assetBundleOutputPath, new AssetBundleBuild[1]
        {
            assetBundleBuild
        }, (BuildAssetBundleOptions) 0, EditorUserBuildSettings.activeBuildTarget);

        Debug.Log("Avatar has been built");
    }

    void BuildAvatarAndInspect() {
        BuildAvatar();
        InspectLastBuild();
    }

    List<AssetInsideBundle> GetMeshAssetsInsideBundle(GameObject rootGameObject) {
        // TODO: Try to create assetbundle for each mesh to get their real filesize

        List<AssetInsideBundle> things = new List<AssetInsideBundle>();

        MeshFilter[] meshFilters = (MeshFilter[])rootGameObject.GetComponentsInChildren<MeshFilter>(true);

        for (int i = 0; i < meshFilters.Length; i++) {
            MeshFilter meshFilter = meshFilters[i];
            Mesh mesh = meshFilter.sharedMesh;

            string pathInHierarchy = Utils.GetGameObjectPath(meshFilter.gameObject);

            string pathToAsset = AssetDatabase.GetAssetPath(mesh);
            
            int existingIndex = things.FindIndex(thing => thing.pathToAsset == pathToAsset);

            if (existingIndex > -1) {
                things[existingIndex].pathsInHierarchy.Add(pathInHierarchy);
            } else {
                things.Add(new AssetInsideBundle() {
                    pathsInHierarchy = new List<string>() { pathInHierarchy },
                    pathToAsset = pathToAsset,
                    fileSizeB = GetFileSizeOfAsset(mesh)
                });
            }
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = (SkinnedMeshRenderer[])rootGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        for (int i = 0; i < skinnedMeshRenderers.Length; i++) {
            SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[i];
            Mesh mesh = skinnedMeshRenderer.sharedMesh;
            
            string pathInHierarchy = Utils.GetGameObjectPath(skinnedMeshRenderer.gameObject);

            string pathToAsset = AssetDatabase.GetAssetPath(mesh);

            int existingIndex = things.FindIndex(thing => thing.pathToAsset == pathToAsset);

            if (existingIndex > -1) {
                things[existingIndex].pathsInHierarchy.Add(pathInHierarchy);
            } else {
                things.Add(new AssetInsideBundle() {
                    pathsInHierarchy = new List<string>() { pathInHierarchy },
                    pathToAsset = pathToAsset,
                    fileSizeB = GetFileSizeOfAsset(mesh)
                });
            }
        }

        return things;
    }

    void InspectLastBuild() {
        Debug.Log("Inspecting last build...");

        string pathToDirInsideAssets = "VRC_Questifyer_Temp";
        string assetBundleName = "vrc_questifyer";

        string pathToAssetBundle = Application.dataPath + "/" + pathToDirInsideAssets + "/" + assetBundleName;

        buildFileSize = GetFileSize(pathToAssetBundle);

        thingsInsideBundle = new List<AssetInsideBundle>();
        thingsInsideBundle.AddRange(GetMeshAssetsInsideBundle(sourceVrcAvatarDescriptor.gameObject));

        thingsInsideBundle.Sort((itemA, itemB) => (int)itemB.fileSizeB - (int)itemA.fileSizeB);
    }

    void RenderAssetInsideBundle(AssetInsideBundle assetInsideBundle) {

        CustomGUI.BoldLabel(assetInsideBundle.pathToAsset);
        GUILayout.Label(System.String.Join("\n", assetInsideBundle.pathsInHierarchy));
        GUILayout.Label(FormatBytes(assetInsideBundle.fileSizeB) + " (estimate)");
    }

    long GetFileSizeOfAsset(Object thing) {
        string absolutePath = Application.dataPath.Replace("Assets", "") + AssetDatabase.GetAssetPath(thing);
        return GetFileSize(absolutePath);
    }

    long GetFileSize(string pathToFile) {
        // in some instances the path is wacko
        try {
            FileInfo fileInfo = new FileInfo(pathToFile);
            return fileInfo.Length;
        } catch (System.Exception err) {
            Debug.Log(err);
            return 0;
        }
    }

    public class ImportedAsset {
        public string propertyName;
        public string pathInAssets;
        public List<string> pathsInHierarchy;
        public Transform transform;
        public Material material;
        public Texture texture;
        public TextureImporter textureImporter;
        public long fileSizeB;
    }

    bool GetIsImportedAssetADuplicate(List<ImportedAsset> existingImportedAssets, string pathToAsset) {
        return existingImportedAssets.Exists(importedAsset => importedAsset.pathInAssets == pathToAsset);
    }

    void RenderImportedAssets() {
        Renderer[] renderers = sourceVrcAvatarDescriptor.gameObject.GetComponentsInChildren<Renderer>(true);
        List<ImportedAsset> importedAssets = new List<ImportedAsset>();

        foreach (Renderer renderer in renderers) {
            string pathInHierarchy = Utils.GetGameObjectPath(renderer.gameObject);

            foreach (Material material in renderer.sharedMaterials) {
                Shader shader = material.shader;

                for(int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++) {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv) {
                        string propertyName = ShaderUtil.GetPropertyName(shader, i);

                        Texture texture = material.GetTexture(propertyName);

                        if (texture == null) {
                            continue;
                        }

                        string pathToAsset = AssetDatabase.GetAssetPath(texture);
        
                        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(pathToAsset);

                        long fileSizeB = EditorTextureUtil.GetStorageMemorySize(texture);
                       
                        int existingIndex = importedAssets.FindIndex(thing => thing.pathInAssets == pathToAsset);

                        if (existingIndex > -1) {
                            importedAssets[existingIndex].pathsInHierarchy.Add(pathInHierarchy);
                        } else {
                            importedAssets.Add(new ImportedAsset() {
                                propertyName = propertyName,
                                pathInAssets = pathToAsset,
                                pathsInHierarchy = new List<string>() { pathInHierarchy },
                                transform = renderer.transform,
                                material = material,
                                texture = texture,
                                textureImporter = importer,
                                fileSizeB = fileSizeB
                            });
                        }
                    }
                }
            }
        }
        
        importedAssets.Sort((itemA, itemB) => (int)itemB.fileSizeB - (int)itemA.fileSizeB);

        long totalSize = 0;

        foreach (ImportedAsset importedAsset in importedAssets) {
            RenderImportedAsset(importedAsset);

            totalSize += importedAsset.fileSizeB;
        }

        EditorGUILayout.Space();

        GUILayout.Label("Total size: " + FormatBytes(totalSize) + " (estimate)");
    }

    // source: https://github.com/Unity-Technologies/UnityCsReference/blob/4d031e55aeeb51d36bd94c7f20182978d77807e4/Modules/QuickSearch/Editor/Utilities/Utils.cs#L347
    public static string FormatBytes(long byteCount)
    {
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        if (byteCount == 0)
            return "0" + suf[0];
        long bytes = System.Math.Abs(byteCount);
        int place = System.Convert.ToInt32(System.Math.Floor(System.Math.Log(bytes, 1024)));
        double num = System.Math.Round(bytes / System.Math.Pow(1024, place), 1);
        return $"{System.Math.Sign(byteCount) * num} {suf[place]}";
    }

    string GetLabelForTextureName(string name) {
        switch (name) {
            case "_MainTex":
                return "Albedo";
            case "_BumpMap":
                return "Normal";
            case "_EmissionMap":
                return "Emission";
            case "_MetallicGlossMap":
                return "Metallic/Gloss";
            default:
                return name;
        }
    }

    string GetLabelForTextureAsset(string textureName, string pathToImage) {
        string textureLabel = GetLabelForTextureName(textureName);
        string fileName = Path.GetFileName(pathToImage);

        return fileName + " (" + textureLabel + ")";
    }

    int GetBitsPerPixel(TextureImporterFormat textureFormat) {
        Debug.Log(textureFormat);
        switch (textureFormat) {
            case TextureImporterFormat.ETC2_RGBA8:
                return 8;
            case TextureImporterFormat.ETC2_RGB4:
            case TextureImporterFormat.ETC_RGB4:
                return 4;
            default:
                return 0;
        }
    }

    void RenderImportedAsset(ImportedAsset importedAsset) {
        EditorGUILayout.Space();

        int maxTextureSize;
        TextureImporterFormat textureFormat;
        int compressionQuality;

        importedAsset.textureImporter.GetPlatformTextureSettings("Android", out maxTextureSize, out textureFormat, out compressionQuality);

        EditorGUILayout.BeginHorizontal();
        CustomGUI.BoldLabel(GetLabelForTextureAsset(GetLabelForTextureName(importedAsset.propertyName), importedAsset.pathInAssets), GUILayout.ExpandWidth(false));

        if (CustomGUI.TinyButton("View")) {
            FocusFileInProjectWindow(importedAsset.pathInAssets);
        }
        if (CustomGUI.TinyButton("Mat")) {
            FocusAssetInProjectWindow(importedAsset.material);
        }
        EditorGUILayout.EndHorizontal();

        var style = new GUIStyle(GUI.skin.label) {
            fontSize = 10
        };
        style.normal.textColor = Color.white;

        foreach (var pathInHierarchy in importedAsset.pathsInHierarchy) {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(GetShortenedPathToTransform(pathInHierarchy), style, GUILayout.ExpandWidth(false));
            if (CustomGUI.TinyButton("View")) {
                FocusGameObjectByPath(pathInHierarchy);
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();

        GUILayout.Label(maxTextureSize.ToString() + ", " + compressionQuality.ToString() + "%, " + FormatBytes(importedAsset.fileSizeB));

        GUILayout.EndHorizontal();
    }

    void RenderLink(string label, string url) {
        Rect rect = EditorGUILayout.GetControlRect();

        if (rect.Contains(Event.current.mousePosition)) {
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (Event.current.type == EventType.MouseUp) {
                Help.BrowseURL(url);
            }
        }

        GUIStyle style = new GUIStyle();
        style.normal.textColor = new Color(0.5f, 0.5f, 1);

        GUI.Label(rect, label, style);
    }

    void RenderErrors() {
        if (errors.Count == 0) {
            return;
        }

        CustomGUI.LineGap();

        GUILayout.Label("Errors:");

        foreach (System.Exception exception in errors) {
            string message = "";

            if (exception is FailedToSwitchMaterialException) {
                message = message + "Failed to switch material: " + exception.Message + "\nMaterial: " + (exception as FailedToSwitchMaterialException).pathToMaterial;
            } else if (exception is FailedToRemoveGameObjectException) {
                message = message + "Failed to remove game object: " + exception.Message + "\nGame object: " + (exception as FailedToRemoveGameObjectException).pathToGameObject;
            } else if (exception is FailedToRemovePhysBoneException) {
                message = message + "Failed to remove PhysBone: " + exception.Message + "\nPhysBone game object: " + (exception as FailedToRemovePhysBoneException).pathToGameObject + " (index " + (exception as FailedToRemovePhysBoneException).physBoneIndex + ")";
            } else if (exception is FailedToRemoveAllPhysBonesException) {
                message = message + "Failed to remove all PhysBones: " + exception.Message + "\nGame object: " + (exception as FailedToRemovePhysBoneException).pathToGameObject;
            }

            CustomGUI.RenderErrorMessage(message);
        }
    }

    void RenderActionsToPerform() {
        if (actionsToPerform.Count == 0) {
            return;
        }

        CustomGUI.LineGap();

        CustomGUI.HorizontalRule();
        
        CustomGUI.LineGap();

        GUILayout.Label("Final actions:");

        foreach (Action action in actionsToPerform) {
            RenderAction(action);
            CustomGUI.SmallLineGap();
        }

        Material[] materials = GetMaterialsInActionsToPerform();

        EditorGUI.BeginDisabledGroup(materials.Length == 0);

        CustomGUI.SmallLineGap();

        if (CustomGUI.StandardButton("Select All Materials")) {
            SelectMaterials(materials);
        }

        EditorGUI.EndDisabledGroup();
    }

    Material[] GetMaterialsInActionsToPerform() {
        return actionsToPerform.Where(action => action is SwitchToMaterialAction && (action as SwitchToMaterialAction).pathToMaterial != "").Select(action => (Material)AssetDatabase.LoadMainAssetAtPath((action as SwitchToMaterialAction).pathToMaterial)).ToArray();
    }

    void SelectMaterials(Material[] materials) {
        Selection.objects = materials;
    }

    void DryRun() {
        // disabled at end of GUI
        isDryRun = true;

        Questify();
    }

    void SelectPhysBoneComponent(VRCPhysBone physBoneToSelect) {
        Selection.activeObject = physBoneToSelect.gameObject;

        Component[] allComponents = physBoneToSelect.transform.GetComponents<Component>();

        foreach (Component component in allComponents)
        {
            InternalEditorUtility.SetIsInspectorExpanded(component, false);
        }
        
        // note this expands ALL physbones (don't know how to get around this)
        InternalEditorUtility.SetIsInspectorExpanded(physBoneToSelect, true);

        ActiveEditorTracker.sharedTracker.ForceRebuild();
    }

    void SelectTransform(Transform thing) {
        Selection.activeObject = thing.gameObject;
    }

    void FocusGameObjectByPathInsideAvatar(string pathInsideAvatar) {
        Transform result = Utils.FindChild(sourceVrcAvatarDescriptor.transform, pathInsideAvatar);
        FocusGameObject(result.gameObject);
    }

    void FocusGameObjectByPath(string pathToObject) {
        var obj = GameObject.Find(pathToObject);

        if (obj == null) {
            obj = Utils.FindGameObjectByPath(pathToObject);
        }

        FocusGameObject(obj);
    }

    void FocusGameObject(GameObject obj) {
        EditorGUIUtility.PingObject(obj);
    }

    void FocusFileInProjectWindow(string pathToFile) {
        var asset = AssetDatabase.LoadMainAssetAtPath(pathToFile);
        FocusAssetInProjectWindow(asset);
    }

    void FocusAssetInProjectWindow(Object asset) {
        EditorUtility.FocusProjectWindow();
        
        EditorGUIUtility.PingObject(asset);
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

                case Types.RemoveAllPhysBones:
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

            case Types.RemovePhysBone:
                action = new RemovePhysBoneAction() {
                    pathToGameObject = fieldValue1,
                    physBoneIndex = fieldValue3
                };
                break;

            case Types.RemoveAllPhysBones:
                action = new RemoveAllPhysBonesAction() {
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
    }

    void RenderActions() {
        if (actions.Count == 0) {
            GUILayout.Label("No actions configured");
            return;
        }

        foreach (Action action in actions) {
            GUILayout.BeginHorizontal();

            RenderAction(action);

            int idx = actions.IndexOf(action);

            if (CustomGUI.TinyButton("x")) {
                DeleteAction(action);
            }

            EditorGUI.BeginDisabledGroup(idx == 0);
            if (CustomGUI.TinyButton("^")) {
                MoveActionUp(action);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(idx == actions.Count - 1);
            if (CustomGUI.TinyButton("v")) {
                MoveActionDown(action);
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }
    }

    void RenderAction(Action action) {
        RenderTypeForAction(action);

        GUIStyle guiStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 10
        };

        if (action is SwitchToMaterialAction) {
            string pathToRenderer = (action as SwitchToMaterialAction).pathToRenderer;
            string pathToMaterial = (action as SwitchToMaterialAction).pathToMaterial;
            string materialIndexStr = (action as SwitchToMaterialAction).materialIndex.ToString();
            bool autoCreated = (action as SwitchToMaterialAction).autoCreated;

            GUILayout.BeginHorizontal();
            GUILayout.Label(GetOutputForPath(pathToRenderer), guiStyle, GUILayout.ExpandWidth(false));

            if (CustomGUI.TinyButton("View")) {
                FocusGameObjectByPathInsideAvatar(pathToRenderer);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(" => " + pathToMaterial + " (" + materialIndexStr + ")" + (autoCreated ? " (created)" : ""), guiStyle, GUILayout.ExpandWidth(false));

            if (CustomGUI.TinyButton("View")) {
                FocusFileInProjectWindow(pathToMaterial);
            }
            GUILayout.EndHorizontal();
        } else if (action is RemoveGameObjectAction) {
            string pathToGameObject = (action as RemoveGameObjectAction).pathToGameObject;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetOutputForPath(pathToGameObject), guiStyle, GUILayout.ExpandWidth(false));

            if (CustomGUI.TinyButton("View")) {
                FocusGameObjectByPathInsideAvatar(pathToGameObject);
            }
            GUILayout.EndHorizontal();
        } else if (action is RemovePhysBoneAction) {
            string pathToGameObject = (action as RemovePhysBoneAction).pathToGameObject;
            string pathToRootTransform = (action as RemovePhysBoneAction).pathToRootTransform;
            string physBoneIndexStr = (action as RemovePhysBoneAction).physBoneIndex.ToString();

            GUILayout.BeginHorizontal();
            GUILayout.Label(GetOutputForPath(pathToGameObject) + " (" + physBoneIndexStr + ")", guiStyle, GUILayout.ExpandWidth(false));
            
            if (CustomGUI.TinyButton("View")) {
                FocusGameObjectByPathInsideAvatar(pathToGameObject);
            }
            GUILayout.EndHorizontal();

            if (pathToRootTransform != null && pathToRootTransform != pathToGameObject) {
                GUILayout.BeginHorizontal();
                GUILayout.Label(" => " + GetOutputForPath(pathToRootTransform), guiStyle, GUILayout.ExpandWidth(false));
            
                if (CustomGUI.TinyButton("View")) {
                    FocusGameObjectByPathInsideAvatar(pathToRootTransform != null ? pathToRootTransform : pathToGameObject);
                }
                GUILayout.EndHorizontal();
            }
        } else if (action is RemoveAllPhysBonesAction) {
            string pathToGameObject = (action as RemoveAllPhysBonesAction).pathToGameObject;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetOutputForPath(pathToGameObject), guiStyle, GUILayout.ExpandWidth(false));

            if (CustomGUI.TinyButton("View")) {
                FocusGameObjectByPathInsideAvatar(pathToGameObject);
            }
            GUILayout.EndHorizontal();
        } else {
            throw new System.Exception("Unknown action!");
        }
    }

    void RenderTypeForAction(Action action) {
        string label;

        if (action is SwitchToMaterialAction) {
            label = "Switch Material";
        } else if (action is RemoveGameObjectAction) {
            label = "Remove Object";
        } else if (action is RemovePhysBoneAction) {
            label = "Remove PhysBone";
        } else if (action is RemoveAllPhysBonesAction) {
            label = "Remove All PhysBones";
        } else {
            throw new System.Exception("Unknown action!");
        }

        GUILayout.Label(label + (action.performAtEnd ? " (end)" : ""));
    }

    string GetOutputForPath(string path) {
        return path != "" ? path : "(root)";
    }

    void DeleteAction(Action action) {
        Debug.Log("Deleting action...");

        List<Action> newActions = actions.ToList();
        newActions.Remove(action);
        actions = newActions;
    }

    void MoveActionUp(Action action) {
        Debug.Log("Moving action up...");

        List<Action> newActions = actions.ToList();
        int idx = newActions.IndexOf(action);
        newActions.Remove(action);
        newActions.Insert(idx - 1, action);
        actions = newActions;
    }

    void MoveActionDown(Action action) {
        Debug.Log("Moving action down...");

        List<Action> newActions = actions.ToList();
        int idx = newActions.IndexOf(action);
        newActions.Remove(action);
        newActions.Insert(idx + 1, action);
        actions = newActions;
    }

    void Questify() {
        successState = SuccessStates.Unknown;

        ClearErrors();

        Debug.Log("Found " + actions.Count + " from filesystem");

        actionsToPerform = actions.ToList();

        GameObject avatar = CreateQuestAvatar(sourceVrcAvatarDescriptor);

        AddActionsToSwitchAllMaterialsToQuestForAvatar(avatar);
        AddActionsToRemovePhysBonesForAvatar(avatar);

        Debug.Log("Added " + (actionsToPerform.Count - actions.Count) + " new actions");

        Debug.Log("Performing " + actionsToPerform.Count + " actions...");

        List<Action> sortedActions = SortActions(actionsToPerform);

        if (isDryRun == false) {
            foreach (Action actionToPerform in actionsToPerform) {
                PerformAction(actionToPerform, avatar);
            }
        }

        successState = SuccessStates.Success;

        HideSuccessMessageAfterDelay();

        if (isDryRun) {
            DestroyImmediate(avatar);
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
        } else if (action is RemoveAllPhysBonesAction) {
            try {
                string pathToGameObject = (action as RemoveAllPhysBonesAction).pathToGameObject;
                RemoveAllPhysBonesOnGameObjectForAvatar(avatar, pathToGameObject);
            } catch (FailedToRemovePhysBoneException exception) {
                errors.Add(exception);
            }
        } else {
            throw new System.Exception("Cannot perform action - unknown action type: " + nameof(action));
        }
    }

    void RemovePhysBoneForAvatar(GameObject avatar, string pathToGameObject, int physBoneIndex = 0) {
        Debug.Log("Removing PhysBones at " + pathToGameObject + " (" + physBoneIndex.ToString() + ")...");

        if (physBoneIndex < 0) {
            throw new FailedToRemovePhysBoneException("Index is less than 0") {
                pathToGameObject = pathToGameObject,
                physBoneIndex = physBoneIndex
            };
        } 

        Transform gameObjectTransform = pathToGameObject == "" ? avatar.transform : Utils.FindChild(avatar.transform, pathToGameObject);

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

    void RemoveAllPhysBonesOnGameObjectForAvatar(GameObject avatar, string pathToGameObject) {
        Debug.Log("Removing all physBones at " + pathToGameObject + "...");

        Transform gameObjectTransform = Utils.FindChild(avatar.transform, pathToGameObject);

        if (gameObjectTransform == null) {
            throw new FailedToRemoveAllPhysBonesException("Game object not found") {
                pathToGameObject = pathToGameObject
            };
        }

        VRCPhysBone[] physBones = gameObjectTransform.gameObject.GetComponents<VRCPhysBone>();

        if (physBones.Length == 0) {
            throw new FailedToRemoveAllPhysBonesException("No PhysBones found on the game object") {
                pathToGameObject = pathToGameObject
            };
        }

        foreach (VRCPhysBone physBone in physBones) {
            DestroyImmediate(physBone);
        }
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

    void ClearErrors() {
        errors = new List<System.Exception>();
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
        Debug.Log("Adding actions to switch all materials to Quest...");

        Renderer[] allRenderers = avatar.GetComponentsInChildren<Renderer>(true);

        Debug.Log("Found " + allRenderers.Length + " renderers");

        foreach (Renderer renderer in allRenderers) {
            AddActionsForSwitchingMeshToQuestMaterialsForAvatar(avatar, renderer);
        }
    }

    void AddActionsToRemovePhysBonesForAvatar(GameObject avatar) {
        Debug.Log("Adding actions to remove PhysBones...");

        VRCPhysBone[] physBones = GetPhysBonesInTransform(avatar.transform);

        if (physBones.Length != isToDeleteEachPhysBone.Length) {
            throw new System.Exception("The number of PhysBones has changed (was " + isToDeleteEachPhysBone.Length + " now " + physBones.Length + ")");
        }

        Dictionary<Transform, int> componentIdxPerTransform = new Dictionary<Transform, int>();

        for (int i = 0; i < physBones.Length; i++) {
            VRCPhysBone physBone = physBones[i];
            bool deleteThisPhysBone = isToDeleteEachPhysBone[i] == true;

            int componentIndex;

            if (componentIdxPerTransform.ContainsKey(physBone.transform)) {
                componentIdxPerTransform[physBone.transform]++;
                componentIndex = componentIdxPerTransform[physBone.transform];
            } else {
                componentIdxPerTransform[physBone.transform] = 0;
                componentIndex = 0;
            }

            if (deleteThisPhysBone) {
                AddActionToDeletePhysBone(physBone, componentIndex, avatar);
            }
        }
    }

    void AddActionToDeletePhysBone(VRCPhysBone physBone, int componentIndex, GameObject avatar) {
        actionsToPerform.Add(new RemovePhysBoneAction() {
            pathToGameObject = Utils.GetRelativeGameObjectPath(physBone.gameObject, avatar),
            pathToRootTransform = Utils.GetRelativeGameObjectPath(physBone.rootTransform ? physBone.rootTransform.gameObject : physBone.transform.gameObject, avatar),
            physBoneIndex = componentIndex
        });
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

                if (pathToMaterial == "") {
                    throw new System.Exception("Failed to find material at path " + pathToMaterial);
                }

                if (pathToMaterial.Contains("Quest")) {
                    Debug.Log("Material is already named with Quest, skipping...");
                    continue;
                }
                
                Debug.Log("Switching material " + pathToMaterial + "...");

                string pathToQuestMaterial = pathToMaterial.Replace(".mat", " Quest.mat");

                Material questMaterial = LooselyGetMaterialAtPath(pathToQuestMaterial);

                bool autoCreated = false;

                if (questMaterial == null) {
                    string pathToQuestMaterialParent = Utils.GetDirectoryPathRelativeToAssets(pathToMaterial);

                    string pathToQuestMaterialInQuestFolder = Path.Combine(pathToQuestMaterialParent, "Quest", Path.GetFileName(pathToMaterial).Replace(".mat", " Quest.mat"));

                    Debug.Log("Looking for a quest folder version: " + pathToQuestMaterialInQuestFolder);

                    Material questMaterialInFolder = LooselyGetMaterialAtPath(pathToQuestMaterialInQuestFolder);

                    if (questMaterialInFolder == null) {
                        if (autoCreateQuestMaterials) {
                            if (isDryRun == false) {
                                questMaterialInFolder = CreateMissingQuestMaterialForRenderer(pathToMaterial, material);
                                autoCreated = true;
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
                    autoCreated = autoCreated
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
            Debug.Log(err);
        }

        RepaintInspector(typeof(Material));

        return createdMaterial;
    }

    public static void RepaintInspector(System.Type t)
    {
        Editor[] ed = (Editor[])Resources.FindObjectsOfTypeAll<Editor>();
        for (int i = 0; i < ed.Length; i++)
        {
            if (ed[i].GetType() == t)
            {
                ed[i].Repaint();
                return;
            }
        }
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

    async Task HideSuccessMessageAfterDelay()
    {   
        await Task.Run(() => ResetSuccessState());
    }
    
    void ResetSuccessState() {
        Thread.Sleep(2000);
        successState = SuccessStates.Unknown;
    }
}
