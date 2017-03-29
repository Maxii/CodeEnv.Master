﻿// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
        GameReferences.HoveredItemHudWindow = Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        Subscribe();
    }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _startingLocalPosition = transform.localPosition;
        if (!_panel.widgetsAreStatic) {
            D.Warn("{0}: Can't UIPanel.widgetsAreStatic = true?", DebugName);
        }
        //D.Log("{0} initial local position: {1}.", DebugName, _startingLocalPosition);
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(SelectionManager.Instance.SubscribeToPropertyChanged<SelectionManager, ISelectable>(sm => sm.CurrentSelection, CurrentSelectionPropChangedHandler));
    }

    public void Show(StringBuilder sb) {
        Show(sb.ToString());
    }

    public void Show(ColoredStringBuilder csb) {
        Show(csb.ToString());
    }

    public void Show(string text) {
        //D.Log("{0}.Show() called at {1}.", DebugName, Utility.TimeStamp);
        var form = PrepareForm(FormID.TextHud);
        (form as TextForm).Text = text;
        ShowForm(form);
    }

    protected override void PositionWindow() {
        //D.Log("{0}.PositionWindow() called.", DebugName);
        Vector3 intendedLocalPosition;
        if (SelectedItemHudWindow.Instance.IsShowing) {
            var selectionPopupLocalCorners = SelectedItemHudWindow.Instance.LocalCorners;
            //D.Log("{0} local corners: {1}.", typeof(SelectedItemHudWindow).Name, selectionPopupLocalCorners.Concatenate());
            intendedLocalPosition = _startingLocalPosition + selectionPopupLocalCorners[1];
        }
        else {
            intendedLocalPosition = _startingLocalPosition;
        }
        transform.localPosition = intendedLocalPosition;
    }

    #region Event and Property Change Handlers

    private void CurrentSelectionPropChangedHandler() {
        //D.Log("{0}.CurrentSelectionPropChangedHandler() called.", GetType().Name);
        if (IsShowing) {
            PositionWindow();
        }
    }

    #endregion

    [Obsolete]
    protected override void __ValidateKilledFadeJobReference(Job fadeJobRef) {
        if (fadeJobRef != null) {
            // 12.9.16 Nothing to be done about this. I just want to know how frequently it occurs. See base.Validate for context.
            D.WarnContext(this, "{0}'s {1} replaced previous reference before this validation test could occur.", DebugName, fadeJobRef.JobName);
        }
    }

    protected override void Cleanup() {
        base.Cleanup();
        GameReferences.HoveredItemHudWindow = null;
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

