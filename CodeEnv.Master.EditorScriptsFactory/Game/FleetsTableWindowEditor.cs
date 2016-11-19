﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetsTableWindowEditor.cs
// Custom editor for the FleetsTableWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Custom editor for the FleetsTableWindow.
/// </summary>
[CustomEditor(typeof(FleetsTableWindow))]
public class FleetsTableWindowEditor : ATableWindowEditor<FleetsTableWindow> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

