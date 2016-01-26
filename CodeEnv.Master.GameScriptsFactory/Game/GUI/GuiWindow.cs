﻿// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Gui Window with fading ability able to handle a single content root.
/// </summary>
public class GuiWindow : AGuiWindow {

    public Transform contentHolder = null; // Has Editor

    protected override Transform ContentHolder { get { return contentHolder; } }

    protected override void Awake() {
        base.Awake();
        InitializeOnAwake();
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        D.Assert(contentHolder != null, gameObject, "{0}.ContentHolder has not been set.", GetType().Name);
    }

    public void Show() { ShowWindow(); }

    public void Hide() { HideWindow(); }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

