// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HoveredItemHudWindow.cs
// HudWIndow displaying item info when hovered over the item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// HudWindow displaying item info when hovered over the item.
/// The current version is located on the left side of the screen and moves 
/// itself to avoid interference with the fixed SelectedItemHudWindow.
/// </summary>
public class HoveredItemHudWindow : AHudWindow<HoveredItemHudWindow>, IHoveredHudWindow {

    private Vector3 _startingLocalPosition;
    private IList<IDisposable> _subscriptions;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.HoveredItemHudWindow = Instance;
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

    public void Show(string text) {
        var form = PrepareForm(FormID.TextHud);
        (form as TextForm).Text = text;
        ShowForm(form);
    }

    public void Show(StringBuilder sb) {
        Show(sb.ToString());
    }

    public void Show(ColoredStringBuilder csb) {
        Show(csb.ToString());
    }

    protected override void PositionWindow() {
        //D.Log("{0}.PositionPopup() called.", GetType().Name);
        Vector3 intendedLocalPosition;
        if (SelectedItemHudWindow.Instance.IsShowing) {
            var selectionPopupLocalCorners = SelectedItemHudWindow.Instance.LocalCorners;
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
            PositionWindow();
        }
    }

    protected override void Cleanup() {
        base.Cleanup();
        References.HoveredItemHudWindow = null;
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

