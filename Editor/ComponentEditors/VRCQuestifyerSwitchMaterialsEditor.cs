using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using PeanutTools_VRCQuestifyer;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(VRCQuestifyerSwitchMaterials))]
public class VRCQuestifyerSwitchMaterialsEditor : VRCQuestifyerBaseEditor {
    public override string title { get; set; } = "Switch Materials";
    SerializedProperty materialSwitchesProperty;
    SerializedProperty alwaysOverwriteWithExistingMaterialsProperty;
    SerializedProperty createInQuestFolderProperty;
    SerializedProperty addQuestSuffixProperty;
    SerializedProperty useToonShaderProperty;
    ReorderableList reorderableList;
    
    public void OnEnable() {
        base.OnEnable();

        Hydrate();
    }

    public override void Hydrate() {
        if (target == null) {
            return;
        }

        var component = target as VRCQuestifyerSwitchMaterials;
        var transformToUse = component.overrideTarget != null ? component.overrideTarget : component.transform;

        materialSwitchesProperty = serializedObject.FindProperty("materialSwitches");
        alwaysOverwriteWithExistingMaterialsProperty = serializedObject.FindProperty("alwaysOverwriteWithExistingMaterials");
        createInQuestFolderProperty = serializedObject.FindProperty("createInQuestFolder");
        addQuestSuffixProperty = serializedObject.FindProperty("addQuestSuffix");
        useToonShaderProperty = serializedObject.FindProperty("useToonShader");

        var renderer = transformToUse.GetComponent<Renderer>();
        var existingMaterials = renderer.sharedMaterials;

        if (materialSwitchesProperty.arraySize != existingMaterials.Length) {
            materialSwitchesProperty.arraySize = existingMaterials.Length;
        }

        reorderableList = new ReorderableList(serializedObject,
            materialSwitchesProperty,
            false, true, false, false);

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var existingMaterial = existingMaterials[index];
            
            SerializedProperty materialSwitchProperty = materialSwitchesProperty.GetArrayElementAtIndex(index);

            SerializedProperty newMaterialProperty = materialSwitchProperty.FindPropertyRelative("newMaterial");
            Material newMaterial = (Material)newMaterialProperty.objectReferenceValue;

            float columnWidth = rect.width / 3;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.ObjectField(new Rect(rect.x, rect.y, columnWidth - 5, EditorGUIUtility.singleLineHeight),
                existingMaterial, typeof(Material), false);
            EditorGUI.EndDisabledGroup();

            EditorGUI.PropertyField(
                new Rect(rect.x + columnWidth, rect.y, columnWidth - 5, EditorGUIUtility.singleLineHeight),
                newMaterialProperty, GUIContent.none);

            string label = GetLabel(newMaterial);
            EditorGUI.LabelField(new Rect(rect.x + 2 * columnWidth, rect.y, columnWidth - 5, EditorGUIUtility.singleLineHeight),
                label);
        };

