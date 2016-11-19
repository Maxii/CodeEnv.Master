﻿// --------------------------------------------------------------------------------------------------------------------
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
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class for AUnitBaseCmdItems that are Starbases.
/// </summary>
public class StarbaseCmdItem : AUnitBaseCmdItem, IStarbaseCmd, IStarbaseCmd_Ltd, ISectorViewHighlightable {

    public const float RadiusMultiplierForApproachWaypointsInscribedSphere = 5F;

    public new StarbaseCmdData Data {
        get { return base.Data as StarbaseCmdData; }
        set { base.Data = value; }
    }

    public StarbaseCmdReport UserReport { get { return GetReport(_gameMgr.UserPlayer); } }

    private StarbasePublisher _publisher;
    private StarbasePublisher Publisher {
        get { return _publisher = _publisher ?? new StarbasePublisher(Data, this); }
    }

    private Rigidbody _highOrbitRigidbody;

    #region Initialization

    protected override AFormationManager InitializeFormationMgr() {
        return new StarbaseFormationManager(this);
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override SectorViewHighlightManager InitializeSectorViewHighlightMgr() {
        return new SectorViewHighlightManager(this, UnitMaxFormationRadius * 10F);
    }

    #endregion

    public StarbaseCmdReport GetReport(Player player) { return Publisher.GetReport(player); }

    public FacilityReport[] GetElementReports(Player player) {
        return Elements.Cast<FacilityItem>().Select(e => e.GetReport(player)).ToArray();
    }

    protected override IconInfo MakeIconInfo() {
        return StarbaseIconInfoFactory.Instance.MakeInstance(UserReport);
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedStarbase, UserReport);
    }

    protected override void HandleDeathBeforeBeginningDeathEffect() {
        base.HandleDeathBeforeBeginningDeathEffect();
        PathfindingManager.Instance.Graph.RemoveFromGraph(this);
        // unlike SettlementCmdItem, no parent orbiter object to disable or destroy
    }

    protected override void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint) {
        if (_highOrbitRigidbody == null) {
            _highOrbitRigidbody = gameObject.AddMissingComponent<Rigidbody>();
            _highOrbitRigidbody.useGravity = false;
            _highOrbitRigidbody.isKinematic = true;
        }
        shipOrbitJoint.connectedBody = _highOrbitRigidbody;
    }

    #region Event and Property Change Handlers

    #endregion

    #region Cleanup

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ISectorViewHighlightable Members

    public bool IsSectorViewHighlightShowing {
        get { return GetHighlightMgr(HighlightMgrID.SectorView).IsHighlightShowing; }
    }

    public void ShowSectorViewHighlight(bool toShow) {
        var sectorViewHighlightMgr = GetHighlightMgr(HighlightMgrID.SectorView) as SectorViewHighlightManager;
        if (!IsDiscernibleToUser) {
            if (sectorViewHighlightMgr.IsHighlightShowing) {
                if (ShowDebugLog) {
                    D.Log("{0} received ShowSectorViewHighlight({1}) when not discernible but showing. Sending Show(false) to sync HighlightMgr.", FullName, toShow);
                }
                sectorViewHighlightMgr.Show(false);
            }
            return;
        }
        sectorViewHighlightMgr.Show(toShow);
    }

    #endregion

}

