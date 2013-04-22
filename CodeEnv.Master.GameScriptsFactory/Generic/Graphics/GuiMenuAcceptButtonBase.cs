// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiMenuAcceptButtonBase.cs
// Base class for GuiMenuAccept buttons that accumulate changes from its sibling
// attached menu items before Raising an event with a Settings object attached.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Base class for GuiMenuAccept buttons that accumulate changes from its sibling
/// attached menu items before Raising an event with a Settings object attached.
/// </summary>
public abstract class GuiMenuAcceptButtonBase : GuiButtonBase, IDisposable {

    // Can be empty
    protected UICheckbox[] checkboxes;
    protected UIPopupList[] popupLists;
    protected UISlider[] sliders;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        AddListeners();
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
        GameObject buttonParent = gameObject.transform.parent.gameObject;

        // acquire all the menu elements here
        checkboxes = buttonParent.GetSafeMonoBehaviourComponentsInChildren<UICheckbox>(includeInactive: true);
        popupLists = buttonParent.GetSafeMonoBehaviourComponentsInChildren<UIPopupList>(includeInactive: true);
        sliders = buttonParent.GetSafeMonoBehaviourComponentsInChildren<UISlider>(includeInactive: true);

        CaptureInitializedState();
        AddMenuElementListeners();
    }

    private void AddListeners() {
        //eventMgr.AddListener<FirstUpdateEvent>(this, OnFirstUpdate);
    }

    //private void OnFirstUpdate(FirstUpdateEvent e) {
    //    //CaptureInitializedState();
    //    // don't listen for menu element changes until the initial state is captured
    //    //AddMenuElementListeners();
    //}

    private void AddMenuElementListeners() {
        if (Utility.CheckForContent<UICheckbox>(checkboxes)) {
            checkboxes.ForAll<UICheckbox>(checkbox => checkbox.onStateChange += OnCheckboxStateChange);
        }
        if (Utility.CheckForContent<UIPopupList>(popupLists)) {
            popupLists.ForAll<UIPopupList>(popupList => popupList.onSelectionChange += OnPopupListSelectionChange);
        }
        if (Utility.CheckForContent<UISlider>(sliders)) {   // this won't work for sliders built for enums
            sliders.ForAll<UISlider>(slider => slider.onValueChange += OnSliderValueChange);
        }
    }

    /// <summary>
    /// Captures the initialized state of all menu elements via the 
    /// derived classes implementation of the abstract RecordXXXState methods.
    /// </summary>
    protected virtual void CaptureInitializedState() {
        foreach (UICheckbox checkbox in checkboxes) {
            string checkboxName = checkbox.name.ToLower();
            bool checkedState = checkbox.isChecked;
            RecordCheckboxState(checkboxName, checkedState);
        }
        foreach (UIPopupList popupList in popupLists) {
            string selection = popupList.selection;
            RecordPopupListState(selection);
        }
        foreach (UISlider slider in sliders) {
            float sliderValue = slider.sliderValue;
            RecordSliderState(sliderValue);
        }
    }

    /// <summary>
    /// Called on a checkbox state change, this base class implementation records
    /// the change via the RecordXXXState methods implemented by the derived class.
    /// </summary>
    /// <param name="state">if set to <c>true</c> [state].</param>
    protected virtual void OnCheckboxStateChange(bool state) {
        string checkboxName = UICheckbox.current.name.ToLower();
        //Debug.Log("Checkbox Named {0} had a state change to {1}.", checkboxName, state);
        RecordCheckboxState(checkboxName, state);
    }

    /// <summary>
    /// Called on a popupList state change, this base class implementation records
    /// the change via the RecordXXXState methods implemented by the derived class.
    /// </summary>
    /// <param name="item">The item.</param>
    protected virtual void OnPopupListSelectionChange(string item) {
        RecordPopupListState(item);
    }

    /// <summary>
    /// Called on a slider state change, this base class implementation records
    /// the change via the RecordXXXState methods implemented by the derived class.
    /// </summary>
    /// <param name="value">The value.</param>
    protected virtual void OnSliderValueChange(float value) {
        RecordSliderState(value);
    }

    /// <summary>
    /// Derived classes implement this abstract method, recording the state of the checkbox that has the provided name.
    /// </summary>
    /// <param name="checkboxName">Name of the checkbox in lower case.</param>
    /// <param name="checkedState">if set to <c>true</c> [checked state].</param>
    protected abstract void RecordCheckboxState(string checkboxName, bool checkedState);

    /// <summary>
    /// Derived classes implement this abstract method, recording the state of the popup list that uses the provided selectionName.
    /// </summary>
    /// <param name="selectionName">Name of the selectionName.</param>
    protected abstract void RecordPopupListState(string selectionName);

    /// <summary>
    /// Derived classes implement this abstract method, recording the state of the slider.     
    /// UNDONE sliderValue insufficient to select which slider
    /// </summary>
    /// <param name="sliderValue">The slider value.</param>
    protected abstract void RecordSliderState(float sliderValue);

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to a GameObject that is also being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

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

