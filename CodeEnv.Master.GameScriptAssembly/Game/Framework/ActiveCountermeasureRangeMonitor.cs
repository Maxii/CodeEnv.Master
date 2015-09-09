// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ActiveCountermeasureRangeMonitor.cs
// Detects IInterceptableOrdnance that enter and exit the range of its active countermeasures and notifies each countermeasure of such.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Detects IInterceptableOrdnance that enter and exit the range of its active countermeasures and notifies each countermeasure of such.
/// </summary>
public class ActiveCountermeasureRangeMonitor : ADetectableRangeMonitor<IInterceptableOrdnance, ActiveCountermeasure>, IActiveCountermeasureRangeMonitor {

    protected override void AssignMonitorTo(ActiveCountermeasure activeCM) {
        activeCM.RangeMonitor = this;
    }

    protected override void OnDetectedItemAdded(IInterceptableOrdnance newlyDetectedOrdnance) {
        if (newlyDetectedOrdnance.Owner == Owner) {
            // its one of ours
            if (ConfirmNotIncoming(newlyDetectedOrdnance)) {
                // ... and its not a danger so ignore it
                RemoveDetectedItem(newlyDetectedOrdnance);
                return;
            }
        }
        IInterceptableOrdnance threat = newlyDetectedOrdnance;
        threat.onDeathOneShot += OnThreatDeath;
        OnThreatInRange(threat);
    }

    protected override void OnDetectedItemRemoved(IInterceptableOrdnance lostDetectionItem) {
        IInterceptableOrdnance previousThreat = lostDetectionItem;
        previousThreat.onDeathOneShot -= OnThreatDeath;
        OnThreatOutOfRange(previousThreat);
    }

    /// <summary>
    /// Called when an IInterceptableOrdnance threat comes into range. This can include ordnance owned by 
    /// the owner of this monitor as ordnance doesn't care who owns it.
    /// </summary>
    /// <param name="newThreat">The IInterceptableOrdnance threat.</param>
    private void OnThreatInRange(IInterceptableOrdnance newThreat) {
        _equipmentList.ForAll(cm => cm.OnThreatInRangeChanged(newThreat, isInRange: true));
    }

    /// <summary>
    /// Called when an IInterceptableOrdnance threat goes out of range. This can include ordnance owned by
    /// the owner of this monitor as ordnance doesn't care who owns it.
    /// </summary>
    /// <param name="previousThreat">The previous threat.</param>
    private void OnThreatOutOfRange(IInterceptableOrdnance previousThreat) {
        _equipmentList.ForAll(cm => cm.OnThreatInRangeChanged(previousThreat, isInRange: false));
    }

    /// <summary>
    /// Called when a detected and tracked threat dies. It is necessary to track each threat's onDeath
    /// event as OnTriggerExit() is not called when a threat inside the collider is destroyed.
    /// </summary>
    /// <param name="deadThreat">The dead threat.</param>
    private void OnThreatDeath(IOrdnance deadThreat) {
        //D.Log("{0} received OnThreatDeath event for {1}.", Name, deadThreat.Name);
        RemoveDetectedItem(deadThreat as IInterceptableOrdnance);
    }

    private bool ConfirmNotIncoming(IInterceptableOrdnance detectedOrdnance) {
        var ordnanceHeading = detectedOrdnance.Heading;
        var bearingToOrdnance = detectedOrdnance.Position - transform.position;
        var dot = Vector3.Dot(ordnanceHeading, bearingToOrdnance);
        return dot >= Constants.ZeroF;  // 0 if orthogonal, +epsilon to +1.0 if some direction the same, -epsilon to -1.0 if some direction opposite
    }

    protected override float RefreshRangeDistance() {
        return _equipmentList.First().RangeDistance;    // currently no qty effects on range distance
    }


}

