// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiShowModeControlButton.cs
// Button that  determines whether the fixed panels of the Gui should be hidden or shown.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Button that determines whether the fixed panels of the Gui should be hidden or shown.
/// </summary>
public class GuiShowModeControlButton : AGuiButton {

    /// <summary>
    /// Indicates whether to show or hide the fixed panels of the Gui when this button is clicked.
    /// </summary>
    [Tooltip("Should the fixed panels of the Gui be shown or hidden when the button is clicked?")]
    [SerializeField]
    ////[FormerlySerializedAs("showModeOnClick")]
    public ShowMode _showModeOnClick = ShowMode.None;

    /// <summary>
    /// The UIPanels that should not be hidden.
    /// </summary>
    [Tooltip("Drag/Drop panels that should not be hidden when Hide is selected above here")]
    [SerializeField]
    ////[FormerlySerializedAs("hideExceptions")]
    public List<UIPanel> _hideExceptions;

    protected override IList<KeyCode> ValidKeys { get { return new List<KeyCode>() { KeyCode.Return }; } }

    protected override void Awake() {
        base.Awake();
        D.Assert(_showModeOnClick != ShowMode.None, gameObject, "Illegal ShowMode setting.");
    }

    #region Event and Property Change Handlers

    protected override void HandleValidClick() {
        if (_showModeOnClick == ShowMode.Show) {
            GuiManager.Instance.ShowFixedPanels();
        }
        else {
            GuiManager.Instance.HideFixedPanels(_hideExceptions);
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

