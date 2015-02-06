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
/// No distinction between AI and Player owned.
/// </summary>
public class SectorCtxControl : ACtxControl {

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Patrol, 
                                                                                                FleetDirective.Move, 
                                                                                                FleetDirective.Explore,
                                                                                                FleetDirective.Guard };

    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override int UniqueSubmenuCountReqd { get { return Constants.Zero; } }

    private SectorExaminer _sectorExaminerMenuOperator;
    private SectorItem _sector;

    public SectorCtxControl(SectorExaminer sectorExaminer)
        : base(sectorExaminer.gameObject) {
        _sectorExaminerMenuOperator = sectorExaminer;
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCommandItem selectedFleet) {
        selectedFleet = selected as FleetCommandItem;
        return selectedFleet != null && selectedFleet.Owner.IsPlayer;
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
                return _sector.HumanPlayerIntelCoverage == IntelCoverage.Comprehensive;
            case FleetDirective.Move:
            case FleetDirective.Guard:
                return _remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_sector.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void OnMenuSelection_RemoteFleetAccess(int itemID) {
        base.OnMenuSelection_RemoteFleetAccess(itemID);

        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _sector;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCommandItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

