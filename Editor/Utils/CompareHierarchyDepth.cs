using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BearsEditorTools
{
    public static class CompareHierarchyDepth
    {
        public static IEnumerable<T> OrderByPositionInHierarchy<T>(this IEnumerable<T> enumerable) where T : Component
        {
            return enumerable.OrderBy(comp => new ComparableLinearHierarchyDepth(comp.transform));
        }

        /// <summary>
        /// Use this to sort a list of gameobjects by their relative position in the hierarchy (as if the entire hierarchy is expanded)
        /// Example: sortedGameobjects = unsortedGameobjects.OrderBy(go => new HierarchySortable(go)).ToList();
        /// </summary>
        private struct ComparableLinearHierarchyDepth : IComparable<ComparableLinearHierarchyDepth>
        {
            private List<int> _indexTree;

            public ComparableLinearHierarchyDepth(Transform t) : this()
            {
                _indexTree = new List<int> { t.GetSiblingIndex() };

                // walk up the parent chain and store the sibling index of each parent.
                while (t.parent)
                {
                    _indexTree.Insert(0, t.parent.GetSiblingIndex());

                    t = t.parent;
                }
            }

            public int CompareTo(ComparableLinearHierarchyDepth other)
            {
                if (_indexTree == null || other._indexTree == null)
                    return 0;
                
                int aCount = this._indexTree.Count;
                int bCount = other._indexTree.Count;
                
                for (int i = 0; i < Math.Max(bCount, aCount); i++)
                {
                    // If either index tree is at its max depth and is shorter than the other, the shorter one is considered to be "less" than the longer one.
                    bool aMaxed = i >= aCount;
                    bool bMaxed = i >= bCount;

                    if (aMaxed && !bMaxed)
                        return -1;
                    else if (!aMaxed && bMaxed)
                        return 1;
                    
                    int a = this._indexTree[i];
                    int b = other._indexTree[i];

                    if (a > b)
                        return 1;
                    else if (a < b)
                        return -1;
                }

                return 0;
            }
        }
    }
}