// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementUnitCreator.cs
// Custom editor for SettlementUnitCreators. 
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
/// Custom editor for SettlementUnitCreators. 
/// </summary>
[CustomEditor(typeof(SettlementUnitCreator))]
public class SettlementCreatorEditor : AUnitCreatorEditor<SettlementUnitCreator> {

    protected override int GetMaxElements() {
        return TempGameValues.MaxFacilitiesPerBase;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

