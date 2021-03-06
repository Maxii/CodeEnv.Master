﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipDesignWindowEditor.cs
// Custom Editor for ShipDesignWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using UnityEditor;

/// <summary>
/// Custom Editor for ShipDesignWindow.
/// </summary>
[System.Obsolete("Extend to all DesignWindows once DesignScreensManager is no longer used")]
[CustomEditor(typeof(ShipDesignWindow))]
public class ShipDesignWindowEditor : AGuiWindowEditor<ShipDesignWindow> {

    protected override void DrawDerivedClassProperties() {  // 4.29.18 Properties not currently used what with use of DesignScreensManager
        base.DrawDerivedClassProperties();
        NGUIEditorTools.SetLabelWidth(140F);
        NGUIEditorTools.DrawProperty("Design Icon Prefab", serializedObject, "_designIconPrefab");
        NGUIEditorTools.DrawProperty("Equipment Icon Prefab", serializedObject, "_equipmentIconPrefab");
        NGUIEditorTools.DrawProperty("3DModelStage Prefab", serializedObject, "_threeDModelStagePrefab");

        ////NGUIEditorTools.SetLabelWidth(160F);
        ////NGUIEditorTools.DrawProperty("Include Obsolete Designs", serializedObject, "_includeObsoleteDesigns");
    }

}

