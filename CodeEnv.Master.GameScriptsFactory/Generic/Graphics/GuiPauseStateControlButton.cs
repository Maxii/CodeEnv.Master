// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPauseStateControlButton.cs
// Executes the selected PauseRequest on button LClk.
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
/// Executes the selected PauseRequest on button LClk.
/// </summary>
public class GuiPauseStateControlButton : AGuiButton {

    protected override IList<KeyCode> ValidKeys { get { return new List<KeyCode>() { KeyCode.Return }; } }

    //[FormerlySerializedAs("pauseRequestOnClick")]
    [Tooltip("The Pause/Resume action to take when clicked")]
    [SerializeField]
    private PauseRequest _pauseRequestOnClick = PauseRequest.None;

    #region Event and Property Change Handlers

    protected override void HandleValidClick() {
        if (_pauseRequestOnClick == default(PauseRequest)) {
            D.WarnContext(this, "{0}.{1} not set.", GetType().Name, typeof(PauseRequest).Name);
        }
        bool toPause = _pauseRequestOnClick == PauseRequest.Pause;
        _gameMgr.RequestPauseStateChange(toPause);
    }

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    public enum PauseRequest {
        None,
        Pause,
        Resume
    }

    #endregion

}

