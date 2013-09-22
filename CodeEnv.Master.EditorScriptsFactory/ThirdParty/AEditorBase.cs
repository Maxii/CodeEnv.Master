// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EditorBase.cs
// Abstract generic base class for all Custom Editors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using UnityEditor;
using UnityEngine;

/// <summary>
/// Abstract generic base class for all Custom Editors. Usage:
/// <code>
/// [CustomEditor(typeof(MyClass))]
/// public class MyClassEditor : BaseEditor<MyClass> { 
/// // empty 
/// }
/// </code>
/// Note that it uses an annotation in your class code to generate a tooltip in the inspector.
/// </summary>
/// <see cref="http://devmag.org.za/2012/07/12/50-tips-for-working-with-unity-best-practices/"/>
public abstract class AEditorBase<T> : Editor where T : MonoBehaviour {

    public override void OnInspectorGUI() {
        T data = (T)target;

        GUIContent label = new GUIContent();
        label.text = "Properties";

        UnityEditorUtility.DrawDefaultInspectors<T>(label, data);

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }


}

