// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EquipmentIconGuiElement.cs
// Gui 'icon' that holds an AEquipmentStat available to be selected for use in a unit design.
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
/// AMultiSizeIconGuiElement that represents an EquipmentStat.
/// </summary>
public class EquipmentIconGuiElement : AEquipmentIconGuiElement {

    private AEquipmentStat _equipmentStat;
    public AEquipmentStat EquipmentStat {
        get { return _equipmentStat; }
        set {
            D.AssertNull(_equipmentStat);  // occurs only once between Resets
            D.AssertNotNull(value);
            SetProperty<AEquipmentStat>(ref _equipmentStat, value, "EquipmentStat", EquipmentStatPropSetHandler);
        }
    }

    public override bool IsInitialized { get { return Size != IconSize.None && EquipmentStat != null; } }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private bool _isInitialDrag;
    private UILabel _iconImageNameLabel;

    protected override void AcquireAdditionalWidgets() {
        _iconImageNameLabel = _topLevelIconWidget.gameObject.GetSingleComponentInChildren<UILabel>();
    }

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        _iconImageSprite.atlas = EquipmentStat.ImageAtlasID.GetAtlas();
        _iconImageSprite.spriteName = EquipmentStat.ImageFilename;
        _iconImageNameLabel.text = EquipmentStat.Name;
        _tooltipContent = EquipmentStat.Name;
    }

    #region Event and Property Change Handlers

    void OnDragStart() {
        D.Assert(IsEnabled);
        _isInitialDrag = true;
    }

    void OnDrag(Vector2 delta) {
        //D.Log("{0}.OnDrag() called.", DebugName);
        if (_isInitialDrag) {
            UICamera.currentTouch.clickNotification = UICamera.ClickNotification.BasedOnDelta;
            // select this icon's stat and show the cursor dragging it
            SFXManager.Instance.PlaySFX(SfxClipID.Swipe);
            UpdateCursor(EquipmentStat);
            _isInitialDrag = false;
        }
    }

    void OnDragEnd() {
        D.Assert(IsEnabled);
        _isInitialDrag = false;
        UpdateCursor(null);
    }

    private void EquipmentStatPropSetHandler() {
        D.AssertNotDefault((int)Size);
        if (IsInitialized) {
            PopulateMemberWidgetValues();
            Show();
        }
    }

    #endregion

    protected override void HandleGuiElementHovered(bool isOver) {
        if (isOver) {
            HoveredHudWindow.Instance.Show(FormID.Equipment, EquipmentStat);
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        _equipmentStat = null;
        _isInitialDrag = false;
        _iconImageNameLabel = null;
        _tooltipContent = null;
    }

    protected override void Cleanup() { }

}

