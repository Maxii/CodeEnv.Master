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

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Attack,
                                                                                                FleetDirective.Move,
                                                                                                FleetDirective.Guard };
    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override string OperatorName { get { return _baseMenuOperator.FullName; } }

    private AUnitBaseCmdItem _baseMenuOperator;

    public BaseCtxControl_AI(AUnitBaseCmdItem baseCmd)
        : base(baseCmd.gameObject, uniqueSubmenusReqd: Constants.Zero, menuPosition: MenuPositionMode.Over) {
        _baseMenuOperator = baseCmd;
    }

    protected override bool TryIsSelectedItemAccessAttempted(ISelectable selected) {
        if (_baseMenuOperator.IsSelected) {
            D.Assert(_baseMenuOperator == selected as AUnitBaseCmdItem);
            return true;
        }
        return false;
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsUser;
    }

    protected override bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Attack:
                return !_remoteUserOwnedSelectedItem.Owner.IsEnemyOf(_baseMenuOperator.Owner);
            case FleetDirective.Move:
            case FleetDirective.Guard:
                return _remoteUserOwnedSelectedItem.Owner.IsEnemyOf(_baseMenuOperator.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuSelection_OptimalFocusDistance() {
        _baseMenuOperator.OptimalCameraViewingDistance = _baseMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuSelection_RemoteFleetAccess(int itemID) {
        base.HandleMenuSelection_RemoteFleetAccess(itemID);

        var directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _baseMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

