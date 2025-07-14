using System;
using System.Collections.Generic;
using System.Linq;
using BearsEditorTools;
using UnityEngine;
using Object = UnityEngine.Object;
using TriInspector;
using UnityEditor;

namespace Bears
{
    [CreateAssetMenu(fileName = "New Favorites Collection", menuName = "Bears/Favorites Collection", order = 0)]
    public class FavoritesCollection : ScriptableObject
    {
        public GameObject[] gameobjects;

        public Shader[] shaders;
        
        public Material[] materials;
        
        public Texture[] textures;
        
        public Object[] objects;
        
        public ObjectToFindInScene[] objectsToFindInScene;
        
        [DeclareHorizontalGroup("ObjectToFind", Sizes = new[] { 0f, 50f })]
        [Serializable]
        public struct ObjectToFindInScene
        {
            [Group("ObjectToFind"), LabelWidth(50)]
            public string regex;
            
            [Group("ObjectToFind")]
            [Button]
            public void Select()
            {
                var foundMatches = GetAllObjectNames(regex);
                if(foundMatches == null || foundMatches.Count == 0)
                {
                    Debug.LogWarning($"No objects found matching regex: {regex}");
                    return;
                }
                
                if (foundMatches.Count > 1)
                {
                    string searchTerm = regex;
                    float singleLineHeight = EditorGUIUtility.singleLineHeight + 3;
                    float windowHeight = singleLineHeight * (1 + foundMatches.Count);
                    bool needScroll = windowHeight > Screen.height * 0.5f;
                    if (needScroll)
                    {
                        windowHeight = Screen.height * 0.5f;
                    }

                    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 12
                    };
                    
                    QuickPopup.ShowWindow(
                        new QuickPopup.WindowParams(
                        width: 300, 
                        height: windowHeight,
                        
                        onGUIFunction: () =>
                        {
                            foreach (var go in foundMatches)
                            {
                                if (GUILayout.Button(go.name, buttonStyle))
                                {
                                    UnityEditor.Selection.activeGameObject = go;
                                    QuickPopup.CloseCurrent();
                                }
                            }
                        },
                        
                        onGUIHeaderFunction: () =>
                        {
                            GUILayout.Label($"{foundMatches.Count} hits for: \"{searchTerm}\"");
                        },
                        
                        needScroll: needScroll));
                }
                else
                {
                    Selection.activeGameObject = foundMatches.First();
                }
       
            }

            public List<GameObject> GetAllObjectNames(string pattern)
            {
                List<GameObject> allObjects = new();
                
                foreach (GameObject go in FindObjectsByType<GameObject>(sortMode:FindObjectsSortMode.None))
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(go.name, pattern))
                    {
                        allObjects.Add(go);
                    }
                }
                return allObjects;
            }
        }
    }
}
