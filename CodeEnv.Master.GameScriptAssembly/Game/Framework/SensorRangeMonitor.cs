// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SensorRangeMonitor.cs
// Detects IDetectable Items that enter and exit the range of its sensors and notifies each with an OnDetection() or OnDetectionLost() event.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Detects IDetectable Items that enter and exit the range of its sensors and notifies each with an OnDetection() or OnDetectionLost() event.
/// TODO Account for a diploRelations change with an owner.
/// </summary>
public class SensorRangeMonitor : ARangedEquipmentMonitor<Sensor, IUnitCmdItem>, ISensorRangeMonitor {
    /************************************************************************************************************************
           * Note: PlayerKnowledge is updated by the detectedItem's DetectionHandlers as only they know when they are no longer detected
           ************************************************************************************************************************/

    /// <summary>
    /// All the detectable, attackable enemy targets that are in range of the sensors of this monitor.
    /// </summary>
    public IList<IElementAttackableTarget> AttackableEnemyTargetsDetected {
        get { return _attackableEnemyTargetsDetected; }
    }

    public override void Add(Sensor sensor) {
        base.Add(sensor);
    }

    public override bool Remove(Sensor sensor) {
        return base.Remove(sensor);
    }

    protected override void AssignMonitorTo(Sensor pieceOfEquipment) {
        pieceOfEquipment.RangeMonitor = this;
    }

    protected override void RemoveMonitorFrom(Sensor pieceOfEquipment) {
        pieceOfEquipment.RangeMonitor = null;
    }

    protected override void OnDetectedItemAdded(IDetectable newlyDetectedItem) {
        base.OnDetectedItemAdded(newlyDetectedItem);
        newlyDetectedItem.OnDetection(ParentItem, RangeCategory);
    }

    protected override void OnDetectedItemRemoved(IDetectable lostDetectionItem) {
        base.OnDetectedItemRemoved(lostDetectionItem);
        lostDetectionItem.OnDetectionLost(ParentItem, RangeCategory);
    }

    protected override float RefreshRangeDistance() {
        return _equipmentList.CalcSensorRangeDistance();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

