// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHoveredHudEquipmentForm.cs
// Abstract base class for a Form that is used to display info about a piece of Equipment in the HoveredHudWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for a Form that is used to display info about a piece of Equipment in the HoveredHudWindow.
/// </summary>
public abstract class AHoveredHudEquipmentForm : AInfoDisplayForm {

    private AEquipmentStat _equipmentStat;
    public AEquipmentStat EquipmentStat {
        get { return _equipmentStat; }
        set {
            D.AssertNull(_equipmentStat);  // occurs only once between Resets
            SetProperty<AEquipmentStat>(ref _equipmentStat, value, "EquipmentStat", EquipmentStatPropSetHandler);
        }
    }

    #region Event and Property Change Handlers

    private void EquipmentStatPropSetHandler() {
        AssignValuesToMembers();
    }

    #endregion

    protected override void ResetForReuse_Internal() {
        _equipmentStat = null;
    }

}

