using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRC Questify")]
[DisallowMultipleComponent]
public class VRC_Questify : MonoBehaviour, IEditorOnly {
    public VRC_Questify.Action[] actions = new VRC_Questify.Action[0];

    public enum ActionType {
        SwitchToMaterial,
        RemoveGameObject,
        RemoveComponent
    }

    [Serializable]
    public struct Action {
        public ActionType type;
        public List<Material> materials;
        public List<Component> components;
    }

    public void AddAction(Action action) {
        var newActions = new VRC_Questify.Action[actions.Length + 1];
        Array.Copy(actions, newActions, actions.Length);
        newActions[actions.Length] = action;
        actions = newActions;
    }
}