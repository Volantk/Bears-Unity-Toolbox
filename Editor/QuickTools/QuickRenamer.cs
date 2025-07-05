using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.ShortcutManagement;
using Object = UnityEngine.Object;

namespace BearsEditorTools
{
    public class QuickRenamer : EditorWindow
    {
        const string TextFieldControlName = "text field pls";

        private string renameToString = "";

        private const string key_NameOfSelf = "..";
        private const string key_ParentName = ",,";
        private const string key_Rename = "R::";
        private const string key_RootName = "^";
        private const string key_Enumerate = "#";

        private int enumerator;

        [Shortcut("Bears/QuickTools/Quick Renamer", KeyCode.R, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        public static void OpenWindow()
        {
            if (Selection.instanceIDs.Length == 0) return;
            QuickRenamer window = ScriptableObject.CreateInstance<QuickRenamer>();
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

            window.position = windowRect;
        }

        private void OnEnable()
        {
            if (Selection.objects.Length == 1)
            {
                renameToString = Selection.objects[0].name;
            }

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

        private void OnGUI()
        {
            HandleInput();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Rename", GUILayout.Width(60.0f));

            GUI.SetNextControlName(TextFieldControlName);
            renameToString = EditorGUILayout.TextField(renameToString);
            //Always want to focus the text field, nothing else.
            EditorGUI.FocusTextInControl(TextFieldControlName);

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndChangeCheck();

            EditorGUILayout.BeginVertical();
            var rect = EditorGUILayout.GetControlRect();
            rect.height *= 6f;
            EditorGUI.LabelField(rect, string.Format("This will rename all selected GameObjects.\n" +
                                                     "{0} = insert current name\n" +
                                                     "{1} = number (will name objects 01, 02, 03, etc.)\n" +
                                                     "{2} = insert parent name\n" +
                                                     "{3} = insert root name\n" +
                                                     "{4}find,replace = Find&Replace syntax\n",
                key_NameOfSelf, key_Enumerate, key_ParentName, key_RootName, key_Rename));

            rect.y += rect.height;
            rect.height /= 3f;
            EditorGUI.LabelField(rect, "Preview:");
            rect.y += rect.height;

            //string textfield = "";


            EditorGUI.LabelField(rect,
                Selection.objects.Length > 0
                    ? ParseInput(Selection.objects[0], renameToString, false)
                    : "Renaming scene, can't preview");

            EditorGUILayout.EndVertical();
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
                RenameSelection();
            }
            else if (current.keyCode == KeyCode.Return)
            {
                RenameSelection();
            }
        }

        private string ParseInput(Object obj, string input, bool increment = true)
        {
            var go = obj as GameObject;

            if (go != null)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return go.name;
                }

                input = DoRename(input, go);

                input = DoAddNameOfSelf(input, go);

                input = DoAddParentName(input, go);

                input = DoAddRootName(input, go);

                input = DoAddEnumeration(input, increment);
            }
            else
            {
                input = DoAddEnumeration(input, increment);
            }

            return input;
        }

        private void RenameSelection()
        {
            enumerator = 0;

            Debug.Log(string.Format("Trying to rename something.\nDebug: {0} gameobjects, {1} objects, {2} instanceIDs",
                Selection.gameObjects.Length, Selection.objects.Length, Selection.instanceIDs.Length));

            if (Selection.gameObjects.Length > 0 && !string.IsNullOrEmpty(renameToString))
            {
                var selectedObjectsSorted = Selection.gameObjects.OrderBy(o => o.transform.GetSiblingIndex()).ThenBy(o => o.transform.parent != null ? o.transform.parent.GetSiblingIndex() : 0).ToArray();
                
                Undo.RecordObjects(Selection.objects, "Rename GameObjects");

                foreach (var go in selectedObjectsSorted)
                {
                    // Check whether gameobject is in a scene or e.g. a model prefab in the project
                    if (go.scene.IsValid())
                    {
                        go.name = ParseInput(go, renameToString);
                    }
                    else
                    {
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(go), ParseInput(go, renameToString));
                    }
                }
            }
            else if (Selection.objects.Length > 0)
            {
                var animatorState = Selection.activeObject as AnimatorState;
                if (animatorState != null)
                {
                    Undo.RecordObject(animatorState, "Rename Animator State");
                    animatorState.name = ParseInput(animatorState, renameToString);
                    return;
                }

                var animatorStateMachine = Selection.activeObject as AnimatorStateMachine;
                if (animatorStateMachine != null)
                {
                    Undo.RecordObject(animatorStateMachine, "Rename Animator StateMachine");
                    animatorStateMachine.name = ParseInput(animatorStateMachine, renameToString);
                    return;
                }

                Debug.Log("Renaming Project Object");

                Undo.RecordObjects(Selection.objects, "Rename GameObjects");

                foreach (var obj in Selection.objects)
                {
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj), ParseInput(obj, renameToString));
                }
            }
            else if (Selection.instanceIDs.Length > 0)
            {
                foreach (var selectedScene in EditorSceneUtility.GetSelectedScenes())
                {
                    var obj = AssetDatabase.LoadAssetAtPath(selectedScene.path, typeof(object));
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj), ParseInput(obj, renameToString));
                }
            }
        }

        private string DoRename(string input, GameObject go)
        {
            if (input.StartsWith(key_Rename))
            {
                input = input.Replace(key_Rename, "");

                if (!input.Contains(",")) return go.name;

                string[] splitInput = input.Split((",").ToCharArray(), 2);

                string findThis = splitInput[0];
                string replaceWithThis = splitInput[1];

                input = go.name.Replace(findThis, replaceWithThis);
            }

            return input;
        }

        private string DoAddNameOfSelf(string input, GameObject go)
        {
            if (input.Contains(key_NameOfSelf))
            {
                input = input.Replace(key_NameOfSelf, go.name);
            }

            return input;
        }

        private string DoAddParentName(string input, GameObject go)
        {
            if (input.Contains(key_ParentName))
            {
                if (go.transform.parent)
                {
                    input = input.Replace(key_ParentName, go.transform.parent.name);
                }
                else
                {
                    input = input.Replace(key_ParentName, "Area");
                }
            }

            return input;
        }

        private string DoAddRootName(string input, GameObject go)
        {
            if (input.Contains(key_RootName))
            {
                input = input.Replace(key_RootName, go.transform.root.name);
            }

            return input;
        }

        private string DoAddEnumeration(string input, bool increment)
        {
            if (input.Contains(key_Enumerate))
            {
                input = input.Replace(key_Enumerate, enumerator.ToString("D2"));

                if (increment)
                {
                    enumerator += 1;
                }
            }

            return input;
        }
    }
}