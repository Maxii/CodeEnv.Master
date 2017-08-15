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

    private static IEnumerable<KeyCode> _validKeys = new KeyCode[] { KeyCode.Return };

    /// <summary>
    /// Indicates whether to show or hide the fixed panels of the Gui when this button is clicked.
    /// </summary>
    [Tooltip("Should the fixed panels of the Gui be shown or hidden when the button is clicked?")]
    [SerializeField]
    private ShowMode _showModeOnClick = ShowMode.None;

#pragma warning disable 0649

    /// <summary>
    /// The UIPanels that should not be hidden.
    /// </summary>
    [Tooltip("Drag/Drop panels that should not be hidden when Hide is selected above here")]
    [SerializeField]
    private List<UIPanel> _hideExceptions;

#pragma warning restore 0649

    protected override IEnumerable<KeyCode> ValidKeys { get { return _validKeys; } }

    protected override void HandleValidClick() {
        if (_showModeOnClick == ShowMode.Show) {
            GuiManager.Instance.ShowHiddenPanels();
        }
        else {
            GuiManager.Instance.HideShowingPanels(_hideExceptions);
        }
    }

    #region Event and Property Change Handlers

    #endregion

    protected override void Cleanup() { }

    #region Debug

    protected override void __Validate() {
        base.__Validate();
        D.Assert(_showModeOnClick != ShowMode.None, gameObject, "Illegal ShowMode setting.");
    }

    #endregion


    #region Nested Classes

    public enum ShowMode {
        None,
        Show,
        Hide
    }

    #endregion
}

