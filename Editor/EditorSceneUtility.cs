#if UNITY_EDITOR
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace BearsEditorTools
{
    public static class EditorSceneUtility
    {
        public static bool IsSceneInBuildSettings(string sceneNameOrPath)
        {
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.path.Contains(sceneNameOrPath))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsSceneInBuildSettings(Scene scene)
        {
            return IsSceneInBuildSettings(scene.name);
        }
        
        public static bool IsSceneInBuildSettings(int instanceID)
        {
            return IsSceneInBuildSettings(GetSceneFromInstanceID(instanceID).name);
        }
        
        public static Scene GetSceneFromInstanceID(int id)
        {
            Type type = typeof(EditorSceneManager);
            MethodInfo mi = type.GetMethod("GetSceneByHandle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            object classInstance = Activator.CreateInstance(type, null);
            return (Scene)mi.Invoke(classInstance, new object[] { id });
        }

        public static Scene[] GetSelectedScenes()
        {
            var selectedScenes = new List<Scene>();

            foreach (var instanceID in Selection.instanceIDs)
            {
                var scene = GetSceneFromInstanceID(instanceID);
                if (scene.IsValid()) selectedScenes.Add(scene);
            }

            return selectedScenes.ToArray();
        }

        public static Scene[] GetUnselectedScenes()
        {
            var selectedScenes = GetSelectedScenes();
            var otherScenes = EditorSceneManager.GetSceneManagerSetup().ToList();

            Debug.Log(selectedScenes.Length);
            Debug.Log(otherScenes.Count);

            if (selectedScenes.Length == otherScenes.Count) return new Scene[] { };

            for (int i = 0; i < otherScenes.Count; i++)
            {
                foreach (var selectedScene in selectedScenes)
                {
                    if (selectedScene.path == otherScenes[i].path)
                    {
                        otherScenes.Remove(otherScenes[i]);
                    }
                }
            }

            return otherScenes.Select(otherScene => EditorSceneManager.GetSceneByPath(otherScene.path)).ToArray();
        }

        public static Scene[] GetAllScenes()
        {
            var scenes = new List<Scene>();
            foreach (var sceneSetup in EditorSceneManager.GetSceneManagerSetup())
            {
                scenes.Add(EditorSceneManager.GetSceneByPath(sceneSetup.path));
            }
            return scenes.ToArray();
        }

        
        public static bool IsSceneValid(int instanceID)
        {
            return GetSceneFromInstanceID(instanceID).IsValid();
        }
    }
}
#endif