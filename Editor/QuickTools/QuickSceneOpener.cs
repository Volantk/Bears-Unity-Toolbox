using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine.SceneManagement;

namespace BearsEditorTools
{
    public class QuickSceneOpener : EditorWindow
    {
        const string TextFieldControlName = "text field pls";

        private struct SceneOpenerScene
        {
            public string name;
            public string lowerCaseName;
            public string directory;
            public string path;
            public Scene scene;
        }

        private readonly List<SceneOpenerScene> scenes = new List<SceneOpenerScene>();

        private Dictionary<string, SceneOpenerScene> filteredScenes = new();

        private string currentFilter = "";
        private string lowerCaseCurrentFilter = "";

        private int selectionID = 0;

        private enum SceneOpenerMode
        {
            Scene = 0,
        }

//        private SceneOpenerMode mode;

        const float WindowWidth = 400;

        [Shortcut("Bears/QuickTools/Quick Scene Opener", KeyCode.S, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void OpenWindow()
        {
            QuickSceneOpener window = ScriptableObject.CreateInstance<QuickSceneOpener>();
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

            windowRect.width = WindowWidth;
            window.position = windowRect;
        }

        private void OnEnable()
        {
            FindScenes();

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

        private void FindScenes()
        {
            scenes.Clear();
            
            string[] sceneGuids = AssetDatabase.FindAssets("t:SceneAsset");
            
            foreach (string guid in sceneGuids)
            {
                SceneOpenerScene s = new SceneOpenerScene();
                
                s.path = AssetDatabase.GUIDToAssetPath(guid);

                s.name = s.path.Substring(0, s.path.LastIndexOf('.'));
                if (s.name.Contains("/"))
                {
                    s.name = s.name.Substring(s.name.LastIndexOf('/') + 1);
                }
                
                s.directory = s.path.Substring(0, s.path.LastIndexOf('/'));
                
                s.lowerCaseName = s.name.ToLower();

                s.scene = SceneManager.GetSceneByPath(s.path);

                scenes.Add(s);
            }

            FindFilteredScenes();
        }

        private bool _showSettings;

        private static bool IsInRootSceneFolder(string path)
        {
            return Path.GetDirectoryName(path) == "Assets/Scenes";
        }

        const string DEFAULT_IGNORE_PATTERN = "Packages/*\n[eE]xample";

        // Editor pref for ignoring scenes
        private static bool IsIgnoredScene(string path)
        {
            // if pref is not set return false
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (!EditorPrefs.HasKey(GetIgnoredScenesProjectPrefKey()))
                return false;

            string ignoredScenes = EditorPrefs.GetString(GetIgnoredScenesProjectPrefKey(), DEFAULT_IGNORE_PATTERN);
            
            if (string.IsNullOrEmpty(ignoredScenes))
            {
                return false;
            }
            
            string[] ignoredPaths = ignoredScenes.Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            Debug.Log(ignoredPaths.Length);
           
            // use regex to check if the path matches any of the ignored paths, split at newline
            foreach (string ignoredPath in ignoredPaths)
            {
                var regex = new System.Text.RegularExpressions.Regex(ignoredPath.Trim(), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (regex.IsMatch(path))
                {
                    numIgnored++;
                    return true;
                }
            }
            
            return false;
            
        }
        
        private static string GetIgnoredScenesProjectPrefKey()
        {
            return "BearsEditorTools.QuickSceneOpener.IgnoredScenes." + Application.productName;
        }
        
        static int numIgnored = 0;

        private void FindFilteredScenes()
        {
            numIgnored = 0;
            
            var s = scenes
                .Where(scene => !IsIgnoredScene(scene.path))
                .Where(scene => MatchesFilter(scene.lowerCaseName, lowerCaseCurrentFilter))
                //This makes scene files in the root scene folder appear first.
                .OrderBy(scene => IsInRootSceneFolder(scene.path) ? -1 : 1)
                //And order by string comparison difference (So that OS doesn't prioritize M"OS"aic)
                .OrderBy(scene => (scene.name.CompareTo(lowerCaseCurrentFilter) < 0) ? 1 : -1)
                .ToList();
            
            // so to dict
            filteredScenes = new();
            foreach (var scene in s)
            {
                filteredScenes.TryAdd(scene.path, scene);
            }
        }

        private void HandleInput()
        {
            Event current = Event.current;

            if (current.type != EventType.KeyDown)
                return;

            if (current.keyCode == KeyCode.Escape)
            {
                this.Close();
            }
            else if (_showSettings)
            {
                return;
            }
            else if (current.keyCode == KeyCode.RightArrow || current.keyCode == KeyCode.LeftArrow)
            {
                // change mode
            }
            else if (current.keyCode == KeyCode.DownArrow)
            {
                ++selectionID;
                selectionID = Mathf.Clamp(selectionID, 0, filteredScenes.Count() - 1);
            }
            else if (current.keyCode == KeyCode.UpArrow)
            {
                --selectionID;
                selectionID = Mathf.Clamp(selectionID, 0, filteredScenes.Count() - 1);
            }
            else if (current.keyCode == KeyCode.Return)
            {
                if (filteredScenes.Count > selectionID)
                {
                    OpenScene(selectionID);
                }
            }
        }

        private void OpenScene(int selectedIndex, bool? additive = null)
        {
            if (Event.current != null && Event.current.control || (additive.HasValue && !additive.Value))
            {
                OpenReplace(selectedIndex);
            }
            else if (Event.current != null && Event.current.shift || (additive.HasValue && additive.Value))
            {
                OpenAdditive(selectedIndex);
            }
            else if (Event.current != null && Event.current.alt)
            {
                RemoveFromLoaded(selectedIndex);
            }
            else 
            {
                string selectedPath = filteredScenes.ElementAt(selectedIndex).Value.path;
                if (OpenSceneIfUserWantsTo(selectedPath))
                {
                    // If the scene was opened, reset the filter.
                    currentFilter = "";
                    lowerCaseCurrentFilter = "";
                    this.Close();
                }
            }
        }
        
        public static bool OpenSceneIfUserWantsTo(string sceneFile)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(sceneFile);
                return true;
            }
            return false;
        }

        private void OpenReplace(int index)
        {
            EditorSceneManager.OpenScene(filteredScenes.Values.ElementAt(index).path);
        }

        private void OpenAdditive(int index)
        {
            string scenePath = filteredScenes.Values.ElementAt(index).path;
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        private void RemoveFromLoaded(int index)
        {
            EditorSceneManager.CloseScene(filteredScenes.Values.ElementAt(index).scene, true);
            currentFilter = "";
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

        private SceneOpenerMode previousMode;

        private void OnGUI()
        {
            HandleInput();
//            switch (mode)
//            {
//                case SceneOpenerMode.Scene:
                    SceneOpenerGUI();
//                    break;
//                default:
//                    throw new ArgumentOutOfRangeException();
//            }
//            previousMode = mode;
        }

        private Vector2 scroller;

        private void SceneOpenerGUI()
        {
            if (_showSettings)
            {
                EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                
                EditorGUILayout.HelpBox("This is a quick scene opener. It allows you to quickly open scenes by filtering them by name.", MessageType.Info);
                
                // show textfield for editing the editor pref for ignored scene paths
                EditorGUILayout.LabelField("Ignored Scenes (regex, 1 rule per line)", EditorStyles.boldLabel);
                string ignoredScenes = EditorPrefs.GetString(GetIgnoredScenesProjectPrefKey(), DEFAULT_IGNORE_PATTERN);
                
                using (var x = new EditorGUI.ChangeCheckScope())
                {
                    ignoredScenes = EditorGUILayout.TextArea(ignoredScenes);

                    bool didReset = false;
                    if (GUILayout.Button("Reset", EditorStyles.miniButton))
                    {
                        ignoredScenes = DEFAULT_IGNORE_PATTERN;
                        didReset = true;
                    }
                    
                    if (x.changed || didReset)
                    {
                        EditorPrefs.SetString(GetIgnoredScenesProjectPrefKey(), ignoredScenes);
                    }
                }

                if(GUILayout.Button("Close", EditorStyles.miniButton))
                {
                    _showSettings = false;
                }
                
                return;
            }
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();

            var filterRect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            
            GUI.SetNextControlName(TextFieldControlName);
            currentFilter = EditorGUI.TextField(filterRect, currentFilter);
            if (currentFilter == "")
            {
                // Draw a label in the text field when it is empty.
                filterRect.x += 5; // Offset the label a bit to the right.
                GUI.color = new Color(1, 1, 1, 0.3f);
                GUI.Label(filterRect, "Filter scenes by name...", EditorStyles.label);
                GUI.color = Color.white;
            }
            
            //Always want to focus the text field, nothing else.
            EditorGUI.FocusTextInControl(TextFieldControlName);

            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                lowerCaseCurrentFilter = currentFilter.ToLower();
                FindFilteredScenes();
                selectionID = 0;
            }

            scroller = EditorGUILayout.BeginScrollView(scroller);
            string lastHeader = "";
            
            GUIStyle style = new GUIStyle(GUI.skin.label);
            // style.alignment = TextAnchor.UpperCenter;
            style.clipping = TextClipping.Overflow;

            Color headerColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            for (int i = 0; i < filteredScenes.Count; ++i)
            {
                SceneOpenerScene scene = filteredScenes.Values.ElementAt(i);
                
                if (lastHeader != scene.directory)
                {
                    GUILayout.Space(3);
                    Rect headerRect = EditorGUILayout.GetControlRect(GUILayout.Height(10));

                    lastHeader = scene.directory;
                    GUI.color = headerColor;
                    
                    EditorGUI.LabelField(headerRect, new GUIContent(scene.directory, scene.directory), EditorStyles.miniLabel);
                    i--; // Skip the next iteration, since we already processed this scene.
                    
                    GUILayout.Space(3);

                    continue;
                }

                const float lineHeight = 13;
                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(lineHeight));
                
                Rect infoRect = rect;
                infoRect.width = lineHeight;
                // if loaded, show checkmark
                EditorGUI.LabelField(infoRect, scene.scene.isLoaded ? "✔" : "", EditorStyles.miniLabel);

                GUI.color = Color.white;

                rect.x += lineHeight;
                rect.width -= lineHeight;

                const float buttonWidth = 18f;
                const float allButtonWidth = buttonWidth * 3;
                rect.width -= allButtonWidth;
                
                bool isCurrentSelected = selectionID == i;

                string displayedName = $"{scene.name}";

                GUIContent label = new GUIContent(displayedName);

                EditorGUILayout.BeginHorizontal();

                style.fontStyle = isCurrentSelected ? FontStyle.Bold : FontStyle.Normal;
                GUI.color = isCurrentSelected ? Color.yellow : Color.white;

                // style.fixedWidth = position.width - buttonWidth * 2;

                // todo context menu!
                if (GUI.Button(rect, label, style))
                {
                    selectionID = i;
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(filteredScenes.Values.ElementAt(i).path));
                    //OpenAdditive(i);
                }
                
                Rect rightRect = rect;
                rightRect.x += rect.width;
                rightRect.width = buttonWidth;

                
                if (GUI.Button(rightRect, new GUIContent("+", "Open this scene additively"), EditorStyles.miniButtonRight))
                {
                    OpenAdditive(i);
                    FindScenes();
                }

                rightRect.x += rightRect.width;
                GUI.enabled = scene.scene.isLoaded && EditorSceneManager.loadedRootSceneCount > 1;
                if (GUI.Button(rightRect, new GUIContent("-", "Remove this scene from the loaded scenes"), EditorStyles.miniButtonMid))
                {
                    RemoveFromLoaded(i);
                    FindScenes();
                }
                GUI.enabled = true;

                rightRect.x += rightRect.width;
                
                if (GUI.Button(rightRect, new GUIContent("↷", "Replace all scenes with this one"), EditorStyles.miniButtonLeft))
                {
                    OpenReplace(i);
                    FindScenes();
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            
            GUI.color = Color.white;
            
            if(GUILayout.Button("Settings", EditorStyles.miniButton))
            {
                _showSettings = !_showSettings;
            }
        }
    }
}