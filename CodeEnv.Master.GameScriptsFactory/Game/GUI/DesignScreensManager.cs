// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DesignScreensManager.cs
// Manager for the DesignsScreen. Allows selection of which kind of design screen to view and shows it. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Manager for the DesignsScreen. Allows selection of which kind of design screen to view and shows it. 
/// <remarks>Allows use of a single UI hierarchy that makes for manageable maintenance by dynamically swapping 
/// AUnitDesignScreen instances. As the instances can change, editor prefab fields and manually wired button 
/// connections won't work. This manager handles it by exposing pass through methods for the buttons to wire
/// too and then routes the method call to the current AUnitDesignScreen instance.</remarks>
/// </summary>
public class DesignScreensManager : AMonoBase {

    private const int ScreenChoiceCheckboxGroupNumber = 1;

    [SerializeField]
    private DesignIconGuiElement _designIconPrefab = null;

    [SerializeField]
    private EquipmentIconGuiElement _equipmentIconPrefab = null;

    [SerializeField]
    private GameObject _threeDModelStagePrefab = null;

    public string DebugName { get { return GetType().Name; } }

    private GuiWindow _screenChoicePopupWindow;
    private AUnitDesignWindow _currentDesignWindow;
    private GameObject _currentDesignWindowGo;

    protected sealed override void Awake() {
        base.Awake();
        ValidatePrefabs();
        InitializeValuesAndReferences();
    }

    private void ValidatePrefabs() {
        D.AssertNotNull(_designIconPrefab);
        D.AssertNotNull(_equipmentIconPrefab);
        D.AssertNotNull(_threeDModelStagePrefab);
    }

    private void InitializeValuesAndReferences() {
        _screenChoicePopupWindow = gameObject.GetSingleComponentInImmediateChildren<GuiWindow>();
        var screenChoiceCheckboxes = _screenChoicePopupWindow.GetComponentsInChildren<UIToggle>();
        D.AssertEqual(5, screenChoiceCheckboxes.Length);
        foreach (var checkbox in screenChoiceCheckboxes) {
            checkbox.group = ScreenChoiceCheckboxGroupNumber;
        }

        _currentDesignWindow = gameObject.GetSingleComponentInChildren<AUnitDesignWindow>();
        _currentDesignWindowGo = _currentDesignWindow.gameObject;
    }

    #region Public API

    // Public to allow drag and drop to buttons in the GUI

    /// <summary>
    /// Initializes the create design popup window in the current DesignScreen.
    /// <remarks>Called by Design's CreateDesign Button.</remarks>
    /// </summary>
    public void __InitializeCreateDesignPopup() {
        _currentDesignWindow.InitializeCreateDesignPopup();
    }

    /// <summary>
    /// Handles re-showing the UI in the current DesignScreen.
    /// <remarks>Called by CreateDesignPopupWindow Cancel Button.</remarks>
    /// </summary>
    public void __HandleCreateDesignPopupCancelled() {
        _currentDesignWindow.HandleCreateDesignPopupCancelled();
    }

    /// <summary>
    /// Creates a design from the chosen Hull and DesignName in the current DesignScreen.
    /// <remarks>Called by CreateDesignPopupWindow's Accept Button.</remarks>
    /// </summary>
    public void __CreateDesign() {
        _currentDesignWindow.CreateDesign();
    }

    /// <summary>
    /// Edits the chosen design in the current DesignScreen.
    /// <remarks>Called by Design's EditDesign Button.</remarks>
    /// </summary>
    public void __EditChosenDesign() {
        _currentDesignWindow.EditChosenDesign();
    }

    /// <summary>
    /// Allows editing of the selected obsolete Design after its name has been changed in the current DesignScreen.
    /// <remarks>Called by RenameObsoleteDesignPopupWindow's Accept Button.</remarks>
    /// </summary>
    public void __EditObsoleteDesign() {
        _currentDesignWindow.EditObsoleteDesign();
    }

    /// <summary>
    /// Handles re-showing the UI in the current DesignScreen.
    /// <remarks>Called by RenameObsoleteDesignPopupWindow Cancel Button.</remarks>
    /// </summary>
    public void __HandleRenameObsoleteDesignPopupCancelled() {
        _currentDesignWindow.HandleRenameObsoleteDesignPopupCancelled();
    }

    /// <summary>
    /// Registers the finished design and shows its new icon in the Design ScrollView of the current DesignScreen.
    /// <remarks>Called by Designer's Apply Button.</remarks>
    /// </summary>
    public void __ApplyDesign() {
        _currentDesignWindow.ApplyDesign();
    }

    /// <summary>
    /// Resets the work in process design to its original state in the current DesignScreen.
    /// <remarks>Called by Designer's Reset Button.</remarks>
    /// </summary>
    public void __ResetDesigner() {
        _currentDesignWindow.ResetDesigner();
    }

    public void __AutoDesigner() {
        _currentDesignWindow.AutoDesigner();
    }

    /// <summary>
    /// Hides the Designer UI portion of the window without applying any design changes in the current DesignScreen.
    /// <remarks>Called by Designer's Close Button.</remarks>
    /// </summary>
    public void __CloseDesigner() {
        _currentDesignWindow.CloseDesigner();
    }

    /// <summary>
    /// Obsoletes the chosen design, if any, in the current DesignScreen.
    /// <remarks>Called by Design's Obsolete Button.</remarks>
    /// </summary>
    public void __ObsoleteChosenDesign() {
        _currentDesignWindow.ObsoleteChosenDesign();
    }

    public void ShowScreenChoicePopupWindow() {
        _screenChoicePopupWindow.Show();
    }

    /// <summary>
    /// Implements the user's choice of a design screen by initializing and showing it.
    /// <remarks>Called by the SelectDesignScreenPopupWindow Accept Button.</remarks>
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    public void ShowChosenDesignScreen() {
        UIToggle chosenToggle = UIToggle.GetActiveToggle(ScreenChoiceCheckboxGroupNumber);
        D.Assert(chosenToggle.value);
        GuiElementID checkboxID = chosenToggle.GetComponent<GuiElement>().ElementID;
        InitializeDesignWindow(checkboxID);
        _currentDesignWindow.Show();
    }

    public void HideCurrentDesignScreen() {
        _currentDesignWindow.Hide();
    }

    #endregion

    /// <summary>
    /// Initializes the design window indicated by checkboxID. If the design window designated by checkboxID is not 
    /// the _currentDesignWindow, then the DesignWindow script will be replaced and then initialized in preparation for showing.
    /// </summary>
    /// <param name="checkboxID">The CheckboxID indicating the chosen design window.</param>
    /// <exception cref="System.NotImplementedException"></exception>
    private void InitializeDesignWindow(GuiElementID checkboxID) {
        Type currentDesignWindowType = _currentDesignWindow.GetType();
        switch (checkboxID) {
            case GuiElementID.ShipDesignWindowCheckbox:
                if (currentDesignWindowType != typeof(ShipDesignWindow)) {
                    _currentDesignWindow.ActivateContent();
                    Destroy(_currentDesignWindow);
                    _currentDesignWindow = _currentDesignWindowGo.AddComponent<ShipDesignWindow>();
                    _currentDesignWindow.AutoExecuteStartingState = false;
                }
                break;
            case GuiElementID.FacilityDesignWindowCheckbox:
                if (currentDesignWindowType != typeof(FacilityDesignWindow)) {
                    _currentDesignWindow.ActivateContent();
                    Destroy(_currentDesignWindow);
                    _currentDesignWindow = _currentDesignWindowGo.AddComponent<FacilityDesignWindow>();
                    _currentDesignWindow.AutoExecuteStartingState = false;
                }
                break;
            case GuiElementID.FleetCmdDesignWindowCheckbox:
                if (currentDesignWindowType != typeof(FleetCmdDesignWindow)) {
                    _currentDesignWindow.ActivateContent();
                    Destroy(_currentDesignWindow);
                    _currentDesignWindow = _currentDesignWindowGo.AddComponent<FleetCmdDesignWindow>();
                    _currentDesignWindow.AutoExecuteStartingState = false;
                }
                break;
            case GuiElementID.StarbaseCmdDesignWindowCheckbox:
                if (currentDesignWindowType != typeof(StarbaseCmdDesignWindow)) {
                    _currentDesignWindow.ActivateContent();
                    Destroy(_currentDesignWindow);
                    _currentDesignWindow = _currentDesignWindowGo.AddComponent<StarbaseCmdDesignWindow>();
                    _currentDesignWindow.AutoExecuteStartingState = false;
                }
                break;
            case GuiElementID.SettlementCmdDesignWindowCheckbox:
                if (currentDesignWindowType != typeof(SettlementCmdDesignWindow)) {
                    _currentDesignWindow.ActivateContent();
                    Destroy(_currentDesignWindow);
                    _currentDesignWindow = _currentDesignWindowGo.AddComponent<SettlementCmdDesignWindow>();
                    _currentDesignWindow.AutoExecuteStartingState = false;
                }
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(checkboxID));
        }
        _currentDesignWindow.__InitializePrefabs(_designIconPrefab, _equipmentIconPrefab, _threeDModelStagePrefab);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

}