        reorderableList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width / 3 - 5, rect.height), "PC");
            EditorGUI.LabelField(new Rect(rect.x + rect.width / 3, rect.y, rect.width / 3 - 5, rect.height), "Quest");
            EditorGUI.LabelField(new Rect(rect.x + 2 * rect.width / 3, rect.y, rect.width / 3 - 5, rect.height), "Result");
        };
    }

    enum Failure {
        NotSet,
        InvalidShader
    }

    Failure? GetFailure(Material material) {
        if (material == null) {
            return Failure.NotSet;
        }

        if (!Common.GetIsMaterialUsingWhitelistedShader(material)) {
            return Failure.InvalidShader;
        }

        return null;
    }

    string GetLabel(Material material) {
        var failure = GetFailure(material);

        switch (failure) {
            case Failure.NotSet:
                return "Skip - not set";
            case Failure.InvalidShader:
                return "Skip - invalid shader";
            default:
                return "Replace";
        }
    }

    public override void RenderGUI() {
        var component = target as VRCQuestifyerSwitchMaterials;
        var transformToUse = component.overrideTarget != null ? component.overrideTarget : component.transform;

        var renderer = transformToUse.GetComponent<Renderer>();

        if (renderer != null && renderer.sharedMaterials.Length != materialSwitchesProperty.arraySize) {
            materialSwitchesProperty.arraySize = renderer.sharedMaterials.Length;
            
            serializedObject.ApplyModifiedProperties();
        }

        serializedObject.Update();

        CustomGUI.MediumLabel("Switch Materials");

        if (renderer == null) {
            CustomGUI.RenderErrorMessage("No renderer found");
            return;
        }

        if (!component.GetIsAnyInvalidMaterials()) {
            CustomGUI.LineGap();

            CustomGUI.RenderSuccessMessage("These materials will be switched:");

            CustomGUI.LineGap();
        }

        RenderMainGUI();

        if (CustomGUI.PrimaryButton("Add Existing Materials")) {
            AddExistingMaterials();
        }

        if (CustomGUI.PrimaryButton("Create Missing Materials")) {
            CreateMissingMaterials();
            AddExistingMaterials();
        }

        CustomGUI.ItalicLabel("It will create these missing Quest materials:");

        CustomGUI.LineGap();

        RenderMaterialsToCreate();
        
        CustomGUI.LineGap();
        
        CustomGUI.MediumLabel("Tools");

        CustomGUI.LineGap();

        if (CustomGUI.StandardButtonWide("Force Max Compression")) {
            ForceMaxCompression();
        }
        CustomGUI.ItalicLabel("Lowers final avatar size by using compression (recommended).");

        CustomGUI.SmallLineGap();

        if (CustomGUI.StandardButtonWide("Force 512x512")) {
            ForceSmallTextureSize();
        }
        CustomGUI.ItalicLabel("Lowers final avatar size by resizing texture (recommended).");

        CustomGUI.LineGap();
        
        CustomGUI.MediumLabel("Settings");

        CustomGUI.LineGap();

        alwaysOverwriteWithExistingMaterialsProperty.boolValue = CustomGUI.Checkbox("Always overwrite with existing", alwaysOverwriteWithExistingMaterialsProperty.boolValue);
        createInQuestFolderProperty.boolValue = CustomGUI.Checkbox("Create inside Quest folder (recommended)", createInQuestFolderProperty.boolValue);
        addQuestSuffixProperty.boolValue = CustomGUI.Checkbox("Add Quest suffix (recommended)", addQuestSuffixProperty.boolValue);
        useToonShaderProperty.boolValue = CustomGUI.Checkbox("Use toon shader", useToonShaderProperty.boolValue);
        
        CustomGUI.LineGap();

        CustomGUI.MediumLabel("Materials");
        
        CustomGUI.LineGap();

        for (int i = 0; i < component.materialSwitches.Length; i++) {
            Material material = component.materialSwitches[i].newMaterial;

            if (material != null) {
                Editor materialEditor = Editor.CreateEditor(material);
                materialEditor.DrawHeader();
                materialEditor.OnInspectorGUI();
            }

            RenderMaterialTextureAnalysis(material, i);
            
            CustomGUI.LineGap();
        }

        serializedObject.ApplyModifiedProperties();
    }

    public override void RenderMainGUI() {
        var component = target as VRCQuestifyerSwitchMaterials;

        if (component.GetIsAnyInvalidMaterials()) {
            CustomGUI.LineGap();

            CustomGUI.RenderWarningMessage("At least one invalid material found - this avatar will probably fail upload");

            CustomGUI.LineGap();
        }

        reorderableList.DoLayoutList();
    }

    public override void RenderExtraGUI() {
        var component = target as VRCQuestifyerSwitchMaterials;

        for (int i = 0; i < component.materialSwitches.Length; i++) {
            Material material = component.materialSwitches[i].newMaterial;
            RenderMaterialTextureAnalysis(material, i);
        }
        
        CustomGUI.LineGap();
    }

    void ForceMaxCompression() {
        var component = target as VRCQuestifyerSwitchMaterials;

        foreach (var materialSwitch in component.materialSwitches) {
            var material = materialSwitch.newMaterial;

            if (material == null) {
                continue;
            }

            var textureImporters = Materials.GetTextureImportersForMaterial(material);

            foreach (var textureImporter in textureImporters) {
                TextureImporterPlatformSettings androidSettings = textureImporter.GetPlatformTextureSettings("Android");
                androidSettings.overridden = true;
                androidSettings.format = TextureImporterFormat.ASTC_4x4;
                androidSettings.compressionQuality = (int)TextureCompressionQuality.Best;

                Debug.Log($"VRCQuestifyer :: Force max compression for '{textureImporter.assetPath}'");

                textureImporter.SetPlatformTextureSettings(androidSettings);

                AssetDatabase.ImportAsset(textureImporter.assetPath, ImportAssetOptions.ForceUpdate);
            }
        }
    }

    void ForceSmallTextureSize() {
        var component = target as VRCQuestifyerSwitchMaterials;

        foreach (var materialSwitch in component.materialSwitches) {
            var material = materialSwitch.newMaterial;

            if (material == null) {
                continue;
            }

            var textureImporters = Materials.GetTextureImportersForMaterial(material);

            foreach (var textureImporter in textureImporters) {
                TextureImporterPlatformSettings androidSettings = textureImporter.GetPlatformTextureSettings("Android");
                androidSettings.overridden = true;
                androidSettings.maxTextureSize = 512;

                Debug.Log($"VRCQuestifyer :: Force small texture size for '{textureImporter.assetPath}'");

                textureImporter.SetPlatformTextureSettings(androidSettings);

                AssetDatabase.ImportAsset(textureImporter.assetPath, ImportAssetOptions.ForceUpdate);
            }
        }
    }

    struct TextureAnalysis {
        public string propertyName;
        public Texture texture;
        public long sizeBytes;
        public int maxTextureSize;
        public TextureImporterFormat textureFormat;
        public int compressionQuality;
    }

    List<TextureAnalysis> GetTextureAnalysis(Material material) {
        var results = new List<TextureAnalysis>();

        if (material == null) {
            return null;
        }

        var texturesWithPropName = Materials.GetTexturesWithPropertyNameForMaterial(material);

        foreach (var textureWithPropName in texturesWithPropName) {
            var textureImporter = Materials.GetTextureImporterForTexture(textureWithPropName.texture);

            if (textureImporter == null) {
                continue;
            }

            int maxTextureSize;
            TextureImporterFormat textureFormat;
            int compressionQuality;

            textureImporter.GetPlatformTextureSettings("Android", out maxTextureSize, out textureFormat, out compressionQuality);

            long sizeBytes = EditorTextureUtil.GetStorageMemorySize(textureWithPropName.texture);

            results.Add(new TextureAnalysis() {
                propertyName = textureWithPropName.propertyName,
                texture = textureWithPropName.texture,
                sizeBytes = sizeBytes,
                maxTextureSize = maxTextureSize,
                textureFormat = textureFormat,
                compressionQuality = compressionQuality
            });
        }

        return results;
    }

    void RenderMaterialTextureAnalysis(Material material, int index) {
        if (material == null) {
            CustomGUI.ItalicLabel("No material");
            return;
        }

        var analysisItems = GetTextureAnalysis(material);
        
        var isAndroid = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;

        foreach (var analysisItem in analysisItems) {
            GUILayout.BeginHorizontal();
            
            GUILayout.BeginVertical(GUILayout.Width(27)); 

            if (GUILayout.Button(GUIContent.none, GUILayout.Width(25), GUILayout.Height(25))) {
                Utils.Ping(analysisItem.texture);
            }

            Rect textureRect = GUILayoutUtility.GetLastRect();

            EditorGUI.DrawPreviewTexture(textureRect, analysisItem.texture);

            GUILayout.EndVertical();
            
            GUILayout.BeginVertical();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{material.name}.{analysisItem.propertyName}: {analysisItem.maxTextureSize.ToString()}x{analysisItem.maxTextureSize.ToString()} at {analysisItem.compressionQuality.ToString()}% (Android)");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{Utils.FormatBytes(analysisItem.sizeBytes)} ({(isAndroid ? "Android" : "Windows")})");
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();
        }
    }

    void RenderMaterialsToCreate() {
        var component = target as VRCQuestifyerSwitchMaterials;
        var transformToUse = component.overrideTarget != null ? component.overrideTarget : component.transform;

        var renderer = transformToUse.GetComponent<Renderer>();
        var existingMaterials = renderer.sharedMaterials;

        if (materialSwitchesProperty.arraySize != existingMaterials.Length) {
            materialSwitchesProperty.arraySize = existingMaterials.Length;
            serializedObject.ApplyModifiedProperties();
        }

        for (var i = 0; i < existingMaterials.Length; i++) {
            Material material = existingMaterials[i];

            SerializedProperty materialSwitchProperty = materialSwitchesProperty.GetArrayElementAtIndex(i);
            Material materialToUse = (Material)materialSwitchProperty.FindPropertyRelative("newMaterial").objectReferenceValue;

            if (materialToUse != null) {
                CustomGUI.Label($"#{i}: Skip - already Quest");
                continue;
            }

            if (!Materials.GetCanMaterialBeCopied(material)) {
                CustomGUI.Label($"#{i}: Skip - invalid material (maybe inside FBX?)");
                continue;
            }
            
            var newPathInsideAssets = Materials.GetVirtualPathInsideAssetsForQuestVersionOfMaterial(material, createInQuestFolderProperty.boolValue, addQuestSuffixProperty.boolValue);

            GUILayout.BeginHorizontal();

            if (newPathInsideAssets == "") {
                CustomGUI.Label($"#{i}: Skip - invalid material");
                GUILayout.EndHorizontal();
                continue;
            }
 
            GUILayout.Label(i.ToString(), GUILayout.Width(15));

            GUILayout.Label(material.name, GUILayout.Width(200));
            GUILayout.Label(material.shader.name, GUILayout.Width(200));

            if (GUILayout.Button("View", GUILayout.Width(100))) {
                Utils.Ping(material);
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            GUILayout.Label($" => {newPathInsideAssets}");

            GUILayout.EndHorizontal();
        }
    }

    public void AddExistingMaterials() {
        Debug.Log("VRCQuestifyer :: Switch materials editor - Add existing materials");

        serializedObject.Update();

        var component = target as VRCQuestifyerSwitchMaterials;
        var transformToUse = component.overrideTarget != null ? component.overrideTarget : component.transform;

        var renderer = transformToUse.GetComponent<Renderer>();
        var existingMaterials = renderer.sharedMaterials;

        if (materialSwitchesProperty.arraySize != existingMaterials.Length) {
            materialSwitchesProperty.arraySize = existingMaterials.Length;
            serializedObject.ApplyModifiedProperties();
        }
        
        Debug.Log($"VRCQuestifyer :: Renderer has {existingMaterials.Length} materials");

        for (var i = 0; i < existingMaterials.Length; i++) {
            Material material = existingMaterials[i];

            SerializedProperty materialSwitchProperty = materialSwitchesProperty.GetArrayElementAtIndex(i);
            Material materialToUse = (Material)materialSwitchProperty.FindPropertyRelative("newMaterial").objectReferenceValue;

            Debug.Log($"VRCQuestifyer :: #{i} ({(material != null ? material.name : "no material")})");

            if (materialToUse != null && !component.alwaysOverwriteWithExistingMaterials) {
                Debug.Log($"VRCQuestifyer :: #{i} skip - user set to something");
                continue;
            }

            var newPathInsideAssets = Materials.GetRealPathInsideAssetsForQuestVersionOfMaterial(material);

            if (newPathInsideAssets == "") {
                Debug.Log($"VRCQuestifyer :: #{i} skip - could not find Quest material");
                continue;
            }

            materialToUse = Materials.LoadMaterial(newPathInsideAssets);

            Debug.Log($"VRCQuestifyer :: #{i} set to {materialToUse.name}");

            materialSwitchProperty.FindPropertyRelative("newMaterial").objectReferenceValue = materialToUse;
        }

        serializedObject.ApplyModifiedProperties();

        Debug.Log("VRCQuestifyer :: Switch materials editor - Add existing materials - done");
    }

    public void CreateMissingMaterials(List<string> recentlyCreatedMaterialPaths = null) {
        Debug.Log("VRCQuestifyer :: Switch materials editor - Create missing materials");

        serializedObject.Update();

        var component = target as VRCQuestifyerSwitchMaterials;
        var transformToUse = component.overrideTarget != null ? component.overrideTarget : component.transform;

        var renderer = transformToUse.GetComponent<Renderer>();
        var existingMaterials = renderer.sharedMaterials;

        if (materialSwitchesProperty.arraySize != existingMaterials.Length) {
            materialSwitchesProperty.arraySize = existingMaterials.Length;
            serializedObject.ApplyModifiedProperties();
        }
        
        Debug.Log($"VRCQuestifyer :: Renderer has {existingMaterials.Length} materials");

        for (var i = 0; i < existingMaterials.Length; i++) {
            Material material = existingMaterials[i];

            SerializedProperty materialSwitchProperty = materialSwitchesProperty.GetArrayElementAtIndex(i);
            Material materialToUse = (Material)materialSwitchProperty.FindPropertyRelative("newMaterial").objectReferenceValue;
            
            Debug.Log($"VRCQuestifyer :: #{i} ({(material != null ? material.name : "no material")})");

            // if user wants to override
            if (materialToUse != null) {
                Debug.Log($"VRCQuestifyer :: #{i} skip - user set to something");
                continue;
            }

            var newPathInsideAssets = Materials.GetVirtualPathInsideAssetsForQuestVersionOfMaterial(material, createInQuestFolderProperty.boolValue, addQuestSuffixProperty.boolValue);

            if (newPathInsideAssets != "" && recentlyCreatedMaterialPaths != null && recentlyCreatedMaterialPaths.Contains(newPathInsideAssets)) {
                var recentlyCreatedMaterial = Materials.LoadMaterial(newPathInsideAssets);

                materialSwitchProperty.FindPropertyRelative("newMaterial").objectReferenceValue = recentlyCreatedMaterial;

                Debug.Log($"VRCQuestifyer :: #{i} use recently created '{recentlyCreatedMaterial}'");
                continue;
            }

            if (newPathInsideAssets == "") {
                Debug.Log($"VRCQuestifyer :: #{i} skip - empty");
                continue;
            }
            
            Debug.Log($"VRCQuestifyer :: #{i} => '{newPathInsideAssets}'");

            var existingMaterialPath = Materials.GetRealPathInsideAssetsForQuestVersionOfMaterial(material);

            if (existingMaterialPath != "") {
                Debug.Log($"VRCQuestifyer :: #{i} skip - already exists");
                continue;
            }

            Material newMaterial = Materials.CopyMaterial(material, newPathInsideAssets);
            
            if (newMaterial == null) {
                Debug.Log($"VRCQuestifyer :: #{i} skip - cannot copy");
                continue;
            }

            if (useToonShaderProperty.boolValue) {
                Materials.SwitchToQuestToonShader(newMaterial);
            } else {
                Materials.SwitchToQuestStandardShader(newMaterial);
            }

            materialSwitchProperty.FindPropertyRelative("newMaterial").objectReferenceValue = newMaterial;

            if (recentlyCreatedMaterialPaths != null) {
                recentlyCreatedMaterialPaths.Add(newPathInsideAssets);
            }
        }

        serializedObject.ApplyModifiedProperties();
        
        Debug.Log("VRCQuestifyer :: Switch materials editor - Create missing materials - done");
    }

    public void ClearMaterials() {
        Debug.Log("VRCQuestifyer :: Switch materials editor - Clear materials");

        serializedObject.Update();

        for (var i = 0; i < materialSwitchesProperty.arraySize; i++) {
            SerializedProperty materialSwitchProperty = materialSwitchesProperty.GetArrayElementAtIndex(i);
            materialSwitchProperty.FindPropertyRelative("newMaterial").objectReferenceValue = null;
        }
        
        serializedObject.ApplyModifiedProperties();

        Debug.Log("VRCQuestifyer :: Switch materials editor - Clear materials - done");
    }
}