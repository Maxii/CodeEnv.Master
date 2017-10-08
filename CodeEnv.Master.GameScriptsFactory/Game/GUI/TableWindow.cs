// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TableWindow.cs
// A full screen window that shows forms that display as tables of information. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A full screen window that shows forms that display as tables of information. 
/// </summary>
public class TableWindow : AFormWindow<TableWindow> {

    [SerializeField]
    private UILabel _titleLabel = null;
    [SerializeField]
    private UIButton _doneButton = null;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        if (!_panel.widgetsAreStatic) {
            D.Warn("{0}: Can't UIPanel.widgetsAreStatic = true?", DebugName);
        }
    }

    protected override IEnumerable<AForm> GetChildFormsToInitialize() {
        return gameObject.GetSafeComponentsInChildren<AForm>(excludeSelf: true, includeInactive: true).Where(form => !(form is ATableRowForm));
    }

    public void ShowSystems() {
        _titleLabel.text = "Known Systems";
        Show(FormID.SystemsTable);
    }

    public void ShowSettlements() {
        _titleLabel.text = "Known Settlements";
        Show(FormID.SettlementsTable);
    }

    public void ShowStarbases() {
        _titleLabel.text = "Known Starbases";
        Show(FormID.StarbasesTable);
    }

    public void ShowFleets() {
        _titleLabel.text = "Known Fleets";
        Show(FormID.FleetsTable);
    }

    private void Show(FormID formID) {
        var form = PrepareForm(formID);
        ShowForm(form);
    }

    #region Event and Property Change Handlers

    #endregion

    /// <summary>
    /// Clicks the done button for this Window.
    /// <remarks>Convenience method used by the window's forms when they want to remotely close
    /// the window and resume the game.</remarks>
    /// </summary>
    public void ClickDoneButton() {
        GameInputHelper.Instance.Notify(_doneButton.gameObject, "OnClick");
    }

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotNull(_titleLabel);
        D.AssertNotNull(_doneButton);
    }

    #endregion

}

