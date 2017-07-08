// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EquipmentStorageIcon.cs
// Icon that represents a AEquipmentStat that can be 'stored' in a slot within a Unit Design.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Icon that represents a AEquipmentStat that can be 'stored' in a slot within a Unit Design.
/// </summary>
public class EquipmentStorageIcon : AEquipmentIcon {

    /// <summary>
    /// Empty 'icon slot' image that is always enabled. When the slot is filled, this background shows through as a 'highlight'.
    /// </summary>
    public UISprite background;

    public UILabel backgroundSlotCategoryLabel;

    private DesignEquipmentStorage _storage;

    private EquipmentSlotID _slotID;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        D.AssertNotNull(background);
        D.Assert(background.enabled);
        // no need to hide icon as not yet instantiated via Initialize
    }

    public void Initialize(DesignEquipmentStorage storage, EquipmentSlotID slotID, AEquipmentStat stat) {
        _storage = storage;
        _slotID = slotID;

        D.AssertEqual(_storage.GetEquipmentStat(slotID), stat);
        backgroundSlotCategoryLabel.text = slotID.Category.GetEnumAttributeText();
        if (stat != null) {
            Show(stat.ImageAtlasID, stat.ImageFilename, stat.Name);
        }
    }

    /// <summary>
    /// Replace a stat in storage with the specified one. Returns <c>true</c> if the 
    /// replacement is allowed - aka it is the correct EquipmentCategory for this slot.
    /// </summary>
    /// <param name="replacementStat">The replacement stat.</param>
    /// <returns></returns>
    public bool Replace(AEquipmentStat replacementStat) {
        if (replacementStat != null && replacementStat.Category != _slotID.Category) {
            // wrong category
            PlaySound(IconSoundID.Error);
            return false;
        }
        var replacedStat = _storage.Replace(_slotID, replacementStat);
        if (replacedStat != replacementStat) {
            HandleStatReplacedWith(replacementStat);
        }
        return true;
    }

    private void RemoveAnyStatPresent() {
        bool isReplaced = Replace(null);
        D.Assert(isReplaced);
        RefreshSelectedItemHudWindow(null);
        UpdateCursor(null);
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// Called when [click].
    /// </summary>
    void OnClick() {
        //D.Log("{0}.OnClick() called.", DebugName);
        RemoveAnyStatPresent();
    }

    /// <summary>
    /// Called when the mouse 'drops' a GameObject onto this gameObject.
    /// Occurs when a dragged mouse has its press released over this gameObject.
    /// </summary>
    /// <param name="droppedGo">The dropped go.</param>
    void OnDrop(GameObject droppedGo) {
        //D.Log("{0}.OnDrop({1}) called.", DebugName, droppedGo.name);
        AEquipmentStat eStat = null;
        EquipmentIcon droppedEquipIcon = droppedGo.GetComponent<EquipmentIcon>();
        if (droppedEquipIcon != null) {
            eStat = droppedEquipIcon.EquipmentStat;
            bool isReplaced = Replace(eStat);
            if (isReplaced) {
                PlaySound(IconSoundID.Place);
            }
        }
        else {
            PlaySound(IconSoundID.Error);
        }
        RefreshSelectedItemHudWindow(eStat);
        UpdateCursor(null);
    }

    #endregion

    protected override void HandleIconSizeSet() {
        base.HandleIconSizeSet();
        ResizeAndAnchorBackground();
    }

    private void ResizeAndAnchorBackground() {
        IntVector2 iconSize = GetIconDimensions(Size);
        background.SetDimensions(iconSize.x, iconSize.y);
        background.SetAnchor(gameObject);
    }

    private void HandleStatReplacedWith(AEquipmentStat replacementStat) {
        if (replacementStat == null) {
            Hide();
        }
        else {
            Show(replacementStat.ImageAtlasID, replacementStat.ImageFilename, replacementStat.Name);
        }
        RefreshSelectedItemHudWindow(replacementStat);
    }

    protected override void Cleanup() { }

}

