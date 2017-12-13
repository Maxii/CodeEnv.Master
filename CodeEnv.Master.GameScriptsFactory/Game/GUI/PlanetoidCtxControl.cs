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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Context Menu Control for <see cref="APlanetoidItem"/>s. 
/// No distinction between AI and User owned.
/// </summary>
public class PlanetoidCtxControl : ACtxControl {

    // No Explore available as Fleets only explore Systems, Sectors and UniverseCenter
    // 3.27.17 TEMP removed until planetoids become IUnitAttackable again           //FleetDirective.Attack };
    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[]   {
                                                                                            FleetDirective.FullSpeedMove,
                                                                                            FleetDirective.Move
                                                                                        };

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected sealed override Vector3 PositionForDistanceMeasurements { get { return _planetoidMenuOperator.Position; } }

    protected sealed override string OperatorName { get { return _planetoidMenuOperator != null ? _planetoidMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _planetoidMenuOperator.IsFocus; } }

    protected override bool SelectedItemMenuHasContent { get { return true; } } // TEMP allows 'die'

    protected APlanetoidItem _planetoidMenuOperator;

    public PlanetoidCtxControl(APlanetoidItem planetoid)
        : base(planetoid.gameObject, uniqueSubmenuQtyReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
        _planetoidMenuOperator = planetoid;
    }

    protected sealed override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_planetoidMenuOperator.IsSelected) {
            D.AssertEqual(_planetoidMenuOperator, selected as APlanetoidItem);
            return true;
        }
        return false;
    }

    protected sealed override bool TryIsSelectedItemUserRemoteFleet(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.IsUserOwned;
    }

    protected sealed override void PopulateMenu_MenuOperatorIsSelected() {
        base.PopulateMenu_MenuOperatorIsSelected();
        __PopulateDieMenu();
    }

    private void __PopulateDieMenu() {
        _ctxObject.menuItems = new CtxMenu.Item[] { new CtxMenu.Item() {
            text = "Die",
            id = -1
        }};
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        FleetCmdItem userRemoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        bool isOrderAuthorizedByUserRemoteFleet = userRemoteFleet.IsAuthorizedForNewOrder(directive);
        // userRemoteFleet.IsCurrentOrderDirectiveAnyOf() not used in criteria as target in current order may not be this Planetoid
        switch (directive) {
            case FleetDirective.Attack:
                // 3.27.17 TEMP modified until planetoids become IUnitAttackable again
                //return !(_planetoidMenuOperator as IUnitAttackable).IsAttackByAllowed(_user)
                //    || !(_remoteUserOwnedSelectedItem as AUnitCmdItem).IsAttackCapable;
                return true;
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                return !isOrderAuthorizedByUserRemoteFleet;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected sealed override void HandleMenuPick_MenuOperatorIsSelected(int itemID) {
        base.HandleMenuPick_MenuOperatorIsSelected(itemID);
        __TellPlanetoidToDie();
    }

    protected sealed override void HandleMenuPick_OptimalFocusDistance() {
        _planetoidMenuOperator.OptimalCameraViewingDistance = _planetoidMenuOperator.Position.DistanceToCamera();
    }

    protected sealed override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteUserFleetOrder(itemID);
    }

    private void IssueRemoteUserFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        IFleetNavigableDestination target = _planetoidMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        var order = new FleetOrder(directive, OrderSource.User, target);
        remoteFleet.CurrentOrder = order;
    }

    private void __TellPlanetoidToDie() {
        _planetoidMenuOperator.__Die();
    }

}

