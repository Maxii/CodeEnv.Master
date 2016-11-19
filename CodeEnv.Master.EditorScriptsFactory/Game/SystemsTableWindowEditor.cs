// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemsTableWindowEditor.cs
// Custom editor for the SystemsTableWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Custom editor for the SystemsTableWindow.
/// </summary>
[CustomEditor(typeof(SystemsTableWindow))]
public class SystemsTableWindowEditor : ATableWindowEditor<SystemsTableWindow> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

