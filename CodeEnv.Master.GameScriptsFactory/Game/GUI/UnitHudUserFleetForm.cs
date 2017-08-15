// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitHudUserFleetForm.cs
// Form used by the UnitHudWindow to display info and allow changes when a user-owned Fleet is selected.
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
/// Form used by the UnitHudWindow to display info and allow changes when a user-owned Fleet is selected.
/// </summary>
public class UnitHudUserFleetForm : AForm {

    private const string UnitIconExtension = " UnitIcon";
    private const string ElementIconExtension = " ElementIcon";

    [SerializeField]
    private FleetIcon _unitIconPrefab = null;
    [SerializeField]
    private ShipIcon _elementIconPrefab = null;

    [SerializeField]
    private UIButton _unitMergeButton = null;
    [SerializeField]
    private UIButton _unitScuttleButton = null;
    [SerializeField]
    private UIButton _createUnitButton = null;
    [SerializeField]
    private UIButton _elementScuttleButton = null;

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

    public override FormID FormID { get { return FormID.UserFleet; } }

    private FleetCmdItem _selectedUnit;
    public FleetCmdItem SelectedUnit {
        get { return _selectedUnit; }
        set {
            D.AssertNull(_selectedUnit);
            SetProperty<FleetCmdItem>(ref _selectedUnit, value, "SelectedUnit", SelectedUnitPropSetHandler);
        }
    }

    private IList<Transform> _sortedUnitIconTransforms;
    private IList<Transform> _sortedElementIconTransforms;
    private HashSet<FleetIcon> _pickedUnitIcons;
    private HashSet<ShipIcon> _pickedElementIcons;
    /// <summary>
    /// The unit icons that are showing keyed by their unit. 
    /// The icons of all the units that populate this form will always be showing.
    /// </summary>
    private IDictionary<FleetCmdItem, FleetIcon> _unitIconLookup;
    /// <summary>
    /// The element icons that are showing keyed by their element. The only icons that
    /// will be showing are those icons that belong to elements of a picked unit.
    /// </summary>
    private IDictionary<ShipItem, ShipIcon> _elementIconLookup;
    private UIGrid _unitIconsGrid;
    private UIGrid _elementIconsGrid;
    private GameManager _gameMgr;

    protected override void InitializeValuesAndReferences() {
        //D.Log("{0} is initializing.", DebugName);
        _gameMgr = GameManager.Instance;
        _unitIconsGrid = gameObject.GetSingleComponentInChildren<FleetIcon>().gameObject.GetSingleComponentInParents<UIGrid>();
        _unitIconsGrid.arrangement = UIGrid.Arrangement.Vertical;
        _unitIconsGrid.sorting = UIGrid.Sorting.Alphabetic;

        _elementIconsGrid = gameObject.GetSingleComponentInChildren<ShipIcon>().gameObject.GetSingleComponentInParents<UIGrid>();
        _elementIconsGrid.arrangement = UIGrid.Arrangement.Horizontal;
        _elementIconsGrid.sorting = UIGrid.Sorting.Custom;
        _elementIconsGrid.onCustomSort = CompareElementIcons;

        _unitIconLookup = new Dictionary<FleetCmdItem, FleetIcon>();
        _elementIconLookup = new Dictionary<ShipItem, ShipIcon>();
        _pickedUnitIcons = new HashSet<FleetIcon>();
        _pickedElementIcons = new HashSet<ShipIcon>();
        _sortedUnitIconTransforms = new List<Transform>();
        _sortedElementIconTransforms = new List<Transform>();

        _unitGuardButton.Initialize();
        _unitPatrolButton.Initialize();
        _unitRepairButton.Initialize();
        _unitRefitButton.Initialize();
        _unitDisbandButton.Initialize();
        _unitExploreButton.Initialize();

        ConnectButtonEventHandlers();
    }

