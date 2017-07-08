// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DesignEquipmentStorage.cs
// Keeps track of a AUnitDesign's inventory of AEquipmentStats and manages the icons that represent that inventory.
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
/// Keeps track of a AUnitDesign's inventory of AEquipmentStats and manages the icons that represent that inventory.
/// </summary>
public class DesignEquipmentStorage : AMonoBase {

    public string DebugName { get { return GetType().Name; } }

    public EquipmentStorageIcon storageIconPrefab;

    public AUnitDesign WorkingDesign { get; private set; }

    private UIWidget _slotContainerWidget;

    private IDictionary<EquipmentSlotID, EquipmentStorageIcon> _storageIconLookup;

    protected override void Awake() {
        base.Awake();
        D.AssertNotNull(storageIconPrefab);
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        GameObject backgroundGo = gameObject.GetSingleComponentInImmediateChildren<UISprite>().gameObject;
        _slotContainerWidget = backgroundGo.GetSingleComponentInImmediateChildren<UIWidget>();

        _storageIconLookup = new Dictionary<EquipmentSlotID, EquipmentStorageIcon>();
    }

    public AEquipmentStat GetEquipmentStat(EquipmentSlotID slotID) {
        return WorkingDesign.GetEquipmentStat(slotID);
    }

    /// <summary>
    /// Attempts to place the stat in an empty slot compatible with the stat's category.
    /// If none are available, the method does nothing.
    /// </summary>
    /// <param name="eStat">The AEquipmentStat.</param>
    public void PlaceInEmptySlot(AEquipmentStat eStat) {
        EquipmentSlotID emptySlotID;
        if (WorkingDesign.TryGetEmptySlotIDFor(eStat.Category, out emptySlotID)) {
            var storageIcon = _storageIconLookup[emptySlotID];
            bool isAccepted = storageIcon.Replace(eStat);
            D.Assert(isAccepted);
            storageIcon.PlaySound(AEquipmentIcon.IconSoundID.Place);
        }
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
    public void InstallEquipmentStorageIconsFor(AUnitDesign design) {
        WorkingDesign = design; //// new ShipDesign(design);

        int reqdSlotQty = design.TotalReqdEquipmentSlots;

        int maxRows, maxColumns;
        var iconSize = __DetermineIconSize(reqdSlotQty, out maxRows, out maxColumns);

        IntVector2 slotDimensions = storageIconPrefab.GetIconDimensions(iconSize);

        for (int y = 0; y < maxRows; y++) {
            for (int x = 0; x < maxColumns; x++) {
                EquipmentSlotID slotID;
                AEquipmentStat stat;
                if (design.GetNextEquipmentStat(out slotID, out stat)) {
                    GameObject storageIconGo = NGUITools.AddChild(_slotContainerWidget.gameObject, storageIconPrefab.gameObject);
                    storageIconGo.name = storageIconPrefab.GetType().Name;

                    // assumes storageIcon's widget pivot is center
                    float xPosition = x * slotDimensions.x + 0.5F * slotDimensions.x;
                    float yPosition = -(y * slotDimensions.y + 0.5F * slotDimensions.y);
                    storageIconGo.transform.localPosition = new Vector3(xPosition, yPosition, 0F);

                    PopulateIcon(storageIconGo, iconSize, slotID, stat);
                }
                else {
                    //D.Log("{0} has populated {1} storage slots.", DebugName, reqdSlotQty);
                    return;
                }
            }
        }
        design.ResetIterators();    // can finish creating those icons that can be shown without finishing GetNextEquipment 
    }

    private AImageIcon.IconSize __DetermineIconSize(int desiredSlots, out int maxRows, out int maxColumns) {
        IntVector2 largeIconDimensions = storageIconPrefab.GetIconDimensions(AImageIcon.IconSize.Large);
        maxRows = _slotContainerWidget.height / largeIconDimensions.y;
        maxColumns = _slotContainerWidget.width / largeIconDimensions.x;
        if (desiredSlots <= maxRows * maxColumns) {
            return AImageIcon.IconSize.Large;
        }
        else {
            IntVector2 smallIconDimensions = storageIconPrefab.GetIconDimensions(AImageIcon.IconSize.Small);
            maxRows = _slotContainerWidget.height / smallIconDimensions.y;
            maxColumns = _slotContainerWidget.width / smallIconDimensions.x;
            if (desiredSlots > maxRows * maxColumns) {
                D.Warn("{0} can only create {1} InventorySlots of the {2} desired.", DebugName, maxRows * maxColumns, desiredSlots);
            }
            return AImageIcon.IconSize.Small;
        }
    }

    private void PopulateIcon(GameObject storageIconGo, AImageIcon.IconSize iconSize, EquipmentSlotID slotID, AEquipmentStat equipStat) {
        EquipmentStorageIcon storageIcon = storageIconGo.GetComponent<EquipmentStorageIcon>();
        storageIcon.Size = iconSize;
        storageIcon.Initialize(this, slotID, equipStat);
        _storageIconLookup.Add(slotID, storageIcon);
    }

    /// <summary>
    /// Removes the icons that represent the equipment in a design.
    /// </summary>
    public void RemoveEquipmentStorageIcons() {
        foreach (var storageIcon in _storageIconLookup.Values) {
            Destroy(storageIcon.gameObject);
        }
        _storageIconLookup.Clear();
    }

    protected override void Cleanup() {
        RemoveEquipmentStorageIcons();
    }

    public override string ToString() {
        return DebugName;
    }

}

