// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EquipmentStorageGuiIcon.cs
// Icon that represents a AEquipmentStat that can be 'stored' in a slot within a Unit Design.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Icon that represents a AEquipmentStat that can be 'stored' in a slot within a Unit Design.
/// </summary>
public class EquipmentStorageGuiIcon : AEquipmentGuiIcon {

    /// <summary>
    /// Empty 'icon slot' sprite that is always enabled. When the slot is filled, this background sprite shows through as a 'highlight'.
    /// </summary>
    public UISprite emptySlotSprite;

    public UILabel emptySlotCategoryLabel;

    private DesignEquipmentStorage _storage;
    private EquipmentSlotID _slotID;

    public void Initialize(DesignEquipmentStorage storage, EquipmentSlotID slotID, AEquipmentStat stat) {
        _storage = storage;
        _slotID = slotID;

        D.AssertNotDefault((int)Size);
        D.AssertEqual(_storage.GetEquipmentStat(slotID), stat);
        emptySlotCategoryLabel.text = slotID.Category.GetEnumAttributeText();
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
        AEquipmentStat unusedReplacedStat;
        return TryReplace(replacementStat, out unusedReplacedStat);
    }

    private bool TryReplace(AEquipmentStat replacementStat, out AEquipmentStat replacedStat) {
        if (replacementStat != null && replacementStat.Category != _slotID.Category) {
            // wrong category
            replacedStat = null;
            return false;
        }
        replacedStat = _storage.Replace(_slotID, replacementStat);
        if (replacedStat != replacementStat) {
            HandleStatReplacedWith(replacementStat);
        }
        return true;
    }

    protected override void AcquireAdditionalIconWidgets(GameObject topLevelIconGo) { }

    private void RemoveAnyStatPresent() {
        AEquipmentStat replacedStat;
        bool isReplaced = TryReplace(null, out replacedStat);
        D.Assert(isReplaced);

        if (replacedStat == null) {
            SFXManager.Instance.PlaySFX(SfxClipID.Error);
        }
        else {
            SFXManager.Instance.PlaySFX(SfxClipID.UnSelect);
        }

        HoveredHudWindow.Instance.Hide();
        UpdateCursor(null);
    }

    #region Event and Property Change Handlers

    void OnHover(bool isOver) {
        if (isOver) {
            AEquipmentStat stat = _storage.GetEquipmentStat(_slotID);
            if (stat != null) {
                HoveredHudWindow.Instance.Show(FormID.Equipment, stat);
            }
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
    }

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
        EquipmentGuiIcon droppedEquipIcon = droppedGo.GetComponent<EquipmentGuiIcon>();
        if (droppedEquipIcon != null) {
            eStat = droppedEquipIcon.EquipmentStat;
            bool isReplaced = Replace(eStat);
            if (isReplaced) {
                SFXManager.Instance.PlaySFX(SfxClipID.Tap);
            }
        }
        else {
            SFXManager.Instance.PlaySFX(SfxClipID.Error);
        }
        UpdateCursor(null);
    }

    #endregion

    protected override void HandleIconSizeSet() {
        base.HandleIconSizeSet();
        ResizeAndAnchorEmptySlotBackgroundSprite();
    }

    private void ResizeAndAnchorEmptySlotBackgroundSprite() {
        IntVector2 iconDimensions = GetIconDimensions(Size);
        emptySlotSprite.SetDimensions(iconDimensions.x, iconDimensions.y);
        emptySlotSprite.SetAnchor(gameObject);
    }

    private void HandleStatReplacedWith(AEquipmentStat replacementStat) {
        if (replacementStat == null) {
            Hide();
        }
        else {
            Show(replacementStat.ImageAtlasID, replacementStat.ImageFilename, replacementStat.Name);
        }
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        _storage = null;
        _slotID = default(EquipmentSlotID);
    }

    protected override void Cleanup() { }

    #region Debug

    protected override void __Validate() {
        base.__Validate();
        D.AssertNotNull(emptySlotSprite);
        D.Assert(emptySlotSprite.enabled);
        D.AssertNotNull(emptySlotCategoryLabel);
    }

    #endregion

}

