using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;
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

namespace PeanutTools_VRC_Questifyer {
    class Action {
        public bool performAtEnd;
    }

    class SwitchToMaterialAction : Action {
        public string pathToRenderer;
        public string pathToMaterial;
        public int materialIndex;
        public bool autoCreated;
    }

    class RemoveGameObjectAction : Action {
        public string pathToGameObject;
    }

    class RemovePhysBoneAction : Action {
        public string pathToGameObject;
        public string pathToRootTransform;
        public int physBoneIndex;
    }

    class RemoveAllPhysBonesAction : Action {
        public string pathToGameObject;
    }
}