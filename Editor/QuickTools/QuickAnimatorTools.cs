using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using UnityEditor.Animations;
using UnityEditor.ShortcutManagement;

namespace BearsEditorTools
{
    public class QuickAnimatorTools : EditorWindow
    {
        const string TextFieldControlName = "text field pls";

        private string input = "";
        private string lowerCaseInput = "";

        private int selectedParameter;

        private AnimatorController controller;

        private List<AnimatorControllerParameter> parameters = new List<AnimatorControllerParameter>();
        private List<AnimatorControllerParameter> filteredParameters = new List<AnimatorControllerParameter>();

        [Shortcut("Bears/QuickTools/Quick Animator Tools", KeyCode.Q, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        public static void OpenWindow()
        {
            Debug.Log("Trying to open animator tool window...");
            if (Selection.objects.Length == 0) return;

            QuickAnimatorTools window = ScriptableObject.CreateInstance<QuickAnimatorTools>();
            window.ShowPopup();
            window.FindParameters();
            window.Focus();
            Rect position = window.position;
            position.x = Screen.width * 0.5f - position.width * 0.5f;
            position.y = Screen.height * 0.5f - position.height * 0.5f;

            position.x = position.x + position.width * 0.5f;
            position.y = position.y + position.height * 0.5f;

            position.height = 200f;

            window.position = position;
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

            else if (current.keyCode == KeyCode.Return)
            {
                if (Event.current.alt)
                {
                    RemoveCondition(filteredParameters[selectedParameter].name);
                    return;
                }
                if (Event.current.shift)
                {
                    AddCondition(filteredParameters[selectedParameter].name);
                    return;
                }
                DebugTransition();
            }
            else if (current.keyCode == KeyCode.DownArrow)
            {
                ++selectedParameter;
                selectedParameter = Mathf.Clamp(selectedParameter, 0, filteredParameters.Count() - 1);
            }
            else if (current.keyCode == KeyCode.UpArrow)
            {
                --selectedParameter;
                selectedParameter = Mathf.Clamp(selectedParameter, 0, filteredParameters.Count() - 1);
            }
        }

        private void DebugTransition()
        {
            var transition = Selection.activeObject as AnimatorStateTransition;
            if (transition == null)
            {
                return;
            }

            foreach (var condition in transition.conditions)
            {
                Debug.Log(string.Format("Transition condition: {0} - {1} - {2}", condition.parameter, condition.mode,
                    condition.threshold));
            }
        }

        private void AddCondition(string conditionName)
        {
            var transition = Selection.activeObject as AnimatorStateTransition;
            if (transition == null)
            {
                return;
            }

            transition.AddCondition(AnimatorConditionMode.If, 0, conditionName);
        }

        private void RemoveCondition(string conditionToRemove)
        {
            var transition = Selection.activeObject as AnimatorStateTransition;
            if (transition == null)
            {
                Debug.Log("Nope.");
                return;
            }
            var conditionsToRemove = transition.conditions.Where(c => c.parameter == conditionToRemove).ToList();

            foreach (var c in conditionsToRemove)
            {
                transition.RemoveCondition(c);
            }
        }

        private void FindParameters()
        {
            if (GetController() == false) return;

            parameters.Clear();

            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                parameters.Add(p);
            }

            FindFilteredParameters();
        }

        private void FindFilteredParameters()
        {
            filteredParameters = parameters
                .Where(parameter => MatchesFilter(parameter.name, lowerCaseInput))
                //This makes scene files in the root scene folder appear first.
                //And order by string comparison difference (So that OS doesn't prioritize M"OS"aic)
                .OrderBy(parameter => (parameter.name.CompareTo(lowerCaseInput) < 0) ? 1 : -1)
                .ToList();
        }

        private bool GetController()
        {
            controller = null;

            controller = GetCurrentController();

            return controller != null;
        }

        private static EditorWindow _animatorWindow;

        private static EditorWindow AnimatorWindow
        {
            get
            {
                if (_animatorWindow != null) return _animatorWindow;

                var name = "UnityEditor.Graphs.AnimatorControllerTool";

                // Get assembly that type is a part of
                Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
                Debug.Log(assembly);
                // Get the type
                Type type = assembly.GetType(name);
                Debug.Log(type);
                // Use the type to get the right window
                EditorWindow animatorWindow = EditorWindow.GetWindow(type);

                _animatorWindow = animatorWindow;

                Debug.Log(animatorWindow);

                return animatorWindow;
            }
        }

        static AnimatorController GetCurrentController()
        {
            AnimatorController controller = null;
            var tool = EditorWindow.mouseOverWindow;

            if (tool == null) return null;


            var toolType = tool.GetType();
            var controllerProperty = toolType.GetProperty("animatorController",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (controllerProperty != null)
            {
                controller = controllerProperty.GetValue(tool, null) as AnimatorController;
            }
            return controller;
        }

        private void OnGUI()
        {
            //if (Event.current != null)
            //{
            //	var screen = Event.current.displayIndex;
            //	if (screen == 0)
            //	{
            //		var pos = position;
            //		var mousePos = Event.current.mousePosition;
            //		pos.x = mousePos.x;
            //		pos.y = mousePos.y;
            //		position = pos;
            //	}
            //
            //}

            if (!GetController()) return;

            if (parameters == null)
            {
                EditorGUILayout.LabelField(
                    "Could not find parameters.\nMake sure mouse is over animator window when opening...");
                return;
            }

            HandleInput();
            EditorGUILayout.LabelField("Edit Transition Parameters");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Filter", GUILayout.Width(45.0f));

            GUI.SetNextControlName(TextFieldControlName);
            input = EditorGUILayout.TextField(input);
            //Always want to focus the text field, nothing else.
            EditorGUI.FocusTextInControl(TextFieldControlName);

            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                lowerCaseInput = input.ToLower();
                FindFilteredParameters();
                selectedParameter = 0;
            }

            for (int i = 0; i < filteredParameters.Count; ++i)
            {
                bool isCurrentSelected = selectedParameter == i;

                AnimatorControllerParameter parameter = filteredParameters[i];

                string displayedName = string.Format("{0}", parameter.name);

                GUIContent label = new GUIContent(displayedName, parameter.name);

                GUIStyle s = GUI.skin.label;

                if (isCurrentSelected)
                {
                    //This bolds the currently selected element, making it easy to see what's selected.
                    s.fontStyle = FontStyle.Bold;
                }

                if (GUILayout.Button(label, s))
                {
                    AddCondition(filteredParameters[i].name);
                }
            }
        }

        /// <summary>
        /// Is the predicate contained in the text?
        /// </summary>
        private static bool MatchesFilter(string text, string predicate)
        {
            if (string.IsNullOrEmpty(predicate))
            {
                return true;
            }

            foreach (string word in predicate.Split(' '))
            {
                if (!text.ToLower().Contains(word))
                {
                    //Found a word the name didn't match, and needs to match all words.
                    return false;
                }
            }
            return true;
        }
    }
}