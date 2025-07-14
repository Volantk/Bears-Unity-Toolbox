using System;
using UnityEditor;
using UnityEngine;

namespace BearsEditorTools.Internal
{
    internal class QuickPopupBaseWindow : EditorWindow
    {
        public QuickPopup.WindowParams windowParams;

        private static QuickPopupBaseWindow _currentWindow;

        public static void ShowWindow(QuickPopup.WindowParams windowParams) 
        {
            QuickPopupBaseWindow window = ScriptableObject.CreateInstance<QuickPopupBaseWindow>();
            
            _currentWindow = window;
            
            window.windowParams = windowParams;
            
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

            windowRect.width = windowParams.Width;
            windowRect.height = windowParams.Height;
            window.position = windowRect;
        }

        private Vector2 scrollPosition;
        void OnGUI()
        {
            windowParams.OnGUIHeaderFunction?.Invoke();

            if (windowParams.NeedScroll)
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            }
            
            windowParams.OnGUIFunction?.Invoke();
            
            if (windowParams.NeedScroll)
            {
                GUILayout.EndScrollView();
            }
            
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                windowParams.OnCloseFunction?.Invoke();
                Close();
            }

            
            GUI.backgroundColor = Color.red;
            
            if (GUILayout.Button("Close"))
            {
                windowParams.OnCloseFunction?.Invoke();
                Close();
            }

            GUI.backgroundColor = Color.white;
            
            // If not focused, close the window
            if (EditorWindow.focusedWindow != this || windowParams.OnGUIFunction == null)
            {
                Close();
                _currentWindow = null;
            }
        }
        
        /*void OnInspectorUpdate()
        {
            Repaint();
        }*/
        public static void CloseCurrent()
        {
            if (_currentWindow)
            {
                _currentWindow.windowParams.OnCloseFunction?.Invoke();
                _currentWindow.Close();
                _currentWindow = null;
            }
        }
    }
}