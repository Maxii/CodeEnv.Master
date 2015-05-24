// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetsScreenManager.cs
// Manager for the Fleets Screen. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Manager for the Fleets Screen. 
/// </summary>
public class FleetsScreenManager : AMonoBase {

    private static string _rowNameExtension = " Row";
    private static string _unknown = Constants.QuestionMark;
    private static string _speedFormat = Constants.FormatFloat_1DpMax;

    public GameObject rowPrefab;

    private SortDirection _sortDirection;
    private SortDirection _lastSortDirection;
    private SortTopic _lastSortTopic;

    private UITable _table;
    private IDictionary<GameObject, IFleetCmdItem> _cmdLookup;

    protected override void Awake() {
        base.Awake();
        Arguments.ValidateNotNull(rowPrefab);
        _table = gameObject.GetSafeMonoBehaviourInChildren<UITable>();
        _table.sorting = UITable.Sorting.Custom;
        _cmdLookup = new Dictionary<GameObject, IFleetCmdItem>();
    }

    /// <summary>
    /// Build a new table sorted by Name.
    /// </summary>
    public void BuildTable() {
        //D.Log("{0}.PopulateTable() called.", GetType().Name);
        ResetSortDirectionState();
        ClearTable();
        AddTableRows();
        _table.onCustomSort = CompareName;
        _table.repositionNow = true;
    }

    private void CloseScreenAndFocusOnItem(GameObject aRowElement) {
        GuiManager.Instance.ClickButton(GuiElementID.FleetScreenDoneButton);

        GameObject row = aRowElement.transform.parent.gameObject;

        IFleetCmdItem cmd = _cmdLookup[row];
        (cmd as ICameraFocusable).IsFocus = true;
    }

    private void ClearTable() {
        var existingRows = _table.GetChildList();
        existingRows.ForAll(r => Destroy(r.gameObject));
    }

    private void ResetSortDirectionState() {
        _sortDirection = SortDirection.Descending;
        _lastSortTopic = SortTopic.None;
        _lastSortDirection = SortDirection.Descending;
    }

    private void AddTableRows() {
        _cmdLookup.Clear();
        var fCmds = GameManager.Instance.GetUserPlayerKnowledge().Fleets;
        fCmds.ForAll(cmd => {
            GameObject row = BuildRow(cmd.DisplayName);
            PopulateRowElementsWithValue(row, cmd);
            _cmdLookup.Add(row, cmd);
        });
    }

    private GameObject BuildRow(string fleetName) {
        GameObject row = NGUITools.AddChild(_table.gameObject, rowPrefab);
        row.name = fleetName + _rowNameExtension;
        // Note: Can't anchor as UIScrollVIew.Movement = Unrestricted which means the row needs to be able to move both in x and y
        return row;
    }

