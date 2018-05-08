// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TableWindowEditor.cs
// Custom editor for TableWindow, patterned after SpaceD.UIWindowInspector.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using UnityEditor;

/// <summary>
/// Custom editor for TableWindow, patterned after SpaceD.UIWindowInspector.  
/// </summary>
[CustomEditor(typeof(TableWindow))]
public class TableWindowEditor : AGuiWindowEditor<TableWindow> {

    protected override void DrawDerivedClassProperties() {
        base.DrawDerivedClassProperties();

        NGUIEditorTools.DrawProperty("Title Label", serializedObject, "_titleLabel");
        NGUIEditorTools.DrawProperty("Done Button", serializedObject, "_doneButton");

    }

}

