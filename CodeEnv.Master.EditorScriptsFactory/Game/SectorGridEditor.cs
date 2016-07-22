// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorGridEditor.cs
// Custom Editor for SectorGrid.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor for SectorGrid.
/// </summary>
[CustomEditor(typeof(SectorGrid))]
public class SectorGridEditor : Editor {

    public override void OnInspectorGUI() {
        var sectorGrid = target as SectorGrid;

        sectorGrid.sectorVisibilityDepth = EditorGUILayout.FloatField(new GUIContent("Sector Visibility Depth", "Controls how many sectors are visible when in SectorViewMode."), sectorGrid.sectorVisibilityDepth);

        sectorGrid.enableGridSizeLimit = EditorGUILayout.Toggle(new GUIContent("Enable GridSize Limits", "Allows limiting the size of the sector grid for debugging."), sectorGrid.enableGridSizeLimit);

        if (sectorGrid.enableGridSizeLimit) {
            EditorGUI.indentLevel++;
            sectorGrid.debugMaxGridSize = EditorGUILayout.Vector3Field(new GUIContent("Max Grid Size", "Max size of the grid of sectors. Must be cube of even values."), sectorGrid.debugMaxGridSize);
            EditorGUI.indentLevel--;
        }

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

