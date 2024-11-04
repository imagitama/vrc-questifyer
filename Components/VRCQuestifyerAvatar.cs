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

[AddComponentMenu("VRC Questify Avatar")]
public class VRCQuestifyerAvatar : VRCQuestifyerBase {
    public bool replaceExistingQuestAvatars = true;
    public bool hideOriginalAvatar = true;
    public bool zoomToClonedAvatar = true;
    public bool removeComponents = true;
    public float moveAvatarBackMeters = 0f;
}