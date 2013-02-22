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

// default namespace

using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// General-purpose Menu Cancel Button that restores the original state of the menu
/// to what it was when it was opened.
/// </summary>
public class GuiMenuCancelButton : GuiButtonBase {

    private UICheckbox[] checkboxes;
    private bool[] openingCheckboxesState;

    private UIPopupList[] popupLists;
    private string[] openingPopupListsSelection;
    GameObject buttonParent;

    protected override void Initialize() {
        base.Initialize();
        buttonParent = gameObject.transform.parent.gameObject;
        checkboxes = buttonParent.GetSafeMonoBehaviourComponentsInChildren<UICheckbox>(includeInactive: true);
        //if (checkboxes.Length == 0) { Debug.LogWarning("There are no checkboxes on Menu {0}".Inject(buttonParent.name)); }
        openingCheckboxesState = new bool[checkboxes.Length];

        popupLists = buttonParent.GetSafeMonoBehaviourComponentsInChildren<UIPopupList>(includeInactive: true);
        //if (popupLists.Length == 0) { Debug.LogWarning("There are no PopupLists on Menu {0}".Inject(buttonParent.name)); }

        openingPopupListsSelection = new string[popupLists.Length];
        CaptureOpeningState();
        tooltip = "Click to cancel changes.";
    }

    void OnEnable() {
        if (isInitialized) {
            CaptureOpeningState();
        }
    }

    private void CaptureOpeningState() {
        for (int i = 0; i < checkboxes.Length; i++) {
            openingCheckboxesState[i] = checkboxes[i].isChecked;
        }
        for (int i = 0; i < popupLists.Length; i++) {
            openingPopupListsSelection[i] = popupLists[i].selection;
        }
    }

    protected override void OnButtonClick(GameObject sender) {
        RestoreOpeningState();
    }

    private void RestoreOpeningState() {
        for (int i = 0; i < checkboxes.Length; i++) {
            checkboxes[i].isChecked = openingCheckboxesState[i];
        }
        for (int i = 0; i < popupLists.Length; i++) {
            popupLists[i].selection = openingPopupListsSelection[i];
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

