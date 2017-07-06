// --------------------------------------------------------------------------------------------------------------------
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
            SetProperty<AEquipmentStat>(ref _equipmentStat, value, "EquipmentStat", EquipmentStatPropSetHandler);
        }
    }

    private bool _isInitialDrag;

    #region Event and Property Change Handlers

    void OnDragStart() {
        _isInitialDrag = true;
    }

    void OnDrag(Vector2 delta) {
        //D.Log("{0}.OnDrag() called.", DebugName);
        if (_isInitialDrag) {
            UICamera.currentTouch.clickNotification = UICamera.ClickNotification.BasedOnDelta;
            // select this icon's stat and show the cursor dragging it
            PlaySound(IconSoundID.Grab);
            RefreshSelectedItemHudWindow(EquipmentStat);
            UpdateCursor(EquipmentStat);
            _isInitialDrag = false;
        }
    }

    void OnDragEnd() {
        _isInitialDrag = false;
        UpdateCursor(null);
    }

    private void EquipmentStatPropSetHandler() {
        AssignValuesToMembers();
    }

    #endregion

    private void AssignValuesToMembers() {
        Size = IconSize.Large;
        Show(EquipmentStat.ImageAtlasID, EquipmentStat.ImageFilename, EquipmentStat.Name);
    }

    public void Reset() {
        _equipmentStat = null;
        _isInitialDrag = false;
    }

    protected override void Cleanup() { }

}

