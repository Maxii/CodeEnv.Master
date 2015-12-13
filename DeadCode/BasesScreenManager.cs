// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BasesScreenManager.cs
// Manager for the Bases Screen.  
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
/// Manager for the Bases Screen.  
/// </summary>
[System.Obsolete]
public class BasesScreenManager : AMonoBase {

    public static int CompareName(Transform rowA, Transform rowB) {
        var rowANameLabel = GetLabel(rowA, GuiElementID.ItemNameLabel);
        var rowBNameLabel = GetLabel(rowB, GuiElementID.ItemNameLabel);
        return rowANameLabel.text.CompareTo(rowBNameLabel.text);
    }

    public static int CompareComposition(Transform rowA, Transform rowB) {
        var rowAIndexLabel = GetLabel(rowA, GuiElementID.CompositionLabel);
        var rowBIndexLabel = GetLabel(rowB, GuiElementID.CompositionLabel);
        return rowAIndexLabel.text.CompareTo(rowBIndexLabel.text);
    }

    private static UILabel GetLabel(Transform row, GuiElementID elementID) {
        var rowLabels = row.gameObject.GetSafeMonoBehavioursInChildren<UILabel>();
        return rowLabels.Single(label => label.gameObject.GetSafeMonoBehaviour<GuiElement>().elementID == elementID);
    }

    public GameObject rowPrefab;

    private GameObject _doneButtonGo;
    private UITable _table;
    private IDictionary<GameObject, IBaseCmdItem> _cmdLookup;

    protected override void Awake() {
        base.Awake();
        Arguments.ValidateNotNull(rowPrefab);
        _table = gameObject.GetSafeFirstMonoBehaviourInChildren<UITable>();
        _table.sorting = UITable.Sorting.Custom;
        _cmdLookup = new Dictionary<GameObject, IBaseCmdItem>();
        _doneButtonGo = gameObject.GetComponentInChildren<InputModeControlButton>().gameObject;
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
    /// Sorts the table based on Composition.
    /// </summary>
    public void SortOnComposition() {
        _table.onCustomSort = CompareComposition;
        _table.repositionNow = true;
    }

    private void CloseScreenAndFocusOnItem(GameObject row) {
        GameInputHelper.Instance.Notify(_doneButtonGo, "OnClick");  // close the screen

        IBaseCmdItem cmd = _cmdLookup[row];
        (cmd as ICameraFocusable).IsFocus = true;
    }

    private void ClearTable() {
        var existingRows = _table.GetChildList();
        existingRows.ForAll(r => Destroy(r.gameObject));
    }

    private void AddTableRows() {
        _cmdLookup.Clear();
        var gameMgr = GameManager.Instance;
        var cmds = gameMgr.GetPlayerKnowledge(gameMgr.UserPlayer).Commands.Where(cmd => cmd is IBaseCmdItem).Cast<IBaseCmdItem>();
        cmds.ForAll(cmd => {
            GameObject row = NGUITools.AddChild(_table.gameObject, rowPrefab);
            row.name = cmd.DisplayName + " Row";
            UIEventListener.Get(row).onDoubleClick += CloseScreenAndFocusOnItem;
            PopulateRowLabelsWithInfo(row, cmd);
            _cmdLookup.Add(row, cmd);
        });
    }

    private void PopulateRowLabelsWithInfo(GameObject row, IBaseCmdItem cmd) {
        var rowSubLabels = row.GetSafeMonoBehavioursInChildren<UILabel>();
        var nameSubLabel = rowSubLabels.Single(sl => sl.GetComponent<GuiElement>().elementID == GuiElementID.ItemNameLabel);
        var compositionSubLabel = rowSubLabels.Single(sl => sl.GetComponent<GuiElement>().elementID == GuiElementID.CompositionLabel);

        var labelText = cmd.GetLabelText(DisplayTargetID.BasesScreen);
        nameSubLabel.text = labelText.GetText(LabelContentID.ParentName);
        compositionSubLabel.text = labelText.GetText(LabelContentID.Composition);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

