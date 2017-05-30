// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiFocusedReadout.cs
// Retained Focus readout class for the Gui, based on Ngui UILabel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Retained Focus readout class for the Gui, based on Ngui UILabel.
/// </summary>
public class GuiFocusedReadout : AGuiLabelReadout {

    protected override string TooltipContent { get { return "The last game object in focus."; } }

    private ICameraFocusable _retainedFocus;
    private IList<IDisposable> _subscriptions;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, ICameraFocusable>(cc => cc.CurrentFocus, CurrentFocusPropChangedHandler));
    }

    // Note: No need to handle the death event of an ICameraFocusable as they always null MainCameraControl.Focus when dieing
    // This generates a Focus Prop Changed event which handles this readout

    #region Event and Property Change Handlers

    private void CurrentFocusPropChangedHandler() {
        ICameraFocusable focus = MainCameraControl.Instance.CurrentFocus;
        TryRetainingFocus(focus);
    }

    private void HandleMiddleClick() {
        if (_retainedFocus != null) {
            _retainedFocus.IsFocus = true;
        }
    }

    private void ClickEventHandler() {
        if (GameInputHelper.Instance.IsMiddleMouseButton) {
            HandleMiddleClick();
        }
    }

    void OnClick() {
        ClickEventHandler();
    }

    #endregion

    private void TryRetainingFocus(ICameraFocusable focus) {
        if (focus != null && focus.IsRetainedFocusEligible) {
            _retainedFocus = focus;
            RefreshReadout(focus.DebugName);
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
    }

}

