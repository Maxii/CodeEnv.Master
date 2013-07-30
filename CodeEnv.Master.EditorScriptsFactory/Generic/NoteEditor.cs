// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NoteEditor.cs
// Allows the addition of a custom note field to any GameObject.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

#if (UNITY_EDITOR)

using UnityEditor;
using UnityEngine;

/// <summary>
/// Allows the addition of a custom note field to any GameObject.
/// </summary>
[CustomEditor(typeof(Note))]
public class NoteEditor : Editor {

    private Note note;
    private Vector2 scrollPos;
    private float textAreaWidth;
    private GUIStyle guiStyle;
    private bool isInitialized;

    private void Init() {
        note = base.target as Note;
        // FIXME
        //EditorWindow editorWindow = EditorWindow.GetWindow<>(TempGameValues.InspectorWindowName);
        //Debug.Log("Editor Window Name = " + editorWindow.item);
        //textAreaWidth = editorWindow._location.width - 50F;
        textAreaWidth = 400F;
        guiStyle = new GUIStyle();
        guiStyle.wordWrap = true;
        guiStyle.stretchWidth = false;
        isInitialized = true;
    }

    public override void OnInspectorGUI() {
        if (!isInitialized) {
            Init();
        }
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(100.0F));
        note.text = GUILayout.TextArea(note.text, guiStyle, GUILayout.Width(textAreaWidth), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
        Repaint();
    }
}

#endif
