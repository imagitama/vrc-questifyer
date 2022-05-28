using UnityEngine;
using UnityEditor;
using System.IO;

namespace VRCQuestifyer {
    public class MaterialNotFoundException : System.Exception {
        public string pathToMaterial;
    }
}