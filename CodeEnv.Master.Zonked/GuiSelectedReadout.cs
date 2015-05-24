// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiSelectedReadout.cs
// Selection readout class for the Gui, based on Ngui UILabel.
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
/// Selection readout class for the Gui, based on Ngui UILabel.
/// </summary>
[Obsolete]
public class GuiSelectedReadout : AGuiLabelReadout {

    protected override string TooltipContent {
        get { return "The currently selected game object."; }
    }

    private IList<IDisposable> _subscriptions;
    private SelectionManager _selectionMgr;

    protected override void Awake() {
        base.Awake();
        _selectionMgr = SelectionManager.Instance;
        Subscribe();
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_selectionMgr.SubscribeToPropertyChanged<SelectionManager, ISelectable>(sm => sm.CurrentSelection, OnCurrentSelectionChanged));
    }

    private void OnCurrentSelectionChanged() {
        string selectionName = string.Empty;
        ISelectable newSelection = _selectionMgr.CurrentSelection;
        if (newSelection != null) {
            selectionName = newSelection.DisplayName;
        }
        RefreshReadout(selectionName);
    }

    void OnClick() {
        if (GameInputHelper.Instance.IsMiddleMouseButton) {
            OnMiddleClick();
        }
    }

    private void OnMiddleClick() {
        var selection = _selectionMgr.CurrentSelection;
        if (selection != null) {
            var focusable = selection as ICameraFocusable;
            if (focusable != null) {
                focusable.IsFocus = true;
            }
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
        _selectionMgr.Dispose();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll<IDisposable>(s => s.Dispose());
        _subscriptions.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

