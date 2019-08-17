using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bears
{
    public class MicroToolbox
    {
        // TODO: QuickRenamer
        // TODO: QuickTransformTools
        // TODO: Reference finder

        private static Vector3 copiedLocalPosition;
        private static Quaternion copiedLocalRotation;
        private static Vector3 copiedLocalScale;

        private static Vector3 copiedWorldPosition;
        private static Quaternion copiedWorldRotation;
        private static Vector3 copiedWorldScale;

        [MenuItem("GameObject/Tools/Apply Changes To Prefab %Q")]
        public static void ApplyPrefab()
        {
            if (PrefabUtility.GetPrefabType(Selection.activeGameObject) == PrefabType.PrefabInstance ||
                PrefabUtility.GetPrefabType(Selection.activeGameObject) == PrefabType.DisconnectedPrefabInstance)
            {
                EditorApplication.ExecuteMenuItem("GameObject/Apply Changes To Prefab");
            }
            else
            {
                EditorApplication.Beep();
                Debug.LogError("Selection is not a prefab!");
            }
        }

        [MenuItem("GameObject/Tools/Unparent From All &P")]
        public static void Unparent()
        {
            if (!Selection.activeGameObject.transform.parent)
            {
                EditorApplication.Beep();
                Debug.LogError("Selection has no parent!");
            }
            else
            {
                EditorApplication.ExecuteMenuItem("GameObject/Clear Parent");
            }
        }

        [MenuItem("GameObject/Tools/New Child &N")]
        public static void CreateNewChild()
        {
            Transform newChild = new GameObject("New Child").transform;
            if (Selection.activeGameObject)
            {
                newChild.parent = Selection.activeGameObject.transform;
            }
            else
            {
                Debug.Log("Child does not have a parent :(  (New Child gameobject created in root)");
            }

            newChild.localPosition = Vector3.zero;
            newChild.localRotation = Quaternion.identity;
            newChild.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(newChild.gameObject, "Create Child");
            Selection.activeGameObject = newChild.gameObject;
        }

        [MenuItem("GameObject/Hierarchy/Move Up &V")]
        public static void HierarchyMoveUp()
        {
            foreach (var t in Selection.transforms)
            {
                if (t.parent != null)
                {
                    Undo.SetTransformParent(t, t.parent.parent, "Move up in hierarchy");
                }
                else return;
            }
        }

        [MenuItem("GameObject/Tools/Copy Transform &%c")]
        public static void CopyTransform()
        {
            copiedLocalPosition = Selection.activeTransform.localPosition;
            copiedLocalRotation = Selection.activeTransform.localRotation;
            copiedLocalScale = Selection.activeTransform.localScale;

            copiedWorldPosition = Selection.activeTransform.position;
            copiedWorldRotation = Selection.activeTransform.rotation;
            copiedWorldScale = Selection.activeTransform.lossyScale;

            Debug.Log(string.Format("Successfully copied the transform of {0}", Selection.activeTransform.name));
        }

        [MenuItem("GameObject/Tools/Paste Local Transform &%v")]
        public static void PasteTransformLocal()
        {
            foreach (Transform transform in Selection.transforms)
            {
                Undo.RecordObject(transform, "Paste Local Transform");
                transform.localPosition = copiedLocalPosition;
                transform.localRotation = copiedLocalRotation;
                transform.localScale = copiedLocalScale;
            }
        }


        [MenuItem("GameObject/Tools/Paste Global Transform &%#v")]
        public static void PasteTransformGlobal()
        {
            foreach (Transform transform in Selection.transforms)
            {
                Undo.RecordObject(transform, "Paste Global Transform");
                transform.position = copiedWorldPosition;
                transform.rotation = copiedWorldRotation;
                transform.localScale = copiedWorldScale;
            }
        }

        public static void PastePositionGlobal()
        {
            foreach (Transform transform in Selection.transforms)
            {
                Undo.RecordObject(transform, "Paste Global Transform");
                transform.position = copiedWorldPosition;
            }
        }

        public static void PastePositionLocal()
        {
            foreach (Transform transform in Selection.transforms)
            {
                Undo.RecordObject(transform, "Paste Local Transform");
                transform.localPosition = copiedLocalPosition;
            }
        }

        public static void PasteRotationLocal()
        {
            foreach (Transform transform in Selection.transforms)
            {
                Undo.RecordObject(transform, "Paste Local Transform");
                transform.localRotation = copiedLocalRotation;
            }
        }

        public static void PasteRotationGlobal()
        {
            foreach (Transform transform in Selection.transforms)
            {
                Undo.RecordObject(transform, "Paste Global Transform");
                transform.rotation = copiedWorldRotation;
            }
        }

        public static void PasteScaleLocal()
        {
            foreach (Transform transform in Selection.transforms)
            {
                Undo.RecordObject(transform, "Paste Local Transform");
                transform.localScale = copiedLocalScale;
            }
        }

        [MenuItem("GameObject/Tools/Reset/Position &g")]
        public static void ResetPosition()
        {
            Undo.RecordObjects(Selection.transforms, "Reset Position");
            foreach (Transform transform in Selection.transforms)
            {
                transform.localPosition = Vector3.zero;
            }
        }

        [MenuItem("GameObject/Tools/Reset/Zero Rotation &r")]
        public static void ResetRotation()
        {
            Undo.RecordObjects(Selection.transforms, "Reset Rotation");
            foreach (Transform transform in Selection.transforms)
            {
                transform.localEulerAngles = Vector3.zero;
            }
        }

        [MenuItem("GameObject/Tools/Reset/Scale &s")]
        public static void ResetScale()
        {
            Undo.RecordObjects(Selection.transforms, "Reset Scale");

            foreach (Transform transform in Selection.transforms)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
        }

        [MenuItem("Tools/Krillbite/Selection/Siblings With Same Name %&Z")]
        public static void SelectSiblingsWithSameName()
        {
            List<Object> newSelection = new List<Object>();
            Transform[] selection = Selection.transforms;

            foreach (Transform go in selection)
            {
                if (go.parent != null)
                {
                    foreach (Transform sibling in go.parent)
                    {
                        if (sibling.name == go.name)
                        {
                            newSelection.Add(sibling.gameObject);
                        }
                    }
                }
                else
                {
                    foreach (var t in Object.FindObjectsOfType<Transform>())
                    {
                        if (t.parent == null && t.name == go.name)
                        {
                            newSelection.Add(t.gameObject);
                        }
                    }
                }
            }

            Undo.RecordObjects(Selection.objects, "Selection");
            Selection.objects = newSelection.ToArray();
        }

        [MenuItem("Tools/Krillbite/Selection/Select Parent &C")]
        public static void SelectParent()
        {
            List<Object> newSelection = new List<Object>();
            Transform[] selection = Selection.transforms;

            foreach (Transform go in selection)
            {
                if (go.parent != null)
                {
                    newSelection.Add(go.parent.gameObject);
                }
                else
                {
                    newSelection.Add(go.gameObject);
                }
            }

            Selection.objects = newSelection.ToArray();
        }

        [MenuItem("Tools/Krillbite/Selection/Select Siblings &z")]
        public static void SelectSiblingsFromSelected()
        {
            List<Object> newSelection = new List<Object>();
            Transform[] selection = Selection.transforms;

            foreach (Transform go in selection)
            {
                foreach (Transform child in go.parent)
                {
                    newSelection.Add(child.gameObject);
                }
            }

            Selection.objects = newSelection.ToArray();
        }

        [MenuItem("Tools/Krillbite/Selection/Select Children &x")]
        public static void SelectChildrenOfSelected()
        {
            List<Object> newSelection = new List<Object>();
            Transform[] selection = Selection.transforms;

            foreach (Transform go in selection)
            {
                foreach (Transform child in go)
                {
                    newSelection.Add(child.gameObject);
                }
            }

            Selection.objects = newSelection.ToArray();
        }

        [MenuItem("Tools/Krillbite/Selection/Enable/Disable Selection %g")]
        public static void ToggleSelectionEnabled()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                Undo.RecordObject(go.gameObject, "Set Active");
                go.SetActive(!go.activeInHierarchy);
            }
        }
    }

    [CustomEditor(typeof(Transform))]
    public class ExtendedTransformInspector : Editor
    {
        private Editor e;

        public override void OnInspectorGUI()
        {
            if (e == null || ReferenceEquals(e, null))
            {
                Type inspectorWindowType = typeof(Editor).Assembly.GetType("UnityEditor.TransformInspector");
                e = Editor.CreateEditor(target, inspectorWindowType);
            } 

            if (e == null || ReferenceEquals(e, null))    
            {
                base.OnInspectorGUI();
            } 
            else
            {
                e.OnInspectorGUI();
                DrawResetButtons();
            }
        }

        private void OnDestroy()
        {
            DestroyImmediate(e);
        }

        private void DrawResetButtons()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel("Reset");

            if (GUILayout.Button("Position"))
            {
                var t = target as Transform;
                t.localPosition = Vector3.zero;
            }

            if (GUILayout.Button("Rotation"))
            {
                var t = target as Transform;
                t.localRotation = Quaternion.identity;
            }

            if (GUILayout.Button("Scale"))
            {
                var t = target as Transform;
                t.localScale = Vector3.one;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}