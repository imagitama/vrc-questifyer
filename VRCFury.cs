using UnityEngine;
using UnityEngine.Animations;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PeanutTools_VRCQuestifyer {
    public static class VRCFury {
        public static bool GetIsVRCFuryInstalled() {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
                System.Type type = assembly.GetType("VF.Component.VRCFuryComponent");
                if (type != null) {
                    return true;
                }
            }

            return false;
        }

        public static bool GetIsTransformToBeDeletedDuringUpload(Transform transform) {
            Transform currentTransform = transform;
            while (currentTransform != null) {
                var vrcFuryComponent = currentTransform.GetComponent("VRCFury");

                if (vrcFuryComponent != null) {
                    var type = vrcFuryComponent.GetType();

                    var method = type.GetMethod("GetAllFeatures", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                    if (method == null) {
                        throw new System.Exception("VRCFury missing method");
                    }

                    var features = method.Invoke(vrcFuryComponent, null) as IEnumerable<dynamic>;

                    if (features != null) {
                        var firstFeature = features.FirstOrDefault();

                        if (firstFeature.GetType().Name == "DeleteDuringUpload") {
                            return true;
                        }
                    }
                }

                currentTransform = currentTransform.parent;
            }

            return false;
        }
    }
}