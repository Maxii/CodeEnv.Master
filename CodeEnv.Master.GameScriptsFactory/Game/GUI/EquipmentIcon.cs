﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EquipmentIcon.cs
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
/// Gui 'icon' that holds an AEquipmentStat available to be selected for use in a unit design.
/// </summary>
public class EquipmentIcon : AEquipmentIcon {

    private AEquipmentStat _equipmentStat;
    public AEquipmentStat EquipmentStat {
        get { return _equipmentStat; }
        set {
            D.AssertNull(_equipmentStat);  // occurs only once between Resets
            D.AssertNotNull(value);
            SetProperty<AEquipmentStat>(ref _equipmentStat, value, "EquipmentStat", EquipmentStatPropSetHandler);
        }
    }

    private bool _isInitialDrag;

    protected override void AcquireAdditionalIconWidgets(GameObject topLevelIconGo) { }

    #region Event and Property Change Handlers

    void OnHover(bool isOver) {
        if (isOver) {
            HoveredHudWindow.Instance.Show(FormID.Equipment, EquipmentStat);
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
    }

    void OnDragStart() {
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
        _isInitialDrag = false;
        UpdateCursor(null);
    }

    private void EquipmentStatPropSetHandler() {
        D.AssertNotDefault((int)Size);
        Show(EquipmentStat.ImageAtlasID, EquipmentStat.ImageFilename, EquipmentStat.Name);
    }

    #endregion

    public override void ResetForReuse() {
        base.ResetForReuse();
        _equipmentStat = null;
        _isInitialDrag = false;
    }

    protected override void Cleanup() { }

}

