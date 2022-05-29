using UnityEngine;
using UnityEditor;
using System.IO;

namespace VRCQuestifyer {
    public class MaterialNotFoundException : System.Exception {
        public string pathToMaterial;
    }

    public class GameObjectNotFoundException : System.Exception {
        public string pathToGameObject;
    }
}