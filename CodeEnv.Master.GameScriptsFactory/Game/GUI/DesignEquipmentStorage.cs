// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DesignEquipmentStorage.cs
// Keeps track of a AUnitMemberDesign's inventory of AEquipmentStats and manages the icons that represent that inventory.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Keeps track of a AUnitMemberDesign's inventory of AEquipmentStats and manages the icons that represent that inventory.
/// </summary>
public class DesignEquipmentStorage : AMonoBase {

    public string DebugName { get { return GetType().Name; } }

    public EquipmentStorageIconGuiElement storageIconPrefab;

    public AUnitMemberDesign WorkingDesign { get; private set; }

    private UIGrid _storageIconGrid;
    private UIWidget _storageIconContainer;

    private IDictionary<EquipmentSlotID, EquipmentStorageIconGuiElement> _storageIconLookup;

    protected override void Awake() {
        base.Awake();
        D.AssertNotNull(storageIconPrefab);
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _storageIconGrid = gameObject.GetSingleComponentInChildren<UIGrid>();
        _storageIconGrid.arrangement = UIGrid.Arrangement.Horizontal;
        _storageIconGrid.sorting = UIGrid.Sorting.None; // IMPROVE
        _storageIconGrid.pivot = UIWidget.Pivot.TopLeft;

        _storageIconContainer = gameObject.GetSingleComponentInImmediateChildren<UIWidget>();

        _storageIconLookup = new Dictionary<EquipmentSlotID, EquipmentStorageIconGuiElement>();
    }

    public AEquipmentStat GetEquipmentStat(EquipmentSlotID slotID) {
        return WorkingDesign.GetEquipmentStat(slotID);
    }

    /// <summary>
    /// Attempts to place the stat in an empty slot compatible with the stat's category.
    /// Returns true if the stat was placed in an appropriate slot, false otherwise.
    /// </summary>
    /// <param name="eStat">The AEquipmentStat.</param>
    /// <returns></returns>
    public bool PlaceInEmptySlot(AEquipmentStat eStat) {
        EquipmentSlotID emptySlotID;
        if (WorkingDesign.TryGetEmptySlotIDFor(eStat.Category, out emptySlotID)) {
            var storageIcon = _storageIconLookup[emptySlotID];
            bool isAccepted = storageIcon.Replace(eStat);
            D.Assert(isAccepted);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Replace a stat in the working design with the one provided, returning the one replaced.
    /// Either can be null.
    /// </summary>
    /// <param name="slotID">The slot ID.</param>
    /// <param name="equipStat">The equip stat. Can be null.</param>
    /// <returns>
    /// The stat that was replaced. Can be null.
    /// </returns>
    public AEquipmentStat Replace(EquipmentSlotID slotID, AEquipmentStat equipStat) {
        AEquipmentStat prevStat = WorkingDesign.Replace(slotID, equipStat);
        return prevStat;
    }

    /// <summary>
    /// Installs the EquipmentStorageIcons in the UI for this design. The number and category of storage
    /// icons needed is determined by the design. Whether those icons currently represent 
    /// installed equipment is determined by the presence of the equipment stat in the design.
    /// </summary>
    /// <param name="design">The design.</param>
    public void InstallEquipmentStorageIconsFor(AUnitMemberDesign design) {
        WorkingDesign = design;

        int reqdSlotQty = design.TotalReqdEquipmentSlots;

        int unusedGridRows, gridColumns;
        IntVector2 storageContainerDimensions = new IntVector2(_storageIconContainer.width, _storageIconContainer.height);
        var iconSize = AMultiSizeIconGuiElement.DetermineGridIconSize(storageContainerDimensions, reqdSlotQty, storageIconPrefab, out unusedGridRows, out gridColumns);

        IntVector2 slotDimensions = storageIconPrefab.GetIconDimensions(iconSize);
        _storageIconGrid.cellHeight = slotDimensions.y;
        _storageIconGrid.cellWidth = slotDimensions.x;

        D.AssertEqual(UIGrid.Arrangement.Horizontal, _storageIconGrid.arrangement);
        _storageIconGrid.maxPerLine = gridColumns;

        string iconGoName = storageIconPrefab.GetType().Name;
        EquipmentSlotID slotID;
        AEquipmentStat stat;
        while (design.TryGetNextEquipmentStat(out slotID, out stat)) {
            GameObject storageIconGo = NGUITools.AddChild(_storageIconGrid.gameObject, storageIconPrefab.gameObject);
            storageIconGo.name = iconGoName;
            PopulateIcon(storageIconGo, iconSize, slotID, stat);
        }
        _storageIconGrid.repositionNow = true;
    }

    private void PopulateIcon(GameObject storageIconGo, AMultiSizeIconGuiElement.IconSize iconSize, EquipmentSlotID slotID, AEquipmentStat equipStat) {
        EquipmentStorageIconGuiElement storageIcon = storageIconGo.GetComponent<EquipmentStorageIconGuiElement>();
        storageIcon.Size = iconSize;
        storageIcon.Initialize(this, slotID, equipStat);
        _storageIconLookup.Add(slotID, storageIcon);
    }

    /// <summary>
    /// Removes the icons that represent the equipment in a design.
    /// </summary>
    public void RemoveEquipmentStorageIcons() {
        if (_storageIconLookup != null) {    // Can be called by AUnitDesignWindow before Awake called
            foreach (var storageIcon in _storageIconLookup.Values) {
                Destroy(storageIcon.gameObject);
            }
            _storageIconLookup.Clear();
        }
    }

    protected override void Cleanup() {
        RemoveEquipmentStorageIcons();
    }

    public override string ToString() {
        return DebugName;
    }

    #region Debug

    #endregion

}

