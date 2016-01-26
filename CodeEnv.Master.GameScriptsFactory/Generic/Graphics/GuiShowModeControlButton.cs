﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiShowModeControlButton.cs
//  Button that  determines whether the fixed panels of the Gui should be hidden or shown.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Button that  determines whether the fixed panels of the Gui should be hidden or shown.
/// </summary>
public class GuiShowModeControlButton : AGuiButton {

    /// <summary>
    /// Indicates whether to show or hide the fixed panels of the Gui when this button is clicked.
    /// </summary>
    public ShowMode showModeOnClick = ShowMode.None;    // Has Editor

    /// <summary>
    /// The UIPanels that should not be hidden.
    /// </summary>
    public List<UIPanel> hideExceptions;    // Has Editor

    protected override IList<KeyCode> ValidKeys { get { return new List<KeyCode>() { KeyCode.Return }; } }

    protected override void Awake() {
        base.Awake();
        D.Assert(showModeOnClick != ShowMode.None, gameObject, "{0} has illegal {1} setting.", GetType().Name, typeof(ShowMode).Name);
    }

    #region Event and Property Change Handlers

    protected override void HandleValidClick() {
        if (showModeOnClick == ShowMode.Show) {
            GuiManager.Instance.ShowFixedPanels();
        }
        else {
            GuiManager.Instance.HideFixedPanels(hideExceptions);
        }
    }

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    public enum ShowMode {
        None,
        Show,
        Hide
    }

    #endregion
}

