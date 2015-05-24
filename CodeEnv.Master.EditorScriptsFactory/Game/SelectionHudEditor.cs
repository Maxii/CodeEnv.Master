// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectionHudEditor.cs
// Custom editor for SelectionHud.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Custom editor for SelectionHud.
/// </summary>
[CustomEditor(typeof(SelectionHud))]
public class SelectionHudEditor : AGuiWindowEditor<SelectionHud> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

