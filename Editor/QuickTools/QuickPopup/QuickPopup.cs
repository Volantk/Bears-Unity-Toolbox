using System;
using BearsEditorTools.Internal;

namespace BearsEditorTools
{
    public static class QuickPopup
    {
        public struct WindowParams
        {
            public float Width;
            public float Height;
            public Action OnGUIHeaderFunction;
            public Action OnGUIFunction;
            public Action OnCloseFunction;
            public bool NeedScroll;
            
            public WindowParams(float width, float height, Action onGUIFunction, bool needScroll = false, Action onGUIHeaderFunction = null, Action onCloseFunction = null)
            {
                Width = width;
                Height = height;
                OnGUIHeaderFunction = onGUIHeaderFunction;
                OnGUIFunction = onGUIFunction;
                OnCloseFunction = onCloseFunction;
                NeedScroll = needScroll;
            }
        }
        
        public static void ShowWindow(WindowParams windowParams)
        {
            QuickPopupBaseWindow.ShowWindow(windowParams);
        }

        public static void CloseCurrent()
        {
            QuickPopupBaseWindow.CloseCurrent();
        }
    }
}