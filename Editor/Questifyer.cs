using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

using VRC.SDK3.Avatars.Components;

using UnityEngine;
using UnityEditor;

namespace PeanutTools_VRCQuestifyer {
    public static class Questifyer {
        public static void QuestifyAvatar(VRCAvatarDescriptor vrcAvatarDescriptor) {
            Debug.Log($"VRCQuestifyer :: Questify avatar '{vrcAvatarDescriptor.gameObject.name}'");
            Questify(vrcAvatarDescriptor.transform);
        }

        public static Transform CloneAndQuestifyAvatar(VRCAvatarDescriptor vrcAvatarDescriptor, bool replaceExistingQuestAvatars = true) {
            Debug.Log($"VRCQuestifyer :: Clone and Questify avatar '{vrcAvatarDescriptor.gameObject.name}'");

            if (vrcAvatarDescriptor.gameObject.name.Contains("[Quest]")) {
                throw new System.Exception("Avatar gameobject already named Quest");
            }

            var avatarComponent = Utils.FindComponentInDescendant<VRCQuestifyerAvatar>(vrcAvatarDescriptor.transform);

            var questName = $"{vrcAvatarDescriptor.gameObject.name} {Common.questTag}";

            if (replaceExistingQuestAvatars) {
                var existingAvatar = GameObject.Find(questName);

                if (existingAvatar != null) {
                    Debug.Log($"VRCQuestifyer :: Existing avatat found and want to replace");

                    GameObject.DestroyImmediate(existingAvatar);
                }
            }
            
            Debug.Log($"VRCQuestifyer :: Instantiate avatar clone");

            GameObject clonedAvatar = GameObject.Instantiate(vrcAvatarDescriptor.gameObject);
            clonedAvatar.name = questName;
            
            Debug.Log($"VRCQuestifyer :: Created '{clonedAvatar.name}'");

            if (avatarComponent == null || avatarComponent.hideOriginalAvatar == true) {
            Debug.Log($"VRCQuestifyer :: Hide original avatar");
                vrcAvatarDescriptor.gameObject.SetActive(false);
            }

            if (avatarComponent == null || avatarComponent.moveAvatarBackMeters != null) {
            Debug.Log($"VRCQuestifyer :: Move avatar back");
                clonedAvatar.transform.position = new Vector3(clonedAvatar.transform.position.x, clonedAvatar.transform.position.y, clonedAvatar.transform.position.z - avatarComponent.moveAvatarBackMeters);
            }

            if (avatarComponent == null || avatarComponent.zoomToClonedAvatar == true) {
            Debug.Log($"VRCQuestifyer :: Zoom to cloned avatar");
                Utils.Focus(clonedAvatar.transform);
            }

            Questify(clonedAvatar.transform, avatarComponent != null && avatarComponent.removeComponents);

            return clonedAvatar.transform;
        }

        public static void Questify(Transform transform, bool removeComponents = true) {
            Debug.Log($"VRCQuestifyer :: Questify '{transform.gameObject.name}'");

            var components = GetComponents(transform);
            
            Debug.Log($"VRCQuestifyer :: Found {components.Count} components");

            foreach (var component in components) {
                QuestifyUsingComponent(component);
            }

            if (removeComponents) {
                Debug.Log($"VRCQuestifyer :: Remove components");
                RemoveAllQuestifyerComponents(transform);
            }

            Debug.Log($"VRCQuestifyer :: Questify done");
        }

        public static Task QuestifyUsingComponent(VRCQuestifyerBase component) {
            Debug.Log($"VRCQuestifyer :: Apply component {component}");
            component.Apply();
            return null;
        }

        public static bool GetIsAvatarQuestifyable(VRCAvatarDescriptor vrcAvatarDescriptor) {
            return Utils.FindComponentInDescendant<VRCQuestifyerAvatar>(vrcAvatarDescriptor.transform);
        }

        public static VRCQuestifyerAvatar FindAvatarComponent(Transform transform) {
            return Utils.FindComponentInDescendant<VRCQuestifyerAvatar>(transform);
        }

        public static bool GetIsQuestifyable(Transform transform) {
            return transform.GetComponent<VRCQuestifyerBase>() != null;
        }

        public static List<VRCQuestifyerBase> GetComponents(Transform transform) {
            return transform.GetComponentsInChildren<VRCQuestifyerBase>(transform).ToList();
        }

        public static void RemoveAllQuestifyerComponents(Transform transform) {
            Debug.Log($"VRCQuestifyer :: Remove all Questifyer components from '{transform.gameObject.name}'");
            Utils.RemoveAllComponents<VRCQuestifyerBase>(transform);
        }

        public static void SetupAvatar(VRCAvatarDescriptor vrcAvatarDescriptor, bool autoDetectExistingQuestMaterials) {
            Debug.Log($"VRCQuestifyer :: Setup avatar '{vrcAvatarDescriptor}'");

            Debug.Log($"VRCQuestifyer :: Create core object");

            var questifyerGameObject = new GameObject();
            questifyerGameObject.name = "Questifyer";
            questifyerGameObject.transform.SetParent(vrcAvatarDescriptor.transform);
            questifyerGameObject.AddComponent(typeof(VRCQuestifyerAvatar));
            
            Debug.Log($"VRCQuestifyer :: Core object created");

            Debug.Log($"VRCQuestifyer :: Create remove blacklisted components object");

            var removeBlacklistedComponentsGameObject = new GameObject();
            removeBlacklistedComponentsGameObject.name = "RemoveBlacklistedComponents";
            removeBlacklistedComponentsGameObject.transform.SetParent(questifyerGameObject.transform);
            var newComponent = (VRCQuestifyerRemoveBlacklistedComponents)removeBlacklistedComponentsGameObject.AddComponent(typeof(VRCQuestifyerRemoveBlacklistedComponents));

            newComponent.overrideTarget = vrcAvatarDescriptor.transform;
            
            Debug.Log($"VRCQuestifyer :: Remove blacklisted components object created");

            List<Renderer> renderers = Utils.FindAllComponents<Renderer>(vrcAvatarDescriptor.transform);

            Debug.Log($"VRCQuestifyer :: Add renderer components");

            foreach (var renderer in renderers) {
                if (renderer.gameObject.GetComponent<VRCQuestifyerSwitchMaterials>() == null) {
                    Debug.Log($"VRCQuestifyer :: Add component 'VRCQuestifyerSwitchMaterials' to '{renderer.transform.gameObject.name}' with renderer '{renderer}'");
                    var switchMaterialsComponent = (VRCQuestifyerSwitchMaterials)renderer.gameObject.AddComponent<VRCQuestifyerSwitchMaterials>(); 

                    if (autoDetectExistingQuestMaterials) {
                        VRCQuestifyerSwitchMaterialsEditor customEditor = (VRCQuestifyerSwitchMaterialsEditor)Editor.CreateEditor(switchMaterialsComponent);
                        customEditor.AddExistingMaterials();
                    }
                }
            }
            
            Debug.Log($"VRCQuestifyer :: Setup avatar done");
        }
    }
}