using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BearsEditorTools
{
    public static class BearsAssetTools
    {
        private static string _AssetPath;
        private static List<string> _AssetPaths = new List<string>();

        public static T FindFirstAssetWithName<T>(string search) where T : Object
        {
            var guid = AssetDatabase.FindAssets(search).FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
                return null;

            var loadedAndCasted = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid)) as T;

            return loadedAndCasted;
        }

        [MenuItem("CONTEXT/MonoBehaviour/Copy Assembly Qualified Type Name")]
        private static void GetAssemblyQualifiedTypeName(MenuCommand command)
        {
            var aqtn = command.context.GetType().AssemblyQualifiedName;
            Debug.Log(aqtn);
            GUIUtility.systemCopyBuffer = aqtn;
        }

        /// <summary>
        /// Runs AssetDatabase.ForceReserializeAssets() on the current selection.
        /// </summary>
        [MenuItem("Assets/Re-serialize Selected Assets", priority = 40)]
        private static void ReserializeSelectedAssets()
        {
            var foundPaths = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            
            List<string> paths = new List<string>();

            foreach (var path in foundPaths)
            {
                if (AssetDatabase.IsValidFolder(path))
                {
                    // get contents
                    var x = new List<string>();
                    GetFilesRecursively(path, ref x);

                    if(x.Count > 0)
                        paths.TryAddRange(x);
                }
                else
                {
                    paths.Add(path);
                }
            }

            // Shouldn't have any duplicates, but let's make sure anyway
            paths = paths.Distinct().ToList();
            
            if (!EditorUtility.DisplayDialog("Re-serialize Assets", $"{paths.Count} asset(s) will be re-serialized.", "OK", "Cancel"))
            {
                Debug.Log("User canceled re-serialization.");
                
                return;
            }

            AssetDatabase.StartAssetEditing();
            
            // Iterate over each path so we can show a progress bar... Yeah. Vain.
            for (var i = 0; i < paths.Count; i++)
            {
                var path = paths[i];

                if (EditorUtility.DisplayCancelableProgressBar($"Re-serializing assets... {i + 1} of {paths.Count}", path.Split('\\', '/').Last(), (i + 1) / (float) paths.Count))
                    break;

                // This is the line that actually does the work.
                AssetDatabase.ForceReserializeAssets(new[] {path});
            }

            AssetDatabase.StopAssetEditing();
            
            EditorUtility.ClearProgressBar();
        }

        private static void GetFilesRecursively(string path, ref List<string> list)
        {
            Debug.Log("Recursively adding folder: " + path);
            list.TryAddRange(GetFilesFromDir(path));
            var subfolders = AssetDatabase.GetSubFolders(path);

            foreach (var dir in subfolders)
            {
                // get contents
                var files = GetFilesFromDir(dir);

                list.TryAddRange(files);

                GetFilesRecursively(dir, ref list);
            }
        }

        private static List<string> GetFilesFromDir(string dir)
        {
            List<string> files =
                Directory.GetFiles(Path.Combine(Application.dataPath.Replace("Assets", ""), dir), "*")
                    .Where(p => !p.EndsWith(".meta"))
                    .Select(p => p.Replace(Application.dataPath, "Assets"))
                    .ToList();
            return files;
        }

        [MenuItem("Assets/Reimport Model With First-time Settings)", true)]
        private static bool ValidateReimportWithFirstTimeImportSettings()
        {
            return Selection.gameObjects.Any(PrefabUtility.IsPartOfModelPrefab);
        }

        [MenuItem("Assets/Copy.../Full File Path", false, 19)]
        private static void CopyFilePathToClipboard()
        {
            _AssetPath = Application.dataPath.Replace("Assets", AssetDatabase.GetAssetPath(Selection.activeObject));
            _AssetPath = _AssetPath.Replace('/', '\\');

            ToClipboard();
        }

        [MenuItem("Assets/Copy.../Folder Path", false, 19)]
        private static void CopyFolderPathToClipboard()
        {
            _AssetPath = Application.dataPath.Replace("Assets", AssetDatabase.GetAssetPath(Selection.activeObject));
            _AssetPath = _AssetPath.Remove(_AssetPath.LastIndexOf('/'), _AssetPath.Length - _AssetPath.LastIndexOf('/'));
            _AssetPath = _AssetPath.Replace('/', '\\');

            ToClipboard();
        }

        [MenuItem("Assets/Copy.../Asset Path", false, 19)]
        private static void CopyRawAssetPathToClipboard()
        {
            _AssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            ToClipboard();
        }

        [MenuItem("Assets/Copy.../Unity GUID", false, 19)]
        private static void CopyGUIDToClipboard()
        {
            _AssetPath = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Selection.activeObject));
            ToClipboard();
        }

        [MenuItem("Assets/Copy.../Resource Path", false, 19)]
        private static void CopyResourcePathToClipboard()
        {
            _AssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!_AssetPath.Contains("Resources"))
            {
                Debug.LogError("Asset is not in resources: " + _AssetPath);
                return;
            }

            int subStart = _AssetPath.IndexOf("Resources", StringComparison.Ordinal);
            subStart += "Resources/".Length;
            int length = _AssetPath.Length;

            _AssetPath = _AssetPath.Substring(subStart, length - subStart);

            ToClipboard();
        }

        [MenuItem("Assets/Copy.../Resource Path", true, 19)]
        private static bool CopyResourcePathToClipboard_Validate()
        {
            _AssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            return _AssetPath.Contains("Resources");
        }

        [MenuItem("Assets/Open In Sublime Text")]
        public static void OpenAssetInSublimeText()
        {
            _AssetPath = Application.dataPath.Replace("Assets", AssetDatabase.GetAssetPath(Selection.activeObject));
            _AssetPath = _AssetPath.Replace('/', '\\');

            OpenInExternalEditor("C:\\Program Files\\Sublime Text\\sublime_text.exe", "\"" + _AssetPath + "\"");
        }

        [MenuItem("Assets/Open In Visual Studio Code")]
        public static void OpenAssetInVisualStudioCode()
        {
            _AssetPath = Application.dataPath.Replace("Assets", AssetDatabase.GetAssetPath(Selection.activeObject));
            _AssetPath = _AssetPath.Replace('/', '\\');

            OpenInExternalEditor("C:\\Program Files (x86)\\Microsoft VS Code\\Code.exe", "--reuse-window \"" + _AssetPath);
        }

        private static void OpenInExternalEditor(string executablePath, string arguments)
        {
            _AssetPath = Application.dataPath.Replace("Assets", AssetDatabase.GetAssetPath(Selection.activeObject));
            _AssetPath = _AssetPath.Replace('/', '\\');

            if (System.IO.File.Exists(executablePath))
            {
                System.Diagnostics.Process.Start(executablePath, arguments);
            }
        }

        private static void ToClipboard()
        {
            Debug.Log("Copied to clipboard: " + _AssetPath);
            EditorGUIUtility.systemCopyBuffer = _AssetPath;
        }

        // Code below is not mine - Bear
        // April 4. 2014: Modified to work with multiple objects instead of just one

        /* Author : Altaf
         * Date : May 20, 2013
         * Purpose : Context menu to copy, cut & paste items 
        */

        [MenuItem("Assets/Cut", false, 19)]
        private static void MoveAsset()
        {
            _AssetPaths.Clear();
            foreach (var obj in Selection.objects)
            {
                _AssetPaths.Add(AssetDatabase.GetAssetPath(obj));
                //Debug.Log(AssetDatabase.GetAssetPath(obj));
            }
        }

        [MenuItem("Assets/Cut", true)]
        private static bool MoveAssetValidate()
        {
            return (Selection.objects != null);
        }

        [MenuItem("Assets/Paste", false, 19)]
        private static void PasteAsset()
        {
            AssetDatabase.StartAssetEditing();
            
            List<string> finalAssetPaths = new  List<string>();
            foreach (string path in _AssetPaths)
            {
                string dstPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                
                string fileExt = System.IO.Path.GetExtension(dstPath);
                
                if (!string.IsNullOrEmpty(fileExt))
                    dstPath = System.IO.Path.GetDirectoryName(dstPath);
                
                string fileName = System.IO.Path.GetFileName(path);

                var p = dstPath + "/" + fileName;
                
                finalAssetPaths.Add(p);
                
                AssetDatabase.MoveAsset(path, p);
            }
            
            AssetDatabase.StopAssetEditing();
            
            AssetDatabase.Refresh();

            Selection.objects = finalAssetPaths.Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>).ToArray();
        }

        [MenuItem("Assets/Paste", true)]
        private static bool PasteAssetValidate()
        {
            //Have we copied anything?
            if (_AssetPaths.Count == 0)
                return false;
            //Try to paste no where?
            if (Selection.activeObject == null)
                return false;

            return true;
        }
        
        /// <summary>
        /// Add range but only if not null
        /// </summary>
        /// <param name="list"></param>
        /// <param name="objects"></param>
        /// <typeparam name="T"></typeparam>
        public static void TryAddRange<T>(this List<T> list, List<T> other)
        {
            if (other != null && other.Count > 0)
            {
                list.AddRange(other);
            }
        }
    }
}