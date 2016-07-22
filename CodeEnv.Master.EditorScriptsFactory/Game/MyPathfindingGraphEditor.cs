// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyPathfindingGraphEditor.cs
// Custom Editor for MyPathfindingGraph.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace Pathfinding {

    using CodeEnv.Master.Common;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Custom Editor for MyPathfindingGraph.
    /// </summary>
    [CustomGraphEditor(typeof(MyPathfindingGraph), "MyPathfindingGraph")]
    public class MyPathfindingGraphEditor : GraphEditor {

        private static readonly Color NodeColor = new Color(0.161F, 0.341F, 1F, 0.5F);  // Light blue

        public override void OnInspectorGUI(NavGraph target) {
            var graph = target as MyPathfindingGraph;
            graph.maxDistance = EditorGUILayout.FloatField(new GUIContent("Max Distance", "The max distance in world space for a connection to be valid. A zero counts as infinity"), graph.maxDistance);
            graph.optimizeForSparseGraph = EditorGUILayout.Toggle(new GUIContent("Optimize For Sparse Graph", "Check online documentation for more information."), graph.optimizeForSparseGraph);
        }

        public override void OnDrawGizmos() {
            var graph = target as MyPathfindingGraph;
            if (graph == null || graph.active == null || !graph.active.showNavGraphs) {
                return;
            }

            Gizmos.color = NodeColor;
            var nodes = graph.nodes;
            for (int i = 0; i < graph.nodeCount; i++) {
                var node = nodes[i];
                if (node.Walkable) {
                    Vector3 position = (Vector3)node.position;
                    Gizmos.DrawCube(position, Vector3.one * HandleUtility.GetHandleSize(position) * 0.1F);
                }
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

