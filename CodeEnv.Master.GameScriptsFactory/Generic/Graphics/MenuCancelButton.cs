// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MenuCancelButton.cs
// Menu Cancel Button that restores the original state of the menu it is a element of 
// to what it was when it was last shown by its parent GuiWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;

/// <summary>
/// Menu Cancel Button that restores the original state of the menu it is a element of 
/// to what it was when it was last shown by its parent GuiWindow.
/// </summary>
public class MenuCancelButton : AGuiButton {

    protected override string TooltipContent { get { return "Click to cancel changes."; } }

    protected AGuiWindow _window;
    private UIToggle[] _checkboxes;
    private bool[] _checkboxesStateOnShow;
    private UIPopupList[] _popupLists;
    private string[] _popupListsSelectionOnShow;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    protected virtual void InitializeValuesAndReferences() {
        _window = gameObject.GetSafeFirstMonoBehaviourInParents<AGuiWindow>();

        _checkboxes = _window.gameObject.GetComponentsInChildren<UIToggle>(includeInactive: true);
        _checkboxesStateOnShow = new bool[_checkboxes.Length];

        _popupLists = _window.gameObject.GetComponentsInChildren<UIPopupList>(includeInactive: true);
        _popupListsSelectionOnShow = new string[_popupLists.Length];

        SubscribeToParentWindowToCaptureMenuState();
    }

    protected virtual void SubscribeToParentWindowToCaptureMenuState() {
        EventDelegate.Add(_window.onShowBegin, CaptureMenuState);
    }

    /// <summary>
    /// Captures the state of the elements in the menu when called.
    /// This is typically called when the menu has just started or completed its showing
    /// transition process and is now ready for user interaction.
    /// </summary>
    protected void CaptureMenuState() {
        //D.Log("{0}.CaptureMenuState() called.", GetType().Name);
        for (int i = 0; i < _checkboxes.Length; i++) {
            _checkboxesStateOnShow[i] = _checkboxes[i].value;
        }
        for (int i = 0; i < _popupLists.Length; i++) {
            _popupListsSelectionOnShow[i] = _popupLists[i].value;
        }
    }

    protected sealed override void OnLeftClick() {
        RestoreMenuState();
    }

    private void RestoreMenuState() {
        RestoreCheckboxesState();
        RestorePopupListsState();
    }

    protected virtual void RestoreCheckboxesState() {
        for (int i = 0; i < _checkboxes.Length; i++) {
            bool checkboxStateOnShow = _checkboxesStateOnShow[i];
            if (_checkboxes[i].value != checkboxStateOnShow) {  // UIToggle fires onChange events when set whether changed or not
                _checkboxes[i].value = checkboxStateOnShow;
            }
        }
    }

    protected virtual void RestorePopupListsState() {
        for (int i = 0; i < _popupLists.Length; i++) {
            string popupListSelectionOnShow = _popupListsSelectionOnShow[i];
            if (_popupLists[i].value != popupListSelectionOnShow) { // UIPopupList fires onChange events when set whether changed or not
                //D.Log("Restoring {0} from {1} to {2}.", _popupLists[i].name, _popupLists[i].value, popupListSelectionOnShow);
                _popupLists[i].value = popupListSelectionOnShow;
            }
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        EventDelegate.Remove(_window.onShowBegin, CaptureMenuState);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

