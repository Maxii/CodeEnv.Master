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

//#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR


// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// General-purpose Menu Cancel Button that restores the original state of the menu
/// to what it was when it was opened.
/// </summary>
public class GuiMenuCancelButton : GuiButtonBase, IDisposable {

    private UICheckbox[] checkboxes;
    private bool[] openingCheckboxesState;

    private UIPopupList[] popupLists;
    private string[] openingPopupListsSelection;
    private bool isInitialized;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        AddListeners();
        tooltip = "Click to cancel changes.";
    }

    private void AddListeners() {
        //eventMgr.AddListener<FirstUpdateEvent>(this, OnFirstUpdate);
    }

    //private void OnFirstUpdate(FirstUpdateEvent e) {
    //    // CaptureOpeningState();
    //}

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
        GameObject buttonParent = gameObject.transform.parent.gameObject;
        checkboxes = buttonParent.GetSafeMonoBehaviourComponentsInChildren<UICheckbox>(includeInactive: true);
        //Debug.Assert(checkboxes.Length == 0, "There are no checkboxes on Menu {0}.".Inject(buttonParent.name)); 
        openingCheckboxesState = new bool[checkboxes.Length];

        popupLists = buttonParent.GetSafeMonoBehaviourComponentsInChildren<UIPopupList>(includeInactive: true);
        //Debug.Assert(popupLists.Length == 0, "There are no PopupLists on Menu {0}.".Inject(buttonParent.name)); 

        openingPopupListsSelection = new string[popupLists.Length];
        CaptureOpeningState();
    }

    void OnEnable() {
        if (isInitialized) {
            //Debug.Log("GuiMenuCancelButton.OnEnable() called.");
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
        isInitialized = true;
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

    void OnDestroy() {
        Dispose();
    }

    private void RemoveListeners() {
        //eventMgr.RemoveListener<FirstUpdateEvent>(this, OnFirstUpdate);
    }

    #region IDisposable
    [NonSerialized]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            RemoveListeners();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }


    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

