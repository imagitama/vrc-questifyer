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

[AddComponentMenu("VRC Questify Rename")]
public class VRCQuestifyerRename : VRCQuestifyerBase {
    public enum RenameMode {
        Replace,
        Prefix,
        Suffix
    }

    public string newName = " [Quest]";
    public RenameMode mode = RenameMode.Suffix;

    public string GetFinalName() {
        var transformToUse = this.overrideTarget != null ? this.overrideTarget : this.transform;
        var originalName = transformToUse.gameObject.name;
        var finalName = mode == RenameMode.Prefix ? $"{newName}{originalName}" : mode == RenameMode.Suffix ? $"{originalName}{newName}" : newName;
        return finalName;
    }

    public override void Apply() {
        var transformToUse = this.overrideTarget != null ? this.overrideTarget : this.transform;
        var finalName = GetFinalName();

        Debug.Log($"VRCQuestifyer :: Rename '{transformToUse.gameObject.name}' => '{finalName}'");

        transformToUse.gameObject.name = finalName;
    }
}