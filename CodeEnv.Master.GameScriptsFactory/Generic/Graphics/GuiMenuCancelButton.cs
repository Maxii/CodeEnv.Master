﻿// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// General-purpose Menu Cancel Button that restores the original state of the menu
/// to what it was when it was opened.
/// </summary>
public class GuiMenuCancelButton : AGuiButton {

    protected override string TooltipContent {
        get { return "Click to cancel changes."; }
    }

    private UIToggle[] _checkboxes;
    private bool[] _openingCheckboxesState;

    private UIPopupList[] _popupLists;
    private string[] _openingPopupListsSelection;
    private bool _isInitialized;

    protected override void Start() {
        base.Start();
        //GameObject buttonParent = gameObject.transform.parent.gameObject;
        UIPanel parentPanel = gameObject.GetSafeMonoBehaviourComponentInParents<UIPanel>();

        _checkboxes = parentPanel.gameObject.GetComponentsInChildren<UIToggle>(includeInactive: true);
        //D.Assert(checkboxes.Length == 0, "There are no checkboxes on Menu {0}.".Inject(buttonParent.name)); 
        _openingCheckboxesState = new bool[_checkboxes.Length];

        _popupLists = parentPanel.gameObject.GetComponentsInChildren<UIPopupList>(includeInactive: true);
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
            _openingCheckboxesState[i] = _checkboxes[i].value;
        }
        for (int i = 0; i < _popupLists.Length; i++) {
            _openingPopupListsSelection[i] = _popupLists[i].value;
        }
        _isInitialized = true;
    }

    protected override void OnLeftClick() {
        RestoreOpeningState();
    }

    private void RestoreOpeningState() {
        for (int i = 0; i < _checkboxes.Length; i++) {
            _checkboxes[i].value = _openingCheckboxesState[i];
        }
        for (int i = 0; i < _popupLists.Length; i++) {
            _popupLists[i].value = _openingPopupListsSelection[i];
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

