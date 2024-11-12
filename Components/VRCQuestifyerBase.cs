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

public abstract class VRCQuestifyerBase : MonoBehaviour, IEditorOnly {
    public Transform overrideTarget; // TODO: Change to override getter

    public virtual void Apply() {
        // perform the actions on the target
    }

    public virtual bool GetIsValid() {
        // used by "avatar" component to output any issues
        return true;
    }

    public bool GetIsBeingDeletedByVrcFury() {
        return VRCFury.GetIsTransformToBeDeletedDuringUpload(this.overrideTarget != null ? this.overrideTarget : this.transform);
    }
}