﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorCtxControl.cs
// Context Menu Control for Sectors, implemented using SectorExaminer.
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
/// Context Menu Control for Sectors, implemented using SectorExaminer.
/// No distinction between AI and User owned.
/// </summary>
public class SectorCtxControl : ACtxControl {

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[]   {
                                                                                            FleetDirective.Patrol,
                                                                                            FleetDirective.Move,
                                                                                            FleetDirective.FullSpeedMove,
                                                                                            FleetDirective.Explore,
                                                                                            FleetDirective.Guard
                                                                                        };

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override Vector3 PositionForDistanceMeasurements { get { return _sector.Position; } }

    protected override string OperatorName { get { return _sector != null ? _sector.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return false; } }

    private SectorExaminer _sectorExaminerMenuOperator;
    private Sector _sector;

    public SectorCtxControl(SectorExaminer sectorExaminer)
        : base(sectorExaminer.gameObject, uniqueSubmenuQtyReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
        _sectorExaminerMenuOperator = sectorExaminer;
    }

    protected override bool TryIsSelectedItemUserRemoteFleet(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.IsUserOwned;
    }

    protected override void PopulateMenu_UserRemoteFleetIsSelected() {
        base.PopulateMenu_UserRemoteFleetIsSelected();
        _sector = SectorGrid.Instance.GetSector(_sectorExaminerMenuOperator.CurrentSectorID);
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        FleetCmdItem userRemoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        bool isOrderAuthorizedByUserRemoteFleet = userRemoteFleet.IsAuthorizedForNewOrder(directive);
        // userRemoteFleet.IsCurrentOrderDirectiveAnyOf() not used in criteria as target in current order may not be this Sector
        switch (directive) {
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                return !isOrderAuthorizedByUserRemoteFleet;
            case FleetDirective.Explore:
                // FIXME: Knowledge does not yet track sectors as explorable items
                var explorableSector = _sector as IFleetExplorable;
                return !isOrderAuthorizedByUserRemoteFleet || !explorableSector.IsExploringAllowedBy(_user) || explorableSector.IsFullyExploredBy(_user);
            case FleetDirective.Patrol:
                return !isOrderAuthorizedByUserRemoteFleet || !(_sector as IPatrollable).IsPatrollingAllowedBy(_user);
            case FleetDirective.Guard:
                return !isOrderAuthorizedByUserRemoteFleet || !(_sector as IGuardable).IsGuardingAllowedBy(_user);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        throw new NotImplementedException("Sectors are not selectable or focusable.");
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteUserFleetOrder(itemID);
    }

    private void IssueRemoteUserFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        IFleetNavigableDestination target = _sector;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        var order = new FleetOrder(directive, OrderSource.User, target);
        remoteFleet.CurrentOrder = order;
    }

}

