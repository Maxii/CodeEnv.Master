// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSettlementCreatorEditor.cs
// Custom editor for DebugSettlementCreators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for DebugSettlementCreators.
/// </summary>
[CustomEditor(typeof(DebugSettlementCreator))]
public class DebugSettlementCreatorEditor : ADebugUnitCreatorEditor {

    //public override void OnInspectorGUI() {
    //    base.OnInspectorGUI();

    //    serializedObject.Update();

    //    EditorGUI.BeginDisabledGroup(_toDisableCreator);
    //    {
    //        // TODO
    //    }
    //    EditorGUI.EndDisabledGroup();

    //    serializedObject.ApplyModifiedProperties();
    //}


}

