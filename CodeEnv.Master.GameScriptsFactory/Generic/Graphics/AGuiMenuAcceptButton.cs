// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiMenuAcceptButton.cs
// Abstract base class for Gui Menu Accept buttons. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Gui Menu Accept buttons. Accumulates changes from its sibling
/// menu items before firing an event with a Settings object attached.
/// </summary>
public abstract class AGuiMenuAcceptButton : AGuiButton {

    // Can be empty
    protected UIToggle[] _checkboxes;
    protected UIPopupList[] _popupLists;
    protected UISlider[] _sliders;

    protected override void Start() {
        base.Start();
        InitializeValuesAndReferences();
        CaptureInitializedState();
        AddMenuElementListeners();
    }

    protected virtual void InitializeValuesAndReferences() {
        UIPanel panel = gameObject.GetSafeMonoBehaviourComponentInParents<UIPanel>();
        _checkboxes = panel.gameObject.GetComponentsInChildren<UIToggle>(includeInactive: true);
        //D.Log("Checkboxes found: {0}.", _checkboxes.Select(c => c.name).Concatenate());
        _popupLists = panel.gameObject.GetComponentsInChildren<UIPopupList>(includeInactive: true);
        _sliders = panel.gameObject.GetComponentsInChildren<UISlider>(includeInactive: true);
    }

    /// <summary>
    /// Captures the initialized state of all menu elements via the 
    /// derived classes implementation of the abstract RecordXXXState methods.
    /// </summary>
    protected virtual void CaptureInitializedState() {
        foreach (UIToggle checkbox in _checkboxes) {
            var checkboxID = checkbox.gameObject.GetSafeMonoBehaviourComponent<AGuiMenuElement>().ElementID;
            bool checkedState = checkbox.value;
            RecordCheckboxState(checkboxID, checkedState);
        }
        foreach (UIPopupList popupList in _popupLists) {
            var popupListID = popupList.gameObject.GetSafeMonoBehaviourComponent<AGuiMenuElement>().ElementID;
            string selection = popupList.value;
            RecordPopupListState(popupListID, selection);
        }
        foreach (UISlider slider in _sliders) {
            var sliderID = slider.gameObject.GetSafeMonoBehaviourComponent<AGuiMenuElement>().ElementID;
            float sliderValue = slider.value;
            RecordSliderState(sliderID, sliderValue);
        }
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
    /// Called on a checkbox state change, this base class implementation records
    /// the change via the RecordXXXState methods implemented by the derived class.
    /// </summary>
    protected virtual void OnCheckboxStateChange() {
        var checkboxID = UIToggle.current.gameObject.GetSafeMonoBehaviourComponent<AGuiMenuElement>().ElementID;
        bool isChecked = UIToggle.current.value;
        //D.Log("Checkbox {0} had a state change to {1}.", checkboxID.GetName(), isChecked);
        RecordCheckboxState(checkboxID, isChecked);
    }

    /// <summary>
    /// Called on a popupList state change, this base class implementation records
    /// the change via the RecordXXXState methods implemented by the derived class.
    /// </summary>
    protected virtual void OnPopupListSelectionChange() {
        var popupListID = UIPopupList.current.gameObject.GetSafeMonoBehaviourComponent<AGuiMenuElement>().ElementID;
        string selectionName = UIPopupList.current.value;
        RecordPopupListState(popupListID, selectionName);
    }

    /// <summary>
    /// Called on a slider state change, this base class implementation records
    /// the change via the RecordXXXState methods implemented by the derived class.
    /// </summary>
    protected virtual void OnSliderValueChange() {
        var sliderID = UISlider.current.gameObject.GetSafeMonoBehaviourComponent<AGuiMenuElement>().ElementID;
        float value = UISlider.current.value;
        RecordSliderState(sliderID, value);
    }

    /// <summary>
    /// Derived classes implement this abstract method, recording the state of the checkbox named <c>checkboxName_lc</c>.
    /// </summary>
    /// <param name="checkboxID">The checkbox identifier.</param>
    /// <param name="checkedState">if set to <c>true</c> [checked state].</param>
    protected virtual void RecordCheckboxState(GuiMenuElementID checkboxID, bool checkedState) { }

    /// <summary>
    /// Derived classes implement this abstract method, recording the state of the popup list named <c>popupListName_lc</c>.
    /// </summary>
    /// <param name="popupListID">The popup list identifier.</param>
    /// <param name="selectionName">Name of the selection.</param>
    protected virtual void RecordPopupListState(GuiMenuElementID popupListID, string selectionName) { }

    /// <summary>
    /// Derived classes implement this abstract method, recording the state of the slider named <c>sliderName_lc</c>.
    /// UNDONE sliderValue insufficient to select which slider
    /// </summary>
    /// <param name="sliderID">The slider identifier.</param>
    /// <param name="sliderValue">The slider value.</param>
    protected virtual void RecordSliderState(GuiMenuElementID sliderID, float sliderValue) { }

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to a GameObject that is also being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

}

