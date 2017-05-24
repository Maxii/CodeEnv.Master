// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ATableWindow.cs
// Abstract base class for all Table-based Windows.
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
using UnityEngine.Serialization;

/// <summary>
/// Abstract base class for all Table-based Windows.
/// OPTIMIZE Reuse rows rather than destroying them.
/// </summary>
public abstract class ATableWindow : AGuiWindow {

    protected static string _unknown = Constants.QuestionMark;
    private static string _rowNameExtension = " Row";

    public ATableRowForm rowPrefab; // Has Editor

    protected override Transform ContentHolder { get { return _contentHolder; } }

    protected SortDirection _sortDirection;
    protected UITable _table;

    private Transform _contentHolder;
    private GuiElementID _lastSortTopic;
    private SortDirection _lastSortDirection;
    private IList<ATableRowForm> _rowForms;

    protected override void Awake() {
        base.Awake();
        InitializeOnAwake();
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        Utility.ValidateNotNull(rowPrefab);
        InitializeContentHolder();
        Subscribe();
    }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _table = gameObject.GetSingleComponentInChildren<UITable>();
        _table.sorting = UITable.Sorting.Custom;
        _rowForms = new List<ATableRowForm>();
        _panel.widgetsAreStatic = true; // OPTIMIZE see http://www.tasharen.com/forum/index.php?topic=261.0
    }

    private void InitializeContentHolder() {
        _contentHolder = gameObject.GetSingleComponentInImmediateChildren<UISprite>().transform;    // background sprite
    }

    private void Subscribe() {
        EventDelegate.Add(onShowBegin, ShowBeginEventHandler);
        EventDelegate.Add(onHideComplete, HideCompleteEventHandler);
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// Event handler called when the GuiWindow begins showing.
    /// </summary>
    private void ShowBeginEventHandler() {
        BuildTable();
    }

    /// <summary>
    /// Event handler called when the GuiWindow completes hiding.
    /// </summary>
    private void HideCompleteEventHandler() {
        Reset();
    }

    private void ItemFocusUserActionEventHandler(object sender, ATableRowForm.TableRowFocusUserActionEventArgs e) {
        CloseScreenAndFocusOnItem(e.ItemToFocusOn);
    }

    #endregion

    /// <summary>
    /// Show the Window.
    /// </summary>
    public void Show() {
        ShowWindow();
    }

    /// <summary>
    /// Hide the Window.
    /// </summary>
    public void Hide() {
        HideWindow();
    }

    /// <summary>
    /// Build a new table sorted by Name.
    /// </summary>
    private void BuildTable() {
        //D.Log("{0}.BuildTable() called.", DebugName);
        ClearTable();   // OPTIMIZE Reqd to destroy the row already present. Can be removed once reuse of rows is implemented
        AddTableRows();
        _table.onCustomSort = CompareName;
        _table.repositionNow = true;
    }

    private void CloseScreenAndFocusOnItem(ICameraFocusable item) {
        GameObject doneButtonGo = gameObject.GetSingleComponentInChildren<InputModeControlButton>().gameObject;
        GameInputHelper.Instance.Notify(doneButtonGo, "OnClick");
        item.IsFocus = true;
    }

    private void ClearTable() {
        var existingRows = _table.GetChildList();
        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing rows before Reposition occurs on LateUpdate
        // This results in an extra 'empty' row that stays until another Reposition() call, usually from sorting something
        existingRows.ForAll(r => DestroyImmediate(r.gameObject));
    }

    private void AddTableRows() {
        IEnumerable<AItem> items = GetItemsUserIsAwareOf();
        items.ForAll((Action<AItem>)(item => {
            ATableRowForm rowForm = BuildRow((string)item.DebugName);
            ConfigureRow(rowForm, item);
            _rowForms.Add(rowForm);
        }));
    }

    /// <summary>
    /// The derived class returns the items (of the type for which it is responsible) that the user has knowledge of. 
    /// A table row will be built to show each item.
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<AItem> GetItemsUserIsAwareOf();

    private ATableRowForm BuildRow(string itemName) {
        GameObject rowGo = NGUITools.AddChild(_table.gameObject, rowPrefab.gameObject);
        rowGo.name = itemName + _rowNameExtension;
        // 5.24.17 FIXME: Can't anchor as UIScrollVIew.Movement = Unrestricted which means the row needs to be able to move both in x and y.
        // Probably need a minimum screen width based on info we will need to show so don't need horizontal scrollbar so can anchor ends
        return rowGo.GetSafeComponent<ATableRowForm>(); ;
    }

    private void ConfigureRow(ATableRowForm rowForm, AItem item) {
        rowForm.Report = GetUserReportFor(item);
        rowForm.itemFocusUserAction += ItemFocusUserActionEventHandler;
    }


    /// <summary>
    /// The derived class returns the UserReport for the provided item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns></returns>
    protected abstract AItemReport GetUserReportFor(AItem item);

    #region Sorting Elements

    // Note: To avoid cluttering the inspector with choices that are not applicable, these public sort methods 
    // are only made available to Table Screen Managers that use them

    public void SortOnName() {
        _table.onCustomSort = CompareName;
        _sortDirection = DetermineSortDirection(GuiElementID.ItemNameLabel);
        _table.repositionNow = true;
    }

    public void SortOnOwner() {
        _table.onCustomSort = CompareOwner;
        _sortDirection = DetermineSortDirection(GuiElementID.Owner);
        _table.repositionNow = true;
    }

    public void SortOnLocation() {
        _table.onCustomSort = CompareLocation;
        _sortDirection = DetermineSortDirection(GuiElementID.Location);
        _table.repositionNow = true;
    }

    #region Sort Support

    // Note 1: These comparison algorithms will be called multiple times when doing the sort so _sortDirection must be setup outside before sorting starts
    // Note 2: All Compare methods are located here. Only a subset are used by any particular Table Screen Manager

    private int CompareName(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.ItemNameLabel;
        var rowANameLabel = GetLabel(rowA, GuiElementID.ItemNameLabel);
        var rowBNameLabel = GetLabel(rowB, GuiElementID.ItemNameLabel);
        return (int)_sortDirection * rowANameLabel.text.CompareTo(rowBNameLabel.text);
    }

    private int CompareOwner(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.Owner;
        var rowAOwnerElement = GetGuiElement(rowA, GuiElementID.Owner) as OwnerGuiElement;
        var rowBOwnerElement = GetGuiElement(rowB, GuiElementID.Owner) as OwnerGuiElement;
        return (int)_sortDirection * rowAOwnerElement.CompareTo(rowBOwnerElement);
    }

    private int CompareLocation(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.Location;
        var rowALocationElement = GetGuiElement(rowA, GuiElementID.Location) as LocationGuiElement;
        var rowBLocationElement = GetGuiElement(rowB, GuiElementID.Location) as LocationGuiElement;
        return (int)_sortDirection * rowALocationElement.CompareTo(rowBLocationElement);
    }

    protected int CompareStrategicResources(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.Resources;
        var rowAResourcesElement = GetGuiElement(rowA, GuiElementID.Resources) as ResourcesGuiElement;
        var rowBResourcesElement = GetGuiElement(rowB, GuiElementID.Resources) as ResourcesGuiElement;
        return (int)_sortDirection * rowAResourcesElement.CompareTo(rowBResourcesElement);
    }

    protected int CompareOrganics(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.OrganicsLabel;
        var rowAOrganicsLabel = GetLabel(rowA, GuiElementID.OrganicsLabel);
        var rowBOrganicsLabel = GetLabel(rowB, GuiElementID.OrganicsLabel);
        return (int)_sortDirection * CompareEmbeddedFloat(rowAOrganicsLabel.text, rowBOrganicsLabel.text);
    }

    protected int CompareParticulates(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.ParticulatesLabel;
        var rowAParticulatesLabel = GetLabel(rowA, GuiElementID.ParticulatesLabel);
        var rowBParticulatesLabel = GetLabel(rowB, GuiElementID.ParticulatesLabel);
        return (int)_sortDirection * CompareEmbeddedFloat(rowAParticulatesLabel.text, rowBParticulatesLabel.text);
    }

    protected int CompareEnergy(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.EnergyLabel;
        var rowAEnergyLabel = GetLabel(rowA, GuiElementID.EnergyLabel);
        var rowBEnergyLabel = GetLabel(rowB, GuiElementID.EnergyLabel);
        return (int)_sortDirection * CompareEmbeddedFloat(rowAEnergyLabel.text, rowBEnergyLabel.text);
    }

    protected int CompareHero(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.Hero;
        var rowAHeroElement = GetGuiElement(rowA, GuiElementID.Hero) as HeroGuiElement;
        var rowBHeroElement = GetGuiElement(rowB, GuiElementID.Hero) as HeroGuiElement;
        return (int)_sortDirection * rowAHeroElement.CompareTo(rowBHeroElement);
    }

    protected int CompareComposition(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.Composition;
        var rowACompElement = GetGuiElement(rowA, GuiElementID.Composition) as ACompositionGuiElement;
        var rowBCompElement = GetGuiElement(rowB, GuiElementID.Composition) as ACompositionGuiElement;
        return (int)_sortDirection * rowACompElement.CompareTo(rowBCompElement);
    }

    protected int CompareHealth(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.Health;
        var rowAHealthElement = GetGuiElement(rowA, GuiElementID.Health) as HealthGuiElement;
        var rowBHealthElement = GetGuiElement(rowB, GuiElementID.Health) as HealthGuiElement;
        return (int)_sortDirection * rowAHealthElement.CompareTo(rowBHealthElement);
    }

    protected int CompareOffensiveStrength(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.OffensiveStrength;
        var rowAStrengthElement = GetGuiElement(rowA, GuiElementID.OffensiveStrength) as StrengthGuiElement;
        var rowBStrengthElement = GetGuiElement(rowB, GuiElementID.OffensiveStrength) as StrengthGuiElement;
        return (int)_sortDirection * rowAStrengthElement.CompareTo(rowBStrengthElement);
    }

    protected int CompareDefensiveStrength(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.DefensiveStrength;
        var rowAStrengthElement = GetGuiElement(rowA, GuiElementID.DefensiveStrength) as StrengthGuiElement;
        var rowBStrengthElement = GetGuiElement(rowB, GuiElementID.DefensiveStrength) as StrengthGuiElement;
        return (int)_sortDirection * rowAStrengthElement.CompareTo(rowBStrengthElement);
    }

    protected int CompareScience(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.ScienceLabel;
        var rowAScienceLabel = GetLabel(rowA, GuiElementID.ScienceLabel);
        var rowBScienceLabel = GetLabel(rowB, GuiElementID.ScienceLabel);
        return (int)_sortDirection * CompareEmbeddedFloat(rowAScienceLabel.text, rowBScienceLabel.text);
    }

    protected int CompareCulture(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.CultureLabel;
        var rowACultureLabel = GetLabel(rowA, GuiElementID.CultureLabel);
        var rowBCultureLabel = GetLabel(rowB, GuiElementID.CultureLabel);
        return (int)_sortDirection * CompareEmbeddedFloat(rowACultureLabel.text, rowBCultureLabel.text);
    }

    protected int CompareNetIncome(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.NetIncome;
        var rowANetIncomeElement = GetGuiElement(rowA, GuiElementID.NetIncome) as NetIncomeGuiElement;
        var rowBNetIncomeElement = GetGuiElement(rowB, GuiElementID.NetIncome) as NetIncomeGuiElement;
        return (int)_sortDirection * rowANetIncomeElement.CompareTo(rowBNetIncomeElement);
    }

    protected int ComparePopulation(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.PopulationLabel;
        var rowAPopulationLabel = GetLabel(rowA, GuiElementID.PopulationLabel);
        var rowBPopulationLabel = GetLabel(rowB, GuiElementID.PopulationLabel);
        return (int)_sortDirection * CompareEmbeddedInt(rowAPopulationLabel.text, rowBPopulationLabel.text);
    }

    protected int CompareApproval(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.Approval;
        var rowAApprovalElement = GetGuiElement(rowA, GuiElementID.Approval) as ApprovalGuiElement;
        var rowBApprovalElement = GetGuiElement(rowB, GuiElementID.Approval) as ApprovalGuiElement;
        return (int)_sortDirection * rowAApprovalElement.CompareTo(rowBApprovalElement);
    }

    protected int CompareSpeed(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.SpeedLabel;
        var rowASpeedLabel = GetLabel(rowA, GuiElementID.SpeedLabel);
        var rowBSpeedLabel = GetLabel(rowB, GuiElementID.SpeedLabel);
        return (int)_sortDirection * CompareEmbeddedFloat(rowASpeedLabel.text, rowBSpeedLabel.text);
    }

    private UILabel GetLabel(Transform row, GuiElementID elementID) {
        return GetGuiElement(row, elementID).gameObject.GetSingleComponentInChildren<UILabel>();
    }

    private AGuiElement GetGuiElement(Transform row, GuiElementID elementID) {
        var rowElements = row.gameObject.GetSafeComponentsInImmediateChildren<AGuiElement>();
        return rowElements.Single(e => e.ElementID == elementID);
    }

    /// <summary>
    /// Assesses the sort direction needed and returns it. Records any 
    /// resulting sorting state changes in prep for the next query.
    /// </summary>
    /// <param name="sortTopic">The current sort topic.</param>
    /// <returns></returns>
    protected SortDirection DetermineSortDirection(GuiElementID sortTopic) {
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
        //D.Log("{0}.AssessSortDirection(Topic: {1}): Direction: {2}, Value: {3}.", DebugName, sortTopic.GetValueName(), sortDirection.GetValueName(), (int)sortDirection);
        return sortDirection;
    }

    private SortDirection ToggleSortDirection() {
        return _lastSortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
    }

    /// <summary>
    /// Compares the int embedded in the text from the RowALabel to the text from the RowBLabel
    /// and returns -1, 0 or +1 depending on the parsing result.
    /// If either text cannot be parsed to an int it means the content of the text is '?' and the result
    /// returned will either be -1 or +1. If both cannot be parsed, 0 is returned as they are equivalent.
    /// </summary>
    /// <param name="rowALabelText">The rowA label text.</param>
    /// <param name="rowBLabelText">The rowB label text.</param>
    /// <returns></returns>
    private int CompareEmbeddedInt(string rowALabelText, string rowBLabelText) {
        int rowAValue;
        bool rowAHasValue = int.TryParse(rowALabelText, out rowAValue);

        int rowBValue;
        bool rowBHasValue = int.TryParse(rowBLabelText, out rowBValue);

        int result;
        if (!rowAHasValue) {
            result = !rowBHasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !rowBHasValue ? Constants.One : rowAValue.CompareTo(rowBValue);
        }
        return result;
    }

    /// <summary>
    /// Compares the float embedded in the text from the RowALabel to the text from the RowBLabel
    /// and returns -1, 0 or +1 depending on the parsing result.
    /// If either text cannot be parsed to a float it means the content of the text is '?' and the result
    /// returned will either be -1 or +1. If both cannot be parsed, 0 is returned as they are equivalent.
    /// </summary>
    /// <param name="rowALabelText">The rowA label text.</param>
    /// <param name="rowBLabelText">The rowB label text.</param>
    /// <returns></returns>
    private int CompareEmbeddedFloat(string rowALabelText, string rowBLabelText) {
        float rowAValue;
        bool rowAHasValue = float.TryParse(rowALabelText, out rowAValue);

        float rowBValue;
        bool rowBHasValue = float.TryParse(rowBLabelText, out rowBValue);

        int result;
        if (!rowAHasValue) {
            result = !rowBHasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !rowBHasValue ? Constants.One : rowAValue.CompareTo(rowBValue);
        }
        return result;
    }

    #endregion

    #endregion

    private void Reset() {
        ResetRows();
        ResetSortDirectionState();
        ClearTable();
        // UNCLEAR detach GuiWindowEvents?
    }


    private void ResetRows() {
        _rowForms.ForAll(rf => rf.Reset());
    }

    private void ResetSortDirectionState() {
        _sortDirection = SortDirection.Descending;
        _lastSortTopic = GuiElementID.None;
        _lastSortDirection = SortDirection.Descending;
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _rowForms.ForAll(rf => rf.itemFocusUserAction -= ItemFocusUserActionEventHandler);
        EventDelegate.Remove(onShowBegin, ShowBeginEventHandler);
        EventDelegate.Remove(onHideComplete, HideCompleteEventHandler);
    }

    #region Nested Classes

    public enum SortDirection {
        Descending = 1,
        Ascending = -1
    }

    #endregion

}

