﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ConstructionGuiModule.cs
// Gui module that allows management of a Base's Constructible Element Designs and Construction Queue.
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
/// Gui module that allows management of a Base's Constructible Element Designs and Construction Queue.
/// </summary>
public class ConstructionGuiModule : AMonoBase {

    private const string ConstructibleDesignIconExtension = " ConstructibleDesignIcon";
    private const string ConstructionQueueIconExtension = " ConstructionQueueIcon";

    [SerializeField]
    private DesignIconGuiElement _constructibleDesignIconPrefab = null;
    [SerializeField]
    private ConstructionQueueIconGuiElement _constructionQueueIconPrefab = null;

    [SerializeField]
    private MyNguiToggleButton _viewAllConstructibleDesignsButton = null;
    [SerializeField]
    private MyNguiToggleButton _viewFacilityConstructibleDesignsButton = null;
    [SerializeField]
    private MyNguiToggleButton _viewShipConstructibleDesignsButton = null;

    public string DebugName { get { return GetType().Name; } }

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
    private UIGrid _constructibleDesignIconsGrid;
    private UIGrid _constructionQueueIconsGrid;
    private GameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        __Validate();
    }

    private void InitializeValuesAndReferences() {
        //D.Log("{0} is initializing.", DebugName);
        _gameMgr = GameManager.Instance;

        _constructibleDesignIconsGrid = gameObject.GetSingleComponentInChildren<DesignIconGuiElement>().gameObject.GetSingleComponentInParents<UIGrid>();
        _constructibleDesignIconsGrid.arrangement = UIGrid.Arrangement.Horizontal;
        _constructibleDesignIconsGrid.sorting = UIGrid.Sorting.Custom;
        _constructibleDesignIconsGrid.onCustomSort = CompareConstructibleDesignIcons;

        _constructionQueueIconsGrid = gameObject.GetSingleComponentInChildren<ConstructionQueueIconGuiElement>().gameObject.GetSingleComponentInParents<UIGrid>();
        _constructionQueueIconsGrid.arrangement = UIGrid.Arrangement.Vertical;
        _constructionQueueIconsGrid.sorting = UIGrid.Sorting.Vertical;

        _sortedConstructibleDesignIconTransforms = new List<Transform>();

        _viewAllConstructibleDesignsButton.Initialize();
        _viewFacilityConstructibleDesignsButton.Initialize();
        _viewShipConstructibleDesignsButton.Initialize();

        ConnectButtonEventHandlers();
    }

    private void ConnectButtonEventHandlers() {
        _viewAllConstructibleDesignsButton.toggleStateChanged += ViewAllConstructibleDesignIconsButtonToggleChangedEventHandler;
        _viewFacilityConstructibleDesignsButton.toggleStateChanged += ViewConstructibleFacilityIconsButtonToggleChangedEventHandler;
        _viewShipConstructibleDesignsButton.toggleStateChanged += ViewConstructibleShipIconsButtonToggleChangedEventHandler;
    }

    private void AssignValuesToMembers() {
        _viewAllConstructibleDesignsButton.SetToggledState(toToggleIn: true, iconColor: TempGameValues.SelectedColor, toNotify: true);

        var unitConstructionQueue = _unitConstructionMgr.GetQueue();
        BuildConstructionQueueIcons(unitConstructionQueue);

        // Constructible Design View buttons don't need to be assessed
    }

    #region Event and Property Change Handlers

    private void SelectedUnitPropSetHandler() {
        D.Assert(_gameMgr.IsPaused);

        _unitConstructionMgr = SelectedUnit.ConstructionMgr;

        Subscribe();
        AssignValuesToMembers();
    }

    private void Subscribe() {
        _unitConstructionMgr.constructionQueueChanged += UnitConstructionQueueChangedEventHandler;
    }

    private void UnitConstructionQueueChangedEventHandler(object sender, EventArgs e) {
        D.AssertEqual(_unitConstructionMgr, sender as ConstructionManager);
        HandleUnitConstructionQueueChanged();
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

    private void ConstructibleDesignIconClickedEventHandler(GameObject go) {
        var inputHelper = GameInputHelper.Instance;
        DesignIconGuiElement iconClicked = go.GetComponent<DesignIconGuiElement>();
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
        ConstructionQueueIconGuiElement iconClicked = go.GetComponent<ConstructionQueueIconGuiElement>();
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

    private void ConstructionQueueIconBuyoutInitiatedEventHandler(object sender, EventArgs e) {
        ConstructionQueueIconGuiElement icon = sender as ConstructionQueueIconGuiElement;
        HandleConstructionQueueIconBuyoutInitiated(icon);
    }

    private void ConstructionQueueIconDragDropEndedEventHandler(object sender, EventArgs e) {
        HandleConstructionQueueIconDragDropEnded();
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

    private void HandleConstructibleDesignIconCntlLeftClicked(DesignIconGuiElement iconClicked) {
        _unitConstructionMgr.AddToQueue(iconClicked.Design as AUnitElementDesign);
    }

    private void HandleConstructibleDesignIconLeftClicked(DesignIconGuiElement iconClicked) {
        _unitConstructionMgr.AddToQueue(iconClicked.Design as AUnitElementDesign);
    }

    private void HandleConstructibleDesignIconShiftLeftClicked(DesignIconGuiElement iconClicked) {
        _unitConstructionMgr.AddToQueue(iconClicked.Design as AUnitElementDesign);
    }

    #endregion

    #region Construction Queue Icon Interaction

    private void HandleConstructionQueueIconLeftClicked(ConstructionQueueIconGuiElement clickedIcon) {
        // a construction icon that is clicked is already present in the queue so remove it
        _unitConstructionMgr.RemoveFromQueue(clickedIcon.Construction);
        // No buttons to assess
    }

    private void HandleConstructionQueueIconCntlLeftClicked(ConstructionQueueIconGuiElement clickedIcon) {
        // a construction icon that is clicked is already present in the queue so remove it
        _unitConstructionMgr.RemoveFromQueue(clickedIcon.Construction);
        // No buttons to assess
    }

    private void HandleConstructionQueueIconShiftLeftClicked(ConstructionQueueIconGuiElement clickedIcon) {
        // a construction icon that is clicked is already present in the queue so remove it
        _unitConstructionMgr.RemoveFromQueue(clickedIcon.Construction);
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

    private void HandleConstructionQueueIconBuyoutInitiated(ConstructionQueueIconGuiElement boughtIcon) {
        _unitConstructionMgr.PurchaseQueuedConstruction(boughtIcon.Construction);
    }

    private void HandleConstructionQueueIconDragDropEnded() {
        IList<ConstructionInfo> unitConstructionQueue = _unitConstructionMgr.GetQueue();

        var verticallySortedConstructionItemsInIcons = _constructionQueueIconsGrid.GetChildList()
            .Select(t => t.GetComponent<ConstructionQueueIconGuiElement>()).Select(icon => icon.Construction);
        if (!unitConstructionQueue.SequenceEqual(verticallySortedConstructionItemsInIcons)) {
            _unitConstructionMgr.RegenerateQueue(verticallySortedConstructionItemsInIcons.ToList());
        }
    }

    private void HandleUnitConstructionQueueChanged() {
        var changedConstructionQueue = _unitConstructionMgr.GetQueue();
        BuildConstructionQueueIcons(changedConstructionQueue);
    }

    #endregion

    private void BuildConstructibleDesignIcons(IEnumerable<AUnitElementDesign> constructibleDesigns) {
        RemoveConstructibleDesignIcons();

        var gridContainerSize = _constructibleDesignIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerSize.x, (int)gridContainerSize.y);

        int desiredGridCells = constructibleDesigns.Count();

        int gridColumns, unusedGridRows;
        AMultiSizeIconGuiElement.IconSize iconSize = AMultiSizeIconGuiElement.DetermineGridIconSize(gridContainerDimensions, desiredGridCells,
            _constructibleDesignIconPrefab, out unusedGridRows, out gridColumns);

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

    private void BuildConstructionQueueIcons(IEnumerable<ConstructionInfo> constructionQueue) {
        RemoveConstructionQueueIcons();

        var gridContainerSize = _constructionQueueIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerSize.x, (int)gridContainerSize.y);

        int desiredGridCells = constructionQueue.Count();

        int unusedGridColumns, unusedGridRows;
        AMultiSizeIconGuiElement.IconSize iconSize = AMultiSizeIconGuiElement.DetermineGridIconSize(gridContainerDimensions, desiredGridCells, _constructionQueueIconPrefab, out unusedGridRows, out unusedGridColumns);

        // configure grid for icon size
        IntVector2 iconDimensions = _constructionQueueIconPrefab.GetIconDimensions(iconSize);
        _constructionQueueIconsGrid.cellHeight = iconDimensions.y;
        _constructionQueueIconsGrid.cellWidth = iconDimensions.x;

        // make grid 1 column wide
        D.AssertEqual(UIGrid.Arrangement.Vertical, _constructionQueueIconsGrid.arrangement);
        _constructionQueueIconsGrid.maxPerLine = Constants.Zero; // one column, infinite rows

        int topToBottomVerticalOffset = Constants.Zero;
        foreach (var constructionItem in constructionQueue) {
            CreateAndAddIcon(constructionItem, iconSize, topToBottomVerticalOffset);
            topToBottomVerticalOffset--;
        }
        _constructionQueueIconsGrid.repositionNow = true;
    }

    private void CreateAndAddIcon(AUnitElementDesign design, AMultiSizeIconGuiElement.IconSize iconSize) {
        GameObject constructibleDesignIconGo = NGUITools.AddChild(_constructibleDesignIconsGrid.gameObject, _constructibleDesignIconPrefab.gameObject);
        constructibleDesignIconGo.name = design.DesignName + ConstructibleDesignIconExtension;
        DesignIconGuiElement constructableDesignIcon = constructibleDesignIconGo.GetSafeComponent<DesignIconGuiElement>();
        constructableDesignIcon.Size = iconSize;
        constructableDesignIcon.Design = design;

        UIEventListener.Get(constructibleDesignIconGo).onClick += ConstructibleDesignIconClickedEventHandler;
        _sortedConstructibleDesignIconTransforms.Add(constructibleDesignIconGo.transform);
    }

    /// <summary>
    /// Creates and adds a ConstructionQueueIconGuiElement to the ConstructionQueueIconsGrid and ScrollView.
    /// </summary>
    /// <param name="itemUnderConstruction">The item under construction.</param>
    /// <param name="iconSize">Size of the icon.</param>
    /// <param name="topToBottomVerticalOffset">The top to bottom vertical offset needed to vertically sort.</param>
    private void CreateAndAddIcon(ConstructionInfo itemUnderConstruction, AMultiSizeIconGuiElement.IconSize iconSize, int topToBottomVerticalOffset) {
        GameObject constructionQueueIconGo = NGUITools.AddChild(_constructionQueueIconsGrid.gameObject, _constructionQueueIconPrefab.gameObject);
        constructionQueueIconGo.name = itemUnderConstruction.Name + ConstructionQueueIconExtension;
        constructionQueueIconGo.transform.SetLocalPositionY(topToBottomVerticalOffset); // initial position set for proper vertical sort

        ConstructionQueueIconGuiElement constructionQueueIcon = constructionQueueIconGo.GetSafeComponent<ConstructionQueueIconGuiElement>();
        constructionQueueIcon.Size = iconSize;
        constructionQueueIcon.Construction = itemUnderConstruction;

        constructionQueueIcon.constructionBuyoutInitiated += ConstructionQueueIconBuyoutInitiatedEventHandler;
        constructionQueueIcon.dragDropEnded += ConstructionQueueIconDragDropEndedEventHandler;
        UIEventListener.Get(constructionQueueIconGo).onClick += ConstructionQueueIconClickedEventHandler;
    }

    private void RemoveConstructibleDesignIcons() {
        IList<Transform> iconTransforms = _constructibleDesignIconsGrid.GetChildList();
        foreach (var it in iconTransforms) {
            var icon = it.GetComponent<DesignIconGuiElement>();
            RemoveIcon(icon);
        }
    }

    private void RemoveConstructionQueueIcons() {
        IList<Transform> iconTransforms = _constructionQueueIconsGrid.GetChildList();
        foreach (var it in iconTransforms) {
            var icon = it.GetComponent<ConstructionQueueIconGuiElement>();
            RemoveIcon(icon);
        }
    }

    private void RemoveIcon(DesignIconGuiElement icon) {
        if (icon.Design != null) {  // no reason for this work if this is debug icon under grid
            bool isRemoved = _sortedConstructibleDesignIconTransforms.Remove(icon.transform);
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

    private void RemoveIcon(ConstructionQueueIconGuiElement icon) {
        if (icon.Construction != TempGameValues.NoConstruction) {   // no reason for this work if this is debug icon under grid
            icon.constructionBuyoutInitiated -= ConstructionQueueIconBuyoutInitiatedEventHandler;
            icon.dragDropEnded -= ConstructionQueueIconDragDropEndedEventHandler;
            UIEventListener.Get(icon.gameObject).onClick -= ConstructionQueueIconClickedEventHandler;
        }
        //D.Log("{0} is removing {1}.", DebugName, icon.DebugName);
        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(icon.gameObject);
    }

    private bool ShouldFacilityDesignsBeIncludedInView() {
        if (!SelectedUnit.IsJoinable) {
            return false;
        }
        int facilitiesUnderConstructionCount = _unitConstructionMgr.GetQueue().Select(c => c.Design).Where(design => design is FacilityDesign).Count();
        if (facilitiesUnderConstructionCount == Constants.Zero) {
            return true;
        }
        int instantiatedFacilityCount = SelectedUnit.Elements.Count();
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

    private int CompareConstructibleDesignIcons(Transform aIconTransform, Transform bIconTransform) {
        int aIndex = _sortedConstructibleDesignIconTransforms.IndexOf(aIconTransform);
        int bIndex = _sortedConstructibleDesignIconTransforms.IndexOf(bIconTransform);
        return aIndex.CompareTo(bIndex);
    }

    public void ResetForReuse() {
        RemoveConstructibleDesignIcons();
        D.AssertEqual(Constants.Zero, _sortedConstructibleDesignIconTransforms.Count);

        ResetViewConstructibleDesignsToggleButtons();

        RemoveConstructionQueueIcons();

        Unsubscribe();
        _selectedUnit = null;
        _unitConstructionMgr = null;
    }

    private void ResetViewConstructibleDesignsToggleButtons() {
        _viewAllConstructibleDesignsButton.SetToggledState(toToggleIn: false);
        _viewFacilityConstructibleDesignsButton.SetToggledState(toToggleIn: false);
        _viewShipConstructibleDesignsButton.SetToggledState(toToggleIn: false);
    }

    private void Unsubscribe() {
        if (_selectedUnit != null) {
            _unitConstructionMgr.constructionQueueChanged -= UnitConstructionQueueChangedEventHandler;
        }
    }

    private void DisconnectButtonEventHandlers() {
        _viewAllConstructibleDesignsButton.toggleStateChanged -= ViewAllConstructibleDesignIconsButtonToggleChangedEventHandler;
        _viewFacilityConstructibleDesignsButton.toggleStateChanged -= ViewConstructibleFacilityIconsButtonToggleChangedEventHandler;
        _viewShipConstructibleDesignsButton.toggleStateChanged -= ViewConstructibleFacilityIconsButtonToggleChangedEventHandler;
    }

    protected override void Cleanup() {
        RemoveConstructibleDesignIcons();
        RemoveConstructionQueueIcons();
        Unsubscribe();
        DisconnectButtonEventHandlers();
    }

    #region Debug

    public void __DisableButtons() {
        D.AssertNull(SelectedUnit);
        _viewAllConstructibleDesignsButton.IsEnabled = false;
        _viewFacilityConstructibleDesignsButton.IsEnabled = false;
        _viewShipConstructibleDesignsButton.IsEnabled = false;
    }

    private void __Validate() {
        D.AssertNotNull(_constructibleDesignIconPrefab);
        D.AssertNotNull(_constructionQueueIconPrefab);

        D.AssertNotNull(_viewAllConstructibleDesignsButton);
        D.AssertNotNull(_viewFacilityConstructibleDesignsButton);
        D.AssertNotNull(_viewShipConstructibleDesignsButton);
    }

    #endregion

}

