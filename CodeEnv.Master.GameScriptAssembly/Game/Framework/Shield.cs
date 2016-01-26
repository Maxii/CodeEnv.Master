// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Shield.cs
// Protective Shield for an element. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Protective Shield for an element. 
/// </summary>
public class Shield : AEquipmentMonitor<ShieldGenerator>, IShield {

    public float MaximumCharge { get { return _equipmentList.Sum(gen => gen.MaximumCharge); } }

    public float CurrentCharge { get { return _equipmentList.Where(gen => gen.IsOperational).Sum(opGen => opGen.CurrentCharge); } }

    protected override bool IsTriggerCollider { get { return false; } }

    protected override bool IsKinematicRigidbodyReqd { get { return true; } }   // avoids CompoundCollider

    public override void Add(ShieldGenerator generator) {
        base.Add(generator);
        generator.hasChargeChanged += GeneratorHasChargeChangedEventHandler;
    }

    protected override void AssignMonitorTo(ShieldGenerator generator) {
        generator.Shield = this;
    }

    /// <summary>
    /// Absorbs the impact value of this deliveryVehicleStrength. If the impact value exceeds the capacity of this shield to absorb it, 
    /// the shield will drop allowing subsequent impacts to be delivered to the target via TakeHit(damage).
    /// </summary>
    /// <param name="deliveryVehicleStrength">The strength of the delivery vehicle impacting this shield.</param>
    public void AbsorbImpact(WDVStrength deliveryVehicleStrength) {
        D.Assert(deliveryVehicleStrength.Category == WDVCategory.Beam);    // for now limiting to only defending against beams
        var operationalGeneratorsWithCharge = _equipmentList.Where(gen => gen.IsOperational && gen.HasCharge);
        DistributeShieldImpactTo(operationalGeneratorsWithCharge, deliveryVehicleStrength.Value);
        // if all the generators go down, the shield will go down allowing the beam to potentially find the parent element during its next raycast
    }

    #region Event and Property Change Handlers

    protected override void IsOperationalPropChangedHandler() {
        string shieldStateMsg = IsOperational ? "is being raised" : "has failed";
        D.Log("{0} {1}.", Name, shieldStateMsg);
    }

    private void GeneratorHasChargeChangedEventHandler(object sender, EventArgs e) {
        AssessIsOperational();
    }

    #endregion

    /// <summary>
    /// Distributes the shield impact to the provided operational generators with a charge remaining. Returns <c>true</c> if the
    /// shield remains operational after absorbing the impact, <c>false</c> otherwise.
    /// </summary>
    /// <param name="operationalGeneratorsWithCharge">The operational generators with charge.</param>
    /// <param name="impactValue">The impact value.</param>
    /// <returns></returns>
    private bool DistributeShieldImpactTo(IEnumerable<ShieldGenerator> operationalGeneratorsWithCharge, float impactValue) {
        var randomOrderedGenerators = operationalGeneratorsWithCharge.Shuffle();
        float unabsorbedImpact = impactValue;
        foreach (var gen in randomOrderedGenerators) {
            if (gen.TryAbsorbImpact(unabsorbedImpact, out unabsorbedImpact)) {
                break;
            }
        }
        bool isShieldStillUp = unabsorbedImpact == Constants.ZeroF;
        D.Log(!isShieldStillUp, "{0} has failed.", Name);
        return isShieldStillUp;
    }

    protected override void AssessIsOperational() {
        IsOperational = _equipmentList.Where(gen => gen.IsOperational && gen.HasCharge).Any();
    }

    protected override float RefreshRangeDistance() {
        // little value in setting RangeDistance to 0 when no generators are operational
        return _equipmentList.First().RangeDistance;  // currently no qty effects on range distance
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _equipmentList.ForAll(gen => gen.hasChargeChanged -= GeneratorHasChargeChangedEventHandler);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

