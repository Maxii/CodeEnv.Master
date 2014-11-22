// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiMenuAcceptButtonBase.cs
// Base class for GuiMenuAccept buttons that accumulate changes from its sibling
// attached menu items before Raising an event with a Settings object attached.
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
/// Base class for GuiMenuAccept buttons that accumulate changes from its sibling
/// attached menu items before Raising an event with a Settings object attached.
/// </summary>
public abstract class AGuiMenuAcceptButtonBase : AGuiButtonBase {

    // Can be empty
    protected UIToggle[] _checkboxes;
    protected UIPopupList[] _popupLists;
    protected UISlider[] _sliders;

    protected override void Start() {
        base.Start();
        GameObject buttonParent = gameObject.transform.parent.gameObject;

        // acquire all the menu elements here
        _checkboxes = buttonParent.GetComponentsInChildren<UIToggle>(includeInactive: true);
        _popupLists = buttonParent.GetComponentsInChildren<UIPopupList>(includeInactive: true);
        _sliders = buttonParent.GetComponentsInChildren<UISlider>(includeInactive: true);

        CaptureInitializedState();
        AddMenuElementListeners();
    }

    private void AddMenuElementListeners() {
        if (Utility.CheckForContent<UIToggle>(_checkboxes)) {
            _checkboxes.ForAll<UIToggle>(checkbox => EventDelegate.Add(checkbox.onChange, OnCheckboxStateChange));
        }
        if (Utility.CheckForContent<UIPopupList>(_popupLists)) {
            _popupLists.ForAll<UIPopupList>(popupList => EventDelegate.Add(popupList.onChange, OnPopupListSelectionChange));
        }
        if (Utility.CheckForContent<UISlider>(_sliders)) {   // this won't work for sliders built for enums
            _sliders.ForAll<UISlider>(slider => EventDelegate.Add(slider.onChange, OnSliderValueChange));
        }
    }

    /// <summary>
    /// Captures the initialized state of all menu elements via the 
    /// derived classes implementation of the abstract RecordXXXState methods.
    /// </summary>
    protected virtual void CaptureInitializedState() {
        foreach (UIToggle checkbox in _checkboxes) {
            bool checkedState = checkbox.value;
            RecordCheckboxState(checkbox.name.ToLower(), checkedState);
        }
        foreach (UIPopupList popupList in _popupLists) {
            string selection = popupList.value;
            RecordPopupListState(popupList.name.ToLower(), selection);
        }
        foreach (UISlider slider in _sliders) {
            float sliderValue = slider.value;
            RecordSliderState(slider.name.ToLower(), sliderValue);
        }
    }

    /// <summary>
    /// Called on a checkbox state change, this base class implementation records
    /// the change via the RecordXXXState methods implemented by the derived class.
    /// </summary>
    protected virtual void OnCheckboxStateChange() {
        string checkboxName = UIToggle.current.name.ToLower();
        bool isChecked = UIToggle.current.value;
        //D.Log("Checkbox Named {0} had a state change to {1}.", checkboxName, isChecked);
        RecordCheckboxState(checkboxName, isChecked);
    }

    /// <summary>
    /// Called on a popupList state change, this base class implementation records
    /// the change via the RecordXXXState methods implemented by the derived class.
    /// </summary>
    protected virtual void OnPopupListSelectionChange() {
        string selectionName = UIPopupList.current.value;
        string popupListName = UIPopupList.current.name.ToLower();
        RecordPopupListState(popupListName, selectionName);
    }

    /// <summary>
    /// Called on a slider state change, this base class implementation records
    /// the change via the RecordXXXState methods implemented by the derived class.
    /// </summary>
    protected virtual void OnSliderValueChange() {
        float value = UISlider.current.value;
        string sliderName = UISlider.current.name.ToLower();
        RecordSliderState(sliderName, value);
    }

    /// <summary>
    /// Derived classes implement this abstract method, recording the state of the checkbox named <c>checkboxName_lc</c>.
    /// </summary>
    /// <param name="checkboxName_lc">Name of the checkbox, lowercase.</param>
    /// <param name="checkedState">if set to <c>true</c> [checked state].</param>
    protected virtual void RecordCheckboxState(string checkboxName_lc, bool checkedState) { }

    /// <summary>
    /// Derived classes implement this abstract method, recording the state of the popup list named <c>popupListName_lc</c>.
    /// </summary>
    /// <param name="popupListName_lc">Name of the popup list, lowercase.</param>
    /// <param name="selectionName">Name of the selection.</param>
    protected virtual void RecordPopupListState(string popupListName_lc, string selectionName) { }

    /// <summary>
    /// Derived classes implement this abstract method, recording the state of the slider named <c>sliderName_lc</c>.
    /// UNDONE sliderValue insufficient to select which slider
    /// </summary>
    /// <param name="sliderName_lc">Name of the slider, lowercase.</param>
    /// <param name="sliderValue">The slider value.</param>
    protected virtual void RecordSliderState(string sliderName_lc, float sliderValue) { }

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to a GameObject that is also being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

}

