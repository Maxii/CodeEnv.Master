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

/// <summary>
/// Custom editor for FleetUnitCreators.
/// </summary>
[CustomEditor(typeof(FleetUnitCreator))]
public class FleetCreatorEditor : AUnitCreatorEditor<FleetUnitCreator> {

    protected override int GetMaxElements() {
        return TempGameValues.MaxShipsPerFleet;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

