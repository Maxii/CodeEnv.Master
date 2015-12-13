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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
        _subscriptions.Add(MainCameraControl.Instance.SubscribeToPropertyChanging<MainCameraControl, ICameraFocusable>(cc => cc.CurrentFocus, CurrentFocusPropChangingHandler));
        _subscriptions.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, ICameraFocusable>(cc => cc.CurrentFocus, CurrentFocusPropChangedHandler));
    }

    #region Event and Property Change Handlers

    private void CurrentFocusPropChangingHandler(ICameraFocusable newFocus) {
        var previousFocus = MainCameraControl.Instance.CurrentFocus;
        if (previousFocus != null && previousFocus.IsRetainedFocusEligible) {
            var previousMortalFocus = previousFocus as IMortalItem;
            if (previousMortalFocus != null) {

                previousMortalFocus.deathOneShot -= RetainedFocusDeathEventHandler;
            }
        }
    }

    private void CurrentFocusPropChangedHandler() {
        ICameraFocusable focus = MainCameraControl.Instance.CurrentFocus;
        TryRetainingFocus(focus);
    }

    private void RetainedFocusDeathEventHandler(object sender, EventArgs e) {
        ICameraFocusable focusableItem = sender as ICameraFocusable;
        D.Assert(focusableItem == _retainedFocus);
        RefreshReadout(string.Empty);
        _retainedFocus = null;
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
            var mortalFocus = focus as IMortalItem;
            if (mortalFocus != null) {

                mortalFocus.deathOneShot += RetainedFocusDeathEventHandler;
            }
            RefreshReadout(focus.DisplayName);
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
        if (_retainedFocus != null) {
            var mortalRetainedFocus = _retainedFocus as IMortalItem;
            if (mortalRetainedFocus != null) {
                //mortalRetainedFocus.onDeathOneShot -= OnRetainedFocusDeath;
                mortalRetainedFocus.deathOneShot -= RetainedFocusDeathEventHandler;
            }
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

