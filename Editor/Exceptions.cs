using UnityEngine;
using UnityEditor;
using System.IO;

namespace VRCQuestifyer {
    public class FailedToSwitchMaterialException : System.Exception {
        public FailedToSwitchMaterialException(string message) : base(message) {
        }
        public string pathToMaterial;
    }

    public class FailedToRemoveGameObjectException : System.Exception {
        public FailedToRemoveGameObjectException(string message) : base(message) {
        }
        public string pathToGameObject;
    }

    public class FailedToRemovePhysBoneException : System.Exception {
        public FailedToRemovePhysBoneException(string message) : base(message) {
        }
        public string pathToGameObject;
        public int physBoneIndex;
    }
}