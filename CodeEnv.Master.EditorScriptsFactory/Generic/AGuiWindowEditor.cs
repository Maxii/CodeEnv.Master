// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiWindowEditor.cs
// Custom editor for AGuiWindows, patterned after SpaceD.UIWindowInspector. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using UnityEditor;

/// <summary>
/// Custom editor for AGuiWindows, patterned after SpaceD.UIWindowInspector. 
/// </summary>
public abstract class AGuiWindowEditor<T> : Editor where T : AGuiWindow {

    public virtual string DebugName { get { return typeof(T).Name; } }

    public override void OnInspectorGUI() {
        var window = target as T;

        serializedObject.Update();

        DrawDerivedClassProperties();

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        {
            NGUIEditorTools.DrawProperty("Start Hidden", serializedObject, "startHidden");
            NGUIEditorTools.DrawProperty("Use Fading", serializedObject, "useFading");
            NGUIEditorTools.DrawProperty("Fading Duration", serializedObject, "fadeDuration");
        }
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();

        DrawEvents(window);
    }

    protected virtual void DrawDerivedClassProperties() { }

    private void DrawEvents(T window) {
        NGUIEditorTools.DrawEvents("On Show Begin", window, window.onShowBegin);
        NGUIEditorTools.DrawEvents("On Show Complete", window, window.onShowComplete);

        NGUIEditorTools.DrawEvents("On Hide Begin", window, window.onHideBegin);
        NGUIEditorTools.DrawEvents("On Hide Complete", window, window.onHideComplete);
    }

    public sealed override string ToString() {
        return DebugName;
    }

}

