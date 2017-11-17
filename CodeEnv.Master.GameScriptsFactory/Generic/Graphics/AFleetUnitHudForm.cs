// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFleetUnitHudForm.cs
// Abstract base class for Fleet Forms used by the UnitHud.
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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Fleet Forms used by the UnitHud.
/// <remarks>Handles both User and AI-owned fleets.</remarks>
/// </summary>
public abstract class AFleetUnitHudForm : AForm {

    private const string UnitIconExtension = " UnitIcon";
    private const string ElementIconExtension = " ElementIcon";
    private const string TitleFormat = "Selected Fleet: {0}";

    [SerializeField]
    private FleetIconGuiElement _unitIconPrefab = null;
    [SerializeField]
    private ShipIconGuiElement _elementIconPrefab = null;

    [SerializeField]
    private UIButton _unitFocusButton = null;
    [SerializeField]
    private UIButton _unitMergeButton = null;
    [SerializeField]
    private UIButton _unitScuttleButton = null;
    [SerializeField]
    private MyNguiToggleButton _unitGuardButton = null;
    [SerializeField]
    private MyNguiToggleButton _unitPatrolButton = null;
    [SerializeField]
    private MyNguiToggleButton _unitRepairButton = null;
    [SerializeField]
    private MyNguiToggleButton _unitRefitButton = null;
    [SerializeField]
    private MyNguiToggleButton _unitDisbandButton = null;
    [SerializeField]
    private MyNguiToggleButton _unitExploreButton = null;

    [SerializeField]
    private UIButton _shipCreateFleetButton = null;
    [SerializeField]
    private UIButton _shipScuttleButton = null;

    private FleetCmdItem _selectedUnit;
    public FleetCmdItem SelectedUnit {
        get { return _selectedUnit; }
        set {
            D.AssertNull(_selectedUnit);
            SetProperty<FleetCmdItem>(ref _selectedUnit, value, "SelectedUnit");
        }
    }

    protected HashSet<FleetIconGuiElement> _pickedUnitIcons;
    protected HashSet<ShipIconGuiElement> _pickedElementIcons;
    protected GameManager _gameMgr;

    private HashSet<MyNguiToggleButton> _toggleButtonsUsedThisSession;
    private IList<Transform> _sortedUnitIconTransforms;
    private IList<Transform> _sortedElementIconTransforms;
    /// <summary>
    /// The unit icons that are showing keyed by their unit. 
    /// The icons of all the units that populate this form will always be showing.
    /// </summary>
    private IDictionary<FleetCmdItem, FleetIconGuiElement> _unitIconLookup;
    private UILabel _formTitleLabel;
    private UIGrid _unitIconsGrid;
    private UIGrid _elementIconsGrid;

    protected override void InitializeValuesAndReferences() {
        //D.Log("{0} is initializing.", DebugName);
        _gameMgr = GameManager.Instance;
        _formTitleLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>();

        _unitIconsGrid = gameObject.GetSingleComponentInChildren<FleetIconGuiElement>().gameObject.GetSingleComponentInParents<UIGrid>();
        _unitIconsGrid.arrangement = UIGrid.Arrangement.Vertical;
        _unitIconsGrid.sorting = UIGrid.Sorting.Alphabetic;

        _elementIconsGrid = gameObject.GetSingleComponentInChildren<ShipIconGuiElement>().gameObject.GetSingleComponentInParents<UIGrid>();
        _elementIconsGrid.arrangement = UIGrid.Arrangement.Horizontal;
        _elementIconsGrid.sorting = UIGrid.Sorting.Custom;
        _elementIconsGrid.onCustomSort = CompareElementIcons;

        _unitIconLookup = new Dictionary<FleetCmdItem, FleetIconGuiElement>();
        _pickedUnitIcons = new HashSet<FleetIconGuiElement>();
        _pickedElementIcons = new HashSet<ShipIconGuiElement>();
        _sortedUnitIconTransforms = new List<Transform>();
        _sortedElementIconTransforms = new List<Transform>();
        _toggleButtonsUsedThisSession = new HashSet<MyNguiToggleButton>();

        _unitGuardButton.Initialize();
        _unitPatrolButton.Initialize();
        _unitRepairButton.Initialize();
        _unitRefitButton.Initialize();
        _unitDisbandButton.Initialize();
        _unitExploreButton.Initialize();

        ConnectButtonEventHandlers();
    }

    private void ConnectButtonEventHandlers() {
        EventDelegate.Add(_unitFocusButton.onClick, UnitFocusButtonClickedEventHandler);
        EventDelegate.Add(_unitMergeButton.onClick, UnitMergeButtonClickedEventHandler);
        EventDelegate.Add(_unitScuttleButton.onClick, UnitScuttleButtonClickedEventHandler);
        _unitGuardButton.toggleStateChanged += UnitGuardButtonToggleChangedEventHandler;
        _unitPatrolButton.toggleStateChanged += UnitPatrolButtonToggleChangedEventHandler;
        _unitRepairButton.toggleStateChanged += UnitRepairButtonToggleChangedEventHandler;
        _unitRefitButton.toggleStateChanged += UnitRefitButtonToggleChangedEventHandler;
        _unitDisbandButton.toggleStateChanged += UnitDisbandButtonToggleChangedEventHandler;
        _unitExploreButton.toggleStateChanged += UnitExploreButtonToggleChangedEventHandler;

        EventDelegate.Add(_shipCreateFleetButton.onClick, ShipCreateFleetButtonClickedEventHandler);
        EventDelegate.Add(_shipScuttleButton.onClick, ShipScuttleButtonClickedEventHandler);
    }

    public sealed override void PopulateValues() {
        D.Assert(_gameMgr.IsPaused);
        D.AssertNotNull(SelectedUnit);  // UNCLEAR call when Unit already destroyed?
        AssignValuesToMembers();
    }

    protected override void AssignValuesToMembers() {
        _formTitleLabel.text = TitleFormat.Inject(SelectedUnit.UnitName);
        IList<FleetCmdItem> units = new List<FleetCmdItem>(__AcquireLocalUnits());
        units.Add(SelectedUnit);

        // Only the selected Unit should initially show as picked
        BuildUnitIcons(units, SelectedUnit);
        AssessButtons();
    }

    #region Event and Property Change Handlers

    private void UnitGuardButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleUnitGuardButtonToggleChanged();
    }

    private void UnitPatrolButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleUnitPatrolButtonToggleChanged();
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

    private void UnitExploreButtonToggleChangedEventHandler(object sender, EventArgs e) {
        HandleUnitExploreButtonToggleChanged();
    }

    private void UnitFocusButtonClickedEventHandler() {
        HandleUnitFocusButtonClicked();
    }

    private void UnitMergeButtonClickedEventHandler() {
        HandleUnitMergeButtonClicked();
    }

    private void UnitScuttleButtonClickedEventHandler() {
        HandleUnitScuttleButtonClicked();
    }

    private void ShipCreateFleetButtonClickedEventHandler() {
        HandleShipCreateFleetButtonClicked();
    }

    private void ShipScuttleButtonClickedEventHandler() {
        HandleShipScuttleButtonClicked();
    }

    private void UnitDeathEventHandler(object sender, EventArgs e) {
        FleetCmdItem unit = sender as FleetCmdItem;
        HandleDeathOf(unit);
    }

    private void ElementDeathEventHandler(object sender, EventArgs e) {
        ShipItem element = sender as ShipItem;
        HandleDeathOf(element);
    }

    private void UnitIconClickedEventHandler(GameObject go) {
        var inputHelper = GameInputHelper.Instance;
        FleetIconGuiElement iconClicked = go.GetComponent<FleetIconGuiElement>();
        if (inputHelper.IsLeftMouseButton) {
            if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftControl, KeyCode.RightControl)) {
                HandleUnitIconCntlLeftClicked(iconClicked);
            }
            else if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftShift, KeyCode.RightShift)) {
                HandleUnitIconShiftLeftClicked(iconClicked);
            }
            else {
                HandleUnitIconLeftClicked(iconClicked);
            }
        }
        else if (inputHelper.IsMiddleMouseButton) {
            HandleUnitIconMiddleClicked(iconClicked);
        }
    }

    private void ElementIconClickedEventHandler(GameObject go) {
        var inputHelper = GameInputHelper.Instance;
        ShipIconGuiElement iconClicked = go.GetComponent<ShipIconGuiElement>();
        if (inputHelper.IsLeftMouseButton) {
            if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftControl, KeyCode.RightControl)) {
                HandleElementIconCntlLeftClicked(iconClicked);
            }
            else if (inputHelper.IsAnyKeyHeldDown(KeyCode.LeftShift, KeyCode.RightShift)) {
                HandleElementIconShiftLeftClicked(iconClicked);
            }
            else {
                HandleElementIconLeftClicked(iconClicked);
            }
        }
        else if (inputHelper.IsMiddleMouseButton) {
            HandleElementIconMiddleClicked(iconClicked);
        }
    }

    #endregion

    #region Unit Interaction

    private void HandleUnitIconLeftClicked(FleetIconGuiElement icon) {
        PickSingleUnitIcon(icon);
    }

    private void HandleUnitIconCntlLeftClicked(FleetIconGuiElement icon) {
        if (icon.IsPicked) {
            D.Assert(_pickedUnitIcons.Contains(icon));
            if (_pickedUnitIcons.Count > Constants.One) {
                // user is unpicking this icon
                icon.IsPicked = false;
                bool isRemoved = _pickedUnitIcons.Remove(icon);
                D.Assert(isRemoved);
            }
            else {
                // clicked on icon that is the single icon already picked so do nothing
                D.AssertEqual(_pickedUnitIcons.First(), icon);
                return;
            }
        }
        else {
            D.Assert(!_pickedUnitIcons.Contains(icon));
            D.AssertNotEqual(Constants.Zero, _pickedUnitIcons.Count);   // There should always be at least one unit icon picked
            icon.IsPicked = true;
            _pickedUnitIcons.Add(icon);
        }
        ShowElementsOfPickedUnitIcons();
        AssessInteractibleHud();
        AssessButtons();
    }

    private void HandleUnitIconShiftLeftClicked(FleetIconGuiElement clickedIcon) {
        var iconsToPick = new List<FleetIconGuiElement>();
        int clickedIconIndex = _sortedUnitIconTransforms.IndexOf(clickedIcon.transform);
        if (_pickedUnitIcons.Any()) {
            FleetIconGuiElement anchorIcon = _pickedUnitIcons.Last();   // should be in order added
            int anchorIconIndex = _sortedUnitIconTransforms.IndexOf(anchorIcon.transform);
            if (anchorIconIndex == clickedIconIndex) {
                // clicked on the already picked anchor icon so do nothing
                return;
            }

            // pick all the icons between the anchor and the clicked icon, inclusive
            UnpickAllUnitIcons();
            // clickedIcon must always be the last icon added so it becomes the next anchorIcon
            if (anchorIconIndex < clickedIconIndex) {
                for (int index = anchorIconIndex; index <= clickedIconIndex; index++) {
                    Transform iconTransform = _sortedUnitIconTransforms[index];
                    FleetIconGuiElement icon = iconTransform.GetComponent<FleetIconGuiElement>();
                    D.AssertNotNull(icon);
                    iconsToPick.Add(icon);
                }
            }
            else {
                for (int index = anchorIconIndex; index >= clickedIconIndex; index--) {
                    Transform iconTransform = _sortedUnitIconTransforms[index];
                    FleetIconGuiElement icon = iconTransform.GetComponent<FleetIconGuiElement>();
                    D.AssertNotNull(icon);
                    iconsToPick.Add(icon);
                }
            }
        }
        else {
            // pick all icons from the first to this one
            for (int index = 0; index <= clickedIconIndex; index++) {
                Transform iconTransform = _sortedUnitIconTransforms[index];
                FleetIconGuiElement icon = iconTransform.GetComponent<FleetIconGuiElement>();
                D.AssertNotNull(icon);
                iconsToPick.Add(icon);
            }
        }

        iconsToPick.ForAll(icon => {
            icon.IsPicked = true;
            _pickedUnitIcons.Add(icon);
        });
        ShowElementsOfPickedUnitIcons();
        AssessInteractibleHud();
        AssessButtons();
    }

    private void HandleUnitIconMiddleClicked(FleetIconGuiElement icon) {
        FocusOn(icon.Unit);
    }

    private void HandleUnitFocusButtonClicked() {
        D.AssertEqual(Constants.One, _pickedUnitIcons.Count);
        FocusOn(_pickedUnitIcons.First().Unit);
    }

    /// <summary>
    /// Handles the unit merge button clicked.
    /// <remarks>11.16.17 No need to cycle paused state as resultingFleet.InitiateExternalCmdStaffOverrideOrder is not used.</remarks>
    /// </summary>
    private void HandleUnitMergeButtonClicked() {
        var pickedUnits = _pickedUnitIcons.Select(icon => icon.Unit);
        var resultingFleet = GameScriptsUtility.Merge(pickedUnits);
        D.Log("{0} has merged {1} to create {2}.", DebugName, pickedUnits.Concatenate(), resultingFleet.DebugName);
        // all fleetCmds except resultingFleet will die, removing them from the display
        //D.Log("{0} has selected {1} as the most effective Cmd with an effectiveness of {2:0.00}.", DebugName, resultingFleet.DebugName, resultingFleet.Data.CurrentCmdEffectiveness);
        bool isSelectedUnitDead = SelectedUnit == null || SelectedUnit.IsDead;
        if (isSelectedUnitDead) {
            // SelectedUnit died and deselected itself which will result in this HUD hiding and being Reset. Both conditions are tested as 
            // the form's reset won't necessarily happen immediately as GuiWindows don't necessarily complete their hide right away
            D.Assert(!resultingFleet.IsSelected);
            //D.Log("{0} is auto selecting {1} to reopen this HUD.", DebugName, resultingFleet.DebugName);
            resultingFleet.IsSelected = true;
        }
    }

    private void HandleUnitScuttleButtonClicked() {
        D.AssertEqual(Constants.One, _pickedUnitIcons.Count);
        // 8.15.17 Resume must occur before scuttle order so it propagates to all subscribers before initiating death
        _gameMgr.RequestPauseStateChange(toPause: false);

        var scuttleOrder = new FleetOrder(FleetDirective.Scuttle, OrderSource.User);
        var pickedUnit = _pickedUnitIcons.First().Unit;
        bool isSelectedUnitSlatedToDie = pickedUnit == SelectedUnit;

        pickedUnit.InitiateNewOrder(scuttleOrder);
        if (isSelectedUnitSlatedToDie) {
            // if SelectedUnit dies, there will be no CurrentSelection so game will resume and shouldn't be re-paused
            return;
        }
        _gameMgr.RequestPauseStateChange(toPause: true);
    }

    private void HandleUnitGuardButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedUnitIcons.Count);
        _toggleButtonsUsedThisSession.Add(_unitGuardButton);
        var pickedUnit = _pickedUnitIcons.First().Unit;
        bool isButtonToggledIn = _unitGuardButton.IsToggledIn;
        if (isButtonToggledIn) {
            IGuardable closestItemAllowedToGuard;
            if (TryFindClosestGuardableItem(pickedUnit.Position, out closestItemAllowedToGuard)) {
                FleetOrder guardOrder = new FleetOrder(FleetDirective.Guard, OrderSource.User, closestItemAllowedToGuard as IFleetNavigableDestination);
                pickedUnit.InitiateNewOrder(guardOrder);
                //D.Log("{0} is issuing an order to {1} to Guard {2}.", DebugName, pickedUnit.DebugName, closestItemAllowedToGuard.DebugName);
                _unitGuardButton.SetIconColor(TempGameValues.SelectedColor);
                AssessUnitButtons();
            }
            else {
                D.Warn("{0} found nothing for {1} to guard.", DebugName, pickedUnit.DebugName);
                _unitGuardButton.SetToggledState(false);    // release the button
            }
        }
        else {
            FleetOrder cancelOrder = new FleetOrder(FleetDirective.Cancel, OrderSource.User);
            pickedUnit.InitiateNewOrder(cancelOrder);
            _unitGuardButton.SetIconColor(GameColor.White);
        }
    }

    protected abstract bool TryFindClosestGuardableItem(Vector3 currentFleetPosition, out IGuardable closestItemAllowedToGuard);

    private void HandleUnitPatrolButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedUnitIcons.Count);
        _toggleButtonsUsedThisSession.Add(_unitPatrolButton);
        var pickedUnit = _pickedUnitIcons.First().Unit;
        bool isButtonToggledIn = _unitPatrolButton.IsToggledIn;
        if (isButtonToggledIn) {
            IPatrollable closestItemAllowedToPatrol;
            if (TryFindClosestPatrollableItem(pickedUnit.Position, out closestItemAllowedToPatrol)) {
                FleetOrder patrolOrder = new FleetOrder(FleetDirective.Patrol, OrderSource.User, closestItemAllowedToPatrol as IFleetNavigableDestination);
                pickedUnit.InitiateNewOrder(patrolOrder);
                //D.Log("{0} is issuing an order to {1} to Patrol {2}.", DebugName, pickedUnit.DebugName, closestItemAllowedToPatrol.DebugName);
                _unitPatrolButton.SetIconColor(TempGameValues.SelectedColor);
                AssessUnitButtons();
            }
            else {
                D.Warn("{0} found nothing for {1} to patrol.", DebugName, pickedUnit.DebugName);
                _unitPatrolButton.SetToggledState(false);    // release the button
            }
        }
        else {
            FleetOrder cancelOrder = new FleetOrder(FleetDirective.Cancel, OrderSource.User);
            pickedUnit.InitiateNewOrder(cancelOrder);
            _unitPatrolButton.SetIconColor(GameColor.White);
        }
    }

    protected abstract bool TryFindClosestPatrollableItem(Vector3 currentFleetPosition, out IPatrollable closestItemAllowedToPatrol);

    private void HandleUnitRepairButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedUnitIcons.Count);
        _toggleButtonsUsedThisSession.Add(_unitRepairButton);
        var pickedUnit = _pickedUnitIcons.First().Unit;
        bool isButtonToggledIn = _unitRepairButton.IsToggledIn;
        if (isButtonToggledIn) {
            IUnitBaseCmd_Ltd closestRepairBase;
            if (TryFindClosestFleetRepairBase(pickedUnit.Position, out closestRepairBase)) {
                FleetOrder repairOrder = new FleetOrder(FleetDirective.Repair, OrderSource.User, closestRepairBase as IFleetNavigableDestination);
                pickedUnit.InitiateNewOrder(repairOrder);
                //D.Log("{0} is issuing an order to {1} to Repair at {2}.", DebugName, pickedUnit.DebugName, closestRepairBase.DebugName);
                _unitRepairButton.SetIconColor(TempGameValues.SelectedColor);
                AssessUnitButtons();
            }
            else {
                D.Warn("{0} found no Base for {1} to repair at.", DebugName, pickedUnit.DebugName);
                _unitRepairButton.SetToggledState(false);    // release the button
            }
        }
        else {
            FleetOrder cancelOrder = new FleetOrder(FleetDirective.Cancel, OrderSource.User);
            pickedUnit.InitiateNewOrder(cancelOrder);
            _unitRepairButton.SetIconColor(GameColor.White);
        }
    }

    protected abstract bool TryFindClosestFleetRepairBase(Vector3 currentFleetPosition, out IUnitBaseCmd_Ltd closestRepairBase);

    private void HandleUnitRefitButtonToggleChanged() {
        D.Warn("{0}.HandleUnitRefitButtonToggleChanged not yet implemented.", DebugName);
        _toggleButtonsUsedThisSession.Add(_unitRefitButton);
        _unitRefitButton.SetToggledState(false); // TEMP release the button
        // UNDONE
    }

    private void HandleUnitDisbandButtonToggleChanged() {
        D.Warn("{0}.HandleUnitDisbandButtonToggleChanged not yet implemented.", DebugName);
        _toggleButtonsUsedThisSession.Add(_unitDisbandButton);
        _unitDisbandButton.SetToggledState(false); // TEMP release the button
        // UNDONE
    }

    private void HandleUnitExploreButtonToggleChanged() {
        D.Warn("{0}.HandleUnitExploreButtonToggleChanged not yet implemented.", DebugName);
        _toggleButtonsUsedThisSession.Add(_unitExploreButton);
        _unitExploreButton.SetToggledState(false); // TEMP release the button
        // UNDONE
    }

    private void PickSingleUnitIcon(FleetIconGuiElement icon) {
        UnpickAllUnitIcons();
        icon.IsPicked = true;
        _pickedUnitIcons.Add(icon);
        ShowElementsOfPickedUnitIcons();

        AssessInteractibleHud();
        AssessButtons();
    }

    private void UnpickAllUnitIcons() {
        foreach (var icon in _pickedUnitIcons) {
            icon.IsPicked = false;
        }
        _pickedUnitIcons.Clear();
    }

    protected virtual void AssessUnitButtons() {
        AssessUnitFocusButton();

        bool isUnitMergeButtonEnabled = false;
        if (_pickedUnitIcons.Count > Constants.One) {
            IEnumerable<FleetCmdItem> pickedUnits = _pickedUnitIcons.Select(icon => icon.Unit);
            int pickedUnitsElementCount = pickedUnits.Sum(unit => unit.ElementCount);
            if (pickedUnitsElementCount <= TempGameValues.MaxShipsPerFleet) {
                isUnitMergeButtonEnabled = true;
            }
        }
        _unitMergeButton.isEnabled = isUnitMergeButtonEnabled;

        _unitScuttleButton.isEnabled = _pickedUnitIcons.Count == Constants.One;

        if (_pickedUnitIcons.Count == Constants.One) {
            // if 1 picked, set toggle state without notify and enable all. If order initiated, it will find closest item to execute on
            var pickedUnit = _pickedUnitIcons.First().Unit;

            bool isOrderedToRepair = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Repair);
            GameColor iconColor = isOrderedToRepair ? TempGameValues.SelectedColor : GameColor.White;
            _unitRepairButton.SetToggledState(isOrderedToRepair, iconColor);
            if (!HasBeenUsedThisSession(_unitRepairButton) && isOrderedToRepair) {
                // Button not yet used during this session (pause) and order already exists so it is from a previous session (pause).
                // Accordingly, button should not be available as cancel only works during same session (pause) order was issued. 
                _unitRepairButton.IsEnabled = false;
            }
            else {
                // Either button has been used this session (pause) or there is no existing order. Either way, the button should
                // be available if there is damage to repair.
                bool isDamaged = pickedUnit.Data.UnitHealth < Constants.OneHundredPercent || pickedUnit.Data.Health < Constants.OneHundredPercent;
                _unitRepairButton.IsEnabled = isDamaged;
            }

            bool isOrderedToRefit = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Refit);
            iconColor = isOrderedToRefit ? TempGameValues.SelectedColor : GameColor.White;
            _unitRefitButton.SetToggledState(isOrderedToRefit, iconColor);
            if (!HasBeenUsedThisSession(_unitRefitButton) && isOrderedToRefit) {
                _unitRefitButton.IsEnabled = false;
            }
            else {
                _unitRefitButton.IsEnabled = IsUnitUpgradeAvailable(pickedUnit);
            }

            bool isOrderedToDisband = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Disband);
            iconColor = isOrderedToDisband ? TempGameValues.SelectedColor : GameColor.White;
            _unitDisbandButton.SetToggledState(isOrderedToDisband, iconColor);
            _unitDisbandButton.IsEnabled = (!HasBeenUsedThisSession(_unitDisbandButton) && isOrderedToDisband) ? false : true;

            bool isOrderedToGuard = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Guard);
            iconColor = isOrderedToGuard ? TempGameValues.SelectedColor : GameColor.White;
            _unitGuardButton.SetToggledState(isOrderedToGuard, iconColor);
            _unitGuardButton.IsEnabled = (!HasBeenUsedThisSession(_unitGuardButton) && isOrderedToGuard) ? false : true;

            bool isOrderedToPatrol = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Patrol);
            iconColor = isOrderedToPatrol ? TempGameValues.SelectedColor : GameColor.White;
            _unitPatrolButton.SetToggledState(isOrderedToPatrol, iconColor);
            _unitPatrolButton.IsEnabled = (!HasBeenUsedThisSession(_unitPatrolButton) && isOrderedToPatrol) ? false : true;

            bool isOrderedToExplore = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Explore);
            iconColor = isOrderedToExplore ? TempGameValues.SelectedColor : GameColor.White;
            _unitExploreButton.SetToggledState(isOrderedToExplore, iconColor);
            _unitExploreButton.IsEnabled = (!HasBeenUsedThisSession(_unitExploreButton) && isOrderedToExplore) ? false : true;
        }
        else {
            // if more than 1 picked unit, un-toggle without notify and disable all
            ResetUnitOrderToggleButtons();
        }
    }

    private bool IsUnitUpgradeAvailable(FleetCmdItem pickedUnit) {  // expensive
        return _gameMgr.PlayersDesigns.AreUnitUpgradeDesignsPresent(pickedUnit.Owner, pickedUnit.Data);
    }

    protected void AssessUnitFocusButton() {
        _unitFocusButton.isEnabled = _pickedUnitIcons.Count == Constants.One;
    }

    protected void DisableUnitButtons() {
        _unitMergeButton.isEnabled = false;
        _unitScuttleButton.isEnabled = false;
        _unitGuardButton.IsEnabled = false;
        _unitPatrolButton.IsEnabled = false;
        _unitRepairButton.IsEnabled = false;
        _unitRefitButton.IsEnabled = false;
        _unitDisbandButton.IsEnabled = false;
        _unitExploreButton.IsEnabled = false;
    }

    #endregion

    #region Element Interaction

    private void HandleElementIconLeftClicked(ShipIconGuiElement icon) {
        if (icon.IsPicked) {
            D.Assert(_pickedElementIcons.Contains(icon));
            // user is unpicking this icon
            icon.IsPicked = false;
            bool isRemoved = _pickedElementIcons.Remove(icon);
            D.Assert(isRemoved);
        }
        else {
            PickSingleElementIcon(icon);
        }
        AssessInteractibleHud();
        AssessElementButtons();
    }

    private void HandleElementIconCntlLeftClicked(ShipIconGuiElement icon) {
        if (icon.IsPicked) {
            D.Assert(_pickedElementIcons.Contains(icon));
            // user is unpicking this icon
            icon.IsPicked = false;
            bool isRemoved = _pickedElementIcons.Remove(icon);
            D.Assert(isRemoved);
        }
        else {
            D.Assert(!_pickedElementIcons.Contains(icon));
            icon.IsPicked = true;
            _pickedElementIcons.Add(icon);
        }
        AssessInteractibleHud();
        AssessElementButtons();
    }

    private void HandleElementIconShiftLeftClicked(ShipIconGuiElement clickedIcon) {
        var iconsToPick = new List<ShipIconGuiElement>();
        int clickedIconIndex = _sortedElementIconTransforms.IndexOf(clickedIcon.transform);
        if (_pickedElementIcons.Any()) {
            ShipIconGuiElement anchorIcon = _pickedElementIcons.Last();   // should be in order added
            int anchorIconIndex = _sortedElementIconTransforms.IndexOf(anchorIcon.transform);
            if (anchorIconIndex == clickedIconIndex) {
                // clicked on the already picked anchor icon so do nothing
                return;
            }

            // pick all the icons between the anchor and the clicked icon, inclusive
            UnpickAllElementIcons();
            // clickedIcon must always be the last icon added so it becomes the next anchorIcon
            if (anchorIconIndex < clickedIconIndex) {
                for (int index = anchorIconIndex; index <= clickedIconIndex; index++) {
                    Transform iconTransform = _sortedElementIconTransforms[index];
                    ShipIconGuiElement icon = iconTransform.GetComponent<ShipIconGuiElement>();
                    if (icon != null) {
                        iconsToPick.Add(icon);
                    }
                }
            }
            else {
                for (int index = anchorIconIndex; index >= clickedIconIndex; index--) {
                    Transform iconTransform = _sortedElementIconTransforms[index];
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
                Transform iconTransform = _sortedElementIconTransforms[index];
                ShipIconGuiElement icon = iconTransform.GetComponent<ShipIconGuiElement>();
                if (icon != null) {
                    iconsToPick.Add(icon);
                }
            }
        }

        iconsToPick.ForAll(icon => {
            icon.IsPicked = true;
            _pickedElementIcons.Add(icon);
        });
        AssessInteractibleHud();
        AssessElementButtons();
    }

    private void HandleElementIconMiddleClicked(ShipIconGuiElement icon) {
        FocusOn(icon.Element);
    }

    /// <summary>
    /// Handles the ship create fleet button clicked.
    /// <remarks>11.16.17 Must cycle paused state to allow resultingFleet to become operational. RebuildUnitIcons
    /// needs resultingFleet's iconInfo to make the icons. The icons aren't available until resultingFleet becomes operational.
    /// resultingFleet.InitiateExternalCmdStaffOverrideOrder is not used.</remarks>
    /// </summary>
    private void HandleShipCreateFleetButtonClicked() {
        Utility.ValidateForRange(_pickedElementIcons.Count, Constants.One, TempGameValues.MaxShipsPerFleet);

        var pickedCmds = _pickedUnitIcons.Select(icon => icon.Unit);
        var pickedShips = _pickedElementIcons.Select(icon => icon.Element);
        IDictionary<FleetCmdItem, IList<ShipItem>> shipsByCmdLookup = new Dictionary<FleetCmdItem, IList<ShipItem>>(_pickedUnitIcons.Count);
        // populate the lookup
        foreach (var cmd in pickedCmds) {
            foreach (var ship in pickedShips) {
                if (cmd.Contains(ship)) {
                    IList<ShipItem> cmdsShips;
                    if (!shipsByCmdLookup.TryGetValue(cmd, out cmdsShips)) {
                        cmdsShips = new List<ShipItem>();
                        shipsByCmdLookup.Add(cmd, cmdsShips);
                    }
                    cmdsShips.Add(ship);
                }
            }
        }

        IList<FleetCmdItem> fleetsToMerge = new List<FleetCmdItem>(shipsByCmdLookup.Count);
        foreach (var cmd in shipsByCmdLookup.Keys) {
            var cmdsPickedShips = shipsByCmdLookup[cmd];
            FleetCmdItem formedFleet = cmd.FormFleetFrom("UserCreatedFleet", cmdsPickedShips);
            fleetsToMerge.Add(formedFleet);
        }

        FleetCmdItem resultingFleet;
        if (fleetsToMerge.Count > Constants.One) {
            resultingFleet = GameScriptsUtility.Merge(fleetsToMerge);
        }
        else {
            resultingFleet = fleetsToMerge.First();
        }

        _gameMgr.RequestPauseStateChange(toPause: false);
        _gameMgr.RequestPauseStateChange(toPause: true);

        var allFleets = new HashSet<FleetCmdItem>(_unitIconLookup.Keys);
        bool isAdded = allFleets.Add(resultingFleet);
        if (!isAdded) {
            // 11.12.17 The same fleet instance can occur for multiple reasons: 1) a fleet that is formed from all of
            // its existing ships is actually the same instance, and 2) a fleet that results from a merge will 
            // always be one of the instances submitted for the merge.
            D.Warn("FYI. {0}: No need to add {1} when it is already present.", DebugName, resultingFleet.DebugName);
        }
        RebuildUnitIcons(allFleets, resultingFleet);
    }

    private void HandleShipScuttleButtonClicked() {
        D.Assert(_pickedElementIcons.Any());
        // 8.15.17 Resume must occur before scuttle order so it propagates to all subscribers before initiating death
        _gameMgr.RequestPauseStateChange(toPause: false);

        var pickedElements = _pickedElementIcons.Select(icon => icon.Element);

        bool isSelectedUnitSlatedToDie = !SelectedUnit.Elements.Cast<ShipItem>().Except(pickedElements).Any();

        var scuttleOrder = new ShipOrder(ShipDirective.Scuttle, OrderSource.User);
        pickedElements.ForAll(element => element.InitiateNewOrder(scuttleOrder));

        if (isSelectedUnitSlatedToDie) {
            // if SelectedUnit dies, there will be no CurrentSelection so game will resume and shouldn't be re-paused
            return;
        }

        _gameMgr.RequestPauseStateChange(toPause: true);
    }

    private void PickSingleElementIcon(ShipIconGuiElement icon) {
        UnpickAllElementIcons();
        icon.IsPicked = true;
        _pickedElementIcons.Add(icon);
    }

    private void UnpickAllElementIcons() {
        foreach (var icon in _pickedElementIcons) {
            icon.IsPicked = false;
        }
        _pickedElementIcons.Clear();
    }

    protected virtual void AssessElementButtons() {
        _shipCreateFleetButton.isEnabled = Utility.IsInRange(_pickedElementIcons.Count, Constants.One, TempGameValues.MaxShipsPerFleet);
        _shipScuttleButton.isEnabled = _pickedElementIcons.Any();
    }

    protected void DisableElementButtons() {
        _shipCreateFleetButton.isEnabled = false;
        _shipScuttleButton.isEnabled = false;
    }

    #endregion

    private void ShowElementsOfPickedUnitIcons() {
        BuildPickedUnitsCompositionIcons();
    }

    private void RebuildUnitIcons(IEnumerable<FleetCmdItem> units, FleetCmdItem unitToPick) {
        BuildUnitIcons(units, unitToPick);
    }

    private void BuildUnitIcons(IEnumerable<FleetCmdItem> units, FleetCmdItem unitToPick) {
        RemoveUnitIcons();

        AMultiSizeIconGuiElement.IconSize iconSize = AMultiSizeIconGuiElement.IconSize.Large;

        // configure grid for icon size
        IntVector2 iconDimensions = _unitIconPrefab.GetIconDimensions(iconSize);
        _unitIconsGrid.cellHeight = iconDimensions.y;
        _unitIconsGrid.cellWidth = iconDimensions.x;

        // make grid 1 column wide
        D.AssertEqual(UIGrid.Arrangement.Vertical, _unitIconsGrid.arrangement);
        _unitIconsGrid.maxPerLine = Constants.Zero; // infinite rows

        units.ForAll(u => CreateAndAddIcon(u, iconSize));
        _unitIconsGrid.repositionNow = true;

        var iconToPick = _unitIconLookup[unitToPick];
        PickSingleUnitIcon(iconToPick);
    }

    /// <summary>
    /// Build the collection of icons that represent the elements in each of the picked Units.
    /// </summary>
    protected virtual void BuildPickedUnitsCompositionIcons() {
        RemoveAllUnitCompositionIcons();

        var gridContainerSize = _elementIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerSize.x, (int)gridContainerSize.y);

        IList<IList<ShipItem>> unitElementLists = GetElementsOfPickedUnitIcons();
        int desiredGridCells = unitElementLists.Sum(list => list.Count);
        desiredGridCells += unitElementLists.Count * 4; // add max potential for blanks // IMPROVE may be more columns than 4

        int gridColumns, unusedGridRows;
        AMultiSizeIconGuiElement.IconSize iconSize = AMultiSizeIconGuiElement.DetermineGridIconSize(gridContainerDimensions, desiredGridCells, _elementIconPrefab,
            out unusedGridRows, out gridColumns);

        // configure grid for icon size
        IntVector2 iconDimensions = _elementIconPrefab.GetIconDimensions(iconSize);
        _elementIconsGrid.cellHeight = iconDimensions.y;
        _elementIconsGrid.cellWidth = iconDimensions.x;

        // make grid gridColumns wide
        D.AssertEqual(UIGrid.Arrangement.Horizontal, _elementIconsGrid.arrangement);
        _elementIconsGrid.maxPerLine = gridColumns;

        int lastListIndex = unitElementLists.Count - Constants.One;

        bool areBlankIconsNeeded = lastListIndex > Constants.Zero;
        GameObject blankIconPrefab = areBlankIconsNeeded ? MakeBlankIconPrefab() : null;

        for (int listIndex = 0; listIndex <= lastListIndex; listIndex++) {
            var aUnitsElements = unitElementLists[listIndex];
            foreach (var element in aUnitsElements) {
                CreateAndAddIcon(element, iconSize);
            }

            if (areBlankIconsNeeded && listIndex < lastListIndex) {
                int currentBlankIconsNeeded = gridColumns - aUnitsElements.Count % gridColumns;
                D.AssertNotEqual(Constants.Zero, currentBlankIconsNeeded);
                D.Assert(currentBlankIconsNeeded <= gridColumns);

                for (int i = 0; i < currentBlankIconsNeeded; i++) {
                    GameObject blankIconGo = NGUITools.AddChild(_elementIconsGrid.gameObject, blankIconPrefab);
                    blankIconGo.name = "Blank Element Icon";
                    _sortedElementIconTransforms.Add(blankIconGo.transform);
                }
            }
        }
        //D.Log("{0}: ElementIcons in sequence: {1}.", DebugName, _sortedElementIconTransforms.Select(t => t.name).Concatenate());
        _elementIconsGrid.repositionNow = true;
    }

    private IList<IList<ShipItem>> GetElementsOfPickedUnitIcons() {
        D.AssertNotEqual(Constants.Zero, _pickedUnitIcons.Count);
        IList<IList<ShipItem>> unitElementLists = new List<IList<ShipItem>>();
        foreach (var unitIcon in _pickedUnitIcons) {
            var operationalElements = unitIcon.Unit.Elements.Where(e => !e.IsDead).Cast<ShipItem>();
            IList<ShipItem> elements = new List<ShipItem>(operationalElements);
            unitElementLists.Add(elements);
        }
        return unitElementLists;
    }

    private void CreateAndAddIcon(FleetCmdItem unit, AMultiSizeIconGuiElement.IconSize iconSize) {
        GameObject unitIconGo = NGUITools.AddChild(_unitIconsGrid.gameObject, _unitIconPrefab.gameObject);
        unitIconGo.name = unit.Name + UnitIconExtension;
        FleetIconGuiElement unitIcon = unitIconGo.GetSafeComponent<FleetIconGuiElement>();
        unitIcon.Size = iconSize;
        unitIcon.Unit = unit;

        UIEventListener.Get(unitIconGo).onClick += UnitIconClickedEventHandler;
        unit.deathOneShot += UnitDeathEventHandler;
        //unit.ownerChanged += UnitOwnerChangedEventHandler;
        _unitIconLookup.Add(unit, unitIcon);
        _sortedUnitIconTransforms.Add(unitIconGo.transform);
    }

    private void CreateAndAddIcon(ShipItem element, AMultiSizeIconGuiElement.IconSize iconSize) {
        GameObject elementIconGo = NGUITools.AddChild(_elementIconsGrid.gameObject, _elementIconPrefab.gameObject);
        elementIconGo.name = element.Name + ElementIconExtension;
        ShipIconGuiElement elementIcon = elementIconGo.GetSafeComponent<ShipIconGuiElement>();
        elementIcon.Size = iconSize;
        elementIcon.Element = element;

        UIEventListener.Get(elementIconGo).onClick += ElementIconClickedEventHandler;
        element.deathOneShot += ElementDeathEventHandler;
        _sortedElementIconTransforms.Add(elementIconGo.transform);
    }

    private void RemoveUnitIcons() {
        IList<Transform> iconTransforms = _unitIconsGrid.GetChildList();
        if (iconTransforms.Any()) {
            foreach (var it in iconTransforms) {
                var icon = it.GetComponent<FleetIconGuiElement>();
                RemoveIcon(icon);
            }
        }
    }

    protected void RemoveAllUnitCompositionIcons() {
        IList<Transform> iconTransforms = _elementIconsGrid.GetChildList();
        if (iconTransforms.Any()) {
            foreach (var it in iconTransforms) {
                var icon = it.GetComponent<ShipIconGuiElement>();
                if (icon != null) {
                    RemoveIcon(icon);
                }
                else {
                    // its a blank icon
                    bool isRemoved = _sortedElementIconTransforms.Remove(it);
                    D.Assert(isRemoved);
                    DestroyImmediate(it.gameObject);
                }
            }
        }
    }

    private void RemoveIcon(FleetIconGuiElement icon) {
        _pickedUnitIcons.Remove(icon);  // may not be present
        if (icon.IsInitialized) {
            D.AssertNotNull(icon.Unit, "{0}: {1}'s Unit has been destroyed.".Inject(DebugName, icon.DebugName));
            FleetIconGuiElement unitIcon;
            bool isIconFound = _unitIconLookup.TryGetValue(icon.Unit, out unitIcon);
            D.Assert(isIconFound);
            D.AssertEqual(icon, unitIcon);

            icon.Unit.deathOneShot -= UnitDeathEventHandler;
            bool isRemoved = _unitIconLookup.Remove(icon.Unit);
            D.Assert(isRemoved);
            isRemoved = _sortedUnitIconTransforms.Remove(icon.transform);
            D.Assert(isRemoved);
            //D.Log("{0} is removing {1}.", DebugName, icon.DebugName);
        }
        else {
            // icon placeholder under grid will not be initialized
            //D.Log("{0} not able to remove icon {1} from collections because it is not initialized.", DebugName, icon.DebugName);
        }

        UIEventListener.Get(icon.gameObject).onClick -= UnitIconClickedEventHandler;
        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(icon.gameObject);
    }

    private void RemoveIcon(ShipIconGuiElement icon) {
        _pickedElementIcons.Remove(icon);   // may not be present
        if (icon.IsInitialized) {
            D.AssertNotNull(icon.Element, "{0}: {1}'s Element has been destroyed.".Inject(DebugName, icon.DebugName));

            icon.Element.deathOneShot -= ElementDeathEventHandler;
            //D.Log("{0} has removed the icon for {1}.", DebugName, icon.Element.DebugName);
            bool isRemoved = _sortedElementIconTransforms.Remove(icon.transform);
            D.Assert(isRemoved);
        }
        else {
            // icon placeholder under grid will not be initialized
            //D.Log("{0} not able to remove icon {1} from collections because it is not initialized.", DebugName, icon.DebugName);
        }

        UIEventListener.Get(icon.gameObject).onClick -= ElementIconClickedEventHandler;
        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(icon.gameObject);
    }

    private void HandleDeathOf(FleetCmdItem unit) {
        //D.Log("{0}.HandleDeathOf({1}) called.", DebugName, unit.DebugName);
        if (unit == SelectedUnit) {
            // Death of the SelectedUnit (SelectionMgr's CurrentSelection) will no longer be selected, hiding the HUD window
            // and resetting the form. Unfortunately, the form's reset won't necessarily happen before the SelectedUnit is destroyed
            // as GuiWindows don't necessarily complete their hide right away. Remove the icon now before the unit is destroyed
            // and wait for the HUD to hide.
            RemoveIcon(_unitIconLookup[SelectedUnit]);
            return;
        }

        var units = new List<FleetCmdItem>(_unitIconLookup.Keys);
        units.Remove(unit);
        RebuildUnitIcons(units, SelectedUnit);
    }

    private void HandleDeathOf(ShipItem element) {
        BuildPickedUnitsCompositionIcons();    // Rebuild from scratch as may need more blanks        
    }

    protected abstract void AssessInteractibleHud();

    private void AssessButtons() {
        AssessUnitButtons();
        AssessElementButtons();
    }

    private void FocusOn(ADiscernibleItem item) {
        item.IsFocus = true;
    }

    private bool HasBeenUsedThisSession(MyNguiToggleButton button) {
        return _toggleButtonsUsedThisSession.Contains(button);
    }
    private GameObject MakeBlankIconPrefab() {
        return new GameObject("BlankIconPrefab");
    }

    private int CompareElementIcons(Transform aIconTransform, Transform bIconTransform) {
        int aIndex = _sortedElementIconTransforms.IndexOf(aIconTransform);
        int bIndex = _sortedElementIconTransforms.IndexOf(bIconTransform);
        return aIndex.CompareTo(bIndex);
    }

    protected override void ResetForReuse_Internal() {
        _toggleButtonsUsedThisSession.Clear();

        RemoveAllUnitCompositionIcons();
        D.AssertEqual(Constants.Zero, _pickedElementIcons.Count, _pickedElementIcons.Concatenate());
        D.AssertEqual(Constants.Zero, _sortedElementIconTransforms.Count, _sortedElementIconTransforms.Concatenate());

        RemoveUnitIcons();
        D.AssertEqual(Constants.Zero, _pickedUnitIcons.Count, _pickedUnitIcons.Concatenate());
        D.AssertEqual(Constants.Zero, _unitIconLookup.Count, _unitIconLookup.Keys.Concatenate());
        D.AssertEqual(Constants.Zero, _sortedUnitIconTransforms.Count, _sortedUnitIconTransforms.Concatenate());

        ResetUnitOrderToggleButtons();
        _selectedUnit = null;

        AssessInteractibleHud();
    }

    private void ResetUnitOrderToggleButtons() {
        _unitGuardButton.SetToggledState(false);
        _unitGuardButton.IsEnabled = false;

        _unitPatrolButton.SetToggledState(false);
        _unitPatrolButton.IsEnabled = false;

        _unitRepairButton.SetToggledState(false);
        _unitRepairButton.IsEnabled = false;

        _unitRefitButton.SetToggledState(false);
        _unitRefitButton.IsEnabled = false;

        _unitDisbandButton.SetToggledState(false);
        _unitDisbandButton.IsEnabled = false;

        _unitExploreButton.SetToggledState(false);
        _unitExploreButton.IsEnabled = false;
    }

    private void DisconnectButtonEventHandlers() {
        EventDelegate.Remove(_unitFocusButton.onClick, UnitFocusButtonClickedEventHandler);
        EventDelegate.Remove(_unitMergeButton.onClick, UnitMergeButtonClickedEventHandler);
        EventDelegate.Remove(_unitScuttleButton.onClick, UnitScuttleButtonClickedEventHandler);
        EventDelegate.Remove(_shipCreateFleetButton.onClick, ShipCreateFleetButtonClickedEventHandler);
        EventDelegate.Remove(_shipScuttleButton.onClick, ShipScuttleButtonClickedEventHandler);

        _unitGuardButton.toggleStateChanged -= UnitGuardButtonToggleChangedEventHandler;
        _unitPatrolButton.toggleStateChanged -= UnitPatrolButtonToggleChangedEventHandler;
        _unitRepairButton.toggleStateChanged -= UnitRepairButtonToggleChangedEventHandler;
        _unitRefitButton.toggleStateChanged -= UnitRefitButtonToggleChangedEventHandler;
        _unitDisbandButton.toggleStateChanged -= UnitDisbandButtonToggleChangedEventHandler;
        _unitExploreButton.toggleStateChanged -= UnitExploreButtonToggleChangedEventHandler;
    }

    protected override void Cleanup() {
        RemoveAllUnitCompositionIcons();
        RemoveUnitIcons();
        DisconnectButtonEventHandlers();
    }

    #region Debug

    protected abstract IEnumerable<FleetCmdItem> __AcquireLocalUnits();

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotNull(_unitIconPrefab);
        D.AssertNotNull(_elementIconPrefab);

        D.AssertNotNull(_unitFocusButton);
        D.AssertNotNull(_unitMergeButton);
        D.AssertNotNull(_unitScuttleButton);
        D.AssertNotNull(_shipCreateFleetButton);
        D.AssertNotNull(_shipScuttleButton);

        D.AssertNotNull(_unitGuardButton);
        D.AssertNotNull(_unitPatrolButton);
        D.AssertNotNull(_unitRepairButton);
        D.AssertNotNull(_unitRefitButton);
        D.AssertNotNull(_unitDisbandButton);
        D.AssertNotNull(_unitExploreButton);
    }

    #endregion


}

