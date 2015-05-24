// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemsScreenManager.cs
// Manager for the Systems Screen.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Manager for the Systems Screen.
/// </summary>
[System.Obsolete]
public class SystemsScreenManager : AMonoBase {

    public static int CompareName(Transform rowA, Transform rowB) {
        var rowANameLabel = GetLabel(rowA, GuiElementID.NameLabel);
        var rowBNameLabel = GetLabel(rowB, GuiElementID.NameLabel);
        return rowANameLabel.text.CompareTo(rowBNameLabel.text);
    }

    public static int CompareSectorIndex(Transform rowA, Transform rowB) {
        //var rowAIndexLabel = GetLabel(rowA, GuiElementID.LocationLabel);
        //var rowBIndexLabel = GetLabel(rowB, GuiElementID.LocationLabel);
        //return rowAIndexLabel.text.CompareTo(rowBIndexLabel.text);
        return 0;
    }

    private static UILabel GetLabel(Transform row, GuiElementID elementID) {
        var rowLabels = row.gameObject.GetSafeMonoBehavioursInChildren<UILabel>();
        return rowLabels.Single(label => label.gameObject.GetSafeMonoBehaviour<GuiElement>().elementID == elementID);
    }

    public GameObject rowPrefab;

    private GameObject _doneButtonGo;
    private UITable _table;
    private IDictionary<GameObject, ISystemItem> _systemLookup;

    protected override void Awake() {
        base.Awake();
        Arguments.ValidateNotNull(rowPrefab);
        _table = gameObject.GetSafeMonoBehaviourInChildren<UITable>();
        _table.sorting = UITable.Sorting.Custom;
        _systemLookup = new Dictionary<GameObject, ISystemItem>();
        _doneButtonGo = gameObject.GetComponentInChildren<GuiInputModeControlButton>().gameObject;
    }

    public void PopulateTable() {
        ClearTable();
        _table.onCustomSort = CompareName;
        AddTableRows();
        _table.repositionNow = true;
    }

    /// <summary>
    /// Sorts the table based on Name.
    /// </summary>
    public void SortOnName() {
        _table.onCustomSort = CompareName;
        _table.repositionNow = true;
    }

    /// <summary>
    /// Sorts the table based on Sector Index.
    /// </summary>
    public void SortOnSectorIndex() {
        _table.onCustomSort = CompareSectorIndex;
        _table.repositionNow = true;
    }

    private void CloseScreenAndFocusOnItem(GameObject row) {
        GameInputHelper.Instance.Notify(_doneButtonGo, "OnClick");  // close the screen

        ISystemItem system = _systemLookup[row];
        (system as ICameraFocusable).IsFocus = true;
    }

    private void ClearTable() {
        var existingRows = _table.GetChildList();
        existingRows.ForAll(r => Destroy(r.gameObject));
    }

    private void AddTableRows() {
        _systemLookup.Clear();
        var gameMgr = GameManager.Instance;
        var systems = gameMgr.GetPlayerKnowledge(gameMgr.UserPlayer).Systems;
        systems.ForAll(system => {
            GameObject row = NGUITools.AddChild(_table.gameObject, rowPrefab);
            row.name = system.DisplayName + " Row";
            UIEventListener.Get(row).onDoubleClick += CloseScreenAndFocusOnItem;
            PopulateRowLabelsWithInfo(row, system);
            _systemLookup.Add(row, system);
        });
    }

    private void PopulateRowLabelsWithInfo(GameObject row, ISystemItem system) {
        var rowSubLabels = row.GetSafeMonoBehavioursInChildren<UILabel>();
        var nameSubLabel = rowSubLabels.Single(sl => sl.GetComponent<GuiElement>().elementID == GuiElementID.NameLabel);
        //var indexSubLabel = rowSubLabels.Single(sl => sl.GetComponent<GuiElement>().elementID == GuiElementID.LocationLabel);

        var labelText = system.GetLabelText(DisplayTargetID.SystemsScreen);
        nameSubLabel.text = labelText.GetText(LabelContentID.Name);
        //indexSubLabel.text = labelText.GetText(LabelContentID.SectorIndex);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

