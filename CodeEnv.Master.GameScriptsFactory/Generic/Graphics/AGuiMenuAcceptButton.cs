// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiMenuAcceptButton.cs
// Abstract base class for Accept or Launch buttons used by AGuiWindow menus and screens. 
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
/// Abstract base class for Accept or Launch buttons used by AGuiWindow menus and screens. 
/// Accummulates values from GuiElements that are components of the menu or screen and when clicked, 
/// communicates those values to the appropriate target.
/// </summary>
public abstract class AGuiMenuAcceptButton : AGuiButton {

    protected UIToggle[] _checkboxes;
    protected UIPopupList[] _popupLists;
    protected UISlider[] _sliders;
    protected UIPanel _panel;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    protected virtual void InitializeValuesAndReferences() {
        _panel = gameObject.GetSafeFirstMonoBehaviourInParents<UIPanel>();
        _checkboxes = _panel.gameObject.GetComponentsInChildren<UIToggle>(includeInactive: true);
        _popupLists = _panel.gameObject.GetComponentsInChildren<UIPopupList>(includeInactive: true);
        _sliders = _panel.gameObject.GetComponentsInChildren<UISlider>(includeInactive: true);
    }

    protected override void OnLeftClick() {
        CaptureState();
    }

    /// <summary>
    /// Captures the state of all GuiElements via the 
    /// derived classes implementation of the abstract RecordXXXState methods.
    /// </summary>
    private void CaptureState() {
        foreach (UIToggle checkbox in _checkboxes) {
            var checkboxID = checkbox.gameObject.GetSafeMonoBehaviour<AGuiMenuElement>().ElementID;
            bool checkedState = checkbox.value;
            RecordCheckboxState(checkboxID, checkedState);
        }
        foreach (UIPopupList popupList in _popupLists) {
            var popupListID = popupList.gameObject.GetSafeMonoBehaviour<AGuiMenuElement>().ElementID;
            string selection = popupList.value;
            RecordPopupListState(popupListID, selection);
        }
        foreach (UISlider slider in _sliders) {
            var sliderID = slider.gameObject.GetSafeMonoBehaviour<AGuiMenuElement>().ElementID;
            float sliderValue = slider.value;
            RecordSliderState(sliderID, sliderValue);
        }
        ValidateStateOnCapture();
    }

    /// <summary>
    /// Records the state of the checkbox that has <c>checkboxID</c>.
    /// </summary>
    /// <param name="checkboxID">The checkbox identifier.</param>
    /// <param name="isChecked">if set to <c>true</c> [checked state].</param>
    protected virtual void RecordCheckboxState(GuiElementID checkboxID, bool isChecked) { }

    /// <summary>
    ///Records the selection of the popup list that has <c>popupListID</c>.
    /// </summary>
    /// <param name="popupListID">The popup list identifier.</param>
    /// <param name="selection">The selection.</param>
    protected virtual void RecordPopupListState(GuiElementID popupListID, string selection) { }

    /// <summary>
    /// Records the value of the slider that has <c>sliderID</c>.
    /// </summary>
    /// <param name="sliderID">The slider identifier.</param>
    /// <param name="value">The slider value.</param>
    protected virtual void RecordSliderState(GuiElementID sliderID, float value) { }

    /// <summary>
    /// Validates the state of the GuiElements of this window when captured.
    /// </summary>
    protected virtual void ValidateStateOnCapture() { }

}

