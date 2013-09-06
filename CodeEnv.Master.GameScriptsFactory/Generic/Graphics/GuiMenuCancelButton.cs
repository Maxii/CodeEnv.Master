// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiMenuCancelButton.cs
// General-purpose Menu Cancel Button that restores the original state of the menu
// to what it was when it was opened.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// General-purpose Menu Cancel Button that restores the original state of the menu
/// to what it was when it was opened.
/// </summary>
public class GuiMenuCancelButton : AGuiButtonBase {

    private UICheckbox[] _checkboxes;
    private bool[] _openingCheckboxesState;

    private UIPopupList[] _popupLists;
    private string[] _openingPopupListsSelection;
    private bool _isInitialized;

    protected override void Awake() {
        base.Awake();
        tooltip = "Click to cancel changes.";
    }

    protected override void Start() {
        base.Start();
        GameObject buttonParent = gameObject.transform.parent.gameObject;
        _checkboxes = buttonParent.GetComponentsInChildren<UICheckbox>(includeInactive: true);
        //D.Assert(checkboxes.Length == 0, "There are no checkboxes on Menu {0}.".Inject(buttonParent.name)); 
        _openingCheckboxesState = new bool[_checkboxes.Length];

        _popupLists = buttonParent.GetComponentsInChildren<UIPopupList>(includeInactive: true);
        //D.Assert(popupLists.Length == 0, "There are no PopupLists on Menu {0}.".Inject(buttonParent.name)); 

        _openingPopupListsSelection = new string[_popupLists.Length];
        CaptureOpeningState();
    }

    protected override void OnEnable() {
        base.OnEnable();
        if (_isInitialized) {
            CaptureOpeningState();
        }
    }

    private void CaptureOpeningState() {
        for (int i = 0; i < _checkboxes.Length; i++) {
            _openingCheckboxesState[i] = _checkboxes[i].isChecked;
        }
        for (int i = 0; i < _popupLists.Length; i++) {
            _openingPopupListsSelection[i] = _popupLists[i].selection;
        }
        _isInitialized = true;
    }

    protected override void OnLeftClick() {
        RestoreOpeningState();
    }

    private void RestoreOpeningState() {
        for (int i = 0; i < _checkboxes.Length; i++) {
            _checkboxes[i].isChecked = _openingCheckboxesState[i];
        }
        for (int i = 0; i < _popupLists.Length; i++) {
            _popupLists[i].selection = _openingPopupListsSelection[i];
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