    private void PopulateRowElementsWithValue(GameObject row, IFleetCmdItem cmd) {
        FleetReport fleetReport = cmd.GetUserReport();
        IIconInfo fleetIconInfo = cmd.IconInfo;
        D.Assert(fleetIconInfo != null);    // a fleet we are aware of should never have a null iconInfo
        var rowElements = row.GetSafeMonoBehavioursInImmediateChildren<GuiElement>();

        rowElements.ForAll(e => {
            switch (e.elementID) {
                case GuiElementID.None:
                    // do nothing as element will warn and correct
                    break;
                case GuiElementID.NameLabel:
                    ConfigureNameElement(e, fleetReport);
                    break;
                case GuiElementID.Owner:
                    ConfigureOwnerElement(e, fleetReport);
                    break;
                case GuiElementID.Location:
                    ConfigureLocationElement(e, fleetReport);
                    break;
                case GuiElementID.Hero:
                    ConfigureHeroElement(e, fleetReport);
                    break;
                case GuiElementID.Size:
                    ConfigureSizeElement(e, fleetReport, fleetIconInfo);
                    break;
                case GuiElementID.Health:
                    ConfigureHealthElement(e, fleetReport);
                    break;
                case GuiElementID.SpeedLabel:
                    ConfigureMaxSpeedElement(e, fleetReport);
                    break;
                case GuiElementID.OffensiveStrength:
                    ConfigureOffensiveStrengthElement(e, fleetReport);
                    break;
                case GuiElementID.DefensiveStrength:
                    ConfigureDefensiveStrengthElement(e, fleetReport);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(e.elementID));
            }
        });
    }

    private void ConfigureNameElement(GuiElement element, FleetReport report) {
        var nameLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        nameLabel.text = report.ParentName != null ? report.ParentName : _unknown;
        UIEventListener.Get(element.gameObject).onDoubleClick += CloseScreenAndFocusOnItem; // OPTIMIZE Cleanup?
    }

    private void ConfigureOwnerElement(GuiElement element, FleetReport report) {
        var ownerElement = element as OwnerGuiElement;
        ownerElement.Owner = report.Owner;
    }

    private void ConfigureLocationElement(GuiElement element, FleetReport report) {
        var locationElement = element as GuiLocationElement;
        locationElement.SetValues(report.SectorIndex, report.__Position);
    }

    private void ConfigureHeroElement(GuiElement element, FleetReport report) {
        var heroElement = element as HeroGuiElement;
        heroElement.__HeroName = "None";    // = report.Hero;
    }

    private void ConfigureSizeElement(GuiElement element, FleetReport report, IIconInfo iconInfo) {
        var sizeElement = element as GuiSizeElement;
        sizeElement.SetValues(iconInfo, report.Category);
    }

    private void ConfigureHealthElement(GuiElement element, FleetReport report) {
        var healthElement = element as HealthGuiElement;
        healthElement.SetValues(report.UnitHealth, report.UnitCurrentHitPoints, report.UnitMaxHitPoints);
    }

    private void ConfigureMaxSpeedElement(GuiElement element, FleetReport report) {
        var speedLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        speedLabel.text = report.UnitFullSpeed.HasValue ? _speedFormat.Inject(report.UnitFullSpeed.Value) : _unknown;
    }

    private void ConfigureOffensiveStrengthElement(GuiElement element, FleetReport report) {
        var strengthElement = element as GuiStrengthElement;
        strengthElement.OffensiveStrength = report.UnitOffensiveStrength;
    }

    private void ConfigureDefensiveStrengthElement(GuiElement element, FleetReport report) {
        var strengthElement = element as GuiStrengthElement;
        strengthElement.DefensiveStrength = report.UnitDefensiveStrength;
    }

    #region Sorting

    public void SortOnName() {
        //D.Log("{0}.SortOnName() called.", GetType().Name);
        _table.onCustomSort = CompareName;
        _sortDirection = DetermineSortDirection(SortTopic.Name);
        _table.repositionNow = true;
    }

    public void SortOnOwner() {
        _table.onCustomSort = CompareOwner;
        _sortDirection = DetermineSortDirection(SortTopic.Owner);
        _table.repositionNow = true;
    }

    public void SortOnLocation() {
        _table.onCustomSort = CompareLocation;
        _sortDirection = DetermineSortDirection(SortTopic.Location);
        _table.repositionNow = true;
    }

    public void SortOnHero() {
        _table.onCustomSort = CompareHero;
        _sortDirection = DetermineSortDirection(SortTopic.Hero);
        _table.repositionNow = true;
    }

    public void SortOnSize() {
        _table.onCustomSort = CompareSize;
        _sortDirection = DetermineSortDirection(SortTopic.Size);
        _table.repositionNow = true;
    }

    public void SortOnHealth() {
        _table.onCustomSort = CompareHealth;
        _sortDirection = DetermineSortDirection(SortTopic.Health);
        _table.repositionNow = true;
    }

    public void SortOnSpeed() {
        _table.onCustomSort = CompareSpeed;
        _sortDirection = DetermineSortDirection(SortTopic.Speed);
        _table.repositionNow = true;
    }

    public void SortOnComposition() {
        _table.onCustomSort = CompareComposition;
        _sortDirection = DetermineSortDirection(SortTopic.Composition);
        _table.repositionNow = true;
    }

    public void SortOnDefensiveStrength() {
        _table.onCustomSort = CompareDefensiveStrength;
        _sortDirection = DetermineSortDirection(SortTopic.DefensiveStrength);
        _table.repositionNow = true;
    }

    public void SortOnOffensiveStrength() {
        _table.onCustomSort = CompareOffensiveStrength;
        _sortDirection = DetermineSortDirection(SortTopic.OffensiveStrength);
        _table.repositionNow = true;
    }

    public void SortOnTotalStrength() {
        _table.onCustomSort = CompareTotalStrength;
        _sortDirection = DetermineSortDirection(SortTopic.TotalStrength);
        _table.repositionNow = true;
    }

    // Note: These comparison algorithms will be called multiple times when doing the sort so _sortDirection must be setup outside before sorting starts

    protected int CompareName(Transform rowA, Transform rowB) {
        //D.Log("{0}.CompareName() called.", GetType().Name);
        _lastSortTopic = SortTopic.Name;
        var rowANameLabel = GetLabel(rowA, GuiElementID.NameLabel);
        var rowBNameLabel = GetLabel(rowB, GuiElementID.NameLabel);
        return (int)_sortDirection * rowANameLabel.text.CompareTo(rowBNameLabel.text);
    }

    protected int CompareOwner(Transform rowA, Transform rowB) {
        _lastSortTopic = SortTopic.Owner;
        var rowAOwnerElement = GetGuiElement(rowA, GuiElementID.Owner) as OwnerGuiElement;
        var rowBOwnerElement = GetGuiElement(rowB, GuiElementID.Owner) as OwnerGuiElement;
        return (int)_sortDirection * rowAOwnerElement.CompareTo(rowBOwnerElement);
    }

    protected int CompareLocation(Transform rowA, Transform rowB) {
        _lastSortTopic = SortTopic.Location;
        var rowALocationElement = GetGuiElement(rowA, GuiElementID.Location) as GuiLocationElement;
        var rowBLocationElement = GetGuiElement(rowB, GuiElementID.Location) as GuiLocationElement;
        return (int)_sortDirection * rowALocationElement.CompareTo(rowBLocationElement);
    }

    protected int CompareHero(Transform rowA, Transform rowB) {
        _lastSortTopic = SortTopic.Hero;
        var rowAHeroElement = GetGuiElement(rowA, GuiElementID.Hero) as HeroGuiElement;
        var rowBHeroElement = GetGuiElement(rowB, GuiElementID.Hero) as HeroGuiElement;
        return (int)_sortDirection * rowAHeroElement.CompareTo(rowBHeroElement);
    }

    protected int CompareSize(Transform rowA, Transform rowB) {
        _lastSortTopic = SortTopic.Size;
        var rowASizeElement = GetGuiElement(rowA, GuiElementID.Size) as GuiSizeElement;
        var rowBSizeElement = GetGuiElement(rowB, GuiElementID.Size) as GuiSizeElement;
        return (int)_sortDirection * rowASizeElement.CompareTo(rowBSizeElement);
    }

    protected int CompareHealth(Transform rowA, Transform rowB) {
        _lastSortTopic = SortTopic.Health;
        var rowAHealthElement = GetGuiElement(rowA, GuiElementID.Health) as HealthGuiElement;
        var rowBHealthElement = GetGuiElement(rowB, GuiElementID.Health) as HealthGuiElement;
        return (int)_sortDirection * rowAHealthElement.CompareTo(rowBHealthElement);
    }

    protected int CompareSpeed(Transform rowA, Transform rowB) {
        _lastSortTopic = SortTopic.Speed;
        var rowASpeedLabel = GetLabel(rowA, GuiElementID.SpeedLabel);
        var rowBSpeedLabel = GetLabel(rowB, GuiElementID.SpeedLabel);
        return (int)_sortDirection * rowASpeedLabel.text.CompareTo(rowBSpeedLabel.text);
    }

    protected int CompareComposition(Transform rowA, Transform rowB) {
        _lastSortTopic = SortTopic.Composition;
        var rowACompositionLabel = GetLabel(rowA, GuiElementID.CompositionLabel);
        var rowBCompositionLabel = GetLabel(rowB, GuiElementID.CompositionLabel);
        return (int)_sortDirection * rowACompositionLabel.text.CompareTo(rowBCompositionLabel.text);
    }

    protected int CompareTotalStrength(Transform rowA, Transform rowB) {
        _lastSortTopic = SortTopic.TotalStrength;
        var rowAStrengthElement = GetGuiElement(rowA, GuiElementID.TotalStrength) as GuiStrengthElement;
        var rowBStrengthElement = GetGuiElement(rowB, GuiElementID.TotalStrength) as GuiStrengthElement;
        return CompareStrength(rowAStrengthElement, rowBStrengthElement);
    }

    protected int CompareOffensiveStrength(Transform rowA, Transform rowB) {
        _lastSortTopic = SortTopic.OffensiveStrength;
        var rowAStrengthElement = GetGuiElement(rowA, GuiElementID.OffensiveStrength) as GuiStrengthElement;
        var rowBStrengthElement = GetGuiElement(rowB, GuiElementID.OffensiveStrength) as GuiStrengthElement;
        return CompareStrength(rowAStrengthElement, rowBStrengthElement);
    }

    protected int CompareDefensiveStrength(Transform rowA, Transform rowB) {
        _lastSortTopic = SortTopic.DefensiveStrength;
        var rowAStrengthElement = GetGuiElement(rowA, GuiElementID.DefensiveStrength) as GuiStrengthElement;
        var rowBStrengthElement = GetGuiElement(rowB, GuiElementID.DefensiveStrength) as GuiStrengthElement;
        return CompareStrength(rowAStrengthElement, rowBStrengthElement);
    }

    private int CompareStrength(GuiStrengthElement rowAStrengthElement, GuiStrengthElement rowBStrengthElement) {
        return (int)_sortDirection * rowAStrengthElement.CompareTo(rowBStrengthElement);
    }

    private UILabel GetLabel(Transform row, GuiElementID elementID) {
        return GetGuiElement(row, elementID).gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
    }

    private GuiElement GetGuiElement(Transform row, GuiElementID elementID) {
        var rowElements = row.gameObject.GetSafeMonoBehavioursInImmediateChildren<GuiElement>();
        return rowElements.Single(e => e.gameObject.GetSafeMonoBehaviour<GuiElement>().elementID == elementID);
    }

    /// <summary>
    /// Assesses the sort direction needed and returns it. Records any 
    /// resulting sorting state changes in prep for the next query.
    /// </summary>
    /// <param name="sortTopic">The current sort topic.</param>
    /// <returns></returns>
    private SortDirection DetermineSortDirection(SortTopic sortTopic) {
        SortDirection sortDirection;
        if (_lastSortTopic != sortTopic) {
            // new sort topic so direction should be descending
            sortDirection = SortDirection.Descending;
            //_lastSortTopic = sortTopic;   // now set in Comparison method as the comparison method is called by UITable on Start
        }
        else {
            // same sort topic so direction should be the 'other' direction
            sortDirection = ToggleSortDirection();
        }
        _lastSortDirection = sortDirection;
        //D.Log("{0}.AssessSortDirection(Topic: {1}): Direction: {2}, Value: {3}.", GetType().Name, sortTopic.GetName(), sortDirection.GetName(), (int)sortDirection);
        return sortDirection;
    }

    private SortDirection ToggleSortDirection() {
        return _lastSortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
    }

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    public enum SortTopic {
        None,

        Name,
        Owner,
        Location,
        Hero,
        Size,
        Health,
        Speed,
        Composition,
        TotalStrength,
        OffensiveStrength,
        DefensiveStrength

    }

    public enum SortDirection {
        Descending = 1,
        Ascending = -1
    }

    #endregion

}

