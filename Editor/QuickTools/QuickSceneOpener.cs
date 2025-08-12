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
            public bool favorited;
            public bool ignored;
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

            const float repaintInterval = 0.05f;
            if (EditorApplication.timeSinceStartup % repaintInterval < 0.01f)
            {
                Repaint();
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
                
                s.favorited = QuickSceneOpenerFavorites.IsFavorite(s.path);
                
                s.ignored = IsIgnoredScene(s.path);

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
            
            string[] ignoredPaths = ignoredScenes.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
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
                .Where(scene => !scene.ignored || scene.favorited) // Filter out ignored scenes, keep favorited scenes
                .Where(scene => MatchesFilter(scene.lowerCaseName, lowerCaseCurrentFilter))
                .OrderBy(scene => scene.favorited)
                //This makes scene files in the root scene folder appear first.
                .ThenBy(scene => IsInRootSceneFolder(scene.path) ? -1 : 1)
                //And order by string comparison difference (So that OS doesn't prioritize M"OS"aic)
                .ThenBy(scene => String.Compare(scene.name, lowerCaseCurrentFilter, StringComparison.OrdinalIgnoreCase) < 0 ? 1 : -1)
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
           OpenReplace(filteredScenes.Values.ElementAt(index));
        }   
        
        private void OpenReplace(SceneOpenerScene sceneOpenerScene)
        {
            EditorSceneManager.OpenScene(sceneOpenerScene.path);
        }

        private void OpenAdditive(int index)
        {
            if (index < 0 || index >= filteredScenes.Count)
            {
                Debug.LogWarning("Index out of range for opening scene additively: " + index);
                return;
            }
            
            OpenAdditive(filteredScenes.Values.ElementAt(index));
        }
        
        private void OpenAdditive(SceneOpenerScene sceneOpenerScene)
        {
            EditorSceneManager.OpenScene(sceneOpenerScene.path, OpenSceneMode.Additive);
        }

        private void RemoveFromLoaded(int index)
        {
            if (index < 0 || index >= filteredScenes.Count)
            {
                Debug.LogWarning("Index out of range for removing scene from loaded: " + index);
                return;
            }
            
            RemoveFromLoaded(filteredScenes.Values.ElementAt(index));
        }
        
        private void RemoveFromLoaded(SceneOpenerScene sceneOpenerScene)
        {
            EditorSceneManager.CloseScene(sceneOpenerScene.scene, true);
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
        static Color HeaderColor = new Color(0.69f, 0.9f, 0.93f);

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
                    FindScenes();
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

            Dictionary<string, SceneOpenerScene>.ValueCollection filteredScenesValues = filteredScenes.Values;
            
            for (int i = 0; i < filteredScenesValues.Count; ++i)
            {
                SceneOpenerScene scene = filteredScenesValues.ElementAt(i);
                
                if (!scene.favorited)
                    continue;

                if (lastHeader != scene.directory)
                {
                    GUIContent headerContent = new GUIContent(scene.directory, scene.directory);
                    
                    lastHeader = DrawHeader(headerContent);

                    i--; // Don't increment i, so we can show the next scene in the same directory.
                    
                    continue;
                }
                
                DrawSceneLine(scene, style, i);
            }
            
            // draw horizontal line after favs
            
            // GUILayout.Space(7);
            Rect lineRect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            GUI.color = Color.gray;
            EditorGUI.DrawRect(lineRect, Color.white);
            // GUILayout.Space(3); 
                
            lastHeader = "";

            for (int i = 0; i < filteredScenesValues.Count; ++i)
            {
                SceneOpenerScene scene = filteredScenesValues.ElementAt(i);

                if (scene.favorited)
                    continue;
                
                if (lastHeader != scene.directory)
                {
                    GUIContent headerContent = new GUIContent(scene.directory, scene.directory);
                    
                    lastHeader = DrawHeader(headerContent);

                    i--; // Don't increment i, so we can show the next scene in the same directory.
                    
                    continue;
                }

                
                DrawSceneLine(scene, style, i);
            }
            EditorGUILayout.EndScrollView();
            
            GUI.color = Color.white;
            
            if(GUILayout.Button("Settings", EditorStyles.miniButton))
            {
                _showSettings = !_showSettings;
            }
        }

        private void DrawSceneLine(SceneOpenerScene scene, GUIStyle style, int index)
        {
            bool isCurrentSelected = selectionID == index;
            
            const float lineHeight = 13;
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(lineHeight));
            
            Rect mouseCheckRect = rect;
            // make it a bit taller
            mouseCheckRect.height += 4;
            mouseCheckRect.y -= 2;
            bool mouseover = mouseCheckRect.Contains(Event.current.mousePosition);
            
            // Draw a rect with a different color if the mouse is over it
            if (mouseover)
            {
                GUI.Box(rect, "", "box");
            }
                
            Rect infoRect = rect;
            infoRect.width = lineHeight;
            // if loaded, show checkmark
            EditorGUI.LabelField(infoRect, scene.scene.isLoaded ? "✔" : "", EditorStyles.miniLabel);

            GUI.color = Color.white;

            rect.x += lineHeight;
            rect.width -= lineHeight;

            const float buttonWidth = 18f;
            const float allButtonWidth = buttonWidth * 4;
            rect.width -= allButtonWidth;

            string displayedName = $"{scene.name}";

            GUIContent label = new GUIContent(displayedName);

            EditorGUILayout.BeginHorizontal();

            style.fontStyle = isCurrentSelected ? FontStyle.Bold : FontStyle.Normal;
            GUI.color = isCurrentSelected ? Color.yellow : Color.white;

            // style.fixedWidth = position.width - buttonWidth * 2;

            if (GUI.Button(rect, label, style))
            {
                
                selectionID = index;
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path));
                //OpenAdditive(i);
            }
                
            // Open context menu on right click
            if ((Event.current.button == 1 || Event.current.type == EventType.ContextClick) && rect.Contains(Event.current.mousePosition))
                ShowContextMenu(scene);
                
            Rect rightRect = rect;
            rightRect.x += rect.width;
            rightRect.width = buttonWidth;

            rightRect.y -= 4;

                
            if (GUI.Button(rightRect, new GUIContent("+", "Open this scene additively"), EditorStyles.miniButtonRight))
            {
                OpenAdditive(scene);
                FindScenes();
            }

            rightRect.x += rightRect.width;
            GUI.enabled = scene.scene.isLoaded && EditorSceneManager.loadedRootSceneCount > 1;
            if (GUI.Button(rightRect, new GUIContent("-", "Remove this scene from the loaded scenes"), EditorStyles.miniButtonMid))
            {
                RemoveFromLoaded(scene);
                FindScenes();
            }
            GUI.enabled = true;

            rightRect.x += rightRect.width;
                
            if (GUI.Button(rightRect, new GUIContent("↷", "Replace all scenes with this one"), EditorStyles.miniButtonLeft))
            {
                OpenReplace(scene);
                FindScenes();
            }
            
            rightRect.x += rightRect.width;
            // fav button. yellow if favorited, gray if not.
            if (scene.favorited)
            {
                GUI.contentColor = Color.yellow;
                if (GUI.Button(rightRect, new GUIContent("★", "Remove from favorites"), EditorStyles.miniButtonLeft))
                {
                    QuickSceneOpenerFavorites.RemoveFavorite(scene.path);
                    FindScenes();
                }
            }
            else
            {
                GUI.contentColor = new Color(1, 1, 1, 0.2f);
                if (GUI.Button(rightRect, new GUIContent("★", "Add to favorites"), EditorStyles.miniButtonLeft))
                {
                    QuickSceneOpenerFavorites.AddFavorite(scene.path);
                    FindScenes();
                }
            }
            
            GUI.contentColor = Color.white;
            
            

            EditorGUILayout.EndHorizontal();
        }

        private static string DrawHeader(GUIContent headerContent)
        {
            GUILayout.Space(3);
            Rect headerRect = EditorGUILayout.GetControlRect(GUILayout.Height(10));

            GUI.color = HeaderColor;

            EditorGUI.LabelField(headerRect, headerContent, EditorStyles.miniLabel);
                    
            GUILayout.Space(3);
            
            return headerContent.text;
        }

        private void ShowContextMenu(SceneOpenerScene scene)
        {
            GenericMenu menu = new GenericMenu();
            
                        
            // favs
            if (QuickSceneOpenerFavorites.IsFavorite(scene.path))
            {
                menu.AddItem(new GUIContent($"Remove from Favorites ({scene.name})"), false, () =>
                {
                    QuickSceneOpenerFavorites.RemoveFavorite(scene.path);
                    FindScenes();
                });
            }
            else
            {
                menu.AddItem(new GUIContent($"Add to Favorites ({scene.name})"), false, () =>
                {
                    QuickSceneOpenerFavorites.AddFavorite(scene.path);
                    FindScenes();
                });
            }
            
            
            // sep
            menu.AddSeparator("");

            string directoryPattern = $"^{scene.directory.Replace("\\", "/")}/.*$"; // Match all files in the directory
            string scenePattern = $"^{scene.path.Replace("\\", "/")}$";
            
            
            menu.AddItem(new GUIContent($"Ignore File ({scenePattern.Replace("/", "∕")})"), false, () =>
            {
                AddIgnorePattern(scenePattern); // Use ^ and $ to match the full path
            });
            
            menu.AddItem(new GUIContent($"Ignore Directory ({directoryPattern.Replace("/", "∕")})"), false, () =>
            {
                AddIgnorePattern(directoryPattern);
            });
            

            
            menu.ShowAsContext();
        }

        private void AddIgnorePattern(string regexPatternForFullScenePath)
        {
            string ignoredScenes = EditorPrefs.GetString(GetIgnoredScenesProjectPrefKey(), DEFAULT_IGNORE_PATTERN);
            if (!string.IsNullOrEmpty(ignoredScenes))
            {
                ignoredScenes += "\n";
            }

            ignoredScenes += regexPatternForFullScenePath;
            EditorPrefs.SetString(GetIgnoredScenesProjectPrefKey(), ignoredScenes);
                
            FindScenes();
        }
    }

    public static class QuickSceneOpenerFavorites
    {
        public static bool IsFavorite(string scenePath)
        {
            return EditorPrefs.GetString(
                GetFavoriteScenesProjectPrefKey(), "")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(favorite => favorite.Equals(scenePath, StringComparison.OrdinalIgnoreCase));
            
        }
        
        public static void AddFavorite(string scenePath)
        {
            if (IsFavorite(scenePath))
                return;

            string favorites = EditorPrefs.GetString(GetFavoriteScenesProjectPrefKey(), "");
            if (!string.IsNullOrEmpty(favorites))
            {
                favorites += ";";
            }
            favorites += scenePath;
            EditorPrefs.SetString(GetFavoriteScenesProjectPrefKey(), favorites);
        }
        
        public static void RemoveFavorite(string scenePath)
        {
            string favorites = EditorPrefs.GetString(GetFavoriteScenesProjectPrefKey(), "");
            if (string.IsNullOrEmpty(favorites))
                return;

            var favoriteList = favorites.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            favoriteList.RemoveAll(favorite => favorite.Equals(scenePath, StringComparison.OrdinalIgnoreCase));
            EditorPrefs.SetString(GetFavoriteScenesProjectPrefKey(), string.Join(";", favoriteList));
        }
        
        public static void ClearFavorites()
        {
            EditorPrefs.DeleteKey(GetFavoriteScenesProjectPrefKey());
        }
        
        private static string GetFavoriteScenesProjectPrefKey()
        {
            return "BearsEditorTools.QuickSceneOpener.FavoriteScenes." + Application.productName;
        }
    }
}