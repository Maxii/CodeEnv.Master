// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ATableWindowEditor.cs
// Custom editor for ATableWindows, patterned after SpaceD.UIWindowInspector.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Custom editor for ATableWindows, patterned after SpaceD.UIWindowInspector.  
/// </summary>
public abstract class ATableWindowEditor<T> : AGuiWindowEditor<T> where T : ATableWindow {

    protected override void DrawDerivedClassProperties() {
        base.DrawDerivedClassProperties();
        NGUIEditorTools.DrawProperty("Row Prefab", serializedObject, "rowPrefab");
    }

}

