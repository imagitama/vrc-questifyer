using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;

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
using VRC.SDK3.Dynamics.PhysBone.Components;

using PeanutTools_VRCQuestifyer;

namespace PeanutTools_VRCQuestifyer {
    public class VRCQuestifyer_EditorWindow : EditorWindow {
        VRCAvatarDescriptor vrcAvatarDescriptor;

        // gui
        Vector2 scrollPosition;
        bool autoDetectExistingQuestMaterials = true;

        [MenuItem("Tools/PeanutTools/VRC Questifyer")]
        public static void ShowWindow() {
            var window = GetWindow<VRCQuestifyer_EditorWindow>();
            window.titleContent = new GUIContent("VRC Questifyer");
            window.minSize = new Vector2(400, 200);
        }

        void OnGUI() {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            CustomGUI.LargeLabel("VRC Questifyer");
            CustomGUI.ItalicLabel("Automatically make your avatar Quest compatible");

            CustomGUI.LineGap();

            CustomGUI.MediumLabel("Questifying Your Avatar");
            
            CustomGUI.LineGap();

            CustomGUI.Label(@"    1. Drag your avatar into the input below

    2. Click the big button to add Questifyer
            
    3. Switch to Android mode

    3. Click the 'Questify' button (copies your avatar and applies all actions)
    
    4. Upload");
            
            CustomGUI.LineGap();

            vrcAvatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", vrcAvatarDescriptor, typeof(VRCAvatarDescriptor));

            if (vrcAvatarDescriptor != null) {
                CustomGUI.LineGap();

                if (Questifyer.GetIsAvatarQuestifyable(vrcAvatarDescriptor)) {
                    CustomGUI.RenderSuccessMessage("This avatar has a Questifyer component and is ready to go");
                    
                    CustomGUI.LineGap();

                    if (CustomGUI.PrimaryButton("Go To Component")) {
                        Utils.Inspect(Questifyer.FindAvatarComponent(vrcAvatarDescriptor.transform).gameObject);
                    }
                    
                    CustomGUI.LineGap();

                    CustomGUI.MediumLabel("Danger Zone");
                    
                    CustomGUI.LineGap();

                    if (CustomGUI.StandardButtonWide("Remove All Components")) {
                        if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to remove all Questifyer components from this avatar and its children?", "Yes", "No")) {
                            Questifyer.RemoveAllQuestifyerComponents(vrcAvatarDescriptor.transform);
                        }
                    }

                    RenderAvatarActions(vrcAvatarDescriptor);
                } else {
                    CustomGUI.RenderWarningMessage("This avatar has not been set up with Questifyer yet");
                    
                    CustomGUI.LineGap();

                    if (CustomGUI.PrimaryButton("Add Questifyer To Avatar")) {
                        Questifyer.SetupAvatar(vrcAvatarDescriptor, autoDetectExistingQuestMaterials);
                    }
                    

                    CustomGUI.Label(@"    1. Adds an object inside your avatar called 'Questifyer'

    2. Adds a 'switch material' component to every renderer (including particle systems)

    3. Done");
                    
                    CustomGUI.LineGap();

                    CustomGUI.MediumLabel("Settings");
                    
                    CustomGUI.LineGap();

                    autoDetectExistingQuestMaterials = CustomGUI.Checkbox("Add existing materials", autoDetectExistingQuestMaterials);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        void RenderAvatarActions(VRCAvatarDescriptor vrcAvatarDescriptor) {
            var components = Questifyer.GetComponents(vrcAvatarDescriptor.transform);
        }
    }
}