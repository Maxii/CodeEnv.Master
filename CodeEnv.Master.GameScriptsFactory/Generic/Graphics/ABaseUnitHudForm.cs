// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ABaseUnitHudForm.cs
// Abstract base class for Settlement and Starbase Forms used by the UnitHud.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Settlement and Starbase Forms used by the UnitHud.
/// <remarks>Handles both User and AI-owned bases.</remarks>
/// </summary>
public abstract class ABaseUnitHudForm : AForm {

    private const string ElementIconExtension = " ElementIcon";
    private const string TitleFormat = "Selected Base: {0}";

    // 9.27.17 Management of ConstructibleElementDesigns and the ConstructionQueue moved to ConstructionGuiModule

    [SerializeField]
    private FacilityIconGuiElement _facilityIconPrefab = null;
    [SerializeField]
    private ShipIconGuiElement _shipIconPrefab = null;

    [SerializeField]
    private UIButton _unitFocusButton = null;
    [SerializeField]
    private UIButton _unitClearOrdersButton = null;
    [SerializeField]
    private UIButton _unitScuttleButton = null;
    [SerializeField]
    private MyNguiToggleButton _unitRepairButton = null;
    [SerializeField]
    private MyNguiToggleButton _unitRefitButton = null;
    [SerializeField]
    private MyNguiToggleButton _unitDisbandButton = null;

    [SerializeField]
    private MyNguiToggleButton _facilityRepairButton = null;
    [SerializeField]
    private MyNguiToggleButton _facilityRefitButton = null;
    [SerializeField]
    private MyNguiToggleButton _facilityDisbandButton = null;
    [SerializeField]
    private UIButton _facilityScuttleButton = null;

    [SerializeField]
    private UIButton _shipCreateFleetButton = null;
    [SerializeField]
    private MyNguiToggleButton _shipRepairButton = null;
    [SerializeField]
    private MyNguiToggleButton _shipRefitButton = null;
    [SerializeField]
    private MyNguiToggleButton _shipDisbandButton = null;
    [SerializeField]
    private UIButton _shipScuttleButton = null;

    private AUnitBaseCmdItem _selectedUnit;
    public AUnitBaseCmdItem SelectedUnit {
        get { return _selectedUnit; }
        set {
            D.AssertNull(_selectedUnit);
            SetProperty<AUnitBaseCmdItem>(ref _selectedUnit, value, "SelectedUnit", SelectedUnitPropChangedHandler);
        }
    }

    protected HashSet<FacilityIconGuiElement> _pickedFacilityIcons;
    protected GameManager _gameMgr;

    private PlayerAIManager _playerAiMgr;
    private HashSet<MyNguiToggleButton> _toggleButtonsUsedThisSession;
    private IList<Transform> _sortedFacilityIconTransforms;
    private IList<Transform> _sortedHangerShipIconTransforms;
    private HashSet<ShipIconGuiElement> _pickedHangerShipIcons;
    private ConstructionGuiModule _elementConstructionModule;
    private UILabel _formTitleLabel;
    private UIGrid _facilityIconsGrid;
    private UIGrid _hangerShipIconsGrid;

    protected override void InitializeValuesAndReferences() {
        //D.Log("{0} is initializing.", DebugName);
        _gameMgr = GameManager.Instance;
        _formTitleLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>();

        _elementConstructionModule = gameObject.GetSingleComponentInChildren<ConstructionGuiModule>();

        _facilityIconsGrid = gameObject.GetSingleComponentInChildren<FacilityIconGuiElement>().gameObject.GetSingleComponentInParents<UIGrid>();
        _facilityIconsGrid.arrangement = UIGrid.Arrangement.Horizontal;
        _facilityIconsGrid.sorting = UIGrid.Sorting.Custom;
        _facilityIconsGrid.onCustomSort = CompareFacilityIcons;

        _hangerShipIconsGrid = gameObject.GetSingleComponentInChildren<ShipIconGuiElement>().gameObject.GetSingleComponentInParents<UIGrid>();
        _hangerShipIconsGrid.arrangement = UIGrid.Arrangement.Horizontal;
        _hangerShipIconsGrid.sorting = UIGrid.Sorting.Custom;
        _hangerShipIconsGrid.onCustomSort = CompareShipIcons;

        _pickedFacilityIcons = new HashSet<FacilityIconGuiElement>();
        _pickedHangerShipIcons = new HashSet<ShipIconGuiElement>();
        _toggleButtonsUsedThisSession = new HashSet<MyNguiToggleButton>();

        _sortedFacilityIconTransforms = new List<Transform>();
        _sortedHangerShipIconTransforms = new List<Transform>();

        _unitRepairButton.Initialize();
        _unitRefitButton.Initialize();
        _unitDisbandButton.Initialize();

        _facilityRepairButton.Initialize();
        _facilityRefitButton.Initialize();
        _facilityDisbandButton.Initialize();

        _shipRepairButton.Initialize();
        _shipRefitButton.Initialize();
        _shipDisbandButton.Initialize();

        ConnectButtonEventHandlers();
    }

    private void ConnectButtonEventHandlers() {
        EventDelegate.Add(_unitFocusButton.onClick, UnitFocusButtonClickedEventHandler);
        EventDelegate.Add(_unitClearOrdersButton.onClick, UnitClearOrdersButtonClickedEventHandler);
        EventDelegate.Add(_unitScuttleButton.onClick, UnitScuttleButtonClickedEventHandler);
        _unitRepairButton.toggleStateChanged += UnitRepairButtonToggleChangedEventHandler;
        _unitRefitButton.toggleStateChanged += UnitRefitButtonToggleChangedEventHandler;
        _unitDisbandButton.toggleStateChanged += UnitDisbandButtonToggleChangedEventHandler;
        //_cmdModuleRefitButton.toggleStateChanged += CmdModuleRefitToggleChangedEventHandler;

        _facilityRepairButton.toggleStateChanged += FacilityRepairButtonToggleChangedEventHandler;
        _facilityRefitButton.toggleStateChanged += FacilityRefitButtonToggleChangedEventHandler;
        _facilityDisbandButton.toggleStateChanged += FacilityDisbandButtonToggleChangedEventHandler;
        EventDelegate.Add(_facilityScuttleButton.onClick, FacilityScuttleButtonClickedEventHandler);

        EventDelegate.Add(_shipCreateFleetButton.onClick, ShipCreateFleetButtonClickedEventHandler);
        _shipRepairButton.toggleStateChanged += ShipRepairButtonToggleChangedEventHandler;
        _shipRefitButton.toggleStateChanged += ShipRefitButtonToggleChangedEventHandler;
        _shipDisbandButton.toggleStateChanged += ShipDisbandButtonToggleChangedEventHandler;
        EventDelegate.Add(_shipScuttleButton.onClick, ShipScuttleButtonClickedEventHandler);
    }

    public sealed override void PopulateValues() {
        D.Assert(_gameMgr.IsPaused);
        D.AssertNotNull(SelectedUnit);  // UNCLEAR populate when Unit already destroyed?
        SubscribeToSelectedUnit();
        AssignValuesToMembers();
        AssessInteractibleHud();
    }

    protected override void AssignValuesToMembers() {
        _formTitleLabel.text = TitleFormat.Inject(SelectedUnit.UnitName);

        InitializeConstructionModule();
        BuildUnitCompositionIcons();
        BuildHangerShipIcons();
        AssessButtons();
    }

    protected virtual void InitializeConstructionModule() {
        _elementConstructionModule.SelectedUnit = SelectedUnit;
    }

    protected void DisableConstructionModuleButtons() {
        _elementConstructionModule.__DisableButtons();
    }

    private void SubscribeToSelectedUnit() {
        SelectedUnit.deathOneShot += UnitDeathEventHandler;
        SelectedUnit.ConstructionMgr.constructionQueueChanged += ConstructionQueueChangedEventHandler;
    }

    #region Event and Property Change Handlers

    private void SelectedUnitPropChangedHandler() {
        HandleSelectedUnitChanged();
    }

    private void ConstructionQueueChangedEventHandler(object sender, EventArgs e) {
        HandleConstructionQueueChanged();
    }

    private void UnitFocusButtonClickedEventHandler() {
        HandleUnitFocusButtonClicked();
    }

    private void UnitClearOrdersButtonClickedEventHandler() {
        HandleUnitClearOrdersButtonClicked();
    }

    private void UnitRepairButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleUnitRepairButtonToggleChanged();
    }

    private void UnitRefitButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleUnitRefitButtonToggleChanged();
    }

    private void UnitDisbandButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleUnitDisbandButtonToggleChanged();
    }

    private void UnitScuttleButtonClickedEventHandler() {
        HandleUnitScuttleButtonClicked();
    }

    private void FacilityRepairButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleFacilityRepairButtonToggleChanged();
    }

    private void FacilityRefitButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleFacilityRefitButtonToggleChanged();
    }

    private void FacilityDisbandButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleFacilityDisbandButtonToggleChanged();
    }

    private void FacilityScuttleButtonClickedEventHandler() {
        HandleFacilityScuttleButtonClicked();
    }

    private void ShipCreateFleetButtonClickedEventHandler() {
        HandleShipCreateHangerFleetButtonClicked();
    }

    private void ShipRepairButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleShipRepairButtonToggleChanged();
    }

    private void ShipRefitButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleShipRefitButtonToggleChanged();
    }

    private void ShipDisbandButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleShipDisbandButtonToggleChanged();
    }

    private void ShipScuttleButtonClickedEventHandler() {
        HandleShipScuttleButtonClicked();
    }

    private void UnitDeathEventHandler(object sender, EventArgs e) {
        AUnitBaseCmdItem unit = sender as AUnitBaseCmdItem;
        HandleDeathOf(unit);
    }

    private void FacilityDeathEventHandler(object sender, EventArgs e) {
        FacilityItem unitFacility = sender as FacilityItem;
        HandleDeathOf(unitFacility);
    }

    private void HangerShipDeathEventHandler(object sender, EventArgs e) {
        ShipItem hangerShip = sender as ShipItem;
        HandleDeathOf(hangerShip);
    }

    private void FacilityIconClickedEventHandler(GameObject go) {
        var inputHelper = GameInputHelper.Instance;
        FacilityIconGuiElement iconClicked = go.GetComponent<FacilityIconGuiElement>();
        if (inputHelper.IsLeftMouseButton) {
            if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftControl, KeyCode.RightControl)) {
                HandleFacilityIconCntlLeftClicked(iconClicked);
            }
            else if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftShift, KeyCode.RightShift)) {
                HandleFacilityIconShiftLeftClicked(iconClicked);
            }
            else {
                HandleFacilityIconLeftClicked(iconClicked);
            }
        }
        else if (inputHelper.IsMiddleMouseButton) {
            HandleFacilityIconMiddleClicked(iconClicked);
        }
    }

    private void HangerShipIconClickedEventHandler(GameObject go) {
        var inputHelper = GameInputHelper.Instance;
        ShipIconGuiElement iconClicked = go.GetComponent<ShipIconGuiElement>();
        if (inputHelper.IsLeftMouseButton) {
            if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftControl, KeyCode.RightControl)) {
                HandleShipIconCntlLeftClicked(iconClicked);
            }
            else if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftShift, KeyCode.RightShift)) {
                HandleShipIconShiftLeftClicked(iconClicked);
            }
            else {
                HandleShipIconLeftClicked(iconClicked);
            }
        }
    }

    #endregion

    private void HandleSelectedUnitChanged() {
        D.AssertNull(_playerAiMgr);
        _playerAiMgr = _gameMgr.GetAIManagerFor(SelectedUnit.Owner);
    }

    private void HandleConstructionQueueChanged() {
        BuildUnitCompositionIcons();
        BuildHangerShipIcons();
    }

    #region Unit Interaction

    private void HandleUnitFocusButtonClicked() {
        FocusOn(SelectedUnit);
    }

    private void HandleUnitClearOrdersButtonClicked() {
        SelectedUnit.ClearOrdersAndIdle();
        AssessUnitButtons();
    }

    private void HandleUnitRepairButtonToggleChanged() {
        BaseOrder order;
        bool isButtonToggledIn = _unitRepairButton.IsToggledIn;
        if (isButtonToggledIn) {
            order = new BaseOrder(BaseDirective.Repair, OrderSource.User, SelectedUnit);
            //D.Log("{0} is issuing an order to {1} to Repair.", DebugName, SelectedUnit.DebugName);
        }
        else {
            order = new BaseOrder(BaseDirective.Cancel, OrderSource.User);
        }
        SelectedUnit.CurrentOrder = order;
        _toggleButtonsUsedThisSession.Add(_unitRepairButton);
        AssessUnitButtons();
    }

    private void HandleUnitRefitButtonToggleChanged() {
        BaseOrder order;
        bool isButtonToggledIn = _unitRefitButton.IsToggledIn;
        if (isButtonToggledIn) {
            order = new BaseOrder(BaseDirective.Refit, OrderSource.User, SelectedUnit);
            //D.Log("{0} is issuing an order to {1} to Refit.", DebugName, SelectedUnit.DebugName);
        }
        else {
            order = new BaseOrder(BaseDirective.Cancel, OrderSource.User);
        }
        SelectedUnit.CurrentOrder = order;
        _toggleButtonsUsedThisSession.Add(_unitRefitButton);
        AssessUnitButtons();
    }

    private void HandleUnitDisbandButtonToggleChanged() {
        BaseOrder order;
        bool isButtonToggledIn = _unitDisbandButton.IsToggledIn;
        if (isButtonToggledIn) {
            order = new BaseOrder(BaseDirective.Disband, OrderSource.User, SelectedUnit);
            //D.Log("{0} is issuing an order to {1} to Disband.", DebugName, SelectedUnit.DebugName);
        }
        else {
            order = new BaseOrder(BaseDirective.Cancel, OrderSource.User);
        }
        SelectedUnit.CurrentOrder = order;
        _toggleButtonsUsedThisSession.Add(_unitDisbandButton);
        AssessUnitButtons();
    }

    private void HandleUnitScuttleButtonClicked() {
        var scuttleOrder = new BaseOrder(BaseDirective.Scuttle, OrderSource.User);
        SelectedUnit.CurrentOrder = scuttleOrder;
        // as SelectedUnit dies, there will be no CurrentSelection so game will resume and shouldn't be re-paused
    }

    /// <summary>
    /// Assesses the unit buttons.
    /// </summary>
    protected virtual void AssessUnitButtons() {
        AssessUnitFocusButton();

        bool isOrderedToRepair = SelectedUnit.IsCurrentOrderDirectiveAnyOf(BaseDirective.Repair);
        GameColor iconColor = isOrderedToRepair ? TempGameValues.SelectedColor : GameColor.White;
        _unitRepairButton.SetToggledState(isOrderedToRepair, iconColor);
        if (HasBeenUsedThisSession(_unitRepairButton)) {
            // 1) already used this session && if isOrderedToRepair -> button should be enabled to allow cancel
            // 2) already used this session && if !isOrderedToRepair -> cancel already used so button should allow for more pushes
            _unitRepairButton.IsEnabled = true;
        }
        else {
            // button not used this session
            if (isOrderedToRepair) {
                // already ordered to repair from some other source prior to this session so disable button as cancel won't work
                _unitRepairButton.IsEnabled = false;
            }
            else {
                // button not yet used this session so let SelectedUnit tell us whether Repair is an authorized order
                _unitRepairButton.IsEnabled = SelectedUnit.IsAuthorizedForNewOrder(BaseDirective.Repair);
            }
        }

        bool isOrderedToRefit = SelectedUnit.IsCurrentOrderDirectiveAnyOf(BaseDirective.Refit);
        iconColor = isOrderedToRefit ? TempGameValues.SelectedColor : GameColor.White;
        _unitRefitButton.SetToggledState(isOrderedToRefit, iconColor);
        if (HasBeenUsedThisSession(_unitRefitButton)) {
            _unitRefitButton.IsEnabled = true;
        }
        else {
            _unitRefitButton.IsEnabled = isOrderedToRefit ? false : SelectedUnit.IsAuthorizedForNewOrder(BaseDirective.Refit);
        }

        bool isOrderedToDisband = SelectedUnit.IsCurrentOrderDirectiveAnyOf(BaseDirective.Disband);
        iconColor = isOrderedToDisband ? TempGameValues.SelectedColor : GameColor.White;
        _unitDisbandButton.SetToggledState(isOrderedToDisband, iconColor);
        if (HasBeenUsedThisSession(_unitDisbandButton)) {
            _unitDisbandButton.IsEnabled = true;
        }
        else {
            _unitDisbandButton.IsEnabled = isOrderedToDisband ? false : SelectedUnit.IsAuthorizedForNewOrder(BaseDirective.Disband);
        }

        _unitClearOrdersButton.isEnabled = true;
        _unitScuttleButton.isEnabled = SelectedUnit.IsAuthorizedForNewOrder(BaseDirective.Scuttle);
    }

    protected void AssessUnitFocusButton() {
        _unitFocusButton.isEnabled = true;
    }

    protected void DisableUnitButtons() {
        _unitClearOrdersButton.isEnabled = false;
        _unitScuttleButton.isEnabled = false;
        _unitRepairButton.IsEnabled = false;
        _unitRefitButton.IsEnabled = false;
        _unitDisbandButton.IsEnabled = false;
    }

    #endregion

    #region Unit Composition (Facility) Icon Interaction

    private void HandleFacilityIconLeftClicked(FacilityIconGuiElement icon) {
        if (icon.IsPicked) {
            D.Assert(_pickedFacilityIcons.Contains(icon));
            // user is unpicking this icon
            icon.IsPicked = false;
            bool isRemoved = _pickedFacilityIcons.Remove(icon);
            D.Assert(isRemoved);
        }
        else {
            PickSingleFacilityIcon(icon);
        }
        AssessInteractibleHud();
        AssessUnitCompositionButtons();
    }

    private void HandleFacilityIconCntlLeftClicked(FacilityIconGuiElement icon) {
        if (icon.IsPicked) {
            D.Assert(_pickedFacilityIcons.Contains(icon));
            // user is unpicking this icon
            icon.IsPicked = false;
            bool isRemoved = _pickedFacilityIcons.Remove(icon);
            D.Assert(isRemoved);
        }
        else {
            D.Assert(!_pickedFacilityIcons.Contains(icon));
            icon.IsPicked = true;
            _pickedFacilityIcons.Add(icon);
        }
        AssessInteractibleHud();
        AssessUnitCompositionButtons();
    }

    private void HandleFacilityIconShiftLeftClicked(FacilityIconGuiElement clickedIcon) {
        var iconsToPick = new List<FacilityIconGuiElement>();
        int clickedIconIndex = _sortedFacilityIconTransforms.IndexOf(clickedIcon.transform);
        if (_pickedFacilityIcons.Any()) {
            FacilityIconGuiElement anchorIcon = _pickedFacilityIcons.Last();   // should be in order added
            int anchorIconIndex = _sortedFacilityIconTransforms.IndexOf(anchorIcon.transform);
            if (anchorIconIndex == clickedIconIndex) {
                // clicked on the already picked anchor icon so do nothing
                return;
            }

            // pick all the icons between the anchor and the clicked icon, inclusive
            UnpickAllFacilityIcons();
            // clickedIcon must always be the last icon added so it becomes the next anchorIcon
            if (anchorIconIndex < clickedIconIndex) {
                for (int index = anchorIconIndex; index <= clickedIconIndex; index++) {
                    Transform iconTransform = _sortedFacilityIconTransforms[index];
                    FacilityIconGuiElement icon = iconTransform.GetComponent<FacilityIconGuiElement>();
                    if (icon != null) {
                        iconsToPick.Add(icon);
                    }
                }
            }
            else {
                for (int index = anchorIconIndex; index >= clickedIconIndex; index--) {
                    Transform iconTransform = _sortedFacilityIconTransforms[index];
                    FacilityIconGuiElement icon = iconTransform.GetComponent<FacilityIconGuiElement>();
                    if (icon != null) {
                        iconsToPick.Add(icon);
                    }
                }
            }
        }
        else {
            // pick all icons from the first to this one
            for (int index = 0; index <= clickedIconIndex; index++) {
                Transform iconTransform = _sortedFacilityIconTransforms[index];
                FacilityIconGuiElement icon = iconTransform.GetComponent<FacilityIconGuiElement>();
                if (icon != null) {
                    iconsToPick.Add(icon);
                }
            }
        }

        iconsToPick.ForAll(icon => {
            icon.IsPicked = true;
            _pickedFacilityIcons.Add(icon);
        });
        AssessInteractibleHud();
        AssessUnitCompositionButtons();
    }

    private void HandleFacilityIconMiddleClicked(FacilityIconGuiElement icon) {
        FocusOn(icon.Element);
    }

    private void HandleFacilityRepairButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedFacilityIcons.Count);
        _toggleButtonsUsedThisSession.Add(_facilityRepairButton);
        FacilityOrder order;
        if (_facilityRepairButton.IsToggledIn) {
            order = new FacilityOrder(FacilityDirective.Repair, OrderSource.User, SelectedUnit);
        }
        else {
            order = new FacilityOrder(FacilityDirective.Cancel, OrderSource.User);
        }
        _pickedFacilityIcons.First().Element.CurrentOrder = order;
        // buttons already assessed
    }

    private void HandleFacilityRefitButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedFacilityIcons.Count);
        _toggleButtonsUsedThisSession.Add(_facilityRefitButton);
        var pickedFacility = _pickedFacilityIcons.First().Element;
        FacilityOrder order;
        if (_facilityRefitButton.IsToggledIn) {
            FacilityDesign designToBeRefit = pickedFacility.Data.Design;
            FacilityDesign refitDesign;
            bool isRefitDesignAvailable = _playerAiMgr.Designs.TryGetUpgradeDesign(designToBeRefit, out refitDesign);
            D.Assert(isRefitDesignAvailable);
            order = new FacilityRefitOrder(OrderSource.User, refitDesign, SelectedUnit);
        }
        else {
            order = new FacilityOrder(FacilityDirective.Cancel, OrderSource.User);
        }
        pickedFacility.CurrentOrder = order;
        // buttons already assessed
    }

    private void HandleFacilityDisbandButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedFacilityIcons.Count);
        _toggleButtonsUsedThisSession.Add(_facilityDisbandButton);
        var pickedFacility = _pickedFacilityIcons.First().Element;
        FacilityOrder order;
        if (_facilityDisbandButton.IsToggledIn) {
            order = new FacilityOrder(FacilityDirective.Disband, OrderSource.User, SelectedUnit);
        }
        else {
            order = new FacilityOrder(FacilityDirective.Cancel, OrderSource.User);
        }
        pickedFacility.CurrentOrder = order;
        // buttons already assessed
    }

    private void HandleFacilityScuttleButtonClicked() {
        D.Assert(_pickedFacilityIcons.Any());
        var pickedElements = _pickedFacilityIcons.Select(icon => icon.Element);

        //D.Log("{0} received an OnClick event telling it to scuttle {1}.", DebugName, pickedElements.Select(e => e.DebugName).Concatenate());
        var scuttleOrder = new FacilityOrder(FacilityDirective.Scuttle, OrderSource.User);
        pickedElements.ForAll(e => e.CurrentOrder = scuttleOrder);
        // buttons already assessed
    }

    protected virtual void AssessUnitCompositionButtons() {

        bool isFacilityScuttleButtonEnabled = false;
        if (_pickedFacilityIcons.Any()) {
            isFacilityScuttleButtonEnabled = _pickedFacilityIcons.Select(icon => icon.Element).All(e => e.IsAuthorizedForNewOrder(FacilityDirective.Scuttle));
        }
        _facilityScuttleButton.isEnabled = isFacilityScuttleButtonEnabled;

        if (_pickedFacilityIcons.Count == Constants.One) {
            // 11.18.17 picked elements are limited to one to make cancel order practical to implement
            var pickedFacility = _pickedFacilityIcons.First().Element;

            bool isOrderedToRepair = pickedFacility.IsCurrentOrderDirectiveAnyOf(FacilityDirective.Repair);
            GameColor iconColor = isOrderedToRepair ? TempGameValues.SelectedColor : GameColor.White;
            _facilityRepairButton.SetToggledState(isOrderedToRepair, iconColor);
            if (HasBeenUsedThisSession(_facilityRepairButton)) {
                // 1) already used this session && if isOrderedToRepair -> button should be enabled to allow cancel
                // 2) already used this session && if !isOrderedToRepair -> cancel already used so button should allow for more pushes
                _facilityRepairButton.IsEnabled = true;
            }
            else {
                // button not used this session
                if (isOrderedToRepair) {
                    // already ordered to repair from some other source prior to this session so disable button as cancel won't work
                    _facilityRepairButton.IsEnabled = false;
                }
                else {
                    // button not yet used this session so let pickedFacility tell us whether Repair is an authorized order
                    _facilityRepairButton.IsEnabled = pickedFacility.IsAuthorizedForNewOrder(FacilityDirective.Repair);
                }
            }

            bool isOrderedToRefit = pickedFacility.IsCurrentOrderDirectiveAnyOf(FacilityDirective.Refit);
            iconColor = isOrderedToRefit ? TempGameValues.SelectedColor : GameColor.White;
            _facilityRefitButton.SetToggledState(isOrderedToRefit, iconColor);
            if (HasBeenUsedThisSession(_facilityRefitButton)) {
                _facilityRefitButton.IsEnabled = true;
            }
            else {
                _facilityRefitButton.IsEnabled = isOrderedToRefit ? false : pickedFacility.IsAuthorizedForNewOrder(FacilityDirective.Refit);
            }

            bool isOrderedToDisband = pickedFacility.IsCurrentOrderDirectiveAnyOf(FacilityDirective.Disband);
            iconColor = isOrderedToDisband ? TempGameValues.SelectedColor : GameColor.White;
            _facilityDisbandButton.SetToggledState(isOrderedToDisband, iconColor);
            if (HasBeenUsedThisSession(_facilityDisbandButton)) {
                _facilityDisbandButton.IsEnabled = true;
            }
            else {
                _facilityDisbandButton.IsEnabled = isOrderedToDisband ? false : pickedFacility.IsAuthorizedForNewOrder(FacilityDirective.Disband);
            }
        }
        else {
            ResetUnitCompositionToggleButtons();
        }
    }

    protected void DisableUnitCompositionButtons() {
        _facilityScuttleButton.isEnabled = false;
        _facilityRepairButton.IsEnabled = false;
        _facilityRefitButton.IsEnabled = false;
        _facilityDisbandButton.IsEnabled = false;
    }

    private void PickSingleFacilityIcon(FacilityIconGuiElement icon) {
        UnpickAllFacilityIcons();
        icon.IsPicked = true;
        _pickedFacilityIcons.Add(icon);
    }

    private void UnpickAllFacilityIcons() {
        foreach (var icon in _pickedFacilityIcons) {
            icon.IsPicked = false;
        }
        _pickedFacilityIcons.Clear();
    }

    #endregion

    #region Ship Hanger Icon Interaction

    private void HandleShipIconLeftClicked(ShipIconGuiElement icon) {
        if (icon.IsPicked) {
            D.Assert(_pickedHangerShipIcons.Contains(icon));
            // user is unpicking this icon
            icon.IsPicked = false;
            bool isRemoved = _pickedHangerShipIcons.Remove(icon);
            D.Assert(isRemoved);
        }
        else {
            PickSingleShipIcon(icon);
        }
        AssessInteractibleHud();
        AssessHangerButtons();
    }

    private void HandleShipIconCntlLeftClicked(ShipIconGuiElement icon) {
        if (icon.IsPicked) {
            D.Assert(_pickedHangerShipIcons.Contains(icon));
            // user is unpicking this icon
            icon.IsPicked = false;
            bool isRemoved = _pickedHangerShipIcons.Remove(icon);
            D.Assert(isRemoved);
        }
        else {
            D.Assert(!_pickedHangerShipIcons.Contains(icon));
            icon.IsPicked = true;
            _pickedHangerShipIcons.Add(icon);
        }
        AssessInteractibleHud();
        AssessHangerButtons();
    }

    private void HandleShipIconShiftLeftClicked(ShipIconGuiElement clickedIcon) {
        var iconsToPick = new List<ShipIconGuiElement>();
        int clickedIconIndex = _sortedHangerShipIconTransforms.IndexOf(clickedIcon.transform);
        if (_pickedHangerShipIcons.Any()) {
            ShipIconGuiElement anchorIcon = _pickedHangerShipIcons.Last();   // should be in order added
            int anchorIconIndex = _sortedHangerShipIconTransforms.IndexOf(anchorIcon.transform);
            if (anchorIconIndex == clickedIconIndex) {
                // clicked on the already picked anchor icon so do nothing
                return;
            }

            // pick all the icons between the anchor and the clicked icon, inclusive
            UnpickAllShipIcons();
            // clickedIcon must always be the last icon added so it becomes the next anchorIcon
            if (anchorIconIndex < clickedIconIndex) {
                for (int index = anchorIconIndex; index <= clickedIconIndex; index++) {
                    Transform iconTransform = _sortedHangerShipIconTransforms[index];
                    ShipIconGuiElement icon = iconTransform.GetComponent<ShipIconGuiElement>();
                    if (icon != null) {
                        iconsToPick.Add(icon);
                    }
                }
            }
            else {
                for (int index = anchorIconIndex; index >= clickedIconIndex; index--) {
                    Transform iconTransform = _sortedHangerShipIconTransforms[index];
                    ShipIconGuiElement icon = iconTransform.GetComponent<ShipIconGuiElement>();
                    if (icon != null) {
                        iconsToPick.Add(icon);
                    }
                }
            }
        }
        else {
            // pick all icons from the first to this one
            for (int index = 0; index <= clickedIconIndex; index++) {
                Transform iconTransform = _sortedHangerShipIconTransforms[index];
                ShipIconGuiElement icon = iconTransform.GetComponent<ShipIconGuiElement>();
                if (icon != null) {
                    iconsToPick.Add(icon);
                }
            }
        }

        iconsToPick.ForAll(icon => {
            icon.IsPicked = true;
            _pickedHangerShipIcons.Add(icon);
        });
        AssessInteractibleHud();
        AssessHangerButtons();
    }

    private void HandleShipCreateHangerFleetButtonClicked() {
        D.Log("{0} is about to create a fleet from a hanger.", DebugName);
        var pickedShips = _pickedHangerShipIcons.Select(icon => icon.Element);
        var createFleetShips = pickedShips.Where(ship => ship.Availability != NewOrderAvailability.Unavailable);
        D.Assert(createFleetShips.Any());

        SelectedUnit.Hanger.FormFleetFrom("HangerFleet", Formation.Globe, createFleetShips);
        BuildHangerShipIcons();
    }

    private void HandleShipRepairButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedHangerShipIcons.Count);
        _toggleButtonsUsedThisSession.Add(_shipRepairButton);
        var pickedShip = _pickedHangerShipIcons.First().Element;
        ShipOrder order;
        if (_shipRepairButton.IsToggledIn) {
            order = new ShipOrder(ShipDirective.Repair, OrderSource.User, SelectedUnit);
        }
        else {
            order = new ShipOrder(ShipDirective.Cancel, OrderSource.User);
        }
        pickedShip.CurrentOrder = order;
        // buttons already assessed
    }

    private void HandleShipRefitButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedHangerShipIcons.Count);
        _toggleButtonsUsedThisSession.Add(_shipRefitButton);
        var pickedShip = _pickedHangerShipIcons.First().Element;
        ShipOrder order;
        if (_shipRefitButton.IsToggledIn) {
            ShipDesign designToBeRefit = pickedShip.Data.Design;
            ShipDesign refitDesign;
            bool isRefitDesignAvailable = _playerAiMgr.Designs.TryGetUpgradeDesign(designToBeRefit, out refitDesign);
            D.Assert(isRefitDesignAvailable);
            order = new ShipRefitOrder(OrderSource.User, refitDesign, SelectedUnit);
        }
        else {
            order = new ShipOrder(ShipDirective.Cancel, OrderSource.User);
        }
        pickedShip.CurrentOrder = order;
        // buttons already assessed
    }

    private void HandleShipDisbandButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedHangerShipIcons.Count);
        _toggleButtonsUsedThisSession.Add(_shipDisbandButton);
        var pickedShip = _pickedHangerShipIcons.First().Element;
        ShipOrder order;
        if (_shipDisbandButton.IsToggledIn) {
            order = new ShipOrder(ShipDirective.Disband, OrderSource.User, SelectedUnit);
        }
        else {
            order = new ShipOrder(ShipDirective.Cancel, OrderSource.User);
        }
        pickedShip.CurrentOrder = order;
        // buttons already assessed
    }

    private void HandleShipScuttleButtonClicked() {
        D.Assert(_pickedHangerShipIcons.Any());

        var pickedShips = _pickedHangerShipIcons.Select(icon => icon.Element);
        var scuttleOrder = new ShipOrder(ShipDirective.Scuttle, OrderSource.User);
        pickedShips.ForAll(e => e.CurrentOrder = scuttleOrder);
        // buttons already assessed
    }

    protected virtual void AssessHangerButtons() {
        bool isShipCreateFleetButtonEnabled = false;
        int createFleetShipCount = _pickedHangerShipIcons.Where(icon => icon.Element.Availability != NewOrderAvailability.Unavailable).Count();
        if (Utility.IsInRange(createFleetShipCount, Constants.One, TempGameValues.MaxShipsPerFleet)) {
            isShipCreateFleetButtonEnabled = true;
        }
        _shipCreateFleetButton.isEnabled = isShipCreateFleetButtonEnabled;

        if (_pickedHangerShipIcons.Count == Constants.One) {
            // 11.18.17 picked elements are limited to one to make cancel order practical to implement
            var pickedShip = _pickedHangerShipIcons.First().Element;

            bool isOrderedToRepair = pickedShip.IsCurrentOrderDirectiveAnyOf(ShipDirective.Repair);
            GameColor iconColor = isOrderedToRepair ? TempGameValues.SelectedColor : GameColor.White;
            _shipRepairButton.SetToggledState(isOrderedToRepair, iconColor);
            if (HasBeenUsedThisSession(_shipRepairButton)) {
                // 1) already used this session && if isOrderedToRepair -> button should be enabled to allow cancel
                // 2) already used this session && if !isOrderedToRepair -> cancel already used so button should allow for more pushes
                _shipRepairButton.IsEnabled = true;
            }
            else {
                // button not used this session
                if (isOrderedToRepair) {
                    // already ordered to repair from some other source prior to this session so disable button as cancel won't work
                    _shipRepairButton.IsEnabled = false;
                }
                else {
                    // button not yet used this session so let pickedShip us whether Repair is an authorized order
                    _shipRepairButton.IsEnabled = pickedShip.IsAuthorizedForNewOrder(ShipDirective.Repair);
                }
            }

            bool isOrderedToRefit = pickedShip.IsCurrentOrderDirectiveAnyOf(ShipDirective.Refit);
            iconColor = isOrderedToRefit ? TempGameValues.SelectedColor : GameColor.White;
            _shipRefitButton.SetToggledState(isOrderedToRefit, iconColor);
            if (HasBeenUsedThisSession(_shipRefitButton)) {
                _shipRefitButton.IsEnabled = true;
            }
            else {
                _shipRefitButton.IsEnabled = isOrderedToRefit ? false : pickedShip.IsAuthorizedForNewOrder(ShipDirective.Refit);
            }

            bool isOrderedToDisband = pickedShip.IsCurrentOrderDirectiveAnyOf(ShipDirective.Disband);
            iconColor = isOrderedToDisband ? TempGameValues.SelectedColor : GameColor.White;
            _shipDisbandButton.SetToggledState(isOrderedToDisband, iconColor);
            if (HasBeenUsedThisSession(_shipDisbandButton)) {
                _shipDisbandButton.IsEnabled = true;
            }
            else {
                _shipDisbandButton.IsEnabled = isOrderedToDisband ? false : pickedShip.IsAuthorizedForNewOrder(ShipDirective.Disband);
            }
        }
        else {
            ResetHangerToggleButtons();
        }

        bool isShipScuttleButtonEnabled = false;
        if (_pickedHangerShipIcons.Any()) {
            isShipScuttleButtonEnabled = _pickedHangerShipIcons.Select(icon => icon.Element).All(e => e.IsAuthorizedForNewOrder(ShipDirective.Scuttle));
        }
        _shipScuttleButton.isEnabled = isShipScuttleButtonEnabled;
    }

    protected void DisableHangerButtons() {
        _shipCreateFleetButton.isEnabled = false;
        _shipRepairButton.IsEnabled = false;
        _shipRefitButton.IsEnabled = false;
        _shipDisbandButton.IsEnabled = false;
        _shipScuttleButton.isEnabled = false;
    }

    private void PickSingleShipIcon(ShipIconGuiElement icon) {
        UnpickAllShipIcons();
        icon.IsPicked = true;
        _pickedHangerShipIcons.Add(icon);
    }

    private void UnpickAllShipIcons() {
        foreach (var icon in _pickedHangerShipIcons) {
            icon.IsPicked = false;
        }
        _pickedHangerShipIcons.Clear();
    }

    #endregion

    /// <summary>
    /// Build the collection of icons that represent the elements of the SelectedItem's composition.
    /// </summary>
    protected virtual void BuildUnitCompositionIcons() {
        RemoveUnitCompositionIcons();

        var gridContainerSize = _facilityIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerSize.x, (int)gridContainerSize.y);

        var selectedUnitElements = SelectedUnit.Elements.Where(e => !e.IsDead).Cast<FacilityItem>();
        int desiredGridCells = selectedUnitElements.Count();

        int gridColumns, unusedGridRows;
        AMultiSizeIconGuiElement.IconSize iconSize = AMultiSizeIconGuiElement.DetermineGridIconSize(gridContainerDimensions, desiredGridCells,
            _facilityIconPrefab, out unusedGridRows, out gridColumns);

        // configure grid for icon size
        IntVector2 iconDimensions = _facilityIconPrefab.GetIconDimensions(iconSize);
        _facilityIconsGrid.cellHeight = iconDimensions.y;
        _facilityIconsGrid.cellWidth = iconDimensions.x;

        // make grid gridColumns wide
        D.AssertEqual(UIGrid.Arrangement.Horizontal, _facilityIconsGrid.arrangement);
        _facilityIconsGrid.maxPerLine = gridColumns;

        foreach (var element in selectedUnitElements) {
            CreateAndAddIcon(element, iconSize);
        }

        //D.Log("{0}: FacilityIcons in sequence: {1}.", DebugName, _sortedFacilityIconTransforms.Select(t => t.name).Concatenate());
        _facilityIconsGrid.repositionNow = true;
    }

    /// <summary>
    /// Build the collection of icons that represent the elements in the hanger.
    /// </summary>
    protected virtual void BuildHangerShipIcons() {
        RemoveHangerShipIcons();

        var gridContainerSize = _hangerShipIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerSize.x, (int)gridContainerSize.y);

        var baseHanger = SelectedUnit.Hanger;
        int desiredGridCells = baseHanger.ShipCount;

        int gridColumns, unusedGridRows;
        AMultiSizeIconGuiElement.IconSize iconSize = AMultiSizeIconGuiElement.DetermineGridIconSize(gridContainerDimensions, desiredGridCells,
            _shipIconPrefab, out unusedGridRows, out gridColumns);

        // configure grid for icon size
        IntVector2 iconDimensions = _shipIconPrefab.GetIconDimensions(iconSize);
        _hangerShipIconsGrid.cellHeight = iconDimensions.y;
        _hangerShipIconsGrid.cellWidth = iconDimensions.x;

        // make grid gridColumns wide
        D.AssertEqual(UIGrid.Arrangement.Horizontal, _hangerShipIconsGrid.arrangement);
        _hangerShipIconsGrid.maxPerLine = gridColumns;

        IEnumerable<ShipItem> shipsInHanger = baseHanger.AllShips;
        foreach (var ship in shipsInHanger) {
            CreateAndAddIcon(ship, iconSize);
        }

        D.Log("{0}: Built ShipIcons in sequence: {1}.", DebugName, _sortedHangerShipIconTransforms.Select(t => t.name).Concatenate());
        _hangerShipIconsGrid.repositionNow = true;
    }

    private void CreateAndAddIcon(FacilityItem element, AMultiSizeIconGuiElement.IconSize iconSize) {
        GameObject elementIconGo = NGUITools.AddChild(_facilityIconsGrid.gameObject, _facilityIconPrefab.gameObject);
        elementIconGo.name = element.Name + ElementIconExtension;
        FacilityIconGuiElement elementIcon = elementIconGo.GetSafeComponent<FacilityIconGuiElement>();
        elementIcon.Size = iconSize;
        elementIcon.Element = element;

        UIEventListener.Get(elementIconGo).onClick += FacilityIconClickedEventHandler;
        element.deathOneShot += FacilityDeathEventHandler;
        _sortedFacilityIconTransforms.Add(elementIconGo.transform);
    }

    private void CreateAndAddIcon(ShipItem element, AMultiSizeIconGuiElement.IconSize iconSize) {
        GameObject elementIconGo = NGUITools.AddChild(_hangerShipIconsGrid.gameObject, _shipIconPrefab.gameObject);
        elementIconGo.name = element.Name + ElementIconExtension;
        ShipIconGuiElement elementIcon = elementIconGo.GetSafeComponent<ShipIconGuiElement>();
        elementIcon.Size = iconSize;
        elementIcon.Element = element;

        UIEventListener.Get(elementIconGo).onClick += HangerShipIconClickedEventHandler;
        element.deathOneShot += HangerShipDeathEventHandler;
        _sortedHangerShipIconTransforms.Add(elementIconGo.transform);
    }

    protected void RemoveUnitCompositionIcons() {
        IList<Transform> iconTransforms = _facilityIconsGrid.GetChildList();
        foreach (var it in iconTransforms) {
            var icon = it.GetComponent<FacilityIconGuiElement>();
            RemoveIcon(icon);
        }
    }

    protected void RemoveHangerShipIcons() {
        IList<Transform> iconTransforms = _hangerShipIconsGrid.GetChildList();
        foreach (var it in iconTransforms) {
            var icon = it.GetComponent<ShipIconGuiElement>();
            RemoveIcon(icon);
        }
    }

    private void RemoveIcon(FacilityIconGuiElement icon) {
        _pickedFacilityIcons.Remove(icon);   // may not be present
        if (icon.IsInitialized) {
            D.AssertNotNull(icon.Element, "{0}: {1}'s Element has been destroyed.".Inject(DebugName, icon.DebugName));

            icon.Element.deathOneShot -= FacilityDeathEventHandler;
            //D.Log("{0} has removed the icon for {1}.", DebugName, icon.Element.DebugName);
            bool isRemoved = _sortedFacilityIconTransforms.Remove(icon.transform);
            D.Assert(isRemoved);
        }
        else {
            // icon placeholder under grid will not be initialized
            //D.Log("{0} not able to remove icon {1} from collections because it is not initialized.", DebugName, icon.DebugName);
        }

        UIEventListener.Get(icon.gameObject).onClick -= FacilityIconClickedEventHandler;
        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(icon.gameObject);
    }

    private void RemoveIcon(ShipIconGuiElement icon) {
        //D.Log("{0} is about to remove and destroy {1}.", DebugName, icon.DebugName);
        _pickedHangerShipIcons.Remove(icon);   // may not be present
        if (icon.IsInitialized) {
            D.AssertNotNull(icon.Element, "{0}: {1}'s Element has been destroyed.".Inject(DebugName, icon.DebugName));

            icon.Element.deathOneShot -= HangerShipDeathEventHandler;
            //D.Log("{0} has removed the icon for {1}.", DebugName, icon.Element.DebugName);
            bool isRemoved = _sortedHangerShipIconTransforms.Remove(icon.transform);
            D.Assert(isRemoved);
        }
        else {
            // icon placeholder under grid will not be initialized
            //D.Log("{0} not able to remove icon {1} from collections because it is not initialized.", DebugName, icon.DebugName);
        }

        UIEventListener.Get(icon.gameObject).onClick -= HangerShipIconClickedEventHandler;
        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(icon.gameObject);
    }

    private void HandleDeathOf(AUnitBaseCmdItem unit) {
        D.AssertEqual(SelectedUnit, unit);
        //D.Log("{0}.HandleDeathOf({1}) called.", DebugName, unit.DebugName);
        // Death of the SelectedUnit (SelectionMgr's CurrentSelection) will no longer be selected, hiding the HUD window
        // and resetting the form. Unfortunately, the form's reset won't necessarily happen before the SelectedUnit is destroyed
        // as GuiWindows don't necessarily complete their hide right away. If there is an icon for the unit, it should be removed
        // before the unit is destroyed, but there is no icon in this HUD for the unit.
        // UNCLEAR if anything else needs to be done before the unit is destroyed and the HUD is hidden.
    }

    private void HandleDeathOf(FacilityItem unitFacility) {
        BuildUnitCompositionIcons();
    }

    private void HandleDeathOf(ShipItem hangerShip) {
        BuildHangerShipIcons();
    }

    protected abstract void AssessInteractibleHud();

    private void AssessButtons() {
        AssessUnitButtons();
        AssessUnitCompositionButtons();
        AssessHangerButtons();
    }

    private void FocusOn(ADiscernibleItem item) {
        item.IsFocus = true;
    }

    private bool HasBeenUsedThisSession(MyNguiToggleButton button) {
        return _toggleButtonsUsedThisSession.Contains(button);
    }

    private int CompareFacilityIcons(Transform aIconTransform, Transform bIconTransform) {
        int aIndex = _sortedFacilityIconTransforms.IndexOf(aIconTransform);
        int bIndex = _sortedFacilityIconTransforms.IndexOf(bIconTransform);
        return aIndex.CompareTo(bIndex);
    }

    private int CompareShipIcons(Transform aIconTransform, Transform bIconTransform) {
        int aIndex = _sortedHangerShipIconTransforms.IndexOf(aIconTransform);
        int bIndex = _sortedHangerShipIconTransforms.IndexOf(bIconTransform);
        return aIndex.CompareTo(bIndex);
    }

    protected override void ResetForReuse_Internal() {
        ResetUnitToggleButtons();

        _elementConstructionModule.ResetForReuse();
        _toggleButtonsUsedThisSession.Clear();

        RemoveUnitCompositionIcons();
        D.AssertEqual(Constants.Zero, _pickedFacilityIcons.Count, _pickedFacilityIcons.Concatenate());
        D.AssertEqual(Constants.Zero, _sortedFacilityIconTransforms.Count, _sortedFacilityIconTransforms.Concatenate());
        ResetUnitCompositionToggleButtons();

        RemoveHangerShipIcons();
        D.AssertEqual(Constants.Zero, _pickedHangerShipIcons.Count);
        D.AssertEqual(Constants.Zero, _sortedHangerShipIconTransforms.Count);
        ResetHangerToggleButtons();

        UnsubscribeFromSelectedUnit();
        _selectedUnit = null;
        _playerAiMgr = null;
        AssessInteractibleHud();
    }

    private void ResetUnitToggleButtons() {
        _unitRepairButton.SetToggledState(false);
        _unitRepairButton.IsEnabled = false;

        _unitRefitButton.SetToggledState(false);
        _unitRefitButton.IsEnabled = false;

        _unitDisbandButton.SetToggledState(false);
        _unitDisbandButton.IsEnabled = false;
    }

    private void ResetUnitCompositionToggleButtons() {
        _facilityRepairButton.SetToggledState(false);
        _facilityRepairButton.IsEnabled = false;

        _facilityRefitButton.SetToggledState(false);
        _facilityRefitButton.IsEnabled = false;

        _facilityDisbandButton.SetToggledState(false);
        _facilityDisbandButton.IsEnabled = false;
    }

    private void ResetHangerToggleButtons() {
        _shipRepairButton.SetToggledState(false);
        _shipRepairButton.IsEnabled = false;

        _shipRefitButton.SetToggledState(false);
        _shipRefitButton.IsEnabled = false;

        _shipDisbandButton.SetToggledState(false);
        _shipDisbandButton.IsEnabled = false;
    }

    private void UnsubscribeFromSelectedUnit() {
        if (_selectedUnit != null) {
            _selectedUnit.deathOneShot -= UnitDeathEventHandler;
            _selectedUnit.ConstructionMgr.constructionQueueChanged -= ConstructionQueueChangedEventHandler;
        }
    }

    private void DisconnectButtonEventHandlers() {
        EventDelegate.Remove(_unitFocusButton.onClick, UnitFocusButtonClickedEventHandler);
        EventDelegate.Remove(_unitClearOrdersButton.onClick, UnitClearOrdersButtonClickedEventHandler);
        EventDelegate.Remove(_unitScuttleButton.onClick, UnitScuttleButtonClickedEventHandler);
        _unitRepairButton.toggleStateChanged -= UnitRepairButtonToggleChangedEventHandler;
        _unitRefitButton.toggleStateChanged -= UnitRefitButtonToggleChangedEventHandler;
        _unitDisbandButton.toggleStateChanged -= UnitDisbandButtonToggleChangedEventHandler;

        _facilityRepairButton.toggleStateChanged -= FacilityRepairButtonToggleChangedEventHandler;
        _facilityRefitButton.toggleStateChanged -= FacilityRefitButtonToggleChangedEventHandler;
        _facilityDisbandButton.toggleStateChanged -= FacilityDisbandButtonToggleChangedEventHandler;
        EventDelegate.Remove(_facilityScuttleButton.onClick, FacilityScuttleButtonClickedEventHandler);

        EventDelegate.Remove(_shipCreateFleetButton.onClick, ShipCreateFleetButtonClickedEventHandler);
        _shipRepairButton.toggleStateChanged -= ShipRepairButtonToggleChangedEventHandler;
        _shipRefitButton.toggleStateChanged -= ShipRefitButtonToggleChangedEventHandler;
        _shipDisbandButton.toggleStateChanged -= ShipDisbandButtonToggleChangedEventHandler;
        EventDelegate.Remove(_shipScuttleButton.onClick, ShipScuttleButtonClickedEventHandler);
    }

    protected override void Cleanup() {
        RemoveUnitCompositionIcons();
        RemoveHangerShipIcons();
        UnsubscribeFromSelectedUnit();
        DisconnectButtonEventHandlers();
    }

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotNull(_facilityIconPrefab);
        D.AssertNotNull(_shipIconPrefab);

        D.AssertNotNull(_unitFocusButton);
        D.AssertNotNull(_unitClearOrdersButton);
        D.AssertNotNull(_unitScuttleButton);
        D.AssertNotNull(_unitRepairButton);
        D.AssertNotNull(_unitRefitButton);
        D.AssertNotNull(_unitDisbandButton);

        D.AssertNotNull(_facilityRepairButton);
        D.AssertNotNull(_facilityRefitButton);
        D.AssertNotNull(_facilityDisbandButton);
        D.AssertNotNull(_facilityScuttleButton);

        D.AssertNotNull(_shipCreateFleetButton);
        D.AssertNotNull(_shipRepairButton);
        D.AssertNotNull(_shipRefitButton);
        D.AssertNotNull(_shipDisbandButton);
        D.AssertNotNull(_shipScuttleButton);
    }

    #endregion


}

