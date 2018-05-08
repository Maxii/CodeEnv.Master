// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CmdSensorRangeMonitor.cs
// SensorRangeMonitor for MR and LR Sensors located with the UnitCmd.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// SensorRangeMonitor for MR and LR Sensors located with the UnitCmd.
/// </summary>
public class CmdSensorRangeMonitor : ASensorRangeMonitor, ICmdSensorRangeMonitor {

    public event EventHandler isOperationalChanged;

    public new IUnitCmd ParentItem {
        get { return base.ParentItem as IUnitCmd; }
        set { base.ParentItem = value; }
    }

    #region Event and Property Change Handlers

    protected override void HandleIsOperationalChanged() {
        base.HandleIsOperationalChanged();
        OnIsOperationalChanged();
    }

    private void OnIsOperationalChanged() {
        if (isOperationalChanged != null) {
            isOperationalChanged(this, EventArgs.Empty);
        }
    }

    #endregion

    #region Debug

    protected override bool __ToReportTargetReacquisitionChanges { get { return false; } }

    protected override void __HandleUnknownTargetDetectedAndAdded(IElementAttackable unknownTgt) {
        base.__HandleUnknownTargetDetectedAndAdded(unknownTgt);
        if (RangeCategory == RangeCategory.Medium) {
            D.Warn("{0} adding unknown target {1}?", DebugName, unknownTgt.DebugName);
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the provided maneuverableItem (ship) is detected by the fleet's Sensors with the range of this monitor.
    /// <remarks>Currently only tracks Enemy elements, so friendly elements won't be present.</remarks>
    /// </summary>
    /// <param name="maneuverableItem">The maneuverable item.</param>
    /// <returns></returns>
    public bool __IsPresentAsEnemy(IManeuverable maneuverableItem) {
        D.Assert(maneuverableItem is IShip);
        return _enemyElementsDetected.Contains(maneuverableItem as IUnitElement_Ltd);
    }

    #endregion

}