    private void ConnectButtonEventHandlers() {
        EventDelegate.Add(_unitMergeButton.onClick, UnitMergeButtonClickedEventHandler);
        EventDelegate.Add(_unitScuttleButton.onClick, UnitScuttleButtonClickedEventHandler);
        EventDelegate.Add(_createUnitButton.onClick, CreateUnitButtonClickedEventHandler);
        EventDelegate.Add(_elementScuttleButton.onClick, ElementScuttleButtomClickedEventHandler);

        _unitGuardButton.toggleStateChanged += UnitGuardButtonToggleChangedEventHandler;
        _unitPatrolButton.toggleStateChanged += UnitPatrolButtonToggleChangedEventHandler;
        _unitRepairButton.toggleStateChanged += UnitRepairButtonToggleChangedEventHandler;
        _unitRefitButton.toggleStateChanged += UnitRefitButtonToggleChangedEventHandler;
        _unitDisbandButton.toggleStateChanged += UnitDisbandButtonToggleChangedEventHandler;
        _unitExploreButton.toggleStateChanged += UnitExploreButtonToggleChangedEventHandler;
    }

    protected override void AssignValuesToMembers() {
        IList<FleetCmdItem> units = new List<FleetCmdItem>(__AcquireLocalUnits());
        units.Add(SelectedUnit);

        // Only the selected Unit should initially show as picked
        BuildUnitIcons(units, SelectedUnit);
    }

    #region Event and Property Change Handlers

    private void SelectedUnitPropSetHandler() {
        D.Assert(_gameMgr.IsPaused);
        AssignValuesToMembers();
    }

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

    private void UnitMergeButtonClickedEventHandler() {
        HandleUnitMergeButtonClicked();
    }

    private void UnitScuttleButtonClickedEventHandler() {
        HandleUnitScuttleButtonClicked();
    }

    private void CreateUnitButtonClickedEventHandler() {
        HandleCreateUnitButtonClicked();
    }

    private void ElementScuttleButtomClickedEventHandler() {
        HandleElementScuttleButtonClicked();
    }

    private void UnitDeathEventHandler(object sender, EventArgs e) {
        FleetCmdItem unit = sender as FleetCmdItem;
        HandleDeathOf(unit);
    }

    private void ElementDeathEventHandler(object sender, EventArgs e) {
        ShipItem element = sender as ShipItem;
        HandleDeathOf(element);
    }

    [Obsolete("Owner changes don't occur while paused")]
    private void UnitOwnerChangedEventHandler(object sender, EventArgs e) {
        FleetCmdItem unit = sender as FleetCmdItem;
        HandleOwnerChanged(unit);
    }

    [Obsolete("Owner changes don't occur while paused")]
    private void ElementOwnerChangedEventHandler(object sender, EventArgs e) {
        ShipItem element = sender as ShipItem;
        HandleOwnerChanged(element);
    }

    private void UnitIconClickedEventHandler(GameObject go) {
        var inputHelper = GameInputHelper.Instance;
        FleetIcon iconClicked = go.GetComponent<FleetIcon>();
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
        ShipIcon iconClicked = go.GetComponent<ShipIcon>();
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

    #region Unit Icon Interaction

    private void HandleUnitIconLeftClicked(FleetIcon icon) {
        PickSingleUnitIcon(icon);
    }

    private void HandleUnitIconCntlLeftClicked(FleetIcon icon) {
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
        AssessInteractableHud();
        AssessButtons();
    }

    private void HandleUnitIconShiftLeftClicked(FleetIcon clickedIcon) {
        var iconsToPick = new List<FleetIcon>();
        int clickedIconIndex = _sortedUnitIconTransforms.IndexOf(clickedIcon.transform);
        if (_pickedUnitIcons.Any()) {
            FleetIcon anchorIcon = _pickedUnitIcons.Last();   // should be in order added
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
                    FleetIcon icon = iconTransform.GetComponent<FleetIcon>();
                    D.AssertNotNull(icon);
                    iconsToPick.Add(icon);
                }
            }
            else {
                for (int index = anchorIconIndex; index >= clickedIconIndex; index--) {
                    Transform iconTransform = _sortedUnitIconTransforms[index];
                    FleetIcon icon = iconTransform.GetComponent<FleetIcon>();
                    D.AssertNotNull(icon);
                    iconsToPick.Add(icon);
                }
            }
        }
        else {
            // pick all icons from the first to this one
            for (int index = 0; index <= clickedIconIndex; index++) {
                Transform iconTransform = _sortedUnitIconTransforms[index];
                FleetIcon icon = iconTransform.GetComponent<FleetIcon>();
                D.AssertNotNull(icon);
                iconsToPick.Add(icon);
            }
        }

        iconsToPick.ForAll(icon => {
            icon.IsPicked = true;
            _pickedUnitIcons.Add(icon);
        });
        ShowElementsOfPickedUnitIcons();
        AssessInteractableHud();
        AssessButtons();
    }

    private void HandleUnitIconMiddleClicked(FleetIcon icon) {
        icon.Unit.IsFocus = true;
    }

    private void PickSingleUnitIcon(FleetIcon icon) {
        UnpickAllUnitIcons();
        icon.IsPicked = true;
        _pickedUnitIcons.Add(icon);
        ShowElementsOfPickedUnitIcons();

        AssessInteractableHud();
        AssessButtons();
    }

    private void UnpickAllUnitIcons() {
        foreach (var icon in _pickedUnitIcons) {
            icon.IsPicked = false;
        }
        _pickedUnitIcons.Clear();
    }

    #endregion

    #region Element Icon Interaction

    private void HandleElementIconLeftClicked(ShipIcon icon) {
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
        AssessInteractableHud();
        AssessRegularButtons();
    }

    private void HandleElementIconCntlLeftClicked(ShipIcon icon) {
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
        AssessInteractableHud();
        AssessRegularButtons();
    }

    private void HandleElementIconShiftLeftClicked(ShipIcon clickedIcon) {
        var iconsToPick = new List<ShipIcon>();
        int clickedIconIndex = _sortedElementIconTransforms.IndexOf(clickedIcon.transform);
        if (_pickedElementIcons.Any()) {
            ShipIcon anchorIcon = _pickedElementIcons.Last();   // should be in order added
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
                    ShipIcon icon = iconTransform.GetComponent<ShipIcon>();
                    if (icon != null) {
                        iconsToPick.Add(icon);
                    }
                }
            }
            else {
                for (int index = anchorIconIndex; index >= clickedIconIndex; index--) {
                    Transform iconTransform = _sortedElementIconTransforms[index];
                    ShipIcon icon = iconTransform.GetComponent<ShipIcon>();
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
                ShipIcon icon = iconTransform.GetComponent<ShipIcon>();
                if (icon != null) {
                    iconsToPick.Add(icon);
                }
            }
        }

        iconsToPick.ForAll(icon => {
            icon.IsPicked = true;
            _pickedElementIcons.Add(icon);
        });
        AssessInteractableHud();
        AssessRegularButtons();
    }

    private void HandleElementIconMiddleClicked(ShipIcon icon) {
        icon.Element.IsFocus = true; ;
    }

    private void PickSingleElementIcon(ShipIcon icon) {
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

    #endregion

    #region Regular Buttons

    private void HandleUnitMergeButtonClicked() {
        D.Warn("{0}.HandleUnitMergeButtonClicked not yet implemented.", DebugName);
        // UNDONE
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

    private void HandleCreateUnitButtonClicked() {  // IMPROVE ability to create a fleet of more than one ship
        var newUnit = _pickedElementIcons.Select(icon => icon.Element).Single().__CreateSingleShipFleet();
        var units = new List<FleetCmdItem>(_unitIconLookup.Keys);
        units.Add(newUnit);
        RebuildUnitIcons(units, newUnit);
    }

    private void HandleElementScuttleButtonClicked() {
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

    private void AssessRegularButtons() {
        bool isUnitMergeButtonEnabled = false;
        if (_pickedUnitIcons.Count > Constants.One) {
            IEnumerable<FleetCmdItem> pickedUnits = _pickedUnitIcons.Select(icon => icon.Unit);
            int pickedUnitsElementCount = pickedUnits.Sum(unit => unit.Elements.Count);
            if (pickedUnitsElementCount <= TempGameValues.MaxShipsPerFleet) {
                isUnitMergeButtonEnabled = true;
            }
        }
        _unitMergeButton.isEnabled = isUnitMergeButtonEnabled;

        _unitScuttleButton.isEnabled = _pickedUnitIcons.Count == Constants.One;

        bool isCreateUnitButtonEnabled = false;  // IMPROVE when fleet can be created with more than one ship 
        if (_pickedElementIcons.Count == Constants.One) {
            var elementCmd = _pickedElementIcons.Select(icon => icon.Element).First().Command;
            isCreateUnitButtonEnabled = elementCmd.Elements.Count > Constants.One;    // Criteria: 1 picked element in fleet of > 1 element
        }
        _createUnitButton.isEnabled = isCreateUnitButtonEnabled;

        _elementScuttleButton.isEnabled = _pickedElementIcons.Any();
    }

    #endregion

    #region Toggle Buttons

    private void HandleUnitGuardButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedUnitIcons.Count);
        var pickedUnit = _pickedUnitIcons.First().Unit;
        bool isButtonToggledIn = _unitGuardButton.IsToggledIn;
        if (isButtonToggledIn) {
            IGuardable closestItemAllowedToGuard;
            if (_gameMgr.UserAIManager.TryFindClosestGuardableItem(pickedUnit.Position, out closestItemAllowedToGuard)) {
                FleetOrder guardOrder = new FleetOrder(FleetDirective.Guard, OrderSource.User, closestItemAllowedToGuard as IFleetNavigableDestination);
                pickedUnit.InitiateNewOrder(guardOrder);
                D.Log("{0} is issuing an order to {1} to Guard {2}.", DebugName, pickedUnit.DebugName, closestItemAllowedToGuard.DebugName);
                _unitGuardButton.SetIconColor(TempGameValues.SelectedColor);
                AssessToggleButtons();
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

    private void HandleUnitPatrolButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedUnitIcons.Count);
        var pickedUnit = _pickedUnitIcons.First().Unit;
        bool isButtonToggledIn = _unitPatrolButton.IsToggledIn;
        if (isButtonToggledIn) {
            IPatrollable closestItemAllowedToPatrol;
            if (_gameMgr.UserAIManager.TryFindClosestPatrollableItem(pickedUnit.Position, out closestItemAllowedToPatrol)) {
                FleetOrder patrolOrder = new FleetOrder(FleetDirective.Patrol, OrderSource.User, closestItemAllowedToPatrol as IFleetNavigableDestination);
                pickedUnit.InitiateNewOrder(patrolOrder);
                D.Log("{0} is issuing an order to {1} to Patrol {2}.", DebugName, pickedUnit.DebugName, closestItemAllowedToPatrol.DebugName);
                _unitPatrolButton.SetIconColor(TempGameValues.SelectedColor);
                AssessToggleButtons();
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

    private void HandleUnitRepairButtonToggleChanged() {
        D.AssertEqual(Constants.One, _pickedUnitIcons.Count);
        var pickedUnit = _pickedUnitIcons.First().Unit;
        bool isButtonToggledIn = _unitRepairButton.IsToggledIn;
        if (isButtonToggledIn) {
            IUnitBaseCmd_Ltd closestRepairBase;
            if (_gameMgr.UserAIManager.TryFindClosestFleetRepairBase(pickedUnit.Position, out closestRepairBase)) {
                FleetOrder repairOrder = new FleetOrder(FleetDirective.Repair, OrderSource.User, closestRepairBase as IFleetNavigableDestination);
                pickedUnit.InitiateNewOrder(repairOrder);
                D.Log("{0} is issuing an order to {1} to Repair at {2}.", DebugName, pickedUnit.DebugName, closestRepairBase.DebugName);
                _unitRepairButton.SetIconColor(TempGameValues.SelectedColor);
                AssessToggleButtons();
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

    private void HandleUnitRefitButtonToggleChanged() {
        D.Warn("{0}.HandleUnitRefitButtonToggleChanged not yet implemented.", DebugName);
        _unitRefitButton.SetToggledState(false); // TEMP release the button
        // UNDONE
    }

    private void HandleUnitDisbandButtonToggleChanged() {
        D.Warn("{0}.HandleUnitDisbandButtonToggleChanged not yet implemented.", DebugName);
        _unitDisbandButton.SetToggledState(false); // TEMP release the button
        // UNDONE
    }

    private void HandleUnitExploreButtonToggleChanged() {
        D.Warn("{0}.HandleUnitExploreButtonToggleChanged not yet implemented.", DebugName);
        _unitExploreButton.SetToggledState(false); // TEMP release the button
        // UNDONE
    }

    private void AssessToggleButtons() {
        if (_pickedUnitIcons.Count > 1) {
            // if more than 1 picked unit, un-toggle without notify and disable all
            ResetUnitOrderToggleButtons();
        }
        else {
            D.AssertEqual(Constants.One, _pickedUnitIcons.Count);
            // if 1 picked, set toggle state without notify and enable all. If order initiated, it will find closest item to execute on
            var pickedUnit = _pickedUnitIcons.First().Unit;

            bool isOrderedToGuard = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Guard);
            GameColor iconColor = isOrderedToGuard ? TempGameValues.SelectedColor : GameColor.White;
            _unitGuardButton.SetToggledState(isOrderedToGuard, iconColor);
            _unitGuardButton.IsEnabled = true;

            bool isOrderedToPatrol = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Patrol);
            iconColor = isOrderedToPatrol ? TempGameValues.SelectedColor : GameColor.White;
            _unitPatrolButton.SetToggledState(isOrderedToPatrol, iconColor);
            _unitPatrolButton.IsEnabled = true;

            bool isOrderedToRepair = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Repair);
            iconColor = isOrderedToRepair ? TempGameValues.SelectedColor : GameColor.White;
            _unitRepairButton.SetToggledState(isOrderedToRepair, iconColor);
            _unitRepairButton.IsEnabled = pickedUnit.Data.UnitHealth < Constants.OneHundredPercent || pickedUnit.Data.Health < Constants.OneHundredPercent;

            bool isOrderedToRefit = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Refit);
            iconColor = isOrderedToRefit ? TempGameValues.SelectedColor : GameColor.White;
            _unitRefitButton.SetToggledState(isOrderedToRefit, iconColor);
            _unitRefitButton.IsEnabled = true;  // IMPROVE only if upgrade is available

            bool isOrderedToDisband = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Disband);
            iconColor = isOrderedToDisband ? TempGameValues.SelectedColor : GameColor.White;
            _unitDisbandButton.SetToggledState(isOrderedToDisband, iconColor);
            _unitDisbandButton.IsEnabled = true;

            bool isOrderedToExplore = pickedUnit.IsCurrentOrderDirectiveAnyOf(FleetDirective.Explore);
            iconColor = isOrderedToExplore ? TempGameValues.SelectedColor : GameColor.White;
            _unitExploreButton.SetToggledState(isOrderedToExplore, iconColor);
            _unitExploreButton.IsEnabled = true;
        }
    }

    #endregion

    private void ShowElementsOfPickedUnitIcons() {
        BuildElementIcons();
    }

    private void RebuildUnitIcons(IList<FleetCmdItem> units, FleetCmdItem unitToPick) {
        BuildUnitIcons(units, unitToPick);
    }

    private void BuildUnitIcons(IEnumerable<FleetCmdItem> units, FleetCmdItem unitToPick) {
        RemoveUnitIcons();

        AGuiIcon.IconSize iconSize = AGuiIcon.IconSize.Large;

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
    private void BuildElementIcons() {
        RemoveElementIcons();

        var gridContainerSize = _elementIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerSize.x, (int)gridContainerSize.y);

        IList<IList<ShipItem>> unitElementLists = GetElementsOfPickedUnitIcons();
        int desiredGridCells = unitElementLists.Sum(list => list.Count);
        desiredGridCells += unitElementLists.Count * 4; // add max potential for blanks // IMPROVE may be more columns than 4

        int gridColumns, unusedGridRows;
        AGuiIcon.IconSize iconSize = AGuiIcon.DetermineGridIconSize(gridContainerDimensions, desiredGridCells, _elementIconPrefab,
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
            var operationalElements = unitIcon.Unit.Elements.Where(e => e.IsOperational).Cast<ShipItem>();
            IList<ShipItem> elements = new List<ShipItem>(operationalElements);
            unitElementLists.Add(elements);
        }
        return unitElementLists;
    }

    private void CreateAndAddIcon(FleetCmdItem unit, AGuiIcon.IconSize iconSize) {
        GameObject unitIconGo = NGUITools.AddChild(_unitIconsGrid.gameObject, _unitIconPrefab.gameObject);
        unitIconGo.name = unit.Name + UnitIconExtension;
        FleetIcon unitIcon = unitIconGo.GetSafeComponent<FleetIcon>();
        unitIcon.Size = iconSize;
        unitIcon.Unit = unit;

        UIEventListener.Get(unitIconGo).onClick += UnitIconClickedEventHandler;
        unit.deathOneShot += UnitDeathEventHandler;
        //unit.ownerChanged += UnitOwnerChangedEventHandler;
        _unitIconLookup.Add(unit, unitIcon);
        _sortedUnitIconTransforms.Add(unitIconGo.transform);
    }

    private void CreateAndAddIcon(ShipItem element, AGuiIcon.IconSize iconSize) {
        GameObject elementIconGo = NGUITools.AddChild(_elementIconsGrid.gameObject, _elementIconPrefab.gameObject);
        elementIconGo.name = element.Name + ElementIconExtension;
        ShipIcon elementIcon = elementIconGo.GetSafeComponent<ShipIcon>();
        elementIcon.Size = iconSize;
        elementIcon.Element = element;

        UIEventListener.Get(elementIconGo).onClick += ElementIconClickedEventHandler;
        element.deathOneShot += ElementDeathEventHandler;
        //element.ownerChanged += ElementOwnerChangedEventHandler;
        _elementIconLookup.Add(element, elementIcon);
        _sortedElementIconTransforms.Add(elementIconGo.transform);
    }

    private void RemoveUnitIcons() {
        IList<Transform> iconTransforms = _unitIconsGrid.GetChildList();
        if (iconTransforms.Any()) {
            foreach (var it in iconTransforms) {
                var icon = it.GetComponent<FleetIcon>();
                RemoveIcon(icon);
            }
        }
    }

    private void RemoveElementIcons() {
        IList<Transform> iconTransforms = _elementIconsGrid.GetChildList();
        if (iconTransforms.Any()) {
            foreach (var it in iconTransforms) {
                var icon = it.GetComponent<ShipIcon>();
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

    private void RemoveIcon(FleetIcon icon) {
        _pickedUnitIcons.Remove(icon);  // may not be present
        if (icon.IsInitialized) {
            D.AssertNotNull(icon.Unit, "{0}: {1}'s Unit has been destroyed.".Inject(DebugName, icon.DebugName));
            FleetIcon unitIcon;
            bool isIconFound = _unitIconLookup.TryGetValue(icon.Unit, out unitIcon);
            D.Assert(isIconFound);
            D.AssertEqual(icon, unitIcon);

            icon.Unit.deathOneShot -= UnitDeathEventHandler;
            //icon.Unit.ownerChanged -= UnitOwnerChangedEventHandler;
            bool isRemoved = _unitIconLookup.Remove(icon.Unit);
            D.Assert(isRemoved);
            isRemoved = _sortedUnitIconTransforms.Remove(icon.transform);
            D.Assert(isRemoved);
            D.Log("{0} is removing {1}.", DebugName, icon.DebugName);
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

    private void RemoveIcon(ShipIcon icon) {
        _pickedElementIcons.Remove(icon);   // may not be present
        if (icon.IsInitialized) {
            D.AssertNotNull(icon.Element, "{0}: {1}'s Element has been destroyed.".Inject(DebugName, icon.DebugName));
            ShipIcon elementIcon;
            bool isIconFound = _elementIconLookup.TryGetValue(icon.Element, out elementIcon);
            D.Assert(isIconFound);
            D.AssertEqual(icon, elementIcon);

            icon.Element.deathOneShot -= ElementDeathEventHandler;
            //icon.Element.ownerChanged -= ElementOwnerChangedEventHandler;
            bool isRemoved = _elementIconLookup.Remove(icon.Element);
            D.Assert(isRemoved);
            D.Log("{0} has removed the icon for {1}. Remaining icons: {2}.", DebugName, icon.Element.DebugName, _elementIconLookup.Keys.Concatenate());
            isRemoved = _sortedElementIconTransforms.Remove(icon.transform);
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
        ShipIcon unusedElementIcon;
        bool isElementFound = _elementIconLookup.TryGetValue(element, out unusedElementIcon);
        D.Assert(isElementFound);   // element's icon must be showing to have scuttled

        BuildElementIcons();    // Rebuild from scratch as may need more blanks        
    }

    [Obsolete("Owner changes don't occur while paused")]
    private void HandleOwnerChanged(FleetCmdItem unit) {
        if (_unitIconLookup.Count == Constants.One) {
            // if the only unitIcon, it will be the selectedItem which will no longer be selected, closing the HUD window
            D.AssertEqual(SelectedUnit, unit);
            return;
        }
        var units = new List<FleetCmdItem>(_unitIconLookup.Keys);
        units.Remove(unit);
        RebuildUnitIcons(units, SelectedUnit);
    }

    [Obsolete("Owner changes don't occur while paused")]
    private void HandleOwnerChanged(ShipItem element) {
        ShipIcon elementIcon;
        if (_elementIconLookup.TryGetValue(element, out elementIcon)) {
            // element has an icon showing
            D.AssertEqual(element, elementIcon.Element);
            BuildElementIcons();    // Rebuild from scratch as may need more blanks
        }
    }

    private void AssessInteractableHud() {
        if (_pickedElementIcons.Count == Constants.One) {
            InteractableHudWindow.Instance.Show(FormID.UserShip, _pickedElementIcons.First().Element.Data);
        }
        else if (_pickedUnitIcons.Count == Constants.One) {
            InteractableHudWindow.Instance.Show(FormID.UserFleet, _pickedUnitIcons.First().Unit.Data);
        }
        else {
            InteractableHudWindow.Instance.Hide();
        }
    }

    private void AssessButtons() {
        AssessRegularButtons();
        AssessToggleButtons();
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
        RemoveElementIcons();
        D.AssertEqual(Constants.Zero, _pickedElementIcons.Count, _pickedElementIcons.Concatenate());
        D.AssertEqual(Constants.Zero, _elementIconLookup.Count, _elementIconLookup.Keys.Concatenate());
        D.AssertEqual(Constants.Zero, _sortedElementIconTransforms.Count, _sortedElementIconTransforms.Concatenate());

        RemoveUnitIcons();
        D.AssertEqual(Constants.Zero, _pickedUnitIcons.Count, _pickedUnitIcons.Concatenate());
        D.AssertEqual(Constants.Zero, _unitIconLookup.Count, _unitIconLookup.Keys.Concatenate());
        D.AssertEqual(Constants.Zero, _sortedUnitIconTransforms.Count, _sortedUnitIconTransforms.Concatenate());

        ResetUnitOrderToggleButtons();
        _selectedUnit = null;

        AssessInteractableHud();
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
        EventDelegate.Remove(_unitMergeButton.onClick, UnitMergeButtonClickedEventHandler);
        EventDelegate.Remove(_unitScuttleButton.onClick, UnitScuttleButtonClickedEventHandler);
        EventDelegate.Remove(_createUnitButton.onClick, CreateUnitButtonClickedEventHandler);
        EventDelegate.Remove(_elementScuttleButton.onClick, ElementScuttleButtomClickedEventHandler);

        _unitGuardButton.toggleStateChanged -= UnitGuardButtonToggleChangedEventHandler;
        _unitPatrolButton.toggleStateChanged -= UnitPatrolButtonToggleChangedEventHandler;
        _unitRepairButton.toggleStateChanged -= UnitRepairButtonToggleChangedEventHandler;
        _unitRefitButton.toggleStateChanged -= UnitRefitButtonToggleChangedEventHandler;
        _unitDisbandButton.toggleStateChanged -= UnitDisbandButtonToggleChangedEventHandler;
        _unitExploreButton.toggleStateChanged -= UnitExploreButtonToggleChangedEventHandler;
    }

    protected override void Cleanup() {
        RemoveElementIcons();
        RemoveUnitIcons();
        DisconnectButtonEventHandlers();
    }

    #region Debug

    private IEnumerable<FleetCmdItem> __AcquireLocalUnits() {   // IMPROVE Units can migrate apart while the HUD is showing
        var aiMgr = GameManager.Instance.UserAIManager;
        float localRange = 100F;
        IEnumerable<IFleetCmd> userFleets;
        if (aiMgr.TryFindMyCloseItems<IFleetCmd>(SelectedUnit.Position, localRange, out userFleets, SelectedUnit)) {
            //D.Log("{0} found {1} local User Fleet(s) within range of {2:0.}. Fleets: {3}.",
            // DebugName, userFleets.Count(), localRange, userFleets.Select(f => f.DebugName).Concatenate());
            return userFleets.Cast<FleetCmdItem>();
        }
        return Enumerable.Empty<FleetCmdItem>();
    }

    protected override void __Validate() {
        base.__Validate();
        D.AssertNotNull(_unitIconPrefab);
        D.AssertNotNull(_elementIconPrefab);

        D.AssertNotNull(_unitMergeButton);
        D.AssertNotNull(_unitScuttleButton);
        D.AssertNotNull(_createUnitButton);
        D.AssertNotNull(_elementScuttleButton);

        D.AssertNotNull(_unitGuardButton);
        D.AssertNotNull(_unitPatrolButton);
        D.AssertNotNull(_unitRepairButton);
        D.AssertNotNull(_unitRefitButton);
        D.AssertNotNull(_unitDisbandButton);
        D.AssertNotNull(_unitExploreButton);
    }

    #endregion

}

