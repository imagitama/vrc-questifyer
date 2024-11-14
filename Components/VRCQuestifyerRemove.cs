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

[AddComponentMenu("VRC Questify Remove")]
public class VRCQuestifyerRemove : VRCQuestifyerBase {
    public override void Apply() {
        var transformToUse = this.overrideTarget != null ? this.overrideTarget : this.transform;

        Debug.Log($"VRCQuestifyer :: Remove '{transformToUse.gameObject.name}'");

        DestroyImmediate(transformToUse.gameObject);
    }
}