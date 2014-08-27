// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCreatorEditor.cs
// Custom editor for FleetUnitCreators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for FleetUnitCreators.
/// </summary>
[CustomEditor(typeof(FleetUnitCreator))]
public class FleetCreatorEditor : AUnitCreatorEditor<FleetUnitCreator> {

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        var fleetCreator = target as FleetUnitCreator;
        fleetCreator.move = GUILayout.Toggle(fleetCreator.move, "Get Fleet Underway");
        if (fleetCreator.move) {
            EditorGUI.indentLevel++;
            fleetCreator.attack = GUILayout.Toggle(fleetCreator.attack, "Attack Targets");
            EditorGUI.indentLevel--;
        }
    }

    protected override int GetMaxElements() {
        return TempGameValues.MaxShipsPerFleet;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

