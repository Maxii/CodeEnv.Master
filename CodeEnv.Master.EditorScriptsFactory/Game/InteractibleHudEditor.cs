// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: InteractibleHudEditor.cs
// Custom editor for the InteractibleHudWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Custom editor for the InteractibleHudWindow.
/// </summary>
[CustomEditor(typeof(InteractibleHudWindow))]
public class InteractibleHudEditor : AGuiWindowEditor<InteractibleHudWindow> {

    protected override void DrawDerivedClassProperties() {
        base.DrawDerivedClassProperties();
        NGUIEditorTools.DrawProperty("Hide Exceptions", serializedObject, "_hideExceptions");
    }

}

