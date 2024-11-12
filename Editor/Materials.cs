using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PeanutTools_VRCQuestifyer {
    public static class Materials {
        public const string questDirectoryName = "Quest";
        public const string questMaterialSuffix = " [Quest]";

        public static bool GetCanMaterialBeCopied(Material material) {
            string materialPath = AssetDatabase.GetAssetPath(material);
            return materialPath.Contains(".mat");
        }

        public static Material CopyMaterial(Material sourceMaterial, string newPathInsideProject) {
            if (sourceMaterial == null || string.IsNullOrEmpty(newPathInsideProject)) {
                throw new System.Exception("Source material or path missing");
            }

            string absolutePathInsideProject = System.IO.Path.Combine(Application.dataPath, newPathInsideProject);

            Debug.Log($"VRCQuestifyer :: Copy material {sourceMaterial} to '{absolutePathInsideProject}'");

            if (!GetCanMaterialBeCopied(sourceMaterial)) {
                return null;
            }

            string directory = System.IO.Path.GetDirectoryName(absolutePathInsideProject);

            if (!System.IO.Directory.Exists(directory)) {
                Debug.Log($"VRCQuestifyer :: Create directory '{directory}'");
                System.IO.Directory.CreateDirectory(directory);
            }

            var assetPath = "Assets/" + newPathInsideProject;

            Debug.Log($"VRCQuestifyer :: Create material '{assetPath}'");

            Material newMaterial = new Material(sourceMaterial);
            AssetDatabase.CreateAsset(newMaterial, assetPath);
            AssetDatabase.SaveAssets();

            var actualNewMaterial = (Material)AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (newMaterial == null) {
                throw new System.Exception("Copied material null");
            }

            Debug.Log("VRCQuestifyer :: Material copied success");

            return actualNewMaterial;
        }

        public static void SwitchToQuestStandardShader(Material material) {
            Debug.Log("VRCQuestifyer :: Switch material to standard shader");
            material.shader = Shader.Find("VRChat/Mobile/Standard Lite");
        }

        public static void SwitchToQuestToonShader(Material material) {
            Debug.Log("VRCQuestifyer :: Switch material to toon shader");
            material.shader = Shader.Find("VRChat/Mobile/Toon Lit");
        }

        public static Material LoadMaterial(string pathInsideAssetsToMaterial) {
            var pathInProject = "Assets/" + pathInsideAssetsToMaterial;

            Debug.Log($"VRCQuestifyer :: Load material '{pathInProject}'");

            var material = AssetDatabase.LoadAssetAtPath<Material>(pathInProject);

            if (material == null) {
                throw new System.Exception($"Failed to load material asset '{pathInProject}'");
            }

            return material;
        }

        static string GetPathInsideAssetsFromPathInProject(string pathInProject) {
            var newPath = pathInProject.Replace("\\", "/").Substring(7);
            return newPath;
        }

        public static string GetRealPathInsideAssetsForQuestVersionOfMaterial(Material material) {
            string materialPath = AssetDatabase.GetAssetPath(material);

            // if a material "instance" or something else weird
            if (string.IsNullOrEmpty(materialPath)) {
                return "";
            }

            string directory = System.IO.Path.GetDirectoryName(materialPath);
            string fileName = System.IO.Path.GetFileName(materialPath);
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(materialPath);
            string extension = System.IO.Path.GetExtension(materialPath);
            string questDirectory = System.IO.Path.Combine(directory, questDirectoryName);

            if (AssetDatabase.IsValidFolder(questDirectory)) {
                var materialPathWithSuffix = System.IO.Path.Combine(
                    questDirectory,
                    fileNameWithoutExtension + questMaterialSuffix + extension
                );

                var assetWithSuffix = AssetDatabase.LoadAssetAtPath<Material>(materialPathWithSuffix);

                if (assetWithSuffix != null) {
                    return GetPathInsideAssetsFromPathInProject(materialPathWithSuffix);
                }

                var materialPathInQuestDirWithoutSuffix = System.IO.Path.Combine(
                    questDirectory,
                    fileName
                );

                var assetInQuestDirWithoutSuffix = AssetDatabase.LoadAssetAtPath<Material>(materialPathInQuestDirWithoutSuffix);

                if (assetInQuestDirWithoutSuffix != null) {
                    return GetPathInsideAssetsFromPathInProject(materialPathInQuestDirWithoutSuffix);
                }
            }
            
            var materialPathWithSuffixInSameDir = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(materialPath),
                fileNameWithoutExtension + questMaterialSuffix + extension
            );

            var assetWithSuffixInSameDir = AssetDatabase.LoadAssetAtPath<Material>(materialPathWithSuffixInSameDir);

            if (assetWithSuffixInSameDir != null) {
                return GetPathInsideAssetsFromPathInProject(materialPathWithSuffixInSameDir);
            }

            return "";
        }

        public static string GetVirtualPathInsideAssetsForQuestVersionOfMaterial(Material material, bool createInQuestFolder, bool addQuestSuffix) {
            string materialPath = AssetDatabase.GetAssetPath(material);

            // if an "instance" or something else weird
            if (string.IsNullOrEmpty(materialPath)) {
                return "";
            }

            string directory = System.IO.Path.GetDirectoryName(materialPath);
            string fileName = System.IO.Path.GetFileName(materialPath);
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(materialPath);
            string extension = System.IO.Path.GetExtension(materialPath);
            string questDirectory = System.IO.Path.Combine(directory, questDirectoryName);

            if (createInQuestFolder) {
                if (addQuestSuffix) {
                    var materialPathWithSuffix = System.IO.Path.Combine(
                        questDirectory,
                        fileNameWithoutExtension + questMaterialSuffix + extension
                    );

                    return GetPathInsideAssetsFromPathInProject(materialPathWithSuffix);
                }

                var materialPathWithoutSuffix = System.IO.Path.Combine(
                    questDirectory,
                    fileName
                );

                return GetPathInsideAssetsFromPathInProject(materialPathWithoutSuffix);
            }
            
            var materialPathWithSuffixInSameDir = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(materialPath),
                fileNameWithoutExtension + questMaterialSuffix + extension
            );

            return materialPathWithSuffixInSameDir;
        }

        public static List<Texture> GetTexturesForMaterial(Material material) {
            var results = new List<Texture>();

            Shader shader = material.shader;

            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++) {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) {
                    continue;
                }

                string propertyName = ShaderUtil.GetPropertyName(shader, i);

                Texture texture = material.GetTexture(propertyName);

                if (texture == null) {
                    continue;
                }

                results.Add(texture);
            }

            return results;
        }

        public class TextureWithPropertyName {
            public string propertyName;
            public Texture texture;
        }

        public static List<TextureWithPropertyName> GetTexturesWithPropertyNameForMaterial(Material material) {
            var results = new List<TextureWithPropertyName>();

            Shader shader = material.shader;

            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++) {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) {
                    continue;
                }

                string propertyName = ShaderUtil.GetPropertyName(shader, i);

                Texture texture = material.GetTexture(propertyName);

                if (texture == null) {
                    continue;
                }

                results.Add(new TextureWithPropertyName() {
                    propertyName = propertyName,
                    texture = texture
                });
            }

            return results;
        }

        public static List<TextureImporter> GetTextureImportersForMaterial(Material material) {
            var textures = GetTexturesForMaterial(material);

            var results = new List<TextureImporter>();

            foreach (var texture in textures) {
                TextureImporter importer = GetTextureImporterForTexture(texture);

                if (importer == null) {
                    continue;
                }

                results.Add(importer);
            }

            return results;
        }

        public static TextureImporter GetTextureImporterForTexture(Texture texture) {
            string pathToAsset = AssetDatabase.GetAssetPath(texture);

            if (pathToAsset == "") {
                return null;
            }

            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(pathToAsset);

            if (importer == null) {
                return null;
            }

            return importer;
        }
    }
}