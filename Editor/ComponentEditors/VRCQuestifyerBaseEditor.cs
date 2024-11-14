using UnityEditor;
using UnityEngine;
using PeanutTools_VRCQuestifyer;

public class VRCQuestifyerBaseEditor : Editor
{
    [System.NonSerialized]
    private string _title = "(no title)";
    public virtual string title
    {
        get => _title;
        set => _title = value;
    }
    [System.NonSerialized]
    private bool _canOverrideTarget = true;
    public virtual bool canOverrideTarget
    {
        get => _canOverrideTarget;
        set => _canOverrideTarget = value;
    }

    public void OnEnable() {
        var customIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PeanutTools/VRC_Questifyer/Editor/Assets/icon.png");

        if (customIcon != null) {
            EditorGUIUtility.SetIconForObject(this, customIcon);
        } else {
            // could happen when switch platform and re-import
        }
    }

    public override void OnInspectorGUI() {
        var component = target as VRCQuestifyerBase;

        CustomGUI.LargeLabel("Questifyer");
        
        CustomGUI.LineGap();
        
        CustomGUI.MediumLabel(this.title);

        CustomGUI.LineGap();

        if (EditorApplication.isPlaying) {
            CustomGUI.ItalicLabel("This component does nothing in play mode");
            return;
        }

        if (canOverrideTarget) {
            var newTarget = (Transform)EditorGUILayout.ObjectField("Target", component.overrideTarget, typeof(Transform));

            // fix not hydrating layout lists
            if (newTarget != component.overrideTarget) {
                component.overrideTarget = newTarget;
                Hydrate();
            }

            CustomGUI.ItalicLabel("Leave blank to use this object");

            CustomGUI.LineGap();
        }

        if (component.GetIsBeingDeletedByVrcFury()) {
            CustomGUI.RenderWarningMessage("Ignored: VRCFury \"Delete During Upload\" component detected in hierarchy");
        }

        RenderGUI();
    }
    
    public virtual void RenderGUI() {
        CustomGUI.Label($"{this.name} no GUI");
    }

    public virtual void RenderExtraGUI() {
    }

    public virtual void RenderMainGUI() {
        CustomGUI.Label($"{this.name} no main GUI");
    }

    public virtual void Hydrate() {

    }

    public string GetTitle() {
        return title;
    }
}