// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FtlDampenerRangeMonitor.cs
// Detects IPropellable ships not owned by Owner that enter and exit the range of its FTL dampening field.
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
/// Detects IPropellable ships not owned by Owner that enter and exit the range of its FTL dampening field. Notifies the
/// IPropellable ships that are FTL capable that their FTL engines have been damped or undamped depending on whether entering 
/// or exiting.
/// <remarks>3.2.17 Currently there is no interaction with the FtlDampener equipment.</remarks>
/// </summary>
public class FtlDampenerRangeMonitor : ADetectableRangeMonitor<IPropellable, FtlDampener>, IFtlDampenerRangeMonitor {

    private static LayerMask FtlCapableObjectLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.Default);

    public new IUnitCmd ParentItem {
        get { return base.ParentItem as IUnitCmd; }
        set { base.ParentItem = value as IUnitCmd; }
    }

    protected override int MaxEquipmentCount { get { return 1; } }

    protected override LayerMask BulkDetectionLayerMask { get { return FtlCapableObjectLayerMask; } }

    protected override bool IsKinematicRigidbodyReqd { get { return false; } }   // Propellables all have Rigidbodies

    protected override void AssignMonitorTo(FtlDampener dampener) {
        dampener.RangeMonitor = this;
    }

    protected override void HandleDetectedObjectAdded(IPropellable newlyDetectedPropellable) {
        D.Assert(newlyDetectedPropellable.IsOperational);
        if (newlyDetectedPropellable.IsFtlCapable) {
            bool toDampen = true;
            if (newlyDetectedPropellable.IsOwnerAccessibleTo(Owner)) {
                Player propellableOwner;
                bool isOwnerFound = newlyDetectedPropellable.TryGetOwner(Owner, out propellableOwner);
                D.Assert(isOwnerFound);
                if (propellableOwner == Owner) {
                    // its one of our ships
                    toDampen = false;
                }
            }
            if (toDampen) {
                newlyDetectedPropellable.HandleFtlDampenerActivated(ParentItem as IUnitCmd_Ltd, RangeCategory);
            }
        }
    }

    protected override void HandleDetectedObjectRemoved(IPropellable lostPropellable) {
        if (lostPropellable.IsOperational && lostPropellable.IsFtlCapable) {
            bool toUndampen = true;
            if (lostPropellable.IsOwnerAccessibleTo(Owner)) {
                Player propellableOwner;
                bool isOwnerFound = lostPropellable.TryGetOwner(Owner, out propellableOwner);
                D.Assert(isOwnerFound);
                if (propellableOwner == Owner) {
                    // its one of our ships
                    toUndampen = false;
                }
            }
            if (toUndampen) {
                lostPropellable.HandleFtlDampenerDeactivated(ParentItem as IUnitCmd_Ltd, RangeCategory);
            }
        }
    }

    #region Event and Property Change Handlers


    /// <summary>
    /// Called when [parent owner changing].
    /// <remarks>Sets IsOperational to false. If not already false, this change removes all detected items
    /// while the parentItem still has the old owner, thereby properly notifying those detected items of the
    /// loss of detection by this item.</remarks>
    /// </summary>
    /// <param name="incomingOwner">The incoming owner.</param>
    protected override void HandleParentItemOwnerChanging(Player incomingOwner) {
        base.HandleParentItemOwnerChanging(incomingOwner);
        IsOperational = false;
    }

    /// <summary>
    /// Called when [parent owner changed].
    /// <remarks>Combined with HandleParentItemOwnerChanging(), this IsOperational change results in re-acquisition of detectable items
    /// using the new owner if any equipment is operational. If no equipment is operational,then the re-acquisition will be deferred
    /// until a pieceOfEquipment becomes operational again. When the re-acquisition occurs, each newly detected item will be properly
    /// notified of its detection by this item.</remarks>
    /// </summary>
    protected override void HandleParentItemOwnerChanged() {
        base.HandleParentItemOwnerChanged();
        AssessIsOperational();
    }

    protected override void HandleIsOperationalChanged() {
        base.HandleIsOperationalChanged();
        if (IsOperational) {
            D.Log(ShowDebugLog, "{0} is now activated and operational, dampening surrounding FTL drives.", DebugName);
        }
    }

    #endregion

    /// <summary>
    /// Reviews the knowledge we have of each detected object (via attempting to access their owner) with the objective of
    /// making sure each object is in the right container, if any.
    /// <remarks>Called when a relations change occurs between the Owner and another player. 
    /// No need to re-acquire each detected item as the only thing they care about is which Cmd and which sensorRange
    /// detected them which hasn't changed.</remarks>
    /// </summary>
    protected override void ReviewKnowledgeOfAllDetectedObjects() { }

    protected override float RefreshRangeDistance() {
        return RangeCategory.__GetBaselineFtlDampenerRange();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    private const float __acceptableThresholdMultiplierBase = 0.01F;

    protected override void __WarnOnErroneousTriggerExit(IPropellable lostDetectionItem) {
        if (lostDetectionItem.IsOperational) {
            float gameSpeedMultiplier = __gameTime.GameSpeedMultiplier;  // 0.25 - 4.0
            float acceptableThresholdMultiplier = 1F - __acceptableThresholdMultiplierBase * gameSpeedMultiplier;   // ~1 - 0.99 - 0.96
            float acceptableThreshold = RangeDistance * acceptableThresholdMultiplier;
            float acceptableThresholdSqrd = acceptableThreshold * acceptableThreshold;
            float lostDetectionItemDistanceSqrd;
            if ((lostDetectionItemDistanceSqrd = Vector3.SqrMagnitude(lostDetectionItem.Position - transform.position)) < acceptableThresholdSqrd) {
                D.Warn("{0}.OnTriggerExit() called. Exit Distance for {1} {2:0.##} is < AcceptableThreshold {3:0.##}.",
                    DebugName, lostDetectionItem.DebugName, Mathf.Sqrt(lostDetectionItemDistanceSqrd), acceptableThreshold);
                if (lostDetectionItemDistanceSqrd == Constants.ZeroF) {
                    D.Error("{0}.OnTriggerExit({1}) called at distance zero. LostItem.position = {2}, {0}.position = {3}. IsHQ = {4}",
                        DebugName, lostDetectionItem.DebugName, lostDetectionItem.Position, transform.position, (lostDetectionItem as IShip_Ltd).IsHQ);
                    // 3.5.17 IsHQ may return false if not yet assigned HQ designation as DebugName does not have [HQ]
                    // UNCLEAR Why the new HQ exits the dampening field. Reset and reacquire on HQ change?
                }
            }
        }
    }

    #endregion

}

