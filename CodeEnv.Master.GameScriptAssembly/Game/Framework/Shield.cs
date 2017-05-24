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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Protective Shield for an element. 
/// </summary>
public class Shield : AEquipmentMonitor<ShieldGenerator>, IShield {

    public float MaximumCharge { get { return _equipmentList.Sum(gen => gen.MaximumCharge); } }

    public float CurrentCharge { get { return _equipmentList.Where(gen => gen.IsOperational).Sum(opGen => opGen.CurrentCharge); } }

    protected override int MaxEquipmentCount { get { return 10; } }

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
        D.AssertEqual(WDVCategory.Beam, deliveryVehicleStrength.Category);    // for now limiting to only defending against beams
        var operationalGeneratorsWithCharge = _equipmentList.Where(gen => gen.IsOperational && gen.HasCharge);
        DistributeShieldImpactTo(operationalGeneratorsWithCharge, deliveryVehicleStrength.Value);
        // if all the generators go down, the shield will go down allowing the beam to potentially find the parent element during its next raycast
    }

    #region Event and Property Change Handlers

    protected override void HandleIsOperationalChanged() {
        D.Log(ShowDebugLog, "{0} {1}.", DebugName, (IsOperational ? "is being raised" : "has failed"));
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
        if (ShowDebugLog && !isShieldStillUp) {
            D.Log(ShowDebugLog, "{0} has failed.", DebugName);
        }
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

    #region Debug Show Shield

    private void InitializeDebugShowShield() {
        DebugControls debugValues = DebugControls.Instance;
        debugValues.showShields += ShowDebugShieldsChangedEventHandler;
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
        DebugControls debugValues = DebugControls.Instance;
        if (debugValues.ShowShields) {
            DrawColliderGizmo drawCntl = gameObject.GetComponent<DrawColliderGizmo>();
            drawCntl.Color = IsOperational ? Color.green : Color.red;
        }
    }

    private void ShowDebugShieldsChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowShield(DebugControls.Instance.ShowShields);
    }

    private void CleanupDebugShowShield() {
        var debugValues = DebugControls.Instance;
        if (debugValues != null) {
            debugValues.showShields -= ShowDebugShieldsChangedEventHandler;
        }
        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        DrawColliderGizmo drawCntl = gameObject.GetComponent<DrawColliderGizmo>();
        Profiler.EndSample();

        if (drawCntl != null) {
            Destroy(drawCntl);
        }
    }

    #endregion

}

