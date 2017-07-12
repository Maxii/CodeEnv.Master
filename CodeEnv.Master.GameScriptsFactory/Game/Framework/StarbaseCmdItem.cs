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
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

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

    public override void FinalInitialize() {
        base.FinalInitialize();
        IsOperational = true;
    }

    #endregion

    public StarbaseCmdReport GetReport(Player player) { return Publisher.GetReport(player); }

    public FacilityReport[] GetElementReports(Player player) {
        return Elements.Cast<FacilityItem>().Select(e => e.GetReport(player)).ToArray();
    }

    protected override IconInfo MakeIconInfo() {
        return StarbaseIconInfoFactory.Instance.MakeInstance(UserReport);
    }

    protected override void ShowSelectedItemInHud() {
        InteractableHudWindow.Instance.Show(FormID.SelectedStarbase, Data);
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

    #region Event and Property Change Handlers

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

}

