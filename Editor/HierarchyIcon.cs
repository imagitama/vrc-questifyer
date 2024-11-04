// using UnityEditor;
// using UnityEngine;
// using System.IO;

// [InitializeOnLoad]
// public class VRCQuestifyer_HierarchyIcon : Editor
// {
//     static Texture2D icon;

//     static Texture2D LoadIcon() {
//         if (!icon) {
//             string imagePath = Application.dataPath + "/PeanutTools/VRC_Questifyer/Editor/Assets/unity editor hierarchy icon.png";
//             byte[] imageBytes = File.ReadAllBytes(imagePath);

//             var texture = new Texture2D(2, 2);
//             texture.LoadImage(imageBytes);

//             icon = texture;
//         }

//         return icon;
//     }

//     [InitializeOnLoadMethod]
//     static void CustomizeHierarchyItem() {
//         EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
//     }

//     static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect) {
//         GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

//         if (obj != null) {
//             if (obj.GetComponent<VRCQuestifyerBase>() != null) {
//                 Texture2D icon = LoadIcon();

//                 if (icon != null) {
//                     float iconSize = 16f;
//                     float iconPadding = 2f; 

//                     Rect iconRect = new Rect(
//                         selectionRect.xMax - iconSize - iconPadding,
//                         selectionRect.y,
//                         iconSize,
//                         selectionRect.height
//                     );

//                     GUI.DrawTexture(iconRect, icon);
//                 }
//             }
//         }
//     }
// }