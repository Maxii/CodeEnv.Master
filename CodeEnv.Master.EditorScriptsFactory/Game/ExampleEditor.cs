// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ExampleEditor.cs
// Example Custom Editor using EditorBase class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using UnityEditor;

/// <summary>
/// Example Custom Editor using EditorBase class.
/// </summary>
[CustomEditor(typeof(PlanetoidItem))]
public class ExampleEditor : AEditorBase<PlanetoidItem> { }

