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

public abstract class VRCQuestifyerBase : MonoBehaviour, IEditorOnly {
    public Transform overrideTarget;

    public virtual void Apply() {
        // perform the actions on the target
    }

    public virtual bool GetIsValid() {
        // used by "avatar" component to output any issues
        return true;
    }
}