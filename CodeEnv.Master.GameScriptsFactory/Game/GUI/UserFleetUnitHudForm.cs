// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserFleetUnitHudForm.cs
// Form used by the UnitHudWindow to display info and allow changes when a user-owned Fleet is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Form used by the UnitHudWindow to display info and allow changes when a user-owned Fleet is selected.
/// </summary>
public class UserFleetUnitHudForm : AFleetUnitHudForm {

    public override FormID FormID { get { return FormID.UserFleet; } }

    private PlayerAIManager _userAiMgr;
    private PlayerAIManager UserAiMgr {
        get {
            _userAiMgr = _userAiMgr ?? _gameMgr.UserAIManager;
            return _userAiMgr;
        }
    }

    protected override bool TryFindClosestGuardableItem(Vector3 currentFleetPosition, out IGuardable closestItemAllowedToGuard) {
        return UserAiMgr.TryFindClosestGuardableItem(currentFleetPosition, out closestItemAllowedToGuard);
    }

    protected override bool TryFindClosestPatrollableItem(Vector3 currentFleetPosition, out IPatrollable closestItemAllowedToPatrol) {
        return UserAiMgr.TryFindClosestPatrollableItem(currentFleetPosition, out closestItemAllowedToPatrol);
    }

    protected override bool TryFindClosestFleetRepairBase(Vector3 currentFleetPosition, out IUnitBaseCmd_Ltd closestRepairBase) {
        return UserAiMgr.TryFindClosestFleetRepairBase(currentFleetPosition, out closestRepairBase);
    }

    protected override void AssessInteractibleHud() {
        if (_pickedElementIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.UserShip, _pickedElementIcons.First().Element.Data);
        }
        else if (_pickedUnitIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.UserFleet, _pickedUnitIcons.First().Unit.Data);
        }
        else {
            InteractibleHudWindow.Instance.Hide();
        }
    }

    #region Debug

    protected override IEnumerable<FleetCmdItem> __AcquireLocalUnits() {
        float localRange = 100F;
        IEnumerable<IFleetCmd> ownerFleets;
        if (UserAiMgr.TryFindMyCloseItems<IFleetCmd>(SelectedUnit.Position, localRange, out ownerFleets, SelectedUnit)) {
            //D.Log("{0} found {1} local Fleet(s) owned by {2} within {3:0.} units of {4}. Fleets: {5}.",
            //    DebugName, ownerFleets.Count(), SelectedUnit.Owner.DebugName, localRange, SelectedUnit.DebugName, 
            //    ownerFleets.Select(f => f.DebugName).Concatenate());
            return ownerFleets.Cast<FleetCmdItem>();
        }
        return Enumerable.Empty<FleetCmdItem>();
    }

    #endregion

}

