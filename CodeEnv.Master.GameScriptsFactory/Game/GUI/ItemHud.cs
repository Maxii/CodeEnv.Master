// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemHud.cs
// Gui WIndow displaying movable custom HUDs for Items.
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
/// Gui WIndow displaying movable custom HUDs for Items.
/// The current version is located on the left side of the screen and moves 
/// itself to avoid interference with the fixed selectable HUD.
/// </summary>
public class ItemHud : AHud<ItemHud> {

    private Vector3 _startingLocalPosition;
    private IList<IDisposable> _subscriptions;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.ItemHud = Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        Subscribe();
    }

    protected override void AcquireReferences() {
        base.AcquireReferences();
        _startingLocalPosition = transform.localPosition;
        //D.Log("{0} initial local position: {1}.", GetType().Name, _startingLocalPosition);
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(SelectionManager.Instance.SubscribeToPropertyChanged<SelectionManager, ISelectable>(sm => sm.CurrentSelection, OnCurrentSelectionChanged));
    }

    protected override void PositionPopup() {
        //D.Log("{0}.PositionPopup() called.", GetType().Name);
        Vector3 intendedLocalPosition;
        if (SelectionHud.Instance.IsShowing) {
            var selectionPopupLocalCorners = SelectionHud.Instance.LocalCorners;
            //D.Log("{0} local corners: {1}.", typeof(SelectionPopup).Name, selectionPopupLocalCorners.Concatenate());
            intendedLocalPosition = _startingLocalPosition + selectionPopupLocalCorners[1];
        }
        else {
            intendedLocalPosition = _startingLocalPosition;
        }
        transform.localPosition = intendedLocalPosition;
    }

    private void OnCurrentSelectionChanged() {
        //D.Log("{0}.OnCurrentSelectionChanged() called.", GetType().Name);
        if (IsShowing) {
            PositionPopup();
        }
    }

    protected override void Cleanup() {
        base.Cleanup();
        References.ItemHud = null;
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

