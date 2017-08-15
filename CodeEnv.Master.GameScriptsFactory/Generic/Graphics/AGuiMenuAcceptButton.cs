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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Accept or Launch buttons used by AGuiWindow menus and screens. 
/// Accumulates values from GuiElements that are components of the menu or screen and when clicked, 
/// communicates those values to the appropriate target.
/// </summary>
public abstract class AGuiMenuAcceptButton : AGuiButton {

    protected PlayerPrefsManager _playerPrefsMgr;

    private UIToggle[] _checkboxes;
    private UIPopupList[] _popupLists;
    private UISlider[] _sliders;
    private UIPanel _panel;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _panel = gameObject.GetSafeFirstComponentInParents<UIPanel>();  // more than one _panel in parents
        _checkboxes = _panel.gameObject.GetComponentsInChildren<UIToggle>(includeInactive: true);
        _popupLists = _panel.gameObject.GetComponentsInChildren<UIPopupList>(includeInactive: true);
        _sliders = _panel.gameObject.GetComponentsInChildren<UISlider>(includeInactive: true);
        _playerPrefsMgr = PlayerPrefsManager.Instance;
    }

    #region Event and Property Change Handlers

    protected override void HandleValidClick() {
        CaptureState();
    }

    #endregion

    /// <summary>
    /// Captures the state of all GuiElements via the 
    /// derived classes implementation of the abstract RecordXXXState methods.
    /// </summary>
    private void CaptureState() {
        foreach (UIToggle checkbox in _checkboxes) {
            var checkboxID = checkbox.GetComponent<AGuiMenuElement>().ElementID;
            bool checkedState = checkbox.value;
            RecordCheckboxState(checkboxID, checkedState);
        }
        foreach (UIPopupList popupList in _popupLists) {
            AGuiMenuPopupListBase popupListBase = popupList.gameObject.GetSafeComponent<AGuiMenuPopupListBase>();
            var popupListID = popupListBase.ElementID;
            string selection = popupListBase.SelectedValue;
            string convertedSelection = popupListBase.ConvertedSelectedValue;
            RecordPopupListState(popupListID, selection, convertedSelection);
        }
        foreach (UISlider slider in _sliders) {
            var sliderID = slider.GetComponent<AGuiMenuElement>().ElementID;
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
    /// Records the selection of the popup list that has <c>popupListID</c>.
    /// </summary>
    /// <param name="popupListID">The popup list identifier.</param>
    /// <param name="selection">The selection.</param>
    /// <param name="convertedSelection">The converted selection.</param>
    protected virtual void RecordPopupListState(GuiElementID popupListID, string selection, string convertedSelection) { }

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

