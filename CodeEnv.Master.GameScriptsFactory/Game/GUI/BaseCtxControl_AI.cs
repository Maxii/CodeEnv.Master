// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseCtxControl_AI.cs
// Context Menu Control for <see cref="AUnitBaseCmdItem"/>s owned by the AI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// Context Menu Control for <see cref="AUnitBaseCmdItem"/>s owned by the AI.
/// </summary>
public class BaseCtxControl_AI : ACtxControl {

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] { FleetDirective.Attack,
                                                                                        FleetDirective.Move,
                                                                                        FleetDirective.FullSpeedMove,
                                                                                        FleetDirective.Patrol,
                                                                                        FleetDirective.Guard,
                                                                                        FleetDirective.CloseOrbit };

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override AItem ItemForDistanceMeasurements { get { return _baseMenuOperator; } }

    protected override string OperatorName { get { return _baseMenuOperator.FullName; } }

    private AUnitBaseCmdItem _baseMenuOperator;

    public BaseCtxControl_AI(AUnitBaseCmdItem baseCmd)
        : base(baseCmd.gameObject, uniqueSubmenusReqd: Constants.Zero, menuPosition: MenuPositionMode.Over) {
        _baseMenuOperator = baseCmd;
    }

    protected override bool TryIsSelectedItemMenuOperator(ISelectable selected) {
        if (_baseMenuOperator.IsSelected) {
            D.Assert(_baseMenuOperator == selected as AUnitBaseCmdItem);
            return true;
        }
        return false;
    }

    protected override bool TryIsSelectedItemUserRemoteFleet(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsUser;
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Attack:
                return !(_baseMenuOperator as IUnitAttackableTarget).IsAttackingAllowedBy(_user);
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                return false;
            case FleetDirective.Patrol:
                return !(_baseMenuOperator as IPatrollable).IsPatrollingAllowedBy(_user);
            case FleetDirective.Guard:
                return !(_baseMenuOperator as IGuardable).IsGuardingAllowedBy(_user);
            case FleetDirective.CloseOrbit:
                return (_baseMenuOperator as IShipCloseOrbitable).IsCloseOrbitAllowedBy(_user);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _baseMenuOperator.OptimalCameraViewingDistance = _baseMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteFleetOrder(itemID);
    }

    private void IssueRemoteFleetOrder(int itemID) {
        var directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _baseMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

