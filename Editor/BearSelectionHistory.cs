using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace BearsEditorTools
{
    /// <summary>
    /// <para>Keeps a history of the selection in the editor, allowing you to undo and redo selections with back and forward shortcuts (mouse buttons).</para>
    /// </summary>
    [InitializeOnLoad]
    public static class BearSelectionHistory
    {
        const int MAX_ENTRIES = 100;
        const int MAX_OBJECTS_PER_ENTRY = 1000; 
        
        private static SelectionHistoryData _SelectionHistory;
        private static int[] LastSelectionSetByThisScript { get; set; }
        
        private static string[] _CachedPrefKeys;
        
        static BearSelectionHistory()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            // store to keep things snappy when doing large selections.
            _CachedPrefKeys = new string[MAX_ENTRIES];
            for (int i = 0; i < MAX_ENTRIES; i++)
            {
                _CachedPrefKeys[i] = $"Bear.SelectionHistory.Entry{i}";
            }
            
            _SelectionHistory = ReadIntArrayPrefs();
        }

        /*private static SelectionHistoryData LoadJsonPref()
        {
            string json = SessionState.GetString("Bear.SelectionHistory", string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                return new SelectionHistoryData(true);
            }
            
            return JsonUtility.FromJson<SelectionHistoryData>(json);
        }
        
        private static void WriteJsonPref()
        {
            // TODO: Do it without json. We are just storing lists of ints, so we could use a more efficient format.
            SessionState.SetString("Bear.SelectionHistory", JsonUtility.ToJson(_SelectionHistory));
        }*/

        private static void SaveToPrefs()
        {
            // Debug.Log($"SaveToPrefs at {EditorApplication.timeSinceStartup}");
            // WriteJsonPref();
            WriteIntArrayPrefs();
        }
        
        private static void LoadFromPrefs()
        {
            // LoadJsonPref();
            ReadIntArrayPrefs();
        }
        
        private static void WriteIntArrayPrefs()
        {
            SessionState.SetInt("Bear.SelectionHistory.CurrentIndex", _SelectionHistory.CurrentHistoryIndex);
            
            int selectionHistoryHistoryCount = _SelectionHistory.HistoryCount;
            
            for (int i = 0; i < MAX_ENTRIES; i++)
            {
                if(selectionHistoryHistoryCount <= i)
                {
                    SessionState.EraseIntArray(_CachedPrefKeys[i]);
                    continue;
                }
                
                SessionState.SetIntArray(_CachedPrefKeys[i], _SelectionHistory.SelectionEntries[i].InstanceIDs);
            }
        }
        
        private static SelectionHistoryData ReadIntArrayPrefs()
        {
            SelectionHistoryData storedHistory = new SelectionHistoryData();
            
            storedHistory.CurrentHistoryIndex = SessionState.GetInt("Bear.SelectionHistory.CurrentIndex", 0);
            storedHistory.SelectionEntries = new List<BearSelectionHistory.SelectionHistoryData.SelectionEntry>();
            
            for (int i = 0; i < MAX_ENTRIES; i++)
            {
                int[] entry = SessionState.GetIntArray(_CachedPrefKeys[i], null);
                if (entry == null || entry.Length == 0)
                    continue;

                storedHistory.SelectionEntries.Add(new SelectionHistoryData.SelectionEntry
                {
                    InstanceIDs = entry
                });
            }
            
            return storedHistory;
        }
        
        private static void OnSelectionChanged()
        {
            // Debug.Log($"OnSelectionChanged at {EditorApplication.timeSinceStartup}");
            
            // if selection equals last selection, do nothing. Blocks our own selection change from being added to history.
            if (LastSelectionSetByThisScript != null && LastSelectionSetByThisScript.Length > 0 && LastSelectionSetByThisScript.SequenceEqual(Selection.instanceIDs))
            {
                // Debug.Log("SelectionHistory: Selection unchanged, not adding to history.");
                return;
            }

            _SelectionHistory.AddToHistory(Selection.instanceIDs);
        }

        [Shortcut("Bears/Selection/Undo", KeyCode.Mouse3)] 
        public static void Undo()
        {
            _SelectionHistory.Back();
        }
        
        [Shortcut("Bears/Selection/Redo", KeyCode.Mouse4 )]
        public static void Redo()
        {
            _SelectionHistory.Forward();
        }

        /// <summary>
        /// Manages a json serializable list of SelectionEntries.
        /// </summary>
        [Serializable]
        private struct SelectionHistoryData
        {
            public List<SelectionEntry> SelectionEntries;
            public int CurrentHistoryIndex;
            public int HistoryCount => SelectionEntries.Count;

            public SelectionHistoryData(bool init) : this()
            {
                SelectionEntries = new List<SelectionEntry>();
            }

            public void Back()
            {
                if (CurrentHistoryIndex <= 0)
                    return;
                
                CurrentHistoryIndex--;
                SelectionEntries[CurrentHistoryIndex].Select();
                // WriteJsonPref();
                SaveToPrefs();
            }
            
            public void Forward()
            {
                if (CurrentHistoryIndex >= HistoryCount - 1)
                    return;
                
                CurrentHistoryIndex++;
                SelectionEntries[CurrentHistoryIndex].Select();
                // WriteJsonPref();
                SaveToPrefs();
            }

            public void AddToHistory(int[] currentSelectedInstanceIds)
            {
                if (currentSelectedInstanceIds.Length == 0)
                    return;

                if (currentSelectedInstanceIds.Length > MAX_OBJECTS_PER_ENTRY)
                {
                    Debug.LogWarning($"SelectionHistory: Selection too large to add to history. Max allowed is {MAX_OBJECTS_PER_ENTRY}, but got {currentSelectedInstanceIds.Length}.");
                    return;
                }

                // If user has gone back in history, remove all entries after the current index to make room for new entries.
                if (CurrentHistoryIndex < HistoryCount - 1)
                {
                    SelectionEntries.RemoveRange(CurrentHistoryIndex + 1, HistoryCount - CurrentHistoryIndex - 1);
                }

                int numTooMany = HistoryCount - MAX_ENTRIES;
                
                if (numTooMany > 0)
                {
                    SelectionEntries.RemoveRange(0, numTooMany);
                    CurrentHistoryIndex = Math.Max(0, CurrentHistoryIndex - numTooMany);
                }
      
                SelectionEntries.Add(new SelectionEntry
                {
                    InstanceIDs = currentSelectedInstanceIds
                });

                CurrentHistoryIndex = SelectionEntries.Count - 1;
                // WriteJsonPref();
                SaveToPrefs();
            }            
            
            [Serializable]
            internal struct SelectionEntry
            {
                public int[] InstanceIDs;
            
                public void Select()
                {
                    // Debug.Log($"Select at {EditorApplication.timeSinceStartup}");
                    
                    LastSelectionSetByThisScript = InstanceIDs;
                    Selection.instanceIDs = InstanceIDs;
                }
            }
        }
    }
}