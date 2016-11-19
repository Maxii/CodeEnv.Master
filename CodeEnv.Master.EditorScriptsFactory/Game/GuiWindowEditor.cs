// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiWindowEditor.cs
// Custom editor for GuiWindows.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Custom editor for GuiWindows.
/// </summary>
[CustomEditor(typeof(GuiWindow))]
public class GuiWindowEditor : AGuiWindowEditor<GuiWindow> {

    protected override void DrawDerivedClassProperties() {
        base.DrawDerivedClassProperties();
        NGUIEditorTools.DrawProperty("Content Holder", serializedObject, "contentHolder");
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

