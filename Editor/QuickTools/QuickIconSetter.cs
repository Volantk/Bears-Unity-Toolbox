using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.ShortcutManagement;

namespace BearsEditorTools
{
    public class QuickIconSetter : EditorWindow
    {
        private int enumerator;

        private static Dictionary<string, Texture2D> iconTextures;

        private static bool _hasBeenCached;

        private static readonly Dictionary<string, string> _IconTypePaths = new Dictionary<string, string>
        { 
            {"GreyLabel", "sv_label_0"},
            {"BlueLabel", "sv_label_1"},
            {"CyanLabel", "sv_label_2"},
            {"GreenLabel", "sv_label_3"},
            {"YellowLabel", "sv_label_4"},
            {"OrangeLabel", "sv_label_5"},
            {"RedLabel", "sv_label_6"},
            {"PinkLabel", "sv_label_7"},
            {"GreyRound", "sv_icon_dot0_pix16_gizmo"},
            {"BlueRound", "sv_icon_dot1_pix16_gizmo"},
            {"CyanRound", "sv_icon_dot2_pix16_gizmo"},
            {"GreenRound", "sv_icon_dot3_pix16_gizmo"},
            {"YellowRound", "sv_icon_dot4_pix16_gizmo"},
            {"OrangeRound", "sv_icon_dot5_pix16_gizmo"},
            {"RedRound", "sv_icon_dot6_pix16_gizmo"},
            {"PinkRound", "sv_icon_dot7_pix16_gizmo"},
            {"GreyDiamond", "sv_icon_dot8_pix16_gizmo"},
            {"BlueDiamond", "sv_icon_dot9_pix16_gizmo"},
            {"CyanDiamond", "sv_icon_dot10_pix16_gizmo"},
            {"GreenDiamond", "sv_icon_dot11_pix16_gizmo"},
            {"YellowDiamond", "sv_icon_dot12_pix16_gizmo"},
            {"OrangeDiamond", "sv_icon_dot13_pix16_gizmo"},
            {"RedDiamond", "sv_icon_dot14_pix16_gizmo"},
            {"PinkDiamond", "sv_icon_dot15_pix16_gizmo"},
        };

        [Shortcut("Bears/QuickTools/Quick Icon Setter", KeyCode.E, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void OpenWindow()
        {
            if (Selection.instanceIDs.Length == 0) return;
            QuickIconSetter window = ScriptableObject.CreateInstance<QuickIconSetter>();
            window.ShowPopup();
            window.Focus();

            Rect windowRect = window.position;

            if (Event.current != null)
            {
                var mousePosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                windowRect.x = mousePosition.x - windowRect.width * 0.5f;
                windowRect.y = mousePosition.y;
            }
            else
            {
                windowRect.x = Screen.width * 0.5f - windowRect.width * 0.5f;
                windowRect.y = Screen.height * 0.5f - windowRect.height * 0.5f;
            }

            windowRect.height = 120;
            window.position = windowRect;
        }

        private void CacheIconTextures()
        {
            if (_hasBeenCached) return;

            iconTextures = new Dictionary<string, Texture2D>();

            foreach (KeyValuePair<string, string> typePath in _IconTypePaths)
            {
                Texture2D texture = EditorGUIUtility.FindTexture(typePath.Value);
                iconTextures[typePath.Key] = texture;
            }

            _hasBeenCached = true;
        }

        private void OnEnable()
        {
            EditorApplication.update += CloseIfNotFocused;
        }

        private void OnDisable()
        {
            EditorApplication.update -= CloseIfNotFocused;
        }

        private void CloseIfNotFocused()
        {
            if (EditorWindow.focusedWindow != this)
            {
                this.Close();
                EditorApplication.update -= CloseIfNotFocused;
            }
        }

        private const int width = 8;
        private const int leftOffset = 4;

        private void OnGUI()
        {
            var buttonStyle = EditorStyles.miniButton;

            CacheIconTextures();

            EditorGUILayout.BeginHorizontal();

            //leftOffset = EditorGUILayout.IntField(leftOffset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical();

            if (GUILayout.Button("Clear", buttonStyle))
            {
                foreach (var gameObject in Selection.gameObjects)
                {
                    SetGameObjectIconByTexture(gameObject, null);
                }
            }

            var rect = EditorGUILayout.GetControlRect();
            rect.width /= width;

            rect.height *= 2;

            var keys = iconTextures.Keys.ToArray();

            var baseRectY = rect.y;

            rect.x = 0;


            for (int i = 0; i < keys.Length; i++)
            {
                var heightThing = Mathf.Floor(i * (1f / width));
                rect.y = baseRectY + rect.height * heightThing;
                rect.x = rect.width * (i % width) + leftOffset;
                var iconType = keys[i];
                var texture = iconTextures[iconType];

                if (GUI.Button(rect, texture, buttonStyle))
                {
                    foreach (var gameObject in Selection.gameObjects)
                    {
                        SetGameObjectIconByType(gameObject, iconType);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }
        
        public static void SetGameObjectIconByType(GameObject go, string iconType)
        {
            Texture2D texture = EditorGUIUtility.FindTexture(_IconTypePaths[iconType]);

            SetGameObjectIconInternal(go, texture);
        }

        public static void SetGameObjectIconByPath(GameObject go, string texturePath)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            SetGameObjectIconInternal(go, texture);
        }

        public static void SetGameObjectIconByTexture(GameObject go, Texture2D texture)
        {
            SetGameObjectIconInternal(go, texture);
        }
        private static void SetGameObjectIconInternal(GameObject go, Texture texture)
        {
            var so = new SerializedObject(go);
            var iconProperty = so.FindProperty("m_Icon");
            iconProperty.objectReferenceValue = texture;
            so.ApplyModifiedProperties();
        }

        private void HandleInput()
        {
            Event current = Event.current;

            if (current.type != EventType.KeyDown)
            {
                return;
            }

            if (current.keyCode == KeyCode.Escape)
            {
                this.Close();
            }
            else if (current.keyCode == KeyCode.Return && current.control)
            {
                // Close immediately if Ctrl+Enter is pressed

                this.Close();
            }
            else if (current.keyCode == KeyCode.Return)
            {
            }
        }
    }
}