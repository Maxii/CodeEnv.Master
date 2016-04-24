// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidCtxControl.cs
// Context Menu Control for <see cref="APlanetoidItem"/>s.
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
/// Context Menu Control for <see cref="APlanetoidItem"/>s. 
/// No distinction between AI and User owned.
/// </summary>
public class PlanetoidCtxControl : ACtxControl {

    // No Explore available as Fleets only explore Systems, Sectors and UniverseCenter
    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] { FleetDirective.FullSpeedMove,
                                                                                        FleetDirective.Move,
                                                                                        FleetDirective.Attack };

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected sealed override AItem ItemForDistanceMeasurements { get { return _planetoidMenuOperator; } }

    protected sealed override string OperatorName { get { return _planetoidMenuOperator.FullName; } }

    protected APlanetoidItem _planetoidMenuOperator;

    public PlanetoidCtxControl(APlanetoidItem planetoid)
        : base(planetoid.gameObject, uniqueSubmenusReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
        _planetoidMenuOperator = planetoid;
    }

    protected sealed override bool TryIsSelectedItemMenuOperator(ISelectable selected) {
        if (_planetoidMenuOperator.IsSelected) {
            D.Assert(_planetoidMenuOperator == selected as APlanetoidItem);
            return true;
        }
        return false;
    }

    protected sealed override bool TryIsSelectedItemUserRemoteFleet(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsUser;
    }

    protected sealed override void PopulateMenu_UserMenuOperatorIsSelected() {
        base.PopulateMenu_UserMenuOperatorIsSelected();
        __PopulateDieMenu();
    }

    private void __PopulateDieMenu() {
        _ctxObject.menuItems = new CtxMenu.Item[] { new CtxMenu.Item() {
            text = "Die",
            id = -1
        }};
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Attack:
                return !(_planetoidMenuOperator as IUnitAttackableTarget).IsAttackingAllowedBy(_user)
                    || !(_remoteUserOwnedSelectedItem as AUnitCmdItem).IsAttackCapable;
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected sealed override void HandleMenuPick_UserMenuOperatorIsSelected(int itemID) {
        base.HandleMenuPick_UserMenuOperatorIsSelected(itemID);
        __TellPlanetoidToDie();
    }

    protected sealed override void HandleMenuPick_OptimalFocusDistance() {
        _planetoidMenuOperator.OptimalCameraViewingDistance = _planetoidMenuOperator.Position.DistanceToCamera();
    }

    protected sealed override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteFleetOrder(itemID);
    }

    private void IssueRemoteFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        IFleetNavigable target = _planetoidMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target);
    }

    private void __TellPlanetoidToDie() {
        _planetoidMenuOperator.__Die();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

