// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitHudUserBaseForm.cs
// Abstract base form used by the UnitHudWindow to display info and allow changes when a user-owned Base is selected.
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
/// Abstract base form used by the UnitHudWindow to display info and allow changes when a user-owned Base is selected.
/// </summary>
public abstract class AUnitHudUserBaseForm : AForm {

    private const string ConstructibleDesignIconExtension = " ConstructibleDesignIcon";
    private const string ConstructionQueueIconExtension = " ConstructionQueueIcon";
    private const string ElementIconExtension = " ElementIcon";
    private const string TitleFormat = "Selected Base: {0}";

    [SerializeField]
    private UnitMemberDesignGuiIcon _constructibleDesignIconPrefab = null;
    [SerializeField]
    private ElementConstructionGuiIcon _constructionQueueIconPrefab = null;
    [SerializeField]
    private FacilityGuiIcon _facilityIconPrefab = null;
    [SerializeField]
    private ShipGuiIcon _shipIconPrefab = null;

    [SerializeField]
    private UIButton _unitFocusButton = null;
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

    [SerializeField]
    private MyNguiToggleButton _viewAllConstructibleDesignsButton = null;
    [SerializeField]
    private MyNguiToggleButton _viewFacilityConstructibleDesignsButton = null;
    [SerializeField]
    private MyNguiToggleButton _viewShipConstructibleDesignsButton = null;

    private AUnitBaseCmdItem _selectedUnit;
    public AUnitBaseCmdItem SelectedUnit {
        get { return _selectedUnit; }
        set {
            D.AssertNull(_selectedUnit);
            SetProperty<AUnitBaseCmdItem>(ref _selectedUnit, value, "SelectedUnit", SelectedUnitPropSetHandler);
        }
    }

    private ConstructionManager _unitConstructionMgr;

    private IList<Transform> _sortedConstructibleDesignIconTransforms;
    private IList<Transform> _sortedFacilityIconTransforms;
    private IList<Transform> _sortedShipIconTransforms;
    private HashSet<FacilityGuiIcon> _pickedFacilityIcons;
    private HashSet<ShipGuiIcon> _pickedShipIcons;
    private IDictionary<AUnitElementDesign, UnitMemberDesignGuiIcon> _constructibleDesignIconLookup;

    private UILabel _formTitleLabel;
    private UIGrid _constructibleDesignIconsGrid;
    private UIGrid _constructionQueueIconsGrid;
    private UIGrid _facilityIconsGrid;
    private UIGrid _shipIconsGrid;

    private GameManager _gameMgr;

    protected override void InitializeValuesAndReferences() {
        //D.Log("{0} is initializing.", DebugName);
        _gameMgr = GameManager.Instance;
        _formTitleLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>();

        _constructibleDesignIconsGrid = gameObject.GetSingleComponentInChildren<UnitMemberDesignGuiIcon>().gameObject.GetSingleComponentInParents<UIGrid>();
        _constructibleDesignIconsGrid.arrangement = UIGrid.Arrangement.Horizontal;
        _constructibleDesignIconsGrid.sorting = UIGrid.Sorting.Custom;
        _constructibleDesignIconsGrid.onCustomSort = CompareConstructableDesignIcons;

        _constructionQueueIconsGrid = gameObject.GetSingleComponentInChildren<ElementConstructionGuiIcon>().gameObject.GetSingleComponentInParents<UIGrid>();
        _constructionQueueIconsGrid.arrangement = UIGrid.Arrangement.Vertical;
        _constructionQueueIconsGrid.sorting = UIGrid.Sorting.Vertical;

        _facilityIconsGrid = gameObject.GetSingleComponentInChildren<FacilityGuiIcon>().gameObject.GetSingleComponentInParents<UIGrid>();
        _facilityIconsGrid.arrangement = UIGrid.Arrangement.Horizontal;
        _facilityIconsGrid.sorting = UIGrid.Sorting.Custom;
        _facilityIconsGrid.onCustomSort = CompareFacilityIcons;

        _shipIconsGrid = gameObject.GetSingleComponentInChildren<ShipGuiIcon>().gameObject.GetSingleComponentInParents<UIGrid>();
        _shipIconsGrid.arrangement = UIGrid.Arrangement.Horizontal;
        _shipIconsGrid.sorting = UIGrid.Sorting.Custom;
        _shipIconsGrid.onCustomSort = CompareShipIcons;

        _constructibleDesignIconLookup = new Dictionary<AUnitElementDesign, UnitMemberDesignGuiIcon>();
        _pickedFacilityIcons = new HashSet<FacilityGuiIcon>();
        _pickedShipIcons = new HashSet<ShipGuiIcon>();

        _sortedConstructibleDesignIconTransforms = new List<Transform>();
        _sortedFacilityIconTransforms = new List<Transform>();
        _sortedShipIconTransforms = new List<Transform>();

        _unitRepairButton.Initialize();
        _unitRefitButton.Initialize();
        _unitDisbandButton.Initialize();

        _facilityRepairButton.Initialize();
        _facilityRefitButton.Initialize();
        _facilityDisbandButton.Initialize();

        _shipRepairButton.Initialize();
        _shipRefitButton.Initialize();
        _shipDisbandButton.Initialize();

        _viewAllConstructibleDesignsButton.Initialize();
        _viewFacilityConstructibleDesignsButton.Initialize();
        _viewShipConstructibleDesignsButton.Initialize();

        ConnectButtonEventHandlers();
    }

    private void ConnectButtonEventHandlers() {
        EventDelegate.Add(_unitFocusButton.onClick, UnitFocusButtonClickedEventHandler);
        EventDelegate.Add(_unitScuttleButton.onClick, UnitScuttleButtonClickedEventHandler);
        _unitRepairButton.toggleStateChanged += UnitRepairButtonToggleChangedEventHandler;
        _unitRefitButton.toggleStateChanged += UnitRefitButtonToggleChangedEventHandler;
        _unitDisbandButton.toggleStateChanged += UnitDisbandButtonToggleChangedEventHandler;

        _viewAllConstructibleDesignsButton.toggleStateChanged += ViewAllConstructibleDesignIconsButtonToggleChangedEventHandler;
        _viewFacilityConstructibleDesignsButton.toggleStateChanged += ViewConstructibleFacilityIconsButtonToggleChangedEventHandler;
        _viewShipConstructibleDesignsButton.toggleStateChanged += ViewConstructibleShipIconsButtonToggleChangedEventHandler;

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

    protected override void AssignValuesToMembers() {
        _formTitleLabel.text = TitleFormat.Inject(SelectedUnit.UnitName);
        _viewAllConstructibleDesignsButton.SetToggledState(toToggleIn: true, iconColor: TempGameValues.SelectedColor, toNotify: true);

        ////var unitConstructionQueue = SelectedUnit.Data.GetConstructionQueue();
        var unitConstructionQueue = _unitConstructionMgr.GetQueue();
        BuildConstructionQueueIcons(unitConstructionQueue);

        BuildUnitCompositionIcons();
        BuildShipIcons();
        AssessButtons();
    }

    #region Event and Property Change Handlers

    private void SelectedUnitPropSetHandler() {
        D.Assert(_gameMgr.IsPaused);

        _unitConstructionMgr = SelectedUnit.ConstructionMgr;

        SubscribeToSelectedUnit();
        AssignValuesToMembers();
        AssessInteractableHud();
    }

    private void SubscribeToSelectedUnit() {
        SelectedUnit.deathOneShot += UnitDeathEventHandler;
        ////SelectedUnit.Data.constructionQueueChanged += UnitConstructionQueueChangedEventHandler;
        _unitConstructionMgr.constructionQueueChanged += UnitConstructionQueueChangedEventHandler;
    }

    private void UnitConstructionQueueChangedEventHandler(object sender, EventArgs e) {
        D.AssertEqual(_unitConstructionMgr, sender as ConstructionManager);
        HandleUnitConstructionQueueChanged();
    }

    private void UnitFocusButtonClickedEventHandler() {
        HandleUnitFocusButtonClicked();
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

    private void ViewAllConstructibleDesignIconsButtonToggleChangedEventHandler(object sender, EventArgs e) {
        //D.Log("{0}.ViewAllConstructibleDesignIconsButtonToggleChangedEventHandler called. IsToggledIn = {1}.", DebugName, _viewAllConstructibleDesignsButton.IsToggledIn);
        if (_viewAllConstructibleDesignsButton.IsToggledIn) {
            HandleViewAllConstructibleDesignsButtonToggledIn();
        }
        else {
            // User clicked button resulting in toggleOut so ignore it and keep the state toggled in
            _viewAllConstructibleDesignsButton.SetToggledState(toToggleIn: true, iconColor: TempGameValues.SelectedColor, toNotify: false);
        }
    }

    private void ViewConstructibleFacilityIconsButtonToggleChangedEventHandler(object sender, EventArgs e) {
        if (_viewFacilityConstructibleDesignsButton.IsToggledIn) {
            HandleViewFacilityConstructibleDesignsButtonToggleIn();
        }
        else {
            // User clicked button resulting in toggleOut so ignore it and keep the state toggled in
            _viewFacilityConstructibleDesignsButton.SetToggledState(toToggleIn: true, iconColor: TempGameValues.SelectedColor, toNotify: false);
        }
    }

    private void ViewConstructibleShipIconsButtonToggleChangedEventHandler(object sender, EventArgs e) {
        if (_viewShipConstructibleDesignsButton.IsToggledIn) {
            HandleViewShipConstructibleDesignsButtonToggledIn();
        }
        else {
            // User clicked button resulting in toggleOut so ignore it and keep the state toggled in
            _viewShipConstructibleDesignsButton.SetToggledState(toToggleIn: true, iconColor: TempGameValues.SelectedColor, toNotify: false);
        }
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
        HandleShipCreateFleetButtonClicked();
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

    private void ShipDeathEventHandler(object sender, EventArgs e) {
        ShipItem hangerShip = sender as ShipItem;
        HandleDeathOf(hangerShip);
    }

    private void ConstructibleDesignIconClickedEventHandler(GameObject go) {
        var inputHelper = GameInputHelper.Instance;
        UnitMemberDesignGuiIcon iconClicked = go.GetComponent<UnitMemberDesignGuiIcon>();
        if (inputHelper.IsLeftMouseButton) {
            if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftControl, KeyCode.RightControl)) {
                HandleConstructibleDesignIconCntlLeftClicked(iconClicked);
            }
            else if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftShift, KeyCode.RightShift)) {
                HandleConstructibleDesignIconShiftLeftClicked(iconClicked);
            }
            else {
                HandleConstructibleDesignIconLeftClicked(iconClicked);
            }
        }
    }

    private void ConstructionQueueIconClickedEventHandler(GameObject go) {
        var inputHelper = GameInputHelper.Instance;
        ElementConstructionGuiIcon iconClicked = go.GetComponent<ElementConstructionGuiIcon>();
        if (inputHelper.IsLeftMouseButton) {
            if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftControl, KeyCode.RightControl)) {
                HandleConstructionQueueIconCntlLeftClicked(iconClicked);
            }
            else if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftShift, KeyCode.RightShift)) {
                HandleConstructionQueueIconShiftLeftClicked(iconClicked);
            }
            else {
                HandleConstructionQueueIconLeftClicked(iconClicked);
            }
        }
    }

    private void FacilityIconClickedEventHandler(GameObject go) {
        var inputHelper = GameInputHelper.Instance;
        FacilityGuiIcon iconClicked = go.GetComponent<FacilityGuiIcon>();
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

    private void ShipIconClickedEventHandler(GameObject go) {
        var inputHelper = GameInputHelper.Instance;
        ShipGuiIcon iconClicked = go.GetComponent<ShipGuiIcon>();
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

    private void ConstructionQueueIconBuyoutInitiatedEventHandler(object sender, EventArgs e) {
        ElementConstructionGuiIcon icon = sender as ElementConstructionGuiIcon;
        HandleConstructionQueueIconBuyoutInitiated(icon);
    }

    private void ConstructionQueueIconDragDropEndedEventHandler(object sender, EventArgs e) {
        HandleConstructionQueueIconDragDropEnded();
    }

    #endregion

    #region Unit Interaction

    private void HandleUnitFocusButtonClicked() {
        FocusOn(SelectedUnit);
    }

    private void HandleUnitRepairButtonToggleChanged() {
        bool isButtonToggledIn = _unitRepairButton.IsToggledIn;
        if (isButtonToggledIn) {
            BaseOrder repairOrder = new BaseOrder(BaseDirective.Repair, OrderSource.User);
            SelectedUnit.InitiateNewOrder(repairOrder);
            //D.Log("{0} is issuing an order to {1} to Repair.", DebugName, SelectedUnit.DebugName);
        }
        else {
            BaseOrder cancelOrder = new BaseOrder(BaseDirective.Cancel, OrderSource.User);
            SelectedUnit.InitiateNewOrder(cancelOrder);
        }
        AssessUnitButtons();
    }

    private void HandleUnitRefitButtonToggleChanged() {
        D.Warn("{0}.HandleUnitRefitButtonToggleChanged not yet implemented.", DebugName);
        AssessUnitButtons();
        // UNDONE
    }

    private void HandleUnitDisbandButtonToggleChanged() {
        D.Warn("{0}.HandleUnitDisbandButtonToggleChanged not yet implemented.", DebugName);
        AssessUnitButtons();
        // UNDONE
    }

    private void HandleUnitScuttleButtonClicked() {
        // 8.15.17 Resume must occur before scuttle order so it propagates to all subscribers before initiating death
        _gameMgr.RequestPauseStateChange(toPause: false);

        var scuttleOrder = new BaseOrder(BaseDirective.Scuttle, OrderSource.User);
        SelectedUnit.InitiateNewOrder(scuttleOrder);
        // as SelectedUnit dies, there will be no CurrentSelection so game will resume and shouldn't be re-paused
    }

    private void AssessUnitButtons() {
        _unitFocusButton.isEnabled = true;

        bool isOrderedToRepair = SelectedUnit.IsCurrentOrderDirectiveAnyOf(BaseDirective.Repair);
        GameColor iconColor = isOrderedToRepair ? TempGameValues.SelectedColor : GameColor.White;
        _unitRepairButton.SetToggledState(isOrderedToRepair, iconColor);
        _unitRepairButton.IsEnabled = SelectedUnit.Data.UnitHealth < Constants.OneHundredPercent || SelectedUnit.Data.Health < Constants.OneHundredPercent;

        bool isOrderedToRefit = SelectedUnit.IsCurrentOrderDirectiveAnyOf(BaseDirective.Refit);
        iconColor = isOrderedToRefit ? TempGameValues.SelectedColor : GameColor.White;
        _unitRefitButton.SetToggledState(isOrderedToRefit, iconColor);
        _unitRefitButton.IsEnabled = true;  // IMPROVE only if upgrade is available

        bool isOrderedToDisband = SelectedUnit.IsCurrentOrderDirectiveAnyOf(BaseDirective.Disband);
        iconColor = isOrderedToDisband ? TempGameValues.SelectedColor : GameColor.White;
        _unitDisbandButton.SetToggledState(isOrderedToDisband, iconColor);
        _unitDisbandButton.IsEnabled = true;

        _unitScuttleButton.isEnabled = true;
    }

    #endregion

    #region Constructible Design Icon Interaction

    private void HandleViewAllConstructibleDesignsButtonToggledIn() {
        bool includeFacilities = ShouldFacilityDesignsBeIncludedInView();
        var constructibleDesigns = GetConstructibleDesigns(includeFacilities, includeShips: true);
        BuildConstructibleDesignIcons(constructibleDesigns);
        _viewAllConstructibleDesignsButton.SetIconColor(TempGameValues.SelectedColor);
        _viewFacilityConstructibleDesignsButton.SetToggledState(toToggleIn: false);
        _viewShipConstructibleDesignsButton.SetToggledState(toToggleIn: false);
    }

    private void HandleViewFacilityConstructibleDesignsButtonToggleIn() {
        bool includeFacilities = ShouldFacilityDesignsBeIncludedInView();
        var constructibleDesigns = GetConstructibleDesigns(includeFacilities, includeShips: false);
        BuildConstructibleDesignIcons(constructibleDesigns);
        _viewFacilityConstructibleDesignsButton.SetIconColor(TempGameValues.SelectedColor);
        _viewAllConstructibleDesignsButton.SetToggledState(toToggleIn: false);
        _viewShipConstructibleDesignsButton.SetToggledState(toToggleIn: false);
    }

    private void HandleViewShipConstructibleDesignsButtonToggledIn() {
        var constructibleDesigns = GetConstructibleDesigns(includeFacilities: false, includeShips: true);
        BuildConstructibleDesignIcons(constructibleDesigns);
        _viewShipConstructibleDesignsButton.SetIconColor(TempGameValues.SelectedColor);
        _viewAllConstructibleDesignsButton.SetToggledState(toToggleIn: false);
        _viewFacilityConstructibleDesignsButton.SetToggledState(toToggleIn: false);
    }

    private void HandleConstructibleDesignIconCntlLeftClicked(UnitMemberDesignGuiIcon iconClicked) {
        ////SelectedUnit.Data.AddToConstructionQueue(iconClicked.Design as AUnitElementDesign);
        _unitConstructionMgr.AddToQueue(iconClicked.Design as AUnitElementDesign);
    }

    private void HandleConstructibleDesignIconLeftClicked(UnitMemberDesignGuiIcon iconClicked) {
        ////SelectedUnit.Data.AddToConstructionQueue(iconClicked.Design as AUnitElementDesign);
        _unitConstructionMgr.AddToQueue(iconClicked.Design as AUnitElementDesign);
    }

    private void HandleConstructibleDesignIconShiftLeftClicked(UnitMemberDesignGuiIcon iconClicked) {
        ////SelectedUnit.Data.AddToConstructionQueue(iconClicked.Design as AUnitElementDesign);
        _unitConstructionMgr.AddToQueue(iconClicked.Design as AUnitElementDesign);
    }

    #endregion

    #region Construction Queue Icon Interaction

    private void HandleConstructionQueueIconLeftClicked(ElementConstructionGuiIcon clickedIcon) {
        // a construction icon that is clicked is already present in the queue so remove it
        ////SelectedUnit.Data.RemoveFromConstructionQueue(clickedIcon.ItemUnderConstruction);
        _unitConstructionMgr.RemoveFromQueue(clickedIcon.ItemUnderConstruction);
        // No buttons to assess
    }

    private void HandleConstructionQueueIconCntlLeftClicked(ElementConstructionGuiIcon clickedIcon) {
        // a construction icon that is clicked is already present in the queue so remove it
        ////SelectedUnit.Data.RemoveFromConstructionQueue(clickedIcon.ItemUnderConstruction);
        _unitConstructionMgr.RemoveFromQueue(clickedIcon.ItemUnderConstruction);
        // No buttons to assess
    }

    private void HandleConstructionQueueIconShiftLeftClicked(ElementConstructionGuiIcon clickedIcon) {
        // a construction icon that is clicked is already present in the queue so remove it
        ////SelectedUnit.Data.RemoveFromConstructionQueue(clickedIcon.ItemUnderConstruction);
        _unitConstructionMgr.RemoveFromQueue(clickedIcon.ItemUnderConstruction);
        // No buttons to assess
    }

    [Obsolete("No designs are currently limited to 1 instance in a base.")]
    private void AddDesignToAvailableConstructibleDesigns(AUnitElementDesign elementDesign) {
        D.Warn("{0}.AddDesignToAvailableConstructibleDesigns() not yet implemented.", DebugName);
        // TODO Add back a ConstructibleDesignIcon for this design to the grid of AvailableConstructibleDesignIcons
        // to allow the design to be picked again. Only needed when the ConstructionQueueIcon for this design
        // is clicked removing it from the queue AND only 1 instance of the design is allowed to be constructed.
        // Most designs allow multiple instances of the design to be constructed.
    }

    private void HandleConstructionQueueIconBuyoutInitiated(ElementConstructionGuiIcon boughtIcon) {
        ////SelectedUnit.Data.PurchaseQueuedConstructionItem(boughtIcon.ItemUnderConstruction);
        _unitConstructionMgr.PurchaseQueuedItem(boughtIcon.ItemUnderConstruction);
    }

    private void HandleConstructionQueueIconDragDropEnded() {
        ////IList<ConstructionTracker> unitConstructionQueue = SelectedUnit.Data.GetConstructionQueue();
        IList<ConstructionTracker> unitConstructionQueue = _unitConstructionMgr.GetQueue();

        var verticallySortedConstructionItemsInIcons = _constructionQueueIconsGrid.GetChildList()
            .Select(t => t.GetComponent<ElementConstructionGuiIcon>()).Select(icon => icon.ItemUnderConstruction);
        if (!unitConstructionQueue.SequenceEqual(verticallySortedConstructionItemsInIcons)) {
            ////SelectedUnit.Data.RegenerateConstructionQueue(verticallySortedConstructionItemsInIcons.ToList());
            _unitConstructionMgr.RegenerateQueue(verticallySortedConstructionItemsInIcons.ToList());
        }
    }

    private void HandleUnitConstructionQueueChanged() {
        ////var changedConstructionQueue = SelectedUnit.Data.GetConstructionQueue();
        var changedConstructionQueue = _unitConstructionMgr.GetQueue();
        BuildConstructionQueueIcons(changedConstructionQueue);
    }

    #endregion

    #region Unit Composition (Facility) Icon Interaction

    private void HandleFacilityIconLeftClicked(FacilityGuiIcon icon) {
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
        AssessInteractableHud();
        AssessFacilityButtons();
    }

    private void HandleFacilityIconCntlLeftClicked(FacilityGuiIcon icon) {
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
        AssessInteractableHud();
        AssessFacilityButtons();
    }

    private void HandleFacilityIconShiftLeftClicked(FacilityGuiIcon clickedIcon) {
        var iconsToPick = new List<FacilityGuiIcon>();
        int clickedIconIndex = _sortedFacilityIconTransforms.IndexOf(clickedIcon.transform);
        if (_pickedFacilityIcons.Any()) {
            FacilityGuiIcon anchorIcon = _pickedFacilityIcons.Last();   // should be in order added
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
                    FacilityGuiIcon icon = iconTransform.GetComponent<FacilityGuiIcon>();
                    if (icon != null) {
                        iconsToPick.Add(icon);
                    }
                }
            }
            else {
                for (int index = anchorIconIndex; index >= clickedIconIndex; index--) {
                    Transform iconTransform = _sortedFacilityIconTransforms[index];
                    FacilityGuiIcon icon = iconTransform.GetComponent<FacilityGuiIcon>();
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
                FacilityGuiIcon icon = iconTransform.GetComponent<FacilityGuiIcon>();
                if (icon != null) {
                    iconsToPick.Add(icon);
                }
            }
        }

        iconsToPick.ForAll(icon => {
            icon.IsPicked = true;
            _pickedFacilityIcons.Add(icon);
        });
        AssessInteractableHud();
        AssessFacilityButtons();
    }

    private void HandleFacilityIconMiddleClicked(FacilityGuiIcon icon) {
        FocusOn(icon.Element);
    }

    private void HandleFacilityRepairButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedFacilityIcons.Count);
        FacilityOrder order;
        if (_facilityRepairButton.IsToggledIn) {
            order = new FacilityOrder(FacilityDirective.Repair, OrderSource.User);
        }
        else {
            order = new FacilityOrder(FacilityDirective.Cancel, OrderSource.User);
        }
        _pickedFacilityIcons.First().Element.InitiateNewOrder(order);
        // buttons already assessed
    }

    private void HandleFacilityRefitButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedFacilityIcons.Count);
        FacilityOrder order;
        if (_facilityRefitButton.IsToggledIn) {
            order = new FacilityOrder(FacilityDirective.Refit, OrderSource.User);
        }
        else {
            order = new FacilityOrder(FacilityDirective.Cancel, OrderSource.User);
        }
        _pickedFacilityIcons.First().Element.InitiateNewOrder(order);
        // buttons already assessed
    }

    private void HandleFacilityDisbandButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedFacilityIcons.Count);
        FacilityOrder order;
        if (_facilityDisbandButton.IsToggledIn) {
            order = new FacilityOrder(FacilityDirective.Disband, OrderSource.User);
        }
        else {
            order = new FacilityOrder(FacilityDirective.Cancel, OrderSource.User);
        }
        _pickedFacilityIcons.First().Element.InitiateNewOrder(order);
        // buttons already assessed
    }

    private void HandleFacilityScuttleButtonClicked() {
        D.Assert(_pickedFacilityIcons.Any());
        // 8.15.17 Resume must occur before scuttle order so it propagates to all subscribers before initiating death
        _gameMgr.RequestPauseStateChange(toPause: false);

        var pickedElements = _pickedFacilityIcons.Select(icon => icon.Element);

        bool isSelectedUnitSlatedToDie = !SelectedUnit.Elements.Cast<FacilityItem>().Except(pickedElements).Any();

        D.Log("{0} received an OnClick event telling it to scuttle {1}.", DebugName, pickedElements.First().DebugName);
        var scuttleOrder = new FacilityOrder(FacilityDirective.Scuttle, OrderSource.User);
        pickedElements.ForAll(element => element.InitiateNewOrder(scuttleOrder));

        if (isSelectedUnitSlatedToDie) {
            // if SelectedUnit dies, there will be no CurrentSelection so game will resume and shouldn't be re-paused
            return;
        }

        _gameMgr.RequestPauseStateChange(toPause: true);
        // buttons already assessed
    }

    private void AssessFacilityButtons() {
        _facilityScuttleButton.isEnabled = _pickedFacilityIcons.Any();

        if (_pickedFacilityIcons.Count == Constants.One) {
            var pickedFacility = _pickedFacilityIcons.First().Element;

            bool isOrderedToRepair = pickedFacility.IsCurrentOrderDirectiveAnyOf(FacilityDirective.Repair);
            GameColor iconColor = isOrderedToRepair ? TempGameValues.SelectedColor : GameColor.White;
            _facilityRepairButton.SetToggledState(isOrderedToRepair, iconColor);
            _facilityRepairButton.IsEnabled = pickedFacility.Data.Health < Constants.OneHundredPercent;

            bool isOrderedToRefit = pickedFacility.IsCurrentOrderDirectiveAnyOf(FacilityDirective.Refit);
            iconColor = isOrderedToRefit ? TempGameValues.SelectedColor : GameColor.White;
            _facilityRefitButton.SetToggledState(isOrderedToRefit, iconColor);
            _facilityRefitButton.IsEnabled = true;  // IMPROVE only if upgrade is available

            bool isOrderedToDisband = pickedFacility.IsCurrentOrderDirectiveAnyOf(FacilityDirective.Disband);
            iconColor = isOrderedToDisband ? TempGameValues.SelectedColor : GameColor.White;
            _facilityDisbandButton.SetToggledState(isOrderedToDisband, iconColor);
            _facilityDisbandButton.IsEnabled = true;
        }
        else {
            ResetFacilityToggleButtons();
        }
    }

    private void PickSingleFacilityIcon(FacilityGuiIcon icon) {
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

    private void HandleShipIconLeftClicked(ShipGuiIcon icon) {
        if (icon.IsPicked) {
            D.Assert(_pickedShipIcons.Contains(icon));
            // user is unpicking this icon
            icon.IsPicked = false;
            bool isRemoved = _pickedShipIcons.Remove(icon);
            D.Assert(isRemoved);
        }
        else {
            PickSingleShipIcon(icon);
        }
        AssessInteractableHud();
        AssessShipButtons();
    }

    private void HandleShipIconCntlLeftClicked(ShipGuiIcon icon) {
        if (icon.IsPicked) {
            D.Assert(_pickedShipIcons.Contains(icon));
            // user is unpicking this icon
            icon.IsPicked = false;
            bool isRemoved = _pickedShipIcons.Remove(icon);
            D.Assert(isRemoved);
        }
        else {
            D.Assert(!_pickedShipIcons.Contains(icon));
            icon.IsPicked = true;
            _pickedShipIcons.Add(icon);
        }
        AssessInteractableHud();
        AssessShipButtons();
    }

    private void HandleShipIconShiftLeftClicked(ShipGuiIcon clickedIcon) {
        var iconsToPick = new List<ShipGuiIcon>();
        int clickedIconIndex = _sortedShipIconTransforms.IndexOf(clickedIcon.transform);
        if (_pickedShipIcons.Any()) {
            ShipGuiIcon anchorIcon = _pickedShipIcons.Last();   // should be in order added
            int anchorIconIndex = _sortedShipIconTransforms.IndexOf(anchorIcon.transform);
            if (anchorIconIndex == clickedIconIndex) {
                // clicked on the already picked anchor icon so do nothing
                return;
            }

            // pick all the icons between the anchor and the clicked icon, inclusive
            UnpickAllShipIcons();
            // clickedIcon must always be the last icon added so it becomes the next anchorIcon
            if (anchorIconIndex < clickedIconIndex) {
                for (int index = anchorIconIndex; index <= clickedIconIndex; index++) {
                    Transform iconTransform = _sortedShipIconTransforms[index];
                    ShipGuiIcon icon = iconTransform.GetComponent<ShipGuiIcon>();
                    if (icon != null) {
                        iconsToPick.Add(icon);
                    }
                }
            }
            else {
                for (int index = anchorIconIndex; index >= clickedIconIndex; index--) {
                    Transform iconTransform = _sortedShipIconTransforms[index];
                    ShipGuiIcon icon = iconTransform.GetComponent<ShipGuiIcon>();
                    if (icon != null) {
                        iconsToPick.Add(icon);
                    }
                }
            }
        }
        else {
            // pick all icons from the first to this one
            for (int index = 0; index <= clickedIconIndex; index++) {
                Transform iconTransform = _sortedShipIconTransforms[index];
                ShipGuiIcon icon = iconTransform.GetComponent<ShipGuiIcon>();
                if (icon != null) {
                    iconsToPick.Add(icon);
                }
            }
        }

        iconsToPick.ForAll(icon => {
            icon.IsPicked = true;
            _pickedShipIcons.Add(icon);
        });
        AssessInteractableHud();
        AssessShipButtons();
    }

    private void HandleShipCreateFleetButtonClicked() {
        D.Warn("{0}.HandleShipCreateFleetButtonClicked() not yet implemented.", DebugName);
        // UNCLEAR use UnitHudUserFleetForm approach to creating fleet? Should created fleet show up as a unit in orbit,
        // in a UnitIcons module? // Should all fleets in orbit around base show up in a UnitIcons module?
    }

    private void HandleShipRepairButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedShipIcons.Count);
        ShipOrder order;
        if (_shipRepairButton.IsToggledIn) {
            order = new ShipOrder(ShipDirective.Repair, OrderSource.User);
        }
        else {
            order = new ShipOrder(ShipDirective.Cancel, OrderSource.User);
        }
        _pickedShipIcons.First().Element.InitiateNewOrder(order);
        // buttons already assessed
    }

    private void HandleShipRefitButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedShipIcons.Count);
        ShipOrder order;
        if (_shipRefitButton.IsToggledIn) {
            order = new ShipOrder(ShipDirective.Refit, OrderSource.User);
        }
        else {
            order = new ShipOrder(ShipDirective.Cancel, OrderSource.User);
        }
        _pickedShipIcons.First().Element.InitiateNewOrder(order);
        // buttons already assessed
    }

    private void HandleShipDisbandButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedShipIcons.Count);
        ShipOrder order;
        if (_shipDisbandButton.IsToggledIn) {
            order = new ShipOrder(ShipDirective.Disband, OrderSource.User);
        }
        else {
            order = new ShipOrder(ShipDirective.Cancel, OrderSource.User);
        }
        _pickedShipIcons.First().Element.InitiateNewOrder(order);
        // buttons already assessed
    }

    private void HandleShipScuttleButtonClicked() {
        D.Assert(_pickedShipIcons.Any());
        // 8.15.17 Resume must occur before scuttle order so it propagates to all subscribers before initiating death
        _gameMgr.RequestPauseStateChange(toPause: false);

        var pickedShips = _pickedShipIcons.Select(icon => icon.Element);

        bool isSelectedUnitSlatedToDie = !SelectedUnit.Elements.Cast<ShipItem>().Except(pickedShips).Any();

        var scuttleOrder = new ShipOrder(ShipDirective.Scuttle, OrderSource.User);
        pickedShips.ForAll(element => element.InitiateNewOrder(scuttleOrder));

        if (isSelectedUnitSlatedToDie) {
            // if SelectedUnit dies, there will be no CurrentSelection so game will resume and shouldn't be re-paused
            return;
        }

        _gameMgr.RequestPauseStateChange(toPause: true);
        // buttons already assessed
    }

    private void AssessShipButtons() {
        bool isShipCreateFleetButtonEnabled = false;
        int pickedShipCount = _pickedShipIcons.Count;
        if (Utility.IsInRange(pickedShipCount, Constants.One, TempGameValues.MaxShipsPerFleet)) {
            isShipCreateFleetButtonEnabled = true;
        }
        _shipCreateFleetButton.isEnabled = isShipCreateFleetButtonEnabled;

        if (_pickedShipIcons.Count == Constants.One) {
            var pickedShip = _pickedShipIcons.First().Element;

            bool isOrderedToRepair = pickedShip.IsCurrentOrderDirectiveAnyOf(ShipDirective.Repair);
            GameColor iconColor = isOrderedToRepair ? TempGameValues.SelectedColor : GameColor.White;
            _shipRepairButton.SetToggledState(isOrderedToRepair, iconColor);
            _shipRepairButton.IsEnabled = pickedShip.Data.Health < Constants.OneHundredPercent;

            bool isOrderedToRefit = pickedShip.IsCurrentOrderDirectiveAnyOf(ShipDirective.Refit);
            iconColor = isOrderedToRefit ? TempGameValues.SelectedColor : GameColor.White;
            _shipRefitButton.SetToggledState(isOrderedToRefit, iconColor);
            _shipRefitButton.IsEnabled = true;  // IMPROVE only if upgrade is available

            bool isOrderedToDisband = pickedShip.IsCurrentOrderDirectiveAnyOf(ShipDirective.Disband);
            iconColor = isOrderedToDisband ? TempGameValues.SelectedColor : GameColor.White;
            _shipDisbandButton.SetToggledState(isOrderedToDisband, iconColor);
            _shipDisbandButton.IsEnabled = true;
        }
        else {
            ResetShipToggleButtons();
        }

        _shipScuttleButton.isEnabled = _pickedShipIcons.Any();
    }

    private void PickSingleShipIcon(ShipGuiIcon icon) {
        UnpickAllShipIcons();
        icon.IsPicked = true;
        _pickedShipIcons.Add(icon);
    }

    private void UnpickAllShipIcons() {
        foreach (var icon in _pickedShipIcons) {
            icon.IsPicked = false;
        }
        _pickedShipIcons.Clear();
    }

    #endregion

    private void BuildConstructibleDesignIcons(IEnumerable<AUnitElementDesign> constructibleDesigns) {
        RemoveConstructibleDesignIcons();

        var gridContainerSize = _constructibleDesignIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerSize.x, (int)gridContainerSize.y);

        int desiredGridCells = constructibleDesigns.Count();

        int gridColumns, unusedGridRows;
        AMultiSizeGuiIcon.IconSize iconSize = AMultiSizeGuiIcon.DetermineGridIconSize(gridContainerDimensions, desiredGridCells, _constructibleDesignIconPrefab,
            out unusedGridRows, out gridColumns);

        // configure grid for icon size
        IntVector2 iconDimensions = _constructibleDesignIconPrefab.GetIconDimensions(iconSize);
        _constructibleDesignIconsGrid.cellHeight = iconDimensions.y;
        _constructibleDesignIconsGrid.cellWidth = iconDimensions.x;

        // make grid gridColumns wide
        D.AssertEqual(UIGrid.Arrangement.Horizontal, _constructibleDesignIconsGrid.arrangement);
        _constructibleDesignIconsGrid.maxPerLine = gridColumns;

        foreach (var design in constructibleDesigns) {
            CreateAndAddIcon(design, iconSize);
        }
        _constructibleDesignIconsGrid.repositionNow = true;
    }

    private void BuildConstructionQueueIcons(IEnumerable<ConstructionTracker> constructionQueue) {
        RemoveConstructionQueueIcons();

        var gridContainerSize = _constructionQueueIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerSize.x, (int)gridContainerSize.y);

        int desiredGridCells = constructionQueue.Count();

        int unusedGridColumns, unusedGridRows;
        AMultiSizeGuiIcon.IconSize iconSize = AMultiSizeGuiIcon.DetermineGridIconSize(gridContainerDimensions, desiredGridCells, _constructionQueueIconPrefab, out unusedGridRows, out unusedGridColumns);

        // configure grid for icon size
        IntVector2 iconDimensions = _constructionQueueIconPrefab.GetIconDimensions(iconSize);
        _constructionQueueIconsGrid.cellHeight = iconDimensions.y;
        _constructionQueueIconsGrid.cellWidth = iconDimensions.x;

        // make grid 1 column wide
        D.AssertEqual(UIGrid.Arrangement.Vertical, _constructionQueueIconsGrid.arrangement);
        _constructionQueueIconsGrid.maxPerLine = Constants.Zero; // one column, infinite rows

        int topToBottomVerticalOffset = Constants.Zero;
        foreach (var constructionItem in constructionQueue) {
            CreateAndAddQueueIcon(constructionItem, iconSize, topToBottomVerticalOffset);
            topToBottomVerticalOffset--;
        }
        _constructionQueueIconsGrid.repositionNow = true;
    }

    /// <summary>
    /// Build the collection of icons that represent the elements of the SelectedItem's composition.
    /// </summary>
    private void BuildUnitCompositionIcons() {
        RemoveUnitCompositionIcons();

        var gridContainerSize = _facilityIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerSize.x, (int)gridContainerSize.y);

        var selectedUnitElements = SelectedUnit.Elements.Where(e => e.IsOperational).Cast<FacilityItem>();
        int desiredGridCells = selectedUnitElements.Count();

        int gridColumns, unusedGridRows;
        AMultiSizeGuiIcon.IconSize iconSize = AMultiSizeGuiIcon.DetermineGridIconSize(gridContainerDimensions, desiredGridCells, _facilityIconPrefab,
            out unusedGridRows, out gridColumns);

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
    private void BuildShipIcons() {
        RemoveShipIcons();

        var gridContainerSize = _shipIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerSize.x, (int)gridContainerSize.y);

        IEnumerable<ShipItem> hangerElements = __AcquireShipsInHanger();
        int desiredGridCells = hangerElements.Count();

        int gridColumns, unusedGridRows;
        AMultiSizeGuiIcon.IconSize iconSize = AMultiSizeGuiIcon.DetermineGridIconSize(gridContainerDimensions, desiredGridCells, _shipIconPrefab,
            out unusedGridRows, out gridColumns);

        // configure grid for icon size
        IntVector2 iconDimensions = _shipIconPrefab.GetIconDimensions(iconSize);
        _shipIconsGrid.cellHeight = iconDimensions.y;
        _shipIconsGrid.cellWidth = iconDimensions.x;

        // make grid gridColumns wide
        D.AssertEqual(UIGrid.Arrangement.Horizontal, _shipIconsGrid.arrangement);
        _shipIconsGrid.maxPerLine = gridColumns;

        foreach (var element in hangerElements) {
            CreateAndAddIcon(element, iconSize);
        }

        //D.Log("{0}: ShipIcons in sequence: {1}.", DebugName, _sortedShipIconTransforms.Select(t => t.name).Concatenate());
        _shipIconsGrid.repositionNow = true;
    }

    private void CreateAndAddIcon(AUnitElementDesign design, AMultiSizeGuiIcon.IconSize iconSize) {
        GameObject constructableDesignIconGo = NGUITools.AddChild(_constructibleDesignIconsGrid.gameObject, _constructibleDesignIconPrefab.gameObject);
        constructableDesignIconGo.name = design.DesignName + ConstructibleDesignIconExtension;
        UnitMemberDesignGuiIcon constructableDesignIcon = constructableDesignIconGo.GetSafeComponent<UnitMemberDesignGuiIcon>();
        constructableDesignIcon.Size = iconSize;
        constructableDesignIcon.Design = design;

        UIEventListener.Get(constructableDesignIconGo).onClick += ConstructibleDesignIconClickedEventHandler;
        _constructibleDesignIconLookup.Add(design, constructableDesignIcon);
        _sortedConstructibleDesignIconTransforms.Add(constructableDesignIconGo.transform);
    }

    /// <summary>
    /// Creates and adds a ElementConstructionGuiIcon to the ConstructionQueueIconsGrid and ScrollView.
    /// </summary>
    /// <param name="itemUnderConstruction">The item under construction.</param>
    /// <param name="iconSize">Size of the icon.</param>
    /// <param name="topToBottomVerticalOffset">The top to bottom vertical offset needed to vertically sort.</param>
    private void CreateAndAddQueueIcon(ConstructionTracker itemUnderConstruction, AMultiSizeGuiIcon.IconSize iconSize, int topToBottomVerticalOffset) {
        GameObject constructionQueueIconGo = NGUITools.AddChild(_constructionQueueIconsGrid.gameObject, _constructionQueueIconPrefab.gameObject);
        constructionQueueIconGo.name = itemUnderConstruction.Design.DesignName + ConstructionQueueIconExtension;
        constructionQueueIconGo.transform.SetLocalPositionY(topToBottomVerticalOffset); // initial position set for proper vertical sort

        ElementConstructionGuiIcon constructionQueueIcon = constructionQueueIconGo.GetSafeComponent<ElementConstructionGuiIcon>();
        constructionQueueIcon.Size = iconSize;
        constructionQueueIcon.ItemUnderConstruction = itemUnderConstruction;

        constructionQueueIcon.constructionBuyoutInitiated += ConstructionQueueIconBuyoutInitiatedEventHandler;
        constructionQueueIcon.dragDropEnded += ConstructionQueueIconDragDropEndedEventHandler;
        UIEventListener.Get(constructionQueueIconGo).onClick += ConstructionQueueIconClickedEventHandler;
    }

    private void CreateAndAddIcon(FacilityItem element, AMultiSizeGuiIcon.IconSize iconSize) {
        GameObject elementIconGo = NGUITools.AddChild(_facilityIconsGrid.gameObject, _facilityIconPrefab.gameObject);
        elementIconGo.name = element.Name + ElementIconExtension;
        FacilityGuiIcon elementIcon = elementIconGo.GetSafeComponent<FacilityGuiIcon>();
        elementIcon.Size = iconSize;
        elementIcon.Element = element;

        UIEventListener.Get(elementIconGo).onClick += FacilityIconClickedEventHandler;
        element.deathOneShot += FacilityDeathEventHandler;
        _sortedFacilityIconTransforms.Add(elementIconGo.transform);
    }

    private void CreateAndAddIcon(ShipItem element, AMultiSizeGuiIcon.IconSize iconSize) {
        GameObject elementIconGo = NGUITools.AddChild(_shipIconsGrid.gameObject, _shipIconPrefab.gameObject);
        elementIconGo.name = element.Name + ElementIconExtension;
        ShipGuiIcon elementIcon = elementIconGo.GetSafeComponent<ShipGuiIcon>();
        elementIcon.Size = iconSize;
        elementIcon.Element = element;

        UIEventListener.Get(elementIconGo).onClick += ShipIconClickedEventHandler;
        element.deathOneShot += ShipDeathEventHandler;
        _sortedShipIconTransforms.Add(elementIconGo.transform);
    }

    private void RemoveConstructibleDesignIcons() {
        IList<Transform> iconTransforms = _constructibleDesignIconsGrid.GetChildList();
        foreach (var it in iconTransforms) {
            var icon = it.GetComponent<UnitMemberDesignGuiIcon>();
            RemoveIcon(icon);
        }
    }

    private void RemoveConstructionQueueIcons() {
        IList<Transform> iconTransforms = _constructionQueueIconsGrid.GetChildList();
        foreach (var it in iconTransforms) {
            var icon = it.GetComponent<ElementConstructionGuiIcon>();
            RemoveIcon(icon);
        }
    }

    private void RemoveUnitCompositionIcons() {
        IList<Transform> iconTransforms = _facilityIconsGrid.GetChildList();
        foreach (var it in iconTransforms) {
            var icon = it.GetComponent<FacilityGuiIcon>();
            RemoveIcon(icon);
        }
    }

    private void RemoveShipIcons() {
        IList<Transform> iconTransforms = _shipIconsGrid.GetChildList();
        foreach (var it in iconTransforms) {
            var icon = it.GetComponent<ShipGuiIcon>();
            RemoveIcon(icon);
        }
    }

    private void RemoveIcon(UnitMemberDesignGuiIcon icon) {
        if (icon.Design != null) {  // no reason for this work if this is debug icon under grid
            bool isRemoved = _sortedConstructibleDesignIconTransforms.Remove(icon.transform);
            D.Assert(isRemoved);
            isRemoved = _constructibleDesignIconLookup.Remove(icon.Design as AUnitElementDesign);
            D.Assert(isRemoved);
            //D.Log("{0} is removing {1}.", DebugName, icon.DebugName);
        }
        else {
            // icon placeholder under grid will not have Design assigned
            //D.Log("{0} not able to remove icon {1} from collections because it is not initialized.", DebugName, icon.DebugName);
        }

        UIEventListener.Get(icon.gameObject).onClick -= ConstructibleDesignIconClickedEventHandler;
        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(icon.gameObject);
    }

    private void RemoveIcon(ElementConstructionGuiIcon icon) {
        if (icon.ItemUnderConstruction != null) {   // no reason for this work if this is debug icon under grid
            icon.constructionBuyoutInitiated -= ConstructionQueueIconBuyoutInitiatedEventHandler;
            icon.dragDropEnded -= ConstructionQueueIconDragDropEndedEventHandler;
            UIEventListener.Get(icon.gameObject).onClick -= ConstructionQueueIconClickedEventHandler;
        }
        //D.Log("{0} is removing {1}.", DebugName, icon.DebugName);
        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(icon.gameObject);
    }

    private void RemoveIcon(FacilityGuiIcon icon) {
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

    private void RemoveIcon(ShipGuiIcon icon) {
        _pickedShipIcons.Remove(icon);   // may not be present
        if (icon.IsInitialized) {
            D.AssertNotNull(icon.Element, "{0}: {1}'s Element has been destroyed.".Inject(DebugName, icon.DebugName));

            icon.Element.deathOneShot -= FacilityDeathEventHandler;
            //D.Log("{0} has removed the icon for {1}.", DebugName, icon.Element.DebugName);
            bool isRemoved = _sortedShipIconTransforms.Remove(icon.transform);
            D.Assert(isRemoved);
        }
        else {
            // icon placeholder under grid will not be initialized
            //D.Log("{0} not able to remove icon {1} from collections because it is not initialized.", DebugName, icon.DebugName);
        }

        UIEventListener.Get(icon.gameObject).onClick -= ShipIconClickedEventHandler;
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
        D.Warn("{0}.HandleDeathOf({1}) not yet implemented.", DebugName, hangerShip.DebugName);
        // UNDONE
        BuildShipIcons();
    }

    private bool ShouldFacilityDesignsBeIncludedInView() {
        int instantiatedFacilityCount = SelectedUnit.Elements.Count();
        ////int facilitiesUnderConstructionCount = SelectedUnit.Data.GetConstructionQueue().Select(tracker => tracker.Design).Where(design => design is FacilityDesign).Count();
        int facilitiesUnderConstructionCount = _unitConstructionMgr.GetQueue().Select(tracker => tracker.Design).Where(design => design is FacilityDesign).Count();
        return instantiatedFacilityCount + facilitiesUnderConstructionCount < TempGameValues.MaxFacilitiesPerBase;
    }

    private IEnumerable<AUnitElementDesign> GetConstructibleDesigns(bool includeFacilities, bool includeShips) {
        // 9.24.17 Both can be false if ViewConstructibleFacilities and Base already has max facilities
        ////D.Assert(includeFacilities || includeShips);

        var playersDesigns = _gameMgr.PlayersDesigns;
        List<AUnitElementDesign> designs = new List<AUnitElementDesign>();
        if (includeFacilities) {
            var facilityDesigns = playersDesigns.GetAllUserFacilityDesigns().Cast<AUnitElementDesign>();
            designs.AddRange(facilityDesigns);
        }
        if (includeShips) {
            var shipDesigns = playersDesigns.GetAllUserShipDesigns().Cast<AUnitElementDesign>();
            designs.AddRange(shipDesigns);
        }
        return designs;
    }

    private void AssessInteractableHud() {
        if (_pickedFacilityIcons.Count == Constants.One) {
            InteractableHudWindow.Instance.Show(FormID.UserFacility, _pickedFacilityIcons.First().Element.Data);
        }
        else if (SelectedUnit != null) {    // 9.14.17 if SelectedUnit has been destroyed, reference will test as null
            InteractableHudWindow.Instance.Show(FormID, SelectedUnit.Data);
        }
        else {
            InteractableHudWindow.Instance.Hide();
        }
    }

    private void AssessButtons() {
        AssessUnitButtons();
        AssessFacilityButtons();
        AssessShipButtons();
    }

    private void FocusOn(ADiscernibleItem item) {
        item.IsFocus = true;
    }

    private int CompareFacilityIcons(Transform aIconTransform, Transform bIconTransform) {
        int aIndex = _sortedFacilityIconTransforms.IndexOf(aIconTransform);
        int bIndex = _sortedFacilityIconTransforms.IndexOf(bIconTransform);
        return aIndex.CompareTo(bIndex);
    }

    private int CompareShipIcons(Transform aIconTransform, Transform bIconTransform) {
        int aIndex = _sortedShipIconTransforms.IndexOf(aIconTransform);
        int bIndex = _sortedShipIconTransforms.IndexOf(bIconTransform);
        return aIndex.CompareTo(bIndex);
    }

    private int CompareConstructableDesignIcons(Transform aIconTransform, Transform bIconTransform) {
        int aIndex = _sortedConstructibleDesignIconTransforms.IndexOf(aIconTransform);
        int bIndex = _sortedConstructibleDesignIconTransforms.IndexOf(bIconTransform);
        return aIndex.CompareTo(bIndex);
    }

    protected override void ResetForReuse_Internal() {
        ResetUnitToggleButtons();

        RemoveConstructibleDesignIcons();
        D.AssertEqual(Constants.Zero, _sortedConstructibleDesignIconTransforms.Count);
        D.AssertEqual(Constants.Zero, _constructibleDesignIconLookup.Count);

        ResetViewConstructibleDesignsToggleButtons();

        RemoveConstructionQueueIcons();

        RemoveUnitCompositionIcons();
        D.AssertEqual(Constants.Zero, _pickedFacilityIcons.Count, _pickedFacilityIcons.Concatenate());
        D.AssertEqual(Constants.Zero, _sortedFacilityIconTransforms.Count, _sortedFacilityIconTransforms.Concatenate());
        ResetFacilityToggleButtons();

        RemoveShipIcons();
        D.AssertEqual(Constants.Zero, _pickedShipIcons.Count);
        D.AssertEqual(Constants.Zero, _sortedShipIconTransforms.Count);
        ResetShipToggleButtons();

        UnsubscribeFromSelectedUnit();
        _selectedUnit = null;
        _unitConstructionMgr = null;
        AssessInteractableHud();
    }

    private void ResetUnitToggleButtons() {
        _unitRepairButton.SetToggledState(false);
        _unitRepairButton.IsEnabled = false;

        _unitRefitButton.SetToggledState(false);
        _unitRefitButton.IsEnabled = false;

        _unitDisbandButton.SetToggledState(false);
        _unitDisbandButton.IsEnabled = false;
    }

    private void ResetViewConstructibleDesignsToggleButtons() {
        _viewAllConstructibleDesignsButton.SetToggledState(toToggleIn: false);
        _viewFacilityConstructibleDesignsButton.SetToggledState(toToggleIn: false);
        _viewShipConstructibleDesignsButton.SetToggledState(toToggleIn: false);
    }

    private void ResetFacilityToggleButtons() {
        _facilityRepairButton.SetToggledState(false);
        _facilityRepairButton.IsEnabled = false;

        _facilityRefitButton.SetToggledState(false);
        _facilityRefitButton.IsEnabled = false;

        _facilityDisbandButton.SetToggledState(false);
        _facilityDisbandButton.IsEnabled = false;
    }

    private void ResetShipToggleButtons() {
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
            ////_selectedUnit.Data.constructionQueueChanged -= UnitConstructionQueueChangedEventHandler;
            _unitConstructionMgr.constructionQueueChanged -= UnitConstructionQueueChangedEventHandler;
        }
    }

    private void DisconnectButtonEventHandlers() {
        EventDelegate.Remove(_unitFocusButton.onClick, UnitFocusButtonClickedEventHandler);
        _unitRepairButton.toggleStateChanged -= UnitRepairButtonToggleChangedEventHandler;
        _unitRefitButton.toggleStateChanged -= UnitRefitButtonToggleChangedEventHandler;
        _unitDisbandButton.toggleStateChanged -= UnitDisbandButtonToggleChangedEventHandler;
        EventDelegate.Remove(_unitScuttleButton.onClick, UnitScuttleButtonClickedEventHandler);

        _viewAllConstructibleDesignsButton.toggleStateChanged -= ViewAllConstructibleDesignIconsButtonToggleChangedEventHandler;
        _viewFacilityConstructibleDesignsButton.toggleStateChanged -= ViewConstructibleFacilityIconsButtonToggleChangedEventHandler;
        _viewShipConstructibleDesignsButton.toggleStateChanged -= ViewConstructibleFacilityIconsButtonToggleChangedEventHandler;

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
        RemoveConstructibleDesignIcons();
        RemoveConstructionQueueIcons();
        RemoveUnitCompositionIcons();
        RemoveShipIcons();
        UnsubscribeFromSelectedUnit();
        DisconnectButtonEventHandlers();
    }

    #region Debug

    private IEnumerable<ShipItem> __AcquireShipsInHanger() {
        return Enumerable.Empty<ShipItem>();
    }

    protected override void __Validate() {
        base.__Validate();
        D.AssertNotNull(_constructibleDesignIconPrefab);
        D.AssertNotNull(_constructionQueueIconPrefab);
        D.AssertNotNull(_facilityIconPrefab);
        D.AssertNotNull(_shipIconPrefab);

        D.AssertNotNull(_unitFocusButton);
        D.AssertNotNull(_unitRepairButton);
        D.AssertNotNull(_unitRefitButton);
        D.AssertNotNull(_unitDisbandButton);
        D.AssertNotNull(_unitScuttleButton);

        D.AssertNotNull(_viewAllConstructibleDesignsButton);
        D.AssertNotNull(_viewFacilityConstructibleDesignsButton);
        D.AssertNotNull(_viewShipConstructibleDesignsButton);

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

