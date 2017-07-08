// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiWindow.cs
// Gui Window with fading ability able to handle a single content root.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Gui Window with fading ability able to handle a single content root.
/// </summary>
public class GuiWindow : AGuiWindow {

    public Transform contentHolder = null; // Has Editor

    protected override Transform ContentHolder { get { return contentHolder; } }

    protected sealed override void Awake() {
        base.Awake();
        InitializeOnAwake();
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        D.AssertNotNull(contentHolder);
    }

    public void Show() {
        ShowWindow();
    }

    public void Hide() {
        HideWindow();
    }

}

