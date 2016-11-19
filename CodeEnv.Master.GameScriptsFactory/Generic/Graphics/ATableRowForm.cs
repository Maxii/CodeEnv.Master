// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ATableRowForm.cs
//  Abstract base class for ReportForms that are TableRows. 
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
public abstract class ATableRowForm : AReportForm {

    /// <summary>
    /// Occurs when the user takes an action requesting that an Item (represented by a TableRow) should become the Focus.
    /// </summary>
    public event EventHandler<TableRowFocusUserActionEventArgs> itemFocusUserAction;

    protected override void InitializeNameGuiElement(AGuiElement e) {
        base.InitializeNameGuiElement(e);
        MyEventListener.Get(e.gameObject).onDoubleClick += NameDoubleClickEventHandler;
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

    protected override void CleanupNameGuiElement(AGuiElement e) {
        base.CleanupNameGuiElement(e);
        MyEventListener.Get(e.gameObject).onDoubleClick -= NameDoubleClickEventHandler;
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

