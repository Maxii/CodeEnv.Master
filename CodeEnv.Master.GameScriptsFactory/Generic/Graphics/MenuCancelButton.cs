// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MenuCancelButton.cs
// Menu Cancel Button that restores the original state of the menu to what it was when it was opened.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Menu Cancel Button that restores the original state of the menu to what it was when it was opened.
/// </summary>
public class MenuCancelButton : AGuiButton {

    protected override string TooltipContent {
        get { return "Click to cancel changes."; }
    }

    private UIToggle[] _checkboxes;
    private bool[] _checkboxesStateOnShow;

    private UIPopupList[] _popupLists;
    private string[] _popupListsSelectionOnShow;

    protected override void Start() {
        base.Start();

        UIPanel parentPanel = gameObject.GetSafeMonoBehaviourComponentInParents<UIPanel>();

        _checkboxes = parentPanel.gameObject.GetComponentsInChildren<UIToggle>(includeInactive: true);
        _checkboxesStateOnShow = new bool[_checkboxes.Length];

        _popupLists = parentPanel.gameObject.GetComponentsInChildren<UIPopupList>(includeInactive: true);
        _popupListsSelectionOnShow = new string[_popupLists.Length];
    }

    /// <summary>
    /// Captures the state of the elements in the menu when called.
    /// This is typically called when the menu has just started or completed its showing
    /// transition process and is now ready for user interaction.
    /// </summary>
    public void CaptureMenuState() {
        D.Log("{0}.CaptureMenuState() called.", GetType().Name);
        for (int i = 0; i < _checkboxes.Length; i++) {
            _checkboxesStateOnShow[i] = _checkboxes[i].value;
        }
        for (int i = 0; i < _popupLists.Length; i++) {
            _popupListsSelectionOnShow[i] = _popupLists[i].value;
        }
    }

    protected override void OnLeftClick() {
        RestoreMenuState();
    }

    private void RestoreMenuState() {
        for (int i = 0; i < _checkboxes.Length; i++) {
            _checkboxes[i].value = _checkboxesStateOnShow[i];
        }
        for (int i = 0; i < _popupLists.Length; i++) {
            _popupLists[i].value = _popupListsSelectionOnShow[i];
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

