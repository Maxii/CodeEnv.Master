// --------------------------------------------------------------------------------------------------------------------
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
/// Context Menu Control for Sectors, implemented using SectorExaminer.
/// No distinction between AI and User owned.
/// </summary>
public class SectorCtxControl : ACtxControl {

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Patrol,
                                                                                                FleetDirective.Move,
                                                                                                FleetDirective.Explore,
                                                                                                FleetDirective.Guard };
    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override string OperatorName { get { return _sector.FullName; } }

    private SectorExaminer _sectorExaminerMenuOperator;
    private SectorItem _sector;

    public SectorCtxControl(SectorExaminer sectorExaminer)
        : base(sectorExaminer.gameObject, uniqueSubmenusReqd: Constants.Zero, toOffsetMenu: true) {
        _sectorExaminerMenuOperator = sectorExaminer;
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsUser;
    }

    protected override void PopulateMenu_RemoteFleetAccess() {
        var sectorIndex = _sectorExaminerMenuOperator.CurrentSectorIndex;
        if (!SectorGrid.Instance.TryGetSector(sectorIndex, out _sector)) {
            D.Warn("There is no {0} at {1}. {2} can not show Context Menu.", typeof(SectorItem).Name, sectorIndex, GetType().Name);
            // no sectorItem present underneath this examiner so don't build the menu
            return;
        }
        base.PopulateMenu_RemoteFleetAccess();
    }

    protected override bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Patrol:
                return false;
            case FleetDirective.Explore:
                return true; // IMPROVE _sectorItem.HumanPlayerIntelCoverage == IntelCoverage.Comprehensive;
            case FleetDirective.Move:
                return false;
            case FleetDirective.Guard:
                return _remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_sector.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuSelection_OptimalFocusDistance() {
        throw new NotImplementedException("SectorExaminer is not selectable or focusable.");
    }

    protected override void HandleMenuSelection_RemoteFleetAccess(int itemID) {
        base.HandleMenuSelection_RemoteFleetAccess(itemID);

        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _sector;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

