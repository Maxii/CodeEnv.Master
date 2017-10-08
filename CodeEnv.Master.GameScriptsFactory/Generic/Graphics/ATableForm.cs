// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ATableForm.cs
// Abstract AForm that displays a table of information in a TableWindow. 
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
/// Abstract AForm that displays a table of information in a TableWindow. 
/// </summary>
public abstract class ATableForm : AForm {

    protected const string Unknown = Constants.QuestionMark;
    private const string RowNameExtension = " Row";

    [SerializeField]
    private ATableRowForm _rowPrefab;

    protected abstract FormID RowFormID { get; }

    protected GameManager _gameMgr;
    protected SortDirection _sortDirection;
    protected UITable _table;

    /// <summary>
    /// The sort direction value that _lastSortDirection should be set to when ResetForReuse.
    /// <remarks>Our goal is to resume the same sort topic and direction that was used when this form was last used.</remarks>
    /// <remarks>This saved value will be the inverse of the _lastSortDirection value since 
    /// DetermineSortDirection() will derive the current sort direction by inverting the value of _lastSortDirection. 
    /// Handled this way to withstand multiple ResetForReuse() calls between use.</remarks>
    /// </summary>
    private SortDirection _lastSortDirectionValueToAssumeWhenResetForReuse = SortDirection.Descending;
    private Transform _tableContainer;
    private UIScrollView _tableScrollView;
    private GuiElementID _lastSortTopic = GuiElementID.NameLabel;
    private SortDirection _lastSortDirection = SortDirection.Descending;
    private IList<ATableRowForm> _rowFormsInUse;
    private List<ATableRowForm> _rowFormsAvailable;

    protected override void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        _table = gameObject.GetSingleComponentInChildren<UITable>();
        _table.sorting = UITable.Sorting.Custom;
        _rowFormsAvailable = new List<ATableRowForm>();
        _rowFormsInUse = new List<ATableRowForm>();
        _tableScrollView = GetComponentInChildren<UIScrollView>();
        _tableContainer = _tableScrollView.transform.parent;
    }

    public sealed override void PopulateValues() {
        AssignValuesToMembers();
    }

    protected sealed override void AssignValuesToMembers() {
        PopulateTableWithRows();
        ResumePreviousSortTopicAndDirection(_lastSortTopic);
    }

    private void PopulateTableWithRows() {
        D.Assert(!_rowFormsInUse.Any());

        RecordAnyExistingRowFormChildrenAsAvailable();

        ATableRowForm rowForm;
        var items = GetItemsUserIsAwareOf();
        foreach (var item in items) {
            if (_rowFormsAvailable.Any()) {
                rowForm = _rowFormsAvailable.First();
                D.Assert(!rowForm.gameObject.activeSelf);
                _rowFormsAvailable.Remove(rowForm);
            }
            else {
                rowForm = BuildRow();
                rowForm.itemFocusUserAction += ItemFocusUserActionEventHandler;
            }
            AssignItemToRowForm(item, rowForm);
            _rowFormsInUse.Add(rowForm);
            rowForm.PopulateValues();
            rowForm.gameObject.SetActive(true);
        }
    }

    private void RecordAnyExistingRowFormChildrenAsAvailable() {
        if (!_rowFormsAvailable.Any()) {
            // first use of this TableForm so check for any unregistered RowForms
            _table.gameObject.GetComponentsInChildren<ATableRowForm>(includeInactive: true, results: _rowFormsAvailable);
            foreach (var rowForm in _rowFormsAvailable) {
                rowForm.ResetForReuse();
                rowForm.itemFocusUserAction += ItemFocusUserActionEventHandler;
                rowForm.gameObject.SetActive(false);
            }
        }
    }

    protected abstract void ResumePreviousSortTopicAndDirection(GuiElementID sortTopicToResume);

    #region Event and Property Change Handlers

    private void ItemFocusUserActionEventHandler(object sender, ATableRowForm.TableRowFocusUserActionEventArgs e) {
        HandleUserInitiatedFocusOn(e.ItemToFocusOn);
    }

    #endregion

    private void HandleUserInitiatedFocusOn(ICameraFocusable item) {
        CloseScreenAndFocusOnItem(item);
    }

    private void CloseScreenAndFocusOnItem(ICameraFocusable item) {
        TableWindow.Instance.ClickDoneButton();
        item.IsFocus = true;
    }

    /// <summary>
    /// The derived class returns the items (of the type for which it is responsible) that the user has knowledge of. 
    /// A table row will be built to show each item.
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<AItem> GetItemsUserIsAwareOf();

    private ATableRowForm BuildRow() {
        GameObject rowGo = NGUITools.AddChild(_table.gameObject, _rowPrefab.gameObject);
        return rowGo.GetSafeComponent<ATableRowForm>(); ;
    }

    private void AssignItemToRowForm(AItem item, ATableRowForm rowForm) {
        //MakeRowDraggable(rowForm);
        rowForm.gameObject.name = item.DebugName + RowNameExtension;
        rowForm.SetSideAnchors(_tableContainer, left: 0, right: 0);
        rowForm.Report = GetUserReportFor(item);
    }

    /// <summary>
    /// Makes the provided row vertically draggable.
    /// <remarks>6.3.17 UNDONE Not currently sufficient as GuiElements cover the row gameObject
    /// where this UIDragScrollView is attached. All GuiElements will need UIDragScrollView to always work.</remarks>
    /// </summary>
    /// <param name="rowForm">The row form.</param>
    private void MakeRowDraggable(ATableRowForm rowForm) {
        UIDragScrollView rowDragger = rowForm.gameObject.AddMissingComponent<UIDragScrollView>();
        rowDragger.scrollView = _tableScrollView;
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
        _sortDirection = DetermineSortDirection(GuiElementID.NameLabel);
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
        _lastSortTopic = GuiElementID.NameLabel;
        var rowANameLabel = GetLabel(rowA, GuiElementID.NameLabel);
        var rowBNameLabel = GetLabel(rowB, GuiElementID.NameLabel);
        return (int)_sortDirection * rowANameLabel.text.CompareTo(rowBNameLabel.text);
    }

    private int CompareOwner(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.Owner;
        var rowAOwnerElement = GetGuiElement(rowA, GuiElementID.Owner) as OwnerIconGuiElement;
        var rowBOwnerElement = GetGuiElement(rowB, GuiElementID.Owner) as OwnerIconGuiElement;
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
        var rowAHeroElement = GetGuiElement(rowA, GuiElementID.Hero) as HeroIconGuiElement;
        var rowBHeroElement = GetGuiElement(rowB, GuiElementID.Hero) as HeroIconGuiElement;
        return (int)_sortDirection * rowAHeroElement.CompareTo(rowBHeroElement);
    }

    protected int CompareComposition(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.Composition;
        var rowACompElement = GetGuiElement(rowA, GuiElementID.Composition) as AUnitCompositionGuiElement;
        var rowBCompElement = GetGuiElement(rowB, GuiElementID.Composition) as AUnitCompositionGuiElement;
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

    protected int CompareConstruction(Transform rowA, Transform rowB) {
        _lastSortTopic = GuiElementID.Construction;
        var rowAConstructionElement = GetGuiElement(rowA, GuiElementID.Construction) as ConstructionIconGuiElement;
        var rowBConstructionElement = GetGuiElement(rowB, GuiElementID.Construction) as ConstructionIconGuiElement;
        return (int)_sortDirection * rowAConstructionElement.CompareTo(rowBConstructionElement);
    }

    private UILabel GetLabel(Transform row, GuiElementID elementID) {
        return GetGuiElement(row, elementID).gameObject.GetSingleComponentInChildren<UILabel>();
    }

    private AGuiElement GetGuiElement(Transform row, GuiElementID elementID) {
        var rowElements = row.gameObject.GetSafeComponentsInChildren<AGuiElement>();
        //D.Log("{0}: GuiElements found in {1} = {2}.", DebugName, row.name, rowElements.Select(e => e.ElementID.GetValueName()).Concatenate());
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
            // new sort topic so lowest values at top
            sortDirection = SortDirection.Ascending;
        }
        else {
            // same sort topic so direction should be the 'other' direction
            sortDirection = ToggleDirection(_lastSortDirection);
        }
        _lastSortDirection = sortDirection;
        _lastSortDirectionValueToAssumeWhenResetForReuse = ToggleDirection(_lastSortDirection);
        //D.Log("{0}.DetermineSortDirection(Topic: {1}): Direction: {2}, Value: {3}.", DebugName, sortTopic.GetValueName(), sortDirection.GetValueName(), (int)sortDirection);
        return sortDirection;
    }

    private SortDirection ToggleDirection(SortDirection direction) {
        return direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
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

    protected override void ResetForReuse_Internal() {
        ResetSortStatesForReuse();
        RecycleRowFormsInUse();
    }

    private void ResetSortStatesForReuse() {
        //D.Log("{0}.ResetSortStatesForReuse() called.", DebugName);
        _lastSortDirection = _lastSortDirectionValueToAssumeWhenResetForReuse;
        // keep _lastSortTopic the same to sort the same topic when reused
    }

    private void RecycleRowFormsInUse() {
        foreach (var rowForm in _rowFormsInUse) {
            D.Assert(rowForm.gameObject.activeSelf);
            rowForm.ResetForReuse();
            rowForm.gameObject.SetActive(false);
            D.Assert(!_rowFormsAvailable.Contains(rowForm));
            _rowFormsAvailable.Add(rowForm);
        }
        _rowFormsInUse.Clear();
    }

    #region Cleanup

    private void Unsubscribe() {
        foreach (var rowForm in _rowFormsInUse) {
            rowForm.itemFocusUserAction -= ItemFocusUserActionEventHandler;
        }
        foreach (var rowForm in _rowFormsAvailable) {
            rowForm.itemFocusUserAction -= ItemFocusUserActionEventHandler;
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    #endregion

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotNull(_rowPrefab);
        D.AssertEqual(RowFormID, _rowPrefab.FormID);
    }

    #endregion

    #region Nested Classes

    public enum SortDirection {
        /// <summary>
        /// Highest values are at the top of the scroll view.
        /// </summary>
        Descending = -1,
        /// <summary>
        /// Lowest values are at the top of the scroll view.
        /// </summary>
        Ascending = 1
    }

    #endregion

}

