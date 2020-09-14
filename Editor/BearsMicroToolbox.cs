using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using UnityEditor.ShortcutManagement;

namespace Bears
{
    public static class BearsMicroToolbox
    {
        public static Transform copiedTransform;
        private static Vector3 copiedLocalPosition;
        private static Quaternion copiedLocalRotation;
        private static Vector3 copiedLocalScale;

        private static Vector3 copiedWorldPosition;
        private static Quaternion copiedWorldRotation;
        private static Vector3 copiedWorldScale;

        private static Vector3? copiedGeometryScale;

        private static Vector3 roundedTransform;

        public class TransformSet
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        //--------------

        // Generic function for rounding Vector3s: 3.14 -> 3
        public static Vector3 RoundVector3(Vector3 v)
        {
            return new Vector3(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));
        }

        // Generic function for rounding Vector3s: 3.14 -> 3
        public static Vector3 RoundVector3Custom(Vector3 v, float roundToNearest = 1f)
        {
            v *= (1 / roundToNearest);
            v = RoundVector3(v) / (1 / roundToNearest);
            return v;
        }

        private static TransformSet MakeSet(Transform t)
        {
            var set = new TransformSet();

            set.position = t.position;
            set.rotation = t.rotation;
            set.scale = t.localScale;

            return set;
        }

        private static void SetTransform(Transform t, TransformSet set, bool doPos = true, bool doRot = true, bool doScale = true)
        {
            if (doPos) t.position = set.position;
            if (doRot) t.rotation = set.rotation;
            if (doScale) t.localScale = set.scale;
        }

        public static void SwapWorldTransforms()
        {
            if (Selection.transforms.Length != 2) return;

            Undo.RecordObjects(Selection.transforms, "Swap position");

            var ob0 = Selection.transforms[0];
            var ob1 = Selection.transforms[1];


            var set0 = MakeSet(ob0);
            var set1 = MakeSet(ob1);

            SetTransform(ob0, set1, true, true, false);
            SetTransform(ob1, set0, true, true, false);
            SetTransform(ob1, set0, true, true, false);
        }

        public static void RoundPositionCustom(float snap, GameObject obj)
        {
            Undo.RecordObjects(Selection.transforms, "Snap To Grid (Custom)");

            if (snap < 0.01f)
            {
                Debug.Log("Snapper was less than 0.01, setting it to 0.25.");
                snap = 0.25f;
            }

            snap = 1 / snap;
            Vector3 value = obj.transform.localPosition * snap;
            obj.transform.localPosition = RoundVector3(value) / snap;
        }

        public static void RoundScaleCustom(float snap, GameObject obj)
        {
            Undo.RecordObjects(Selection.transforms, "Snap To Grid (Custom)");

            if (snap < 0.01f)
            {
                Debug.Log("Snapper was less than 0.01, setting it to 0.25.");
                snap = 0.25f;
            }

            snap = 1 / snap;
            Vector3 value = obj.transform.localScale * snap;
            obj.transform.localScale = RoundVector3(value) / snap;
        }

        //-------------------------------

        private static int GetHierarchyDepth(Transform target)
        {
            var parent = target.transform;

            int depth = 0;

            while (parent.parent != null)
            {
                depth++;
                parent = parent.parent;
            }

            return depth;
        }


        [MenuItem("GameObject/PREFAB ACTIONS/Apply Overrides In Source Prefab", true)]
        public static bool ValidateMenuItemApplyPrefabOverrides()
        {
            return Selection.activeGameObject != null;
        }

        /*
        [MenuItem("GameObject/PREFAB ACTIONS/Apply Overrides In Source Prefab", priority = -99997)]
        [Shortcut("Krillbite/Prefab Actions/Apply Overrides In Source Prefab")]
        public static void ApplyPrefabOverrides()
        {
            var prefabRoots = Selection.gameObjects.Select(PrefabUtility.GetNearestPrefabInstanceRoot).Distinct()
                .OrderByDescending(p => GetHierarchyDepth(p.transform)); // Sort by hierarchy depth, so we do the innermost ones first

            foreach (var prefabRoot in prefabRoots)
            {
                var type = PrefabUtility.GetPrefabAssetType(prefabRoot);

                bool isPrefab = type == PrefabAssetType.Regular || type == PrefabAssetType.Variant;

                if (!isPrefab)
                {
                    EditorApplication.Beep();
                    Debug.LogWarning("ApplyPrefab(): Selection was not a prefab we handle: " + type);
                    continue;
                }

                Debug.Log($"Applying {prefabRoot}");

                List<PrefabOverride> overrides = new List<PrefabOverride>();

                overrides.TryAddRange(PrefabUtility.GetObjectOverrides(prefabRoot));
                overrides.TryAddRange(PrefabUtility.GetAddedGameObjects(prefabRoot));
                overrides.TryAddRange(PrefabUtility.GetAddedComponents(prefabRoot));

                foreach (var objectOverride in overrides)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Applying Prefab", $"Applying {prefabRoot.name}...", overrides.GetProgressAt(objectOverride)))
                    {
                        break;
                    }

                    // This is rather slow, because it's applying and saving the asset for each property. Maybe there's a way to batch it?
                    objectOverride.Apply();
                }

                EditorUtility.ClearProgressBar();
            }
        }
*/
//
//	[MenuItem("GameObject/Tools/Prefab Actions/Apply Selected Prefabs")]
//	public static void ApplyPrefabsForSelected()
//	{
//		var selection = Selection.gameObjects.ToList();
//		foreach (var go in selection)
//		{
//			var t = PrefabUtility.GetPrefabAssetType(go);
//			if (t == PrefabAssetType.Regular || t == PrefabAssetType.Variant)
//			{
//				Selection.objects = new[] { go };
//				ApplyPrefab();
////				EditorApplication.ExecuteMenuItem("GameObject/Apply Changes To Prefab");
//			}
//		}
//		Selection.objects = selection.ToArray();
//	}
        //[MenuItem("GameObject/Tools/Prefab Actions/(Careful) Apply Selected Prefabs AND SAVE SCENE")]
        //public static void ApplyPrefabsForSelectedThenSave()
        //{
        //	var selection = Selection.gameObjects.ToList();
        //	foreach (var go in selection)
        //	{
        //		if (PrefabUtility.GetPrefabType(go) == PrefabType.PrefabInstance || PrefabUtility.GetPrefabType(go) == PrefabType.DisconnectedPrefabInstance)
        //		{
        //			Selection.objects = new[] { go };
        //			EditorApplication.ExecuteMenuItem("GameObject/Apply Changes To Prefab");
        //		}
        //	}
        //	Selection.objects = selection.ToArray();
        //	EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        //}

