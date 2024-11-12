using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace PeanutTools_VRCQuestifyer {
    public static class CustomGUI {
        public static void SmallLineGap() {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        public static void LineGap() {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        public static void ItalicLabel(string text) {
            GUIStyle italicStyle = new GUIStyle(GUI.skin.label);
            italicStyle.fontStyle = FontStyle.Italic;
            GUILayout.Label(text, italicStyle);
        }

        public static void LargeLabel(string text) {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;
            GUILayout.Label(text, style);
        }

        public static void MediumLabel(string text) {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 16;
            GUILayout.Label(text, style);
        }

        public static void MediumClickableLabel(string text, GameObject gameObject) {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 16;
            
            GUILayout.Label(text, style);

            Rect labelRect = GUILayoutUtility.GetLastRect();

            if (Event.current.type == EventType.MouseUp && labelRect.Contains(Event.current.mousePosition)) {
                // Selection.activeGameObject = gameObject;
                Utils.Ping(gameObject);
            }
        }

        public static void Label(string text) {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            GUILayout.Label(text, style);
        }

        public static void BoldLabel(string text, params GUILayoutOption[] options) {
            GUILayout.Label(text, EditorStyles.boldLabel, options);
        }

        public static void MyLinks(string repoName) {
            GUILayout.Label("Links:");

            RenderLink("  Download new versions from GitHub", "https://github.com/imagitama/" + repoName);
            RenderLink("  Get support from my Discord", "https://discord.gg/R6Scz6ccdn");
            RenderLink("  Follow me on Twitter", "https://twitter.com/@HiPeanutBuddha");
        }

        public static void HorizontalRule() {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
        }

        public static void ForceRefresh() {
            GUI.FocusControl(null);
        }

        public static void RenderLink(string label, string url) {
            Rect rect = EditorGUILayout.GetControlRect();

            if (rect.Contains(Event.current.mousePosition)) {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

                if (Event.current.type == EventType.MouseUp) {
                    Help.BrowseURL(url);
                }
            }

            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(0.5f, 0.5f, 1);

            GUI.Label(rect, label, style);
        }

        public static bool PrimaryButton(string label) {
            return GUILayout.Button(label, GUILayout.Width(250), GUILayout.Height(50));
        }

        public static bool StandardButton(string label) {
            return GUILayout.Button(label, GUILayout.Width(150), GUILayout.Height(25));
        }

        public static bool StandardButtonWide(string label) {
            return GUILayout.Button(label, GUILayout.Width(200), GUILayout.Height(25));
        }

        public static bool TinyButton(string label) {
            return GUILayout.Button(label, GUILayout.Width(50), GUILayout.Height(15));
        }

        public static bool ToggleButton(string label, bool isOpen) {
            GUIStyle style = new GUIStyle(GUI.skin.button);

            if (isOpen) {
                style.normal.background = style.active.background;
                style.fontStyle = FontStyle.Bold;
            }

            return GUILayout.Button(label + "...", style, GUILayout.Width(150), GUILayout.Height(25));
        }

        public static string RenderAssetFolderSelector(ref string pathToUse) {
            GUILayout.Label("Path:");
            pathToUse = EditorGUILayout.TextField(pathToUse);
            
            if (CustomGUI.StandardButton("Select Folder")) {
                string absolutePath = EditorUtility.OpenFolderPanel("Select a folder", Application.dataPath, "");
                string pathInsideProject = absolutePath.Replace(Application.dataPath + "/", "").Replace(Application.dataPath, "");
                pathToUse = pathInsideProject;
                CustomGUI.ForceRefresh();
            }
            
            return "";
        }

        public static void RenderMessage(string message, Color color) {
            GUIStyle guiStyle = new GUIStyle(GUI.skin.label) {
                normal = { textColor = color },
                focused = { textColor = color },
                active = { textColor = color },
                hover = { textColor = color },
                onNormal = { textColor = color },
                onFocused = { textColor = color },
                onActive = { textColor = color },
                onHover = { textColor = color }
            };
            GUILayout.Label(message, guiStyle);
        }

        public static void RenderSuccessMessage(string message) {
            RenderMessage(message, new Color(0.5f, 1.0f, 0.5f));
        }

        public static void RenderErrorMessage(string message) {
            RenderMessage(message, new Color(1.0f, 0.5f, 0.5f));
        }

        public static void RenderWarningMessage(string message) {
            RenderMessage(message, new Color(1.0f, 1.0f, 0.5f));
        }

        public static bool Checkbox(string label, bool value) {
            return EditorGUILayout.Toggle(label, value);
        }

        public static string TextInput(string label, string value) {
            return EditorGUILayout.TextField(label, value);
        }

        public static float FloatInput(string label, float value) {
            return EditorGUILayout.FloatField(label, value);
        }
    }
}