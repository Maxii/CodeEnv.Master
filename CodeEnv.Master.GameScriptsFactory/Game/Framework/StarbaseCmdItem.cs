// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCommandItem.cs
// Class for AUnitBaseCmdItems that are Starbases.
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
/// Class for AUnitBaseCmdItems that are Starbases.
/// </summary>
public class StarbaseCmdItem : AUnitBaseCmdItem, IStarbaseCmd, IStarbaseCmd_Ltd, ISectorViewHighlightable, IShipExplorable {

    public const float RadiusMultiplierForApproachWaypointsInscribedSphere = 5F;

    public bool IsEstablished { get { return Data.IsEstablished; } }

    public new StarbaseCmdData Data {
        get { return base.Data as StarbaseCmdData; }
        set { base.Data = value; }
    }

    public StarbaseCmdReport UserReport { get { return GetReport(_gameMgr.UserPlayer); } }

    private Rigidbody _highOrbitRigidbody;

    #region Initialization

    protected override AFormationManager InitializeFormationMgr() {
        return new StarbaseFormationManager(this);
    }

    protected override ItemHoveredHudManager InitializeHoveredHudManager() {
        return new ItemHoveredHudManager(Data.Publisher);
    }

    protected override SectorViewHighlightManager InitializeSectorViewHighlightMgr() {
        return new SectorViewHighlightManager(this, UnitMaxFormationRadius * 10F);
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        IsOperational = true;
    }

    #endregion

    public StarbaseCmdReport GetReport(Player player) { return Data.Publisher.GetReport(player); }

    public FacilityReport[] GetElementReports(Player player) {
        return Elements.Cast<FacilityItem>().Select(e => e.GetReport(player)).ToArray();
    }

    protected override TrackingIconInfo MakeIconInfo() {
        return StarbaseIconInfoFactory.Instance.MakeInstance(UserReport);
    }

    protected override void ShowSelectedItemHud() {
        // 9.10.17 UnitHudWindow's StarbaseForm will auto show InteractibleHudWindow's StarbaseForm
        if (Owner.IsUser) {
            UnitHudWindow.Instance.Show(FormID.UserStarbase, this);
        }
        else {
            UnitHudWindow.Instance.Show(FormID.AiStarbase, this);
        }
    }

    protected override void PrepareForDeathEffect() {
        base.PrepareForDeathEffect();
        PathfindingManager.Instance.Graph.RemoveFromGraph(this);
        // unlike SettlementCmdItem, no parent orbiter object to disable or destroy
    }

    protected override void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint) {
        if (_highOrbitRigidbody == null) {
            _highOrbitRigidbody = GeneralFactory.Instance.MakeShipHighOrbitAttachPoint(gameObject);
        }
        _highOrbitRigidbody.gameObject.SetActive(true);
        shipOrbitJoint.connectedBody = _highOrbitRigidbody;
    }

    protected override void AttemptHighOrbitRigidbodyDeactivation() {
        D.Assert(_highOrbitRigidbody.gameObject.activeSelf);
        _highOrbitRigidbody.gameObject.SetActive(false);
    }

    protected override void HandleUserIntelCoverageChanged() {
        base.HandleUserIntelCoverageChanged();
    }

    #region Event and Property Change Handlers

    #endregion

    #region StateMachine Support Members

    protected override bool TryPickFacilityToRefitCmdModule(IEnumerable<FacilityItem> candidates, out FacilityItem facility) {
        D.Assert(!candidates.IsNullOrEmpty());
        facility = null;
        bool toRefitCmdModule = OwnerAiMgr.Designs.AreUpgradeDesignsAvailable(Data.CmdModuleDesign);
        if (toRefitCmdModule) {
            facility = candidates.SingleOrDefault(f => f.IsHQ);
            if (facility == null) {
                facility = candidates.First();
            }
        }
        return toRefitCmdModule;
    }

    #endregion

    #region Cleanup

    #endregion

    #region ISectorViewHighlightable Members

    public bool IsSectorViewHighlightShowing {
        get { return GetHighlightMgr(HighlightMgrID.SectorView).IsHighlightShowing; }
    }

    public void ShowSectorViewHighlight(bool toShow) {
        var sectorViewHighlightMgr = GetHighlightMgr(HighlightMgrID.SectorView) as SectorViewHighlightManager;
        if (!IsDiscernibleToUser) {
            if (sectorViewHighlightMgr.IsHighlightShowing) {
                D.Log(ShowDebugLog, "{0} received ShowSectorViewHighlight({1}) when not discernible but showing. Sending Show(false) to sync HighlightMgr.", DebugName, toShow);
                sectorViewHighlightMgr.Show(false);
            }
            return;
        }
        sectorViewHighlightMgr.Show(toShow);
    }

    #endregion

    #region IShipExplorable Members

    public void RecordExplorationCompletedBy(Player player) {
        SetIntelCoverage(player, IntelCoverage.Comprehensive);
        D.Assert(Data.IsFullyExploredBy(player));
    }

    public bool IsFullyExploredBy(Player player) {
        return Data.IsFullyExploredBy(player);
    }

    public bool IsExploringAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
            return true;
        }
        if (Owner.IsAtWarWith(player)) {
            return false;
        }
        if (Owner.IsEnemyOf(player)) {
            Player starbaseOwner = Owner;
            D.Assert(starbaseOwner.IsRelationshipWith(player, DiplomaticRelationship.ColdWar));
            var sector = SectorGrid.Instance.GetSector(SectorID);
            D.Assert(sector.IsOwnerAccessibleTo(player));
            Player sectorOwner = sector.Owner;
            if (sectorOwner == starbaseOwner) {
                // we are in ColdWar and this is starbase's territory
                if (OwnerAiMgr.IsPolicyToEngageColdWarEnemies) {
                    // starbase is authorized to attack player so exploring not allowed
                    return false;
                }
            }
        }
        return true;
    }

    #endregion

}

