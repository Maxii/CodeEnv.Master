// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AiFleetUnitHudForm.cs
// Form used by the UnitHudWindow to display info and allow changes when a AI-owned Fleet is selected.
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

/// <summary>
/// Form used by the UnitHudWindow to display info and allow changes when a AI-owned Fleet is selected.
/// </summary>
public class AiFleetUnitHudForm : AFleetUnitHudForm {

    public override FormID FormID { get { return FormID.AiFleet; } }

    private PlayerAIManager _ownerAiMgr;
    private PlayerAIManager OwnerAiMgr {
        get {
            _ownerAiMgr = _ownerAiMgr ?? _gameMgr.GetAIManagerFor(SelectedUnit.Owner);
            return _ownerAiMgr;
        }
    }

    protected override void AssessUnitButtons() {
        if (DebugControls.Instance.AreAiUnitHudButtonsFunctional) {
            base.AssessUnitButtons();
        }
        else {
            DisableUnitButtons();
            AssessUnitFocusButton();
        }
    }

    protected override void AssessElementButtons() {
        if (DebugControls.Instance.AreAiUnitHudButtonsFunctional) {
            base.AssessElementButtons();
        }
        else {
            DisableElementButtons();
        }
    }

    protected override void AssessInteractibleHud() {
        if (_pickedElementIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.AiShip, _pickedElementIcons.First().Element.UserReport);
        }
        else if (_pickedUnitIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.AiFleet, _pickedUnitIcons.First().Unit.UserReport);
        }
        else {
            InteractibleHudWindow.Instance.Hide();
        }
    }

    protected override void BuildPickedUnitsCompositionIcons() {
        bool isPickedUnitsCompositionKnownToUser = _pickedUnitIcons.Select(icon => icon.Unit).All(unit => unit.UserReport.UnitComposition != null);
        if (isPickedUnitsCompositionKnownToUser) {
            base.BuildPickedUnitsCompositionIcons();
        }
        else {
            RemoveAllUnitCompositionIcons();
        }
    }

    protected override bool TryFindClosestGuardableItem(Vector3 currentFleetPosition, out IGuardable closestItemAllowedToGuard) {
        return OwnerAiMgr.TryFindClosestGuardableItem(currentFleetPosition, out closestItemAllowedToGuard);
    }

    protected override bool TryFindClosestPatrollableItem(Vector3 currentFleetPosition, out IPatrollable closestItemAllowedToPatrol) {
        return OwnerAiMgr.TryFindClosestPatrollableItem(currentFleetPosition, out closestItemAllowedToPatrol);
    }

    protected override bool TryFindClosestFleetRepairBase(Vector3 currentFleetPosition, out IUnitBaseCmd_Ltd closestRepairBase) {
        return OwnerAiMgr.TryFindClosestFleetRepairBase(currentFleetPosition, out closestRepairBase);
    }

    #region Debug

    protected override IEnumerable<FleetCmdItem> __AcquireLocalUnits() {
        float localRange = 100F;
        IEnumerable<IFleetCmd> ownerFleets;
        if (OwnerAiMgr.TryFindMyCloseItems<IFleetCmd>(SelectedUnit.Position, localRange, out ownerFleets, SelectedUnit)) {
            //D.Log("{0} found {1} local Fleet(s) owned by {2} within {3:0.} units of {4}. Fleets: {5}.",
            //    DebugName, ownerFleets.Count(), SelectedUnit.Owner.DebugName, localRange, SelectedUnit.DebugName, 
            //    ownerFleets.Select(f => f.DebugName).Concatenate());
            return ownerFleets.Cast<FleetCmdItem>();
        }
        return Enumerable.Empty<FleetCmdItem>();
    }

    #endregion

}

