using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;

namespace BearsEditorTools
{
    public class QuickTransformer : EditorWindow
    {
        /*
        private Vector2 scroller;

        [Shortcut("Bears/QuickTools/Quick Transformer", KeyCode.T, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        public static void OpenWindow()
        {
            if (Selection.gameObjects.Length == 0) return;
            QuickTransformer window = ScriptableObject.CreateInstance<QuickTransformer>();
            window.ShowPopup();
            window.Focus();
            Rect position = window.position;
            position.x = Screen.width * 0.5f - position.width * 0.5f;
            position.y = Screen.height * 0.5f - position.height * 0.5f;
            position.height = 500f;
            window.position = position;

            Gridify.Initialize();
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
                //Reset();
                Close();
                EditorApplication.update -= CloseIfNotFocused;
            }
        }

        private void Reset()
        {
            //	operation = null;
        }

        private void MoveObjects()
        {
        }

        private void RotateObjects()
        {
        }

        private void ScaleObjects()
        {
        }

        private void OrganizeObjects()
        {
        }

        private static Vector3 positionRange = new Vector3(1, 0, 1);
        private static Vector3 rotationRange = new Vector3(0f, 360f, 0f);
        private static Vector3 scaleRange = new Vector3(0f, 0.1f, 0f);

        private Vector3 Random(float lower, float upper)
        {
            return new Vector3(UnityEngine.Random.Range(lower, upper), UnityEngine.Random.Range(lower, upper), UnityEngine.Random.Range(lower, upper));
        }

        private Vector3 Random(Vector3 lower, Vector3 upper)
        {
            return new Vector3(UnityEngine.Random.Range(lower.x, upper.x), UnityEngine.Random.Range(lower.y, upper.y), UnityEngine.Random.Range(lower.z, upper.z));
        }

        private void OnGUI()
        {
            DrawRandomizationGUI();
            if (GUILayout.Button("Drop To Ground", EditorStyles.miniButton))
            {
                foreach (var transform in Selection.transforms)
                {
                    Undo.RecordObject(transform, "Drop To Ground");
                    BearsBagOfTricks.DropToGround(false);
                }
            }

            if (GUILayout.Button("Drop To Ground (Bounds)", EditorStyles.miniButton))
            {
                foreach (var transform in Selection.transforms)
                {
                    Undo.RecordObject(transform, "Drop To Ground");
                    BearsBagOfTricks.DropToGround(true);
                }
            }

            Gridify.DrawGUI();
        }

        private void DrawRandomizationGUI()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Randomization Ranges", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Position", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                foreach (var transform in Selection.transforms)
                {
                    Undo.RecordObject(transform, "Randomize");
                    transform.position += Random(-positionRange, positionRange);
                }
            }

            positionRange = LayoutBetterVector3Field("Position", positionRange);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rotation", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                foreach (var transform in Selection.transforms)
                {
                    Undo.RecordObject(transform, "Randomize");
                    transform.eulerAngles += Random(-rotationRange * 360, rotationRange * 360);
                }
            }

            rotationRange = LayoutBetterVector3Field("Rotation", rotationRange);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scale", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                foreach (var transform in Selection.transforms)
                {
                    Undo.RecordObject(transform, "Randomize");
                    transform.localScale += Random(-scaleRange, scaleRange);
                }
            }

            scaleRange = LayoutBetterVector3Field("Scale", scaleRange);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        
        public static Vector3 LayoutBetterVector3Field(string label, Vector3 f)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (label != null)
                {
                    EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(100f));
                }

                var rect = EditorGUILayout.GetControlRect();

                rect.width *= 0.3333f;
                f.x        =  EditorGUI.FloatField(rect, f.x);
                rect.x     += rect.width;
                f.y        =  EditorGUI.FloatField(rect, f.y);
                rect.x     += rect.width;
                f.z        =  EditorGUI.FloatField(rect, f.z);
            }
            EditorGUILayout.EndHorizontal();

            return f;
        }
        */

    }
}