// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EquipmentStorageIconGuiElement.cs
// AMultiSizeIconGuiElement that represents an EquipmentStat stored in a slot within a Unit Design.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AMultiSizeIconGuiElement that represents an EquipmentStat stored in a slot within a Unit Design. Also represents an empty slot.
/// </summary>
public class EquipmentStorageIconGuiElement : AEquipmentIconGuiElement {

    [Tooltip("The widget that controls whether the stat image icon/name is showing.")]
    [SerializeField]
    private GameObject _showHideCntlWidgetGameObject = null;
    protected override GameObject ShowHideControlWidgetGameObject { get { return _showHideCntlWidgetGameObject; } }

    /// <summary>
    /// Empty 'icon slot' sprite that is always enabled. When the slot is filled, this background sprite shows through as a 'highlight'.
    /// </summary>
    [SerializeField]
    private UISprite _emptySlotSprite = null;

    [SerializeField]
    private UILabel _emptySlotCategoryLabel = null;

    public override bool IsInitialized {
        get { return Size != default(IconSize) && _storage != null && _slotID != default(OptionalEquipSlotID); }
    }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private AEquipmentStat _currentStat;
    private DesignEquipmentStorage _storage;
    private OptionalEquipSlotID _slotID;

    private UILabel _iconImageNameLabel;

    public void Initialize(DesignEquipmentStorage storage, OptionalEquipSlotID slotID, AEquipmentStat stat) {
        _storage = storage;
        _slotID = slotID;
        _currentStat = stat;

        D.Assert(IsInitialized);
        D.AssertEqual(_storage.GetEquipmentStat(slotID), stat);

        _emptySlotCategoryLabel.text = slotID.SupportedMount.GetEnumAttributeText();
        PopulateMemberWidgetValues();
        if (stat != null) {
            Show();
        }
    }

    /// <summary>
    /// Replace a stat in storage with the specified one. Returns <c>true</c> if the 
    /// replacement is allowed - aka it is the correct EquipmentCategory for this slot.
    /// </summary>
    /// <param name="replacementStat">The replacement stat.</param>
    /// <returns></returns>
    public bool Replace(AEquipmentStat replacementStat) {
        D.Assert(IsEnabled);
        AEquipmentStat unusedReplacedStat;
        return TryReplace(replacementStat, out unusedReplacedStat);
    }

    private bool TryReplace(AEquipmentStat replacementStat, out AEquipmentStat replacedStat) {
        if (replacementStat != null && !replacementStat.Category.AllowedMounts().Contains(_slotID.SupportedMount)) {
            // wrong category
            replacedStat = null;
            return false;
        }
        replacedStat = _storage.Replace(_slotID, replacementStat);
        _currentStat = replacementStat;
        PopulateMemberWidgetValues();
        if (replacedStat != replacementStat) {
            HandleStatReplacedWith(replacementStat);
        }
        return true;
    }

    protected override void AcquireAdditionalWidgets() {
        _iconImageNameLabel = _topLevelIconWidget.gameObject.GetSingleComponentInChildren<UILabel>();
    }

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        if (_currentStat != null) {
            _iconImageSprite.atlas = _currentStat.ImageAtlasID.GetAtlas();
            _iconImageSprite.spriteName = _currentStat.ImageFilename;
            _iconImageNameLabel.text = _currentStat.Name;
            _tooltipContent = _currentStat.Name;
        }
        else {
            _iconImageSprite.atlas = AtlasID.None.GetAtlas();
            _iconImageSprite.spriteName = null;
            _iconImageNameLabel.text = null;
            _tooltipContent = null;
        }
    }

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
        EquipmentIconGuiElement droppedEquipIcon = droppedGo.GetComponent<EquipmentIconGuiElement>();
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

    protected override void HandleGuiElementHovered(bool isOver) {
        if (isOver) {
            if (_currentStat != null) {
                HoveredHudWindow.Instance.Show(FormID.Equipment, _currentStat);
            }
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
    }

    protected override void ResizeAnyWidgetsThatAreNotShowHideWidgetChildren(int x, int y) {
        base.ResizeAnyWidgetsThatAreNotShowHideWidgetChildren(x, y);
        gameObject.GetComponent<UIWidget>().SetDimensions(x, y);
        _iconShowHideControlWidget.SetAnchor(gameObject);
        _emptySlotSprite.SetDimensions(x, y);
        _emptySlotSprite.SetAnchor(gameObject);
    }

    private void HandleStatReplacedWith(AEquipmentStat replacementStat) {
        if (replacementStat == null) {
            Hide();
        }
        else {
            Show();
        }
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        _storage = null;
        _slotID = default(OptionalEquipSlotID);
        _currentStat = null;
        _iconImageNameLabel = null;
        _tooltipContent = null;
    }

    protected override void Cleanup() { }

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotNull(_emptySlotSprite);
        D.Assert(_emptySlotSprite.enabled);
        D.AssertNotNull(_emptySlotCategoryLabel);

        D.AssertNotNull(_showHideCntlWidgetGameObject);
        D.AssertNotEqual(gameObject, _showHideCntlWidgetGameObject);
        UnityUtility.ValidateComponentPresence<UIWidget>(gameObject);
    }

    #endregion


}

