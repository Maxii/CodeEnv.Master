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
using UnityEngine;

/// <summary>
/// Protective Shield for an element. 
/// </summary>
public class Shield : AEquipmentMonitor<ShieldGenerator>, IShield {

    public float MaximumCharge { get { return _equipmentList.Sum(gen => gen.MaximumCharge); } }

    public float CurrentCharge { get { return _equipmentList.Where(gen => gen.IsOperational).Sum(opGen => opGen.CurrentCharge); } }

    protected override bool IsTriggerCollider { get { return false; } }

    protected override bool IsKinematicRigidbodyReqd { get { return true; } }   // avoids CompoundCollider

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        InitializeDebugShowShield();
    }

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
        D.Log(ShowDebugLog, "{0} {1}.", FullName, shieldStateMsg);
        HandleDebugShieldIsOperationalChanged();
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
        D.Log(ShowDebugLog && !isShieldStillUp, "{0} has failed.", FullName);
        return isShieldStillUp;
    }

    protected override void AssessIsOperational() {
        IsOperational = _equipmentList.Where(gen => gen.IsOperational && gen.HasCharge).Any();
    }

    protected override float RefreshRangeDistance() {
        float baselineRange = RangeCategory.GetBaselineShieldRange();
        // IMPROVE add factors based on IUnitElement Type and/or Category. DONOT vary by Cmd
        float range = baselineRange;
        return range;
    }

    protected override void Cleanup() {
        base.Cleanup();
        CleanupDebugShowShield();
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _equipmentList.ForAll(gen => gen.hasChargeChanged -= GeneratorHasChargeChangedEventHandler);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Show Shield

    private void InitializeDebugShowShield() {
        DebugValues debugValues = DebugValues.Instance;
        debugValues.showShieldsChanged += ShowDebugShieldsChangedEventHandler;
        if (debugValues.ShowShields) {
            EnableDebugShowShield(true);
        }
    }

    private void EnableDebugShowShield(bool toEnable) {
        DrawColliderGizmo drawCntl = gameObject.AddMissingComponent<DrawColliderGizmo>();
        drawCntl.Color = IsOperational ? Color.green : Color.red;
        drawCntl.enabled = toEnable;
    }

    private void HandleDebugShieldIsOperationalChanged() {
        DebugValues debugValues = DebugValues.Instance;
        if (debugValues.ShowShields) {
            DrawColliderGizmo drawCntl = gameObject.GetComponent<DrawColliderGizmo>();
            drawCntl.Color = IsOperational ? Color.green : Color.red;
        }
    }

    private void ShowDebugShieldsChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowShield(DebugValues.Instance.ShowShields);
    }

    private void CleanupDebugShowShield() {
        var debugValues = DebugValues.Instance;
        if (debugValues != null) {
            debugValues.showShieldsChanged -= ShowDebugShieldsChangedEventHandler;
        }
        DrawColliderGizmo drawCntl = gameObject.GetComponent<DrawColliderGizmo>();
        if (drawCntl != null) {
            Destroy(drawCntl);
        }
    }

    #endregion

}

