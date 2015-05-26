// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiMenuCancelButton.cs
// The first Menu Cancel Button that restores the original state of the menu
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
/// The first Menu Cancel Button that restores the original state of the menu to what it was when it was opened.
/// WARNING: This button requires the presence of UIPlayAnimation with proper ifDisabledOnPlay
/// and disableWhenFinished settings as indicated in ValidateSetup().
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

        ValidateSetup();
        UIPanel parentPanel = gameObject.GetSafeMonoBehaviourInParents<UIPanel>();

        _checkboxes = parentPanel.gameObject.GetComponentsInChildren<UIToggle>(includeInactive: true);
        //D.Assert(checkboxes.Length == 0, "There are no checkboxes on Menu {0}.".Inject(buttonParent.name)); 
        _openingCheckboxesState = new bool[_checkboxes.Length];

        _popupLists = parentPanel.gameObject.GetComponentsInChildren<UIPopupList>(includeInactive: true);
        //D.Assert(popupLists.Length == 0, "There are no PopupLists on Menu {0}.".Inject(buttonParent.name)); 

        _openingPopupListsSelection = new string[_popupLists.Length];
        CaptureMenuOpeningState();
    }

    protected override void OnEnable() {
        base.OnEnable();
        if (_isInitialized) {
            CaptureMenuOpeningState();
        }
    }

    /// <summary>
    /// Captures the state of the menu each time it opens.
    /// Warning: As this method is called from OnEnable(), this approach
    /// relies on this button being disabled when the menu is closed and re-enabled
    /// (calling OnEnable()) when opened again. If this doesn't happen, then the
    /// opening state of the menu that is captured will always be the state when 
    /// Start() was called, even if the menu's values were changed and accepted
    /// previously. See ValidationSetup() below.
    /// </summary>
    private void CaptureMenuOpeningState() {
        for (int i = 0; i < _checkboxes.Length; i++) {
            _openingCheckboxesState[i] = _checkboxes[i].value;
        }
        for (int i = 0; i < _popupLists.Length; i++) {
            _openingPopupListsSelection[i] = _popupLists[i].value;
        }
        _isInitialized = true;
    }

    protected override void OnLeftClick() {
        RestoreMenuOpeningState();
    }

    private void RestoreMenuOpeningState() {
        for (int i = 0; i < _checkboxes.Length; i++) {
            _checkboxes[i].value = _openingCheckboxesState[i];
        }
        for (int i = 0; i < _popupLists.Length; i++) {
            _popupLists[i].value = _openingPopupListsSelection[i];
        }
    }

    private void ValidateSetup() {
        var nguiPlayAnimation = UnityUtility.ValidateMonoBehaviourPresence<UIPlayAnimation>(gameObject);
        D.Assert(nguiPlayAnimation.ifDisabledOnPlay == AnimationOrTween.EnableCondition.EnableThenPlay);
        D.Assert(nguiPlayAnimation.playDirection != AnimationOrTween.Direction.Toggle); // its either Forward or Reverse
        if (nguiPlayAnimation.playDirection == AnimationOrTween.Direction.Forward) {
            D.Assert(nguiPlayAnimation.disableWhenFinished == AnimationOrTween.DisableCondition.DisableAfterForward);
        }
        else {
            D.Assert(nguiPlayAnimation.disableWhenFinished == AnimationOrTween.DisableCondition.DisableAfterReverse);
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

