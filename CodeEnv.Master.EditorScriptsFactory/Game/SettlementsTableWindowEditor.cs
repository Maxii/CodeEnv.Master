// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementsTableWindowEditor.cs
// Custom editor for the SettlementsTableWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Custom editor for the SettlementsTableWindow.
/// </summary>
[CustomEditor(typeof(SettlementsTableWindow))]
public class SettlementsTableWindowEditor : ATableWindowEditor<SettlementsTableWindow> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

