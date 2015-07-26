// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HoveredItemHudEditor.cs
// Custom editor for the HoveredItemHudWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Custom editor for the HoveredItemHudWindow.
/// </summary>
[CustomEditor(typeof(HoveredItemHudWindow))]
public class HoveredItemHudEditor : AGuiWindowEditor<HoveredItemHudWindow> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

