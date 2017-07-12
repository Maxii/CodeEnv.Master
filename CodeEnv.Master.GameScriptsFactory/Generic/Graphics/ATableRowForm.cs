// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ATableRowForm.cs
// Abstract base class for ReportForms that are TableRows. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for ReportForms that are TableRows. 
/// </summary>
public abstract class ATableRowForm : AItemReportForm {

    /// <summary>
    /// Occurs when the user takes an action requesting that an Item (represented by a TableRow) should become the Focus.
    /// </summary>
    public event EventHandler<TableRowFocusUserActionEventArgs> itemFocusUserAction;

    private UISprite _rowSprite;

    protected override void InitializeNonGuiElementMembers() {
        base.InitializeNonGuiElementMembers();
        _rowSprite = GetComponent<UISprite>();
    }

    protected override void InitializeNameGuiElement(AGuiElement e) {
        base.InitializeNameGuiElement(e);
        UIEventListener.Get(e.gameObject).onDoubleClick += NameDoubleClickEventHandler;
    }

    protected override void AssignValueToNameGuiElement() {
        base.AssignValueToNameGuiElement();
        _nameLabel.text = Report.Name != null ? Report.Name : Unknown;
    }

    public void SetSideAnchors(Transform target, int left, int right) {
        _rowSprite.leftAnchor.target = target;
        _rowSprite.leftAnchor.absolute = left;
        _rowSprite.rightAnchor.target = target;
        _rowSprite.rightAnchor.absolute = right;
        _rowSprite.ResetAnchors();
        _rowSprite.UpdateAnchors();
    }

    #region Event and Property Change Handlers

    private void NameDoubleClickEventHandler(GameObject go) {
        OnItemFocusUserAction();
    }

    private void OnItemFocusUserAction() {
        if (itemFocusUserAction != null) {
            itemFocusUserAction(this, new TableRowFocusUserActionEventArgs(Report.Item as ICameraFocusable));
        }
    }

    #endregion

    public override void Reset() {
        base.Reset();
        _rowSprite.leftAnchor.target = null;
        _rowSprite.rightAnchor.target = null;
        _rowSprite.ResetAnchors();
    }

    protected override void CleanupNameGuiElement(AGuiElement e) {
        base.CleanupNameGuiElement(e);
        UIEventListener.Get(e.gameObject).onDoubleClick -= NameDoubleClickEventHandler;
    }

    #region Nested Classes

    public class TableRowFocusUserActionEventArgs : EventArgs {

        public ICameraFocusable ItemToFocusOn { get; private set; }

        public TableRowFocusUserActionEventArgs(ICameraFocusable itemToFocusOn) {
            ItemToFocusOn = itemToFocusOn;
        }

    }

    #endregion

}