        [Shortcut("GameObject/Tools/Prefab Actions/Revert Selected Prefabs")]
        public static void RevertPrefab()
        {
            if (Selection.gameObjects.Length > 0)
            {
                foreach (GameObject obj in Selection.gameObjects)
                {
                    PrefabUtility.RevertPrefabInstance(obj, InteractionMode.UserAction);
                }
            }
            else
            {
                EditorApplication.Beep();
                Debug.LogError("Cannot revert prefabs - nothing selected");
            }
        }
        
        public static Scene GetSceneFromInstanceID(int id)
        {
            Type type = typeof(EditorSceneManager);
            MethodInfo mi = type.GetMethod("GetSceneByHandle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            object classInstance = Activator.CreateInstance(type, null);
            return (Scene)mi.Invoke(classInstance, new object[] { id });
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


        [Shortcut("GameObject/Tools/New Child", KeyCode.N, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void CreateNewChild()
        {
            var newBornChildren = new List<GameObject>();

            foreach (var id in Selection.instanceIDs)
            {
                var p = EditorUtility.InstanceIDToObject(id) as GameObject;
                var s = GetSceneFromInstanceID(id);

                if (p != null)
                {
                    Transform newChild = new GameObject("New Child").transform;

                    newChild.parent = p.transform;

                    newChild.localPosition = Vector3.zero;
                    newChild.localRotation = Quaternion.identity;
                    newChild.localScale = Vector3.one;

                    Undo.RegisterCreatedObjectUndo(newChild.gameObject, "Create Child");

                    newBornChildren.Add(newChild.gameObject);
                }
                else
                {
                    Transform newChild = new GameObject("New Child").transform;

                    newChild.localPosition = Vector3.zero;
                    newChild.localRotation = Quaternion.identity;
                    newChild.localScale = Vector3.one;

                    Undo.RegisterCreatedObjectUndo(newChild.gameObject, "Create Child");

                    SceneManager.MoveGameObjectToScene(newChild.gameObject, s);

                    newBornChildren.Add(newChild.gameObject);
                }
            }

            Selection.objects = newBornChildren.ToArray();
        }

        [Shortcut("GameObject/Tools/Move Selection Up In The Hierarchy", KeyCode.V, ShortcutModifiers.Alt)]
        public static void HierarchyMoveUp()
        {
            // Selected transforms, sorted by hierarchy and parent
            // Using descending order so that objects come out in the right order at the end
            var transforms = Selection.transforms.OrderByDescending(t => t.GetSiblingIndex()).ThenBy(t => t.parent);
            
            foreach (var t in transforms)
            {
                // No parent, object is already at top level, do nothing
                if (t.parent == null) 
                    return;

                // Skip objects that are part of a prefab, since we don't wanna accidentally do something Unity isn't expecting...
                // Do the change inside the prefab instead.
                if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject) && PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject) != t.gameObject)
                {
                    Debug.LogWarning($"Can't move {t.gameObject} up in the hierarchy because it's part of a prefab", t.gameObject);
                    continue;
                }
                
                // The index below the parent in the hiearchy (this makes objects show up underneath their parent when they're moved, which is nice and intuitive.
                int newSiblingIndex = t.parent.GetSiblingIndex() + 1;

                // Moving up to the scene root requires a little extra handling...
                // If you're setting a null parent on something that isn't in the currently active scene, it will be moved there. So we set to null and move to the scene afterwards.
                if (t.parent.parent == null)
                {
                    Undo.SetTransformParent(t, null, "Move up in hierarchy");
                    Undo.MoveGameObjectToScene(t.gameObject, t.gameObject.scene, "Move up in hierarchy");
                }
                else // This is the most common use case. Just move the object one level up in the hierarchy, to the parent's parent
                {
                    Undo.SetTransformParent(t, t.parent.parent, "Move up in hierarchy");
                }
                
                // Recording sibling index edit, so we can undo things. This was initially used to ensure we could restore accidentally edited prefab instances. We skip those now, but this is kept it in for safety. 
                Undo.RecordObject(t, "Move up in hierarchy");
                
                // Keep hierarchy order intact
                t.SetSiblingIndex(newSiblingIndex);

                // Make sure scene gets updated (so hierarchy immediately displays the changes)
                EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
            }
        }

        //[MenuItem("Krillbite/Shortcuts/Copy\\Paste/Align Camera To View &%#F")]
        //public static void AlignCameraToView()
        //{
        //	var gameCamera = GetMainCamera().transform;
        //	var sceneCamera = SceneView.lastActiveSceneView.camera.transform;
        //	
        //	Undo.RecordObject(gameCamera, "Align Camera To View");
        //
        //	gameCamera.rotation = sceneCamera.rotation;
        //	gameCamera.position = sceneCamera.position;
        //}

        public static string AddFloatSignThing(string input)
        {
            return input.Replace(",", "f,").Replace(")", "f)");
        }

        public static void CopyTransformToClipboard(string transformType = "Position", bool worldSpace = false, bool formatFloatValues = true)
        {
            string output = null;

            if (transformType == "Position" && !worldSpace)
            {
                output = Selection.activeTransform.localPosition.ToString("F4");
            }

            if (transformType == "Position" && worldSpace)
            {
                output = Selection.activeTransform.position.ToString("F4");
            }

            if (transformType == "Rotation" && !worldSpace)
            {
                output = Selection.activeTransform.localRotation.eulerAngles.ToString("F4");
            }

            if (transformType == "Rotation" && worldSpace)
            {
                output = Selection.activeTransform.rotation.eulerAngles.ToString("F4");
            }

            if (transformType == "Scale" && !worldSpace)
            {
                output = Selection.activeTransform.localScale.ToString("F4");
            }

            if (transformType == "Scale" && worldSpace)
            {
                output = Selection.activeTransform.lossyScale.ToString("F4");
            }

            if (formatFloatValues)
            {
                output = AddFloatSignThing(output);
            }

            EditorGUIUtility.systemCopyBuffer = output;

            Debug.Log("Copied values clipboard: " + output);
        }

        [Shortcut("Krillbite/Shortcuts/Copy\\Paste/Copy Transform", KeyCode.C, ShortcutModifiers.Action | ShortcutModifiers.Alt)]
        public static void CopyTransform()
        {
            if (Selection.activeTransform == null) return;

            var t = Selection.activeTransform;

            copiedTransform = t;
            copiedLocalPosition = Selection.activeTransform.localPosition;
            copiedLocalRotation = Selection.activeTransform.localRotation;
            copiedLocalScale = Selection.activeTransform.localScale;

            copiedWorldPosition = Selection.activeTransform.position;
            copiedWorldRotation = Selection.activeTransform.rotation;
            copiedWorldScale = Selection.activeTransform.lossyScale;

            var geometryChild = GetGeometryChild(t);

            if (geometryChild != null)
            {
                copiedGeometryScale = geometryChild.localScale;
            }
            else
            {
                copiedGeometryScale = null;
            }

            Debug.Log(string.Format("Successfully copied the transform of {0}{1}", Selection.activeTransform.name, copiedGeometryScale.HasValue ? " (also copied geometry scale!)" : ""));
        }

        [Shortcut("Krillbite/Shortcuts/Copy\\Paste/Paste Transform (Local)", KeyCode.V, ShortcutModifiers.Action | ShortcutModifiers.Alt)]
        public static void PasteTransformLocal()
        {
            foreach (Transform t in Selection.transforms)
            {
                Undo.RecordObject(t, "Paste Local Transform");
                t.localPosition = copiedLocalPosition;
                t.localRotation = copiedLocalRotation;
                t.localScale = copiedLocalScale;

                var geometryChild = GetGeometryChild(t);

                if (geometryChild != null && copiedGeometryScale.HasValue)
                {
                    Undo.RecordObject(geometryChild, "Paste Local Transform");
                    geometryChild.localScale = copiedGeometryScale.Value;
                }
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

        [Shortcut("Krillbite/Shortcuts/Copy\\Paste/Paste Transform (Global)", KeyCode.V, ShortcutModifiers.Action | ShortcutModifiers.Shift | ShortcutModifiers.Alt)]
        public static void PasteTransformGlobal()
        {
            foreach (Transform t in Selection.transforms)
            {
                Undo.RecordObject(t, "Paste Global Transform");
                t.position = copiedWorldPosition;
                t.rotation = copiedWorldRotation;
                t.localScale = copiedWorldScale;
            }
        }

        /*
        [Shortcut("Krillbite/Shortcuts/Copy\\Paste/Paste Transform (Preserve Children) (Global)", KeyCode.B, ShortcutModifiers.Alt | ShortcutModifiers.Action)]
        public static void PasteTransformGlobalNoChildren()
        {
            foreach (Transform t in Selection.transforms)
            {
                Undo.RecordObject(t, "Paste Global Transform");
                t.SetWorldWithoutAffectingChildren(copiedWorldPosition, copiedWorldRotation, copiedWorldScale);
            }
        }

        [Shortcut("Krillbite/Shortcuts/Copy\\Paste/Paste Transform (Preserve Children) (Local)", KeyCode.B, ShortcutModifiers.Alt | ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        public static void PasteTransformLocalNoChildren()
        {
            foreach (Transform t in Selection.transforms)
            {
                Undo.RecordObject(t, "Paste Local Transform");
                t.SetLocalWithoutAffectingChildren(copiedLocalPosition, copiedLocalRotation, copiedLocalScale);
            }
        }
        */

        public static void PastePositionGlobal()
        {
            foreach (Transform transform in Selection.transforms)
            {
                Undo.RecordObject(transform, "Paste Global Transform");
                transform.position = copiedWorldPosition;
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

        [Shortcut("Krillbite/Shortcuts/Reset/Position", KeyCode.G, ShortcutModifiers.Alt)]
        public static void ResetPosition()
        {
            Undo.RecordObjects(Selection.transforms, "Reset Position");
            foreach (Transform transform in Selection.transforms)
            {
                transform.localPosition = Vector3.zero;
            }
        }

        [Shortcut("Krillbite/Shortcuts/Reset/Rotation", KeyCode.R, ShortcutModifiers.Alt)]
        public static void ResetRotation()
        {
            Undo.RecordObjects(Selection.transforms, "Reset Rotation");
            foreach (Transform transform in Selection.transforms)
            {
                transform.localEulerAngles = Vector3.zero;
            }
        }

        [Shortcut("Krillbite/Shortcuts/Reset/Scale", KeyCode.S, ShortcutModifiers.Alt)]
        public static void ResetScale()
        {
            foreach (Transform transform in Selection.transforms)
            {
                Undo.RecordObject(transform, "Reset Scale");
                transform.localScale = new Vector3(1, 1, 1);
            }
        }

        [Shortcut("Krillbite/Shortcuts/Reset/Position (Keep Children's Positions)", KeyCode.G, ShortcutModifiers.Alt | ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        public static void ResetPositionNoChildren()
        {
            ResetTransformWithoutAffectingChildren(Selection.transforms, true, false, false);
        }

        [Shortcut("Krillbite/Shortcuts/Reset/Rotation (Keep Children's Positions)", KeyCode.R, ShortcutModifiers.Alt | ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        public static void ResetRotationNoChildren()
        {
            ResetTransformWithoutAffectingChildren(Selection.transforms, false, true, false);
        }

        [Shortcut("Krillbite/Shortcuts/Reset/Scale (Keep Children's Positions)", KeyCode.S, ShortcutModifiers.Alt | ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        public static void ResetScaleNoChildren()
        {
            ResetTransformWithoutAffectingChildren(Selection.transforms, false, false, true);
        }

        private static Transform GetGeometryChild(Transform t)
        {
            Transform geometry = null;

            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                if (child.name == "Geometry")
                {
                    geometry = child;
                    break;
                }
            }

            return geometry;
        }

        [Shortcut("Krillbite/Shortcuts/Snap/Position To Grid (Custom Value)", KeyCode.D, ShortcutModifiers.Alt)]
        static void DoRoundPositionHotkey()
        {
            Undo.RecordObjects(Selection.gameObjects, "Snap Position To Grid (Custom value)");

            foreach (GameObject obj in Selection.gameObjects)
            {
                RoundPositionCustom(1, obj);
            }
        }

        [Shortcut("Krillbite/Shortcuts/Select/Siblings With Same Name", KeyCode.Z, ShortcutModifiers.Action | ShortcutModifiers.Alt)]
        public static void SelectSiblingsWithSameName()
        {
            List<Object> newSelection = new List<Object>();
            Transform[] selection = Selection.transforms;

            foreach (Transform go in selection)
            {
                var searchString = GetSearchModifiedString(go.name);

                if (go.parent != null)
                {
                    foreach (Transform sibling in go.parent)
                    {
                        if (GetSearchModifiedString(sibling.name) == searchString)
                        {
                            newSelection.Add(sibling.gameObject);
                        }
                    }
                }
                else
                {
                    foreach (var allScene in GetAllScenes())
                    {
                        foreach (var o in allScene.GetRootGameObjects())
                        {
                            var t = o.transform;
                            if (t.parent == null && GetSearchModifiedString(t.name) == searchString)
                            {
                                newSelection.Add(t.gameObject);
                            }
                        }
                    }
                }
            }

            Undo.RecordObjects(Selection.objects, "Selection");
            Selection.objects = newSelection.ToArray();
        }

        public static string GetSearchModifiedString(string input)
        {
            return StringWithoutBlenderNumbering(StringWithoutParentheses(input));
        }

        private static string StringWithoutBlenderNumbering(string input)
        {
            return Regex.Replace(input, @"\s*\.[0-9]{3}", "");
        }

        private static string StringWithoutParentheses(string input)
        {
            return Regex.Replace(input, @"\s*\(.*\)", "");
        }

        #region Snapping

        [Shortcut("Krillbite/Shortcuts/Snap/Rotation to nearest 15 deg")]
        public static void RoundRotation()
        {
            Undo.RecordObjects(Selection.transforms, "Round Rotation to nearest 15 deg");

            foreach (GameObject obj in Selection.gameObjects)
            {
                float snapDegrees = 15f;
                Vector3 rot = obj.transform.localEulerAngles / snapDegrees;
                obj.transform.localEulerAngles = RoundVector3(rot) * snapDegrees;
            }
        }

        [Shortcut("Krillbite/Shortcuts/Snap/Scale (Custom)")]
        public static void RoundScale()
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                RoundScaleCustom(1, obj);
            }
        }

        #endregion

        [Shortcut("Krillbite/Shortcuts/Select/Select Player")]
        public static void SelectPlayer()
        {
            Selection.activeGameObject = GameObject.Find("Player");
        }

        [Shortcut("Krillbite/Shortcuts/Select/Select Player Camera", KeyCode.Alpha2, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void SelectPlayerCamera()
        {
            Selection.activeGameObject = GetMainCamera();
        }

        private static GameObject GetMainCamera()
        {
            GameObject camera = null;

            if (Camera.main != null)
            {
                camera = Camera.main.gameObject;
            }

            if (camera != null)
            {
                return camera;
            }

            return camera.GetComponentInChildren<Camera>().gameObject;
        }

        [Shortcut("Krillbite/Shortcuts/Select/Deselect Random")]
        public static void DeselectRandom()
        {
            List<Object> newSelection = new List<Object>();
            Transform[] selection = Selection.transforms;

            foreach (Transform go in selection)
            {
                float num = Random.Range(0.0f, 10.0f);

                if (num > 3.333f)
                {
                    newSelection.Add(go.gameObject);
                }
            }

            Selection.objects = newSelection.ToArray();
        }

        [Shortcut("Krillbite/Shortcuts/Select/Select Parent", KeyCode.C, ShortcutModifiers.Alt)]
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

        [Shortcut("Krillbite/Shortcuts/Create New Parent", KeyCode.P, ShortcutModifiers.Shift | ShortcutModifiers.Alt)]
        public static void CreateNewParent()
        {
            var parentTarget = Selection.activeGameObject;

            var newParent = new GameObject(parentTarget.name + " PARENT").transform;
            newParent.transform.parent = parentTarget.transform.parent;
            newParent.transform.position = parentTarget.transform.position;
            Undo.RegisterCreatedObjectUndo(newParent.gameObject, "Parent Selected To New");

            foreach (var transform in Selection.transforms)
            {
                Undo.SetTransformParent(transform, newParent, "Parent Selected To New");
            }

            EditorGUIUtility.PingObject(Selection.activeInstanceID);
        }


        [Shortcut("GameObject/Shortcuts/Un-Parent", KeyCode.P, ShortcutModifiers.Alt)]
        public static void BreakParentHotkey()
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

        [Shortcut("Krillbite/Shortcuts/Select/Prefab Parent", KeyCode.C, ShortcutModifiers.Shift | ShortcutModifiers.Alt)]
        public static void SelectPrefabParent()
        {
            List<Object> newSelection = new List<Object>();
            Transform[] selection = Selection.transforms;

            foreach (Transform go in selection)
            {
                var pp = PrefabUtility.GetNearestPrefabInstanceRoot(go.gameObject);

                if (pp != null)
                {
                    newSelection.Add(pp);
                }
            }

            Selection.objects = newSelection.ToArray();
        }

        [Shortcut("Krillbite/Shortcuts/Select/Siblings", KeyCode.Z, ShortcutModifiers.Alt)]
        public static void SelectSiblingsFromSelected()
        {
            List<Object> newSelection = new List<Object>();
            Transform[] selection = Selection.transforms;

            foreach (Transform go in selection)
            {
                if (go.parent)
                {
                    foreach (Transform child in go.parent)
                    {
                        newSelection.Add(child.gameObject);
                    }
                }
            }

            Selection.objects = newSelection.ToArray();
        }

        [Shortcut("Krillbite/Shortcuts/Select/Children", KeyCode.X, ShortcutModifiers.Alt)]
        public static void SelectChildrenOfSelected()
        {
            List<Object> newSelection = new List<Object>();

            Transform[] selection = Selection.transforms;

            foreach (var go in selection)
            {
                foreach (Transform child in go)
                {
                    newSelection.Add(child.gameObject);
                }
            }

            foreach (var scene in GetSelectedScenes())
            {
                if (!scene.IsValid()) continue;
                if (!scene.isLoaded) continue;

                foreach (var go in scene.GetRootGameObjects())
                {
                    newSelection.Add(go);
                }
            }

            Selection.objects = newSelection.ToArray();
        }

//	[Shortcut("Krillbite/Shortcuts/Select/Wall " + BearsHotkeys.SelectWal)]
        public static void SelectWall()
        {
            List<Object> newSelection = new List<Object>();

            Transform[] selection = Selection.transforms;

            Transform[] sceneTransforms = Object.FindObjectsOfType<Transform>();

            //Debug.Log("Total Transforms found: " + sceneTransforms.Length);

            float distance = 8;
            float minDistance = 0.00001f;

            foreach (var t in selection)
            {
                var rightPoint = t.position + t.right * distance;
                var leftPoint = t.position + -t.right * distance;

                var frontPoint = t.position + t.forward * distance;
                var backPoint = t.position + -t.forward * distance;

                var correctDist = Vector3.Distance(leftPoint, rightPoint);

                foreach (var sceneTransform in sceneTransforms)
                {
                    if (sceneTransform.forward == t.forward)
                    {
                        var checkDistance = Vector3.Distance(sceneTransform.position, rightPoint) +
                                            Vector3.Distance(sceneTransform.position, leftPoint);

                        if (Math.Abs(checkDistance - correctDist) < minDistance)
                        {
                            if (PrefabUtility.GetOutermostPrefabInstanceRoot(sceneTransform.gameObject) == sceneTransform.gameObject)
                            {
                                newSelection.Add(sceneTransform.gameObject);
                            }
                        }
                    }
                }

                // if not finding anything, check front and back instead
                if (newSelection.Count == 0)
                {
                    foreach (var sceneTransform in sceneTransforms)
                    {
                        if (sceneTransform.forward == t.forward)
                        {
                            var checkDistance = Vector3.Distance(sceneTransform.position, frontPoint) +
                                                Vector3.Distance(sceneTransform.position, backPoint);

                            if (Math.Abs(checkDistance - correctDist) < minDistance)
                            {
                                if (PrefabUtility.GetOutermostPrefabInstanceRoot(sceneTransform.gameObject) == sceneTransform.gameObject)
                                {
                                    newSelection.Add(sceneTransform.gameObject);
                                }
                            }
                        }
                    }
                }
            }

            Selection.objects = newSelection.ToArray();
        }

        private static Transform[] cutObjects;

        
        [Shortcut("GameObject/Shortcuts/Move Tagged Objects", KeyCode.V, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        public static void MoveCutObjects()
        {
            if (cutObjects == null)
            {
                return;
            }

            Transform parent = Selection.activeTransform;

            if (parent == null)
            {
                var scene = GetSceneFromInstanceID(Selection.activeInstanceID);

                if (scene.IsValid())
                {
                    foreach (var t in cutObjects)
                    {
                        EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
                        
                        Undo.SetTransformParent(t, null, "Move Objects");
                        Undo.MoveGameObjectToScene(t.gameObject, scene, "Move Objects");
                    }
                    
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
            else
            {
                foreach (Transform t in cutObjects)
                {
                    if (t.gameObject.scene != parent.gameObject.scene)
                    {
                        EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
                        
                        Undo.SetTransformParent(t, null, "Move Objects");
                        Undo.MoveGameObjectToScene(t.gameObject, parent.gameObject.scene, "Move Objects");
                    }

                    EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
                    Undo.SetTransformParent(t, parent, "Move Objects");
                }
            }

            //Select the objects whose parents just changed, this expands them in the hierarchy view.
            Selection.objects = cutObjects.Select(t => t.gameObject).ToArray();
        }

        [Shortcut("GameObject/Shortcuts/Tag Selection For Move", KeyCode.X, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        public static void CutSelection()
        {
            cutObjects = Selection.transforms;
        }

        private static EditorWindow _gameView;

        private static EditorWindow GameView
        {
            get
            {
                if (_gameView != null) return _gameView;

                var name = "UnityEditor.GameView";

                // Get assembly that type is a part of
                Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;

                // Get the type
                Type type = assembly.GetType(name);

                // Use the type to get the right window
                EditorWindow gameView = EditorWindow.GetWindow(type);

                _gameView = gameView;

                return gameView;
            }
        }

        [Shortcut("Krillbite/Shortcuts/Toggle Game View Maximized", KeyCode.F, ShortcutModifiers.Action)]
        public static void ToggleGameViewMaximized()
        {
            // 16 nov 2016
            // This throws a weird error sometimes:
            // "Invalid editor window UnityEditor.ConsoleWindow"
            // Debug logging GameView.name gives me "UnityEngine.Debug:Log(Object)". Related?
            GameView.maximized = !GameView.maximized;
        }

        [Shortcut("GameObject/Tools/Toggle GameObjects", KeyCode.G, ShortcutModifiers.Action)]
        public static void ToggleSelectionEnabled()
        {
            foreach (var instanceID in Selection.instanceIDs)
            {
                GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

                if (go != null)
                {
                    Undo.RecordObject(go, "Set Active");
                    go.SetActive(!go.activeInHierarchy);
                }
                else
                {
                    Scene scene = GetSceneFromInstanceID(instanceID);

                    if (scene.IsValid())
                    {
                        if (scene.isLoaded)
                        {
                            // If the scene isn't saved before closing, all changes will be lost.
                            EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] {scene});

                            EditorSceneManager.CloseScene(scene, false);
                        }
                        else
                        {
                            EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
                        }
                    }
                }
            }
        }

        /*
        [Shortcut("Krillbite/Shortcuts/Take Screenshot")]
        public static void ScreenshotFromEditor()
        {
            Screenshotter.TakeScreenshot();
        }
        */

        public static void LiftDecal()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                Undo.RecordObject(go.gameObject, "Lift Decal");
                go.transform.localPosition += new Vector3(0.0f, 0.002f, 0.0f);
            }
        }

        public static void DropToGround(bool useBounds)
        {
            foreach (Transform t in Selection.transforms)
            {
                Undo.RecordObject(t.transform, "Drop To Ground");
                RaycastHit rayHit;
                Physics.Raycast(t.position, Vector3.down, out rayHit);

                Debug.DrawLine(t.position, rayHit.point, Color.red, 5f);

                if (rayHit.point != Vector3.zero)
                {
                    if (useBounds)
                    {
                        var bounds = new Bounds();

                        var renderer = t.GetComponent<Renderer>();
                        var collider = t.GetComponent<Collider>();

                        if (renderer == null && collider == null)
                        {
                            Debug.Log("Drop To Ground: No renderer or collider attached to object, aborting.");
                            continue;
                        }

                        if (renderer != null)
                        {
                            bounds = renderer.bounds;
                        }
                        else if (collider != null)
                        {
                            bounds = collider.bounds;
                        }

                        t.position = new Vector3(t.position.x, rayHit.point.y + bounds.extents.y, t.position.z);
                    }
                    else
                    {
                        t.position = new Vector3(t.position.x, rayHit.point.y, t.position.z);
                    }
                }
            }
        }

        public static void ReplaceSelectionWith(GameObject gameObject)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefab == null)
            {
                Debug.LogError(gameObject.name + " is not part of a prefab. Aborting.", gameObject);
                return;
            }

            ReplaceWithPrefab(Selection.gameObjects.ToList(), PrefabUtility.GetCorrespondingObjectFromSource(gameObject));
        }

        public static void ReplaceWithPrefab(List<GameObject> toBeReplaced, UnityEngine.Object prefabToReplaceWith, bool preserveOriginal = false, Vector3? positionOffset = null, Vector3? rotationOffset = null, Vector3? scaleOffset = null)
        {
            Debug.Log("To be replaced with: " + prefabToReplaceWith, prefabToReplaceWith);
            List<GameObject> newSelection = new List<GameObject>();

            foreach (GameObject go in toBeReplaced)
            {
                Transform newGo = ((GameObject) PrefabUtility.InstantiatePrefab(prefabToReplaceWith)).transform;
                newGo.parent = go.transform.parent;

                if (positionOffset.HasValue)
                    newGo.localPosition = go.transform.localPosition + go.transform.TransformDirection(positionOffset.Value);
                else
                    newGo.localPosition = go.transform.localPosition;

                if (rotationOffset.HasValue)
                    newGo.localRotation = Quaternion.Euler((go.transform.localRotation.eulerAngles + rotationOffset.Value));
                else
                    newGo.localRotation = go.transform.localRotation;

                if (scaleOffset.HasValue)
                    newGo.localScale = go.transform.localScale + scaleOffset.Value;
                else
                    newGo.localScale = go.transform.localScale;

                //newGo.gameObject.isStatic = go.gameObject.isStatic;

                newGo.gameObject.SetActive(go.gameObject.activeSelf);
                newSelection.Add(newGo.gameObject);

                Undo.RegisterCreatedObjectUndo(newGo.gameObject, "Replace with prefab");

                if (!preserveOriginal)
                {
                    Undo.DestroyObjectImmediate(go.gameObject);
                }
            }

            Selection.objects = newSelection.ToArray();
        }

        public static void FixBoxColliderSize()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                Undo.RecordObject(Selection.activeGameObject, "Fix Box Collider");

                if (Selection.activeGameObject.GetComponent<BoxCollider>() != null)
                {
                    BoxCollider collider = go.gameObject.GetComponent<BoxCollider>();
                    go.transform.localScale = Vector3.Scale(collider.size, go.transform.localScale);
                    go.transform.localPosition = go.transform.localPosition + collider.center;
                    collider.size = Vector3.one;
                    collider.center = Vector3.zero;
                }
                else
                {
                    Debug.Log(string.Format("Collider not found on GameObject: {0}", go.name));
                }
            }
        }

        public static void FindGameObjectsAtPosition()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                Undo.RecordObject(Selection.activeGameObject, "Fix Box Collider");

                if (Selection.activeGameObject.GetComponent<BoxCollider>() != null)
                {
                    BoxCollider collider = go.gameObject.GetComponent<BoxCollider>();
                    go.transform.localScale = Vector3.Scale(collider.size, go.transform.localScale);
                    go.transform.localPosition = go.transform.localPosition + collider.center;
                    collider.size = Vector3.one;
                    collider.center = Vector3.zero;
                }
                else
                {
                    Debug.Log(string.Format("Collider not found on GameObject: {0}", go.name));
                }
            }
        }

        private struct StoredTransform
        {
            public Matrix4x4 matrix;

            public Vector3 worldPosition;
            public Quaternion worldRotation;
            public Vector3 oldLocalScale;
            public Vector3 oldParentLocalScale;

            public Transform proxyTransform;
            public Transform parentTransform;

            public StoredTransform(Transform t)
            {
                this.matrix = t.worldToLocalMatrix;
                worldPosition = t.position;
                worldRotation = t.rotation;
                oldLocalScale = t.localScale;
                oldParentLocalScale = t.parent ? t.parent.localScale : Vector3.one;

                parentTransform = t.parent;

                proxyTransform = new GameObject("__TEMP_RESET_TRANSFORM_" + t.name).transform;
                Undo.RegisterCreatedObjectUndo(proxyTransform.gameObject, "Reset Transform");

                proxyTransform.SetParent(t.parent, false);
                proxyTransform.position = t.position;
                proxyTransform.rotation = t.rotation;
                proxyTransform.localScale = t.localScale;

                proxyTransform.SetParent(null, true);

//			proxyTransform = 
            }

            public void ApplyWithMatrix(Transform t)
            {
                t.localPosition = matrix.MultiplyPoint(Vector3.zero);
                t.localRotation = Quaternion.LookRotation(matrix.MultiplyVector(Vector3.forward));
                t.localScale = matrix.MultiplyVector(Vector3.one);
            }

            public void ApplyWithCachedValues(Transform t, bool position, bool rotation, bool scale)
            {
                if (position)
                    t.position = worldPosition;

                if (rotation)
                    t.rotation = worldRotation;

                if (scale)
                {
                    var s = oldLocalScale;
                    s.Scale(oldParentLocalScale);
                    t.localScale = s;
                }
            }

            public void ApplyWithTransform(Transform t, bool position, bool rotation, bool scale)
            {
                proxyTransform.SetParent(parentTransform, true);
                if (position)
                    t.position = proxyTransform.position;

                if (rotation)
                    t.rotation = proxyTransform.rotation;

                if (scale)
                {
                    t.localScale = proxyTransform.localScale;
                }

                Object.DestroyImmediate(proxyTransform.gameObject);
            }
        }

        public static void ResetTransformWithoutAffectingChildren(Transform[] targets, bool position = true, bool rotation = true, bool scale = true)
        {
            foreach (var target in targets)
            {
                if (target == null || (!position && !rotation && !scale)) return;

                Undo.RecordObject(target, "Reset Transform");

                List<Transform> children = new List<Transform>();
                
                foreach (Transform child in target)
                {
                    children.Add(child);
                }

                Dictionary<Transform, StoredTransform> originalMatrices = children.ToDictionary(t => t, t => new StoredTransform(t));

                foreach (var t in children)
                {
                    Undo.RecordObject(t, "Reset Transform");
                }

                if (position) target.localPosition = Vector3.zero;
                if (rotation) target.localRotation = Quaternion.identity;
                if (scale) target.localScale = Vector3.one;

                foreach (var t in originalMatrices.Keys)
                {
                    originalMatrices[t].ApplyWithTransform(t, position, rotation, scale);
                }
            }
        }

        [MenuItem("GameObject/Tools/Sort Selected Transforms By Name")]
        public static void SortTransformsInHierarchy()
        {
            var transforms = Selection.transforms.ToList();

            transforms = transforms.OrderByDescending(t => t.name).Reverse().ToList();

            var siblingIndexStart = 99999;

            Undo.RecordObjects(Selection.objects, "Sort Transforms By Name");

            foreach (var transform in transforms)
            {
                if (transform.GetSiblingIndex() < siblingIndexStart)
                {
                    siblingIndexStart = transform.GetSiblingIndex();
                }
            }

            for (int i = 0; i < transforms.Count; i++)
            {
                var t = transforms[i];

                t.SetSiblingIndex(i + siblingIndexStart);
            }
        }

        [Shortcut("GameObject/Tools/Reveal Mesh In Project", KeyCode.F, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void RevealMeshLocationInProject()
        {
            if (Selection.activeGameObject == null) return;

            if (Selection.activeGameObject.GetComponentsInChildren<MeshFilter>().Length > 0)
            {
                MeshFilter meshFilter = Selection.activeGameObject.GetComponentsInChildren<MeshFilter>().First(mesh => AssetDatabase.GetAssetPath(mesh.sharedMesh) != "Library/unity default resources");

                if (meshFilter != null)
                {
                    EditorGUIUtility.PingObject(meshFilter.sharedMesh);
                    return;
                }
            }

            SkinnedMeshRenderer skinnedMesh = Selection.activeGameObject.GetComponentInChildren<SkinnedMeshRenderer>();

            if (skinnedMesh != null)
            {
                EditorGUIUtility.PingObject(skinnedMesh.sharedMesh);
                return;
            }
        }

        [Shortcut("GameObject/Tools/Reveal Selection In Project", KeyCode.F, ShortcutModifiers.Alt)]
        public static void RevealSelectionInProject()
        {
            var scenes = GetSelectedScenes();
            if (scenes.Length > 0)
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(scenes[0].path, typeof(object)));
                return;
            }

            if (Selection.activeGameObject == null) return;

            UnityEngine.Object prefab = PrefabUtility.GetCorrespondingObjectFromSource(Selection.activeGameObject);

            if (prefab != null)
            {
                Selection.objects = new[] {prefab};
                //EditorGUIUtility.PingObject();
            }
        }


        private static AnimatorStateTransition[] copyAnimatorStateTransitions;
        private static AnimatorTransition[] copyAnimatorTransitions;
        private static AnimatorCondition[] copyAnimatorConditions;

//	[MenuItem("Krillbite/Shortcuts/Animator/Copy Animator Transition " + BearsHotkeys.CopyAnimatorTransition)]
        private static void CopyAnimatorTransition()
        {
            Debug.Log("Selected: " + Selection.activeObject);

            var animatorState = Selection.activeObject as AnimatorState;
            var animatorStateMachine = Selection.activeObject as AnimatorStateMachine;

            if (animatorState != null)
            {
                Debug.Log("Copying Animator State Transitions");
                copyAnimatorStateTransitions = animatorState.transitions;
            }

            if (animatorStateMachine != null)
            {
                Debug.Log("Copying Animator State MACHINE Transitions");
                //copyAnimatorStateTransitions = animatorState.transitions;
            }
        }

//	[MenuItem("Krillbite/Shortcuts/Animator/Paste Animator Transition " + BearsHotkeys.PasteAnimatorTransition)]
        private static void PasteAnimatorTransition()
        {
            var sel = Selection.activeObject;
            if (copyAnimatorStateTransitions != null && sel is AnimatorState)
            {
                var state = (AnimatorState) sel;
                Undo.RecordObject(state, "Set Transition");

                foreach (var transition in copyAnimatorStateTransitions)
                {
                    // TODO ADD TRANSITIONS _TO_ STATE AS WELL

                    if (transition.destinationState != null || transition.destinationStateMachine != null)
                    {
                        var t = new AnimatorStateTransition
                        {
                            canTransitionToSelf = transition.canTransitionToSelf,
                            duration = transition.duration,
                            exitTime = transition.exitTime,
                            hasExitTime = transition.hasExitTime,
                            hasFixedDuration = transition.hasFixedDuration,
                            interruptionSource = transition.interruptionSource,
                            offset = transition.offset,
                            orderedInterruption = transition.orderedInterruption,
                            destinationState = transition.destinationState,
                            destinationStateMachine = transition.destinationStateMachine,
                            conditions = transition.conditions,
                            isExit = transition.isExit,
                            mute = transition.mute,
                            solo = transition.solo
                        };

                        state.AddTransition(t);
                    }
                }
            }
        }

//	[MenuItem("Krillbite/Shortcuts/Animator/Make Transition Instant " + BearsHotkeys.SetAnimatorTransitionInstant)]
        private static void SetAnimatorTransitionsInstant()
        {
            foreach (var obj in Selection.objects)
            {
                var transition = obj as AnimatorStateTransition;

                if (transition == null) return;

                Undo.RecordObject(transition, "Set Transition");

                transition.duration = 0;
                transition.hasFixedDuration = true;
                transition.hasExitTime = false;
            }
        }

//    [MenuItem("Krillbite/Shortcuts/Animator/Make Transition Wait For End " + BearsHotkeys.SetAnimatorTransitionWaitForEnd)]
        private static void SetAnimatorTransitionsWaitForEnd()
        {
            foreach (var obj in Selection.objects)
            {
                var transition = obj as AnimatorStateTransition;

                if (transition == null) return;

                Undo.RecordObject(transition, "Set Transition");

                transition.exitTime = 1;
                transition.hasExitTime = true;
                transition.duration = 0f;
            }
        }

//	[MenuItem("Krillbite/Shortcuts/Animator/Copy Animator Condition " + BearsHotkeys.CopyAnimatorCondition)]
        private static void CopyAnimatorCondition()
        {
            var transition = Selection.activeObject as AnimatorTransitionBase;

            if (transition != null)
            {
                copyAnimatorConditions = transition.conditions;
            }
        }

//	[MenuItem("Krillbite/Shortcuts/Animator/Paste Animator Condition " + BearsHotkeys.PasteAnimatorCondition)]
        private static void PasteAnimatorCondition()
        {
            foreach (var transition in Selection.objects.OfType<AnimatorTransitionBase>())
            {
                Undo.RecordObject(transition, "Set Condition");
                transition.conditions = copyAnimatorConditions;
            }
        }

        [MenuItem("Krillbite/Unity Internal/Unload Unused Assets")]
        private static void UnloadUnusedAssets()
        {
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        [MenuItem("Krillbite/Unity Internal/Clear Progress Bar")]
        private static void ClearProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}