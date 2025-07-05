using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;

namespace BearsEditorTools
{
    public class QuickComponentReorderer : EditorWindow
    {
        private readonly Color buttonHighlightColor = new Color(0.4f, 1f, 1f);

        private static Vector2 scroller;

        private List<Component> componentList = new List<Component>();

        private int componentToMove = 0;
        private int componentsDesiredPosition = 0;

        private bool firstPicked = false;

        private bool help = false;

        private static QuickComponentReorderer window;

        [Shortcut("Bears/QuickTools/Quick Component Reorderer")]
        public static void OpenWindow()
        {
            window = ScriptableObject.CreateInstance<QuickComponentReorderer>();
            window.ShowPopup();
            window.Focus();
            Rect windowRect = window.position;

            if (Event.current != null)
            {
                var mousePosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                windowRect.x = mousePosition.x - windowRect.width * 0.5f;
                windowRect.y = mousePosition.y;

                //TODO: Get correct screen size... Screen.width returned 452 on my current setup, which is very wrong.
                //var screenOvershoot = (windowRect.x + windowRect.width) - Screen.width;
                //if (screenOvershoot > 0f)
                //{
                //	windowRect.x -= screenOvershoot;
                //}
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
                EditorSceneManager.GetSceneManagerSetup();
            }
        }

        private void OnGUI()
        {
            if (Selection.activeGameObject == null)
            {
                EditorGUILayout.HelpBox("Please select one, and only one, gameobject.", MessageType.Info);

                return;
            }

            titleContent.text = "Component Reorderer";

            scroller = EditorGUILayout.BeginScrollView(scroller);

            GetComponentsFromActiveObject();

            if (help)
            {
                EditorGUILayout.HelpBox(
                    "To reorder components, first click the one you want to move, then click the one in the spot you want to move it to.",
                    MessageType.Info);
                EditorGUILayout.HelpBox("This can NOT be undone.", MessageType.Warning);
            }
            // Starts at 1 to avoid listing the Transform component. Unity doesn't like it if you try to move that one...
            for (int i = 1; i < componentList.Count; i++)
            {
                var component = componentList[i];
                if (componentToMove == i && firstPicked)
                {
                    GUI.color = buttonHighlightColor;
                }

                EditorGUILayout.BeginHorizontal();

                {
                    var rect = EditorGUILayout.GetControlRect();

                    rect.width -= 30f;

                    var buttonLabel = component.GetType().ToString();

                    if (buttonLabel.Contains('.'))
                    {
                        var chopIndex = buttonLabel.LastIndexOf('.') + 1;
                        buttonLabel = buttonLabel.Substring(chopIndex, buttonLabel.Length - chopIndex);
                    }

                    if (component == null)
                    {
                        var originalColor = GUI.color;
                        GUI.color = Color.grey;
                        GUI.Button(rect, "COMPONENT IS NULL", EditorStyles.toolbarButton);
                        GUI.color = originalColor;
                    }
                    else if (GUI.Button(rect, buttonLabel, EditorStyles.toolbarButton))
                    {
                        if (!firstPicked)
                        {
                            componentToMove = i;
                            firstPicked = true;
                            continue;
                        }
                        else
                        {
                            componentsDesiredPosition = i;
                            UpdateComponentOrder();
                            firstPicked = false;
                            componentToMove = -1;
                            componentsDesiredPosition = -1;
                        }
                    }

                    //rect.x += rect.width;
                    //rect.width = 30f;
                    //if (GUI.Button(rect, "X", EditorStyles.toolbarButton))
                    //{
                    //	throw new NotImplementedException();
                    //	// DestroyImmediate(component);
                    //}
                }

                EditorGUILayout.EndHorizontal();

                GUI.color = Color.white;
            }

            EditorGUILayout.EndScrollView();

            help = EditorGUILayout.Toggle("Toggle help", help);
        }

        private void GetComponentsFromActiveObject()
        {
            Selection.activeGameObject.GetComponents(componentList);
        }

        private void MoveComponent(Component component, int desiredPosition)
        {
            // TODO: Make undo work.
            //Undo.RecordObject(Selection.ActiveGameobject, string.Format("Reorder Components ({0})", component.GetType().ToString()));

            List<Component> tempComponentList = new List<Component>();
            Selection.activeGameObject.GetComponents(tempComponentList);
            while (tempComponentList.IndexOf(component) != desiredPosition)
            {
                int index = tempComponentList.IndexOf(component);

                if (index > desiredPosition)
                {
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(component);
                }
                if (index < desiredPosition)
                {
                    UnityEditorInternal.ComponentUtility.MoveComponentDown(component);
                }
                Selection.activeGameObject.GetComponents(tempComponentList);
            }
        }

        private void UpdateComponentOrder()
        {
            for (int i = 0; i < componentList.Count; i++)
            {
                if (i != componentToMove) continue;

                var c = componentList[i];
                MoveComponent(c, componentsDesiredPosition);
            }
        }
    }
}