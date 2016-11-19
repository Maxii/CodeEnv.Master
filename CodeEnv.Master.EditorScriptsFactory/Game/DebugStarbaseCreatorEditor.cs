// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugStarbaseCreatorEditor.cs
// Custom editor for DebugStarbaseCreators. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Custom editor for DebugStarbaseCreators. 
/// </summary>
[CustomEditor(typeof(DebugStarbaseCreator))]
public class DebugStarbaseCreatorEditor : ADebugUnitCreatorEditor {

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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

