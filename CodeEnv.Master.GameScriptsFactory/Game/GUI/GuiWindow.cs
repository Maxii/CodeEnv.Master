// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiWindow.cs
// Gui Window with fading ability able to handle a single heirarchy of content.
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

/// <summary>
/// Gui Window with fading ability able to handle a single heirarchy of content.
/// </summary>
public class GuiWindow : AGuiWindow {

    protected override void Awake() {
        base.Awake();
        InitializeOnAwake();
    }

    public void Show() { ShowWindow(); }

    public void Hide() { HideWindow(); }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

