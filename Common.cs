using VRC.SDKBase.Validation;
using UnityEngine;
using System.Linq;

namespace PeanutTools_VRCQuestifyer {
    // any logic common between editor and non-editor code
    public static class Common {
        public static string questTag = "[Quest]";

        public static string[] disallowedQuestComponentNames = new string[] {
            "UnityEngine.Light",
            "UnityEngine.ReflectionProbe",
            "UnityEngine.Camera",
            "UnityEngine.TrailRenderer",
            "UnityEngine.LineRenderer",
            "UnityEngine.Cloth",
            "UnityEngine.Rigidbody",
            "UnityEngine.DynamicBone",
            "UnityEngine.DynamicBoneCollider",
            "UnityEngine.Shader"
        };

        public static bool GetIsMaterialUsingWhitelistedShader(Material material) {
            return AvatarValidation.ShaderWhiteList.Any(shaderName => material.shader.name == shaderName);
        }
    }
}