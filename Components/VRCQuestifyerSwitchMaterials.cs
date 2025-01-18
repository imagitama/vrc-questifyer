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
using PeanutTools_VRCQuestifyer;

[AddComponentMenu("VRC Questify Switch Materials")]
public class VRCQuestifyerSwitchMaterials : VRCQuestifyerBase {
    [System.Serializable]
    public struct MaterialSwitch {
        public Material newMaterial;
    }

    public MaterialSwitch[] materialSwitches;
    public bool alwaysOverwriteWithExistingMaterials = true;
    public bool createInQuestFolder = true;
    public bool addQuestSuffix = true;
    public bool useToonShader = false;

    public void Init() {
        var transformToUse = this.overrideTarget != null ? this.overrideTarget : this.transform;
        var renderer = transformToUse.GetComponent<Renderer>();

        materialSwitches = new MaterialSwitch[renderer.sharedMaterials.Length];
    }

    public override void Apply() {
        Debug.Log("VRCQuestifyer :: Switch materials - apply");

        var transformToUse = this.overrideTarget != null ? this.overrideTarget : this.transform;
        var renderer = transformToUse.GetComponent<Renderer>();
        
        Debug.Log($"VRCQuestifyer :: Renderer from {renderer.sharedMaterials.Length} => {materialSwitches.Length}");

        var materialsToUse = new Material[renderer.sharedMaterials.Length];
        
        for (var i = 0; i < materialSwitches.Length; i++) {
            if (materialSwitches[i].newMaterial == null) {
                materialsToUse[i] = renderer.sharedMaterials[i];
                Debug.Log($"VRCQuestifyer :: #{i} is skipped");
                continue;
            }

            Debug.Log($"VRCQuestifyer :: #{i} - {renderer.sharedMaterials[i].name} => {materialSwitches[i].newMaterial.name}");

            materialsToUse[i] = materialSwitches[i].newMaterial;
        }

        renderer.sharedMaterials = materialsToUse;
        
        Debug.Log("VRCQuestifyer :: Switch materials - apply done");
    }

    public bool GetIsAnyInvalidMaterials() {
        var transformToUse = this.overrideTarget != null ? this.overrideTarget : this.transform;

        var renderer = transformToUse.GetComponent<Renderer>();

        if (renderer == null) {
            return false;
        }

        var existingMaterials = renderer.sharedMaterials;

        for (int i = 0; i < existingMaterials.Length; i++) {
            var existingMaterial = existingMaterials[i];
            
            if (existingMaterial == null) {
                continue;
            };

            var newMaterial = materialSwitches[i].newMaterial;

            if (newMaterial == null) {
                if (!Common.GetIsMaterialUsingWhitelistedShader(existingMaterial)) {
                    return true;
                }

                return false;
            }

            if (!Common.GetIsMaterialUsingWhitelistedShader(newMaterial)) {
                return true;
            }
        }

        return false;
    }

    public override bool GetIsValid() {
        return GetIsAnyInvalidMaterials() == false;
    }
}