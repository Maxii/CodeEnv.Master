// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HoveredHudWindow.cs
// HudWindow displaying item info when hovered over the item.
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
/// HudWindow displaying non-interactable info about the object the mouse is hovering over.
/// <remarks>The current version is located on the left side of the screen and moves itself to avoid interference 
/// with the fixed InteractableHudWindow.</remarks>
/// </summary>
public class HoveredHudWindow : AHudWindow<HoveredHudWindow>, IHoveredHudWindow {

    [SerializeField]
    private bool _showAboveInteractableHud = false;

    private Vector3 _startingLocalPosition;
    private IList<IDisposable> _subscriptions;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        GameReferences.HoveredHudWindow = Instance;
    }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _startingLocalPosition = transform.localPosition;
        if (!_panel.widgetsAreStatic) {
            D.Warn("{0}: Can't UIPanel.widgetsAreStatic = true?", DebugName);
        }
        //D.Log("{0} initial local position: {1}.", DebugName, _startingLocalPosition);
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(InteractableHudWindow.Instance.SubscribeToPropertyChanged<InteractableHudWindow, bool>(iHud => iHud.IsShowing, InteractableHudIsShowingPropChangedHandler));
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

    public void Show(FormID formID, AUnitDesign design) {
        //D.Log("{0}.Show({1}) called.", DebugName, design.DebugName);
        var form = PrepareForm(formID);
        (form as AHoveredHudDesignForm).Design = design;
        ShowForm(form);
    }

    public void Show(FormID formID, AEquipmentStat stat) {
        var form = PrepareForm(formID);
        (form as AHoveredHudEquipmentForm).EquipmentStat = stat;
        ShowForm(form);
    }

    public void Show(FormID formID, AItemReport report) {   // 7.9.17 IMPROVE Not currently used as hovered items use Show(text)
        var form = PrepareForm(formID);
        (form as AItemReportForm).Report = report;
        ShowForm(form);
    }

    protected override void PositionWindow() {
        //D.Log("{0}.PositionWindow() called.", DebugName);
        Vector3 intendedLocalPosition = _startingLocalPosition;
        if (_showAboveInteractableHud) {
            if (InteractableHudWindow.Instance.IsShowing) {
                var selectionPopupLocalCorners = InteractableHudWindow.Instance.LocalCorners;
                //D.Log("{0} local corners: {1}.", typeof(InteractableHudWindow).Name, selectionPopupLocalCorners.Concatenate());
                intendedLocalPosition = _startingLocalPosition + selectionPopupLocalCorners[1];
            }
        }
        transform.localPosition = intendedLocalPosition;
    }

    #region Event and Property Change Handlers

    private void InteractableHudIsShowingPropChangedHandler() {
        //D.Log("{0}.InteractableHudIsShowingPropChangedHandler() called.", DebugName);
        if (IsShowing) {
            PositionWindow();
        }
    }

    #endregion

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
    }

    protected override void Cleanup() {
        base.Cleanup();
        GameReferences.HoveredHudWindow = null;
    }

    #region Debug

    [Obsolete]
    protected override void __ValidateKilledFadeJobReference(Job fadeJobRef) {
        if (fadeJobRef != null) {
            // 12.9.16 Nothing to be done about this. I just want to know how frequently it occurs. See base.Validate for context.
            D.WarnContext(this, "{0}'s {1} replaced previous reference before this validation test could occur.", DebugName, fadeJobRef.JobName);
        }
    }

    #endregion

}

