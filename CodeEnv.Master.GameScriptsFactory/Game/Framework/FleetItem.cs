// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetItem.cs
// The data-holding class for all fleets in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all fleets in the game.
/// </summary>
public class FleetItem : Item {

    public new FleetData Data {
        get { return base.Data as FleetData; }
        set { base.Data = value; }
    }

    private ShipItem _flagship;
    public ShipItem Flagship {
        get { return _flagship; }
        set { SetProperty<ShipItem>(ref _flagship, value, "Flagship", OnFlagshipChanged); }
    }

    private ShipItem _lastFleetShipDestroyed;
    /// <summary>
    /// The most recent member of this fleet that has been destroyed. Can be 
    /// null if no ships belonging to this fleet have been destroyed.
    /// </summary>
    public ShipItem LastFleetShipDestroyed {
        get { return _lastFleetShipDestroyed; }
        set { SetProperty<ShipItem>(ref _lastFleetShipDestroyed, value, "LastFleetShipDestroyed", OnLastFleetShipDestroyedChanged); }
    }

    public IList<ShipItem> Ships { get; private set; }

    private bool __engageAutoPilotOnCourseSuccess;

    private GameManager _gameMgr;
    //private FleetAutoPilot _autoPilot;
    private FleetAutoPilot _autoPilot;

    protected override void Awake() {
        base.Awake();
        GameObject fleetParent = _transform.parent.gameObject;
        Ships = fleetParent.GetSafeMonoBehaviourComponentsInChildren<ShipItem>().ToList();
    }

    protected override void Subscribe() {
        base.Subscribe();
        _gameMgr = GameManager.Instance;
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.GameState, OnGameStateChanged));
    }

    private void OnFinalDestinationReachedChanged() {
        if (_autoPilot.IsFinalDestinationReached) {
            D.Log("AutoPilot FinalDestination {0} reached.", _autoPilot.FinalDestination);
            __PlotRandomCourseAndEngageAutoPilot();
        }
    }

    private void OnGameStateChanged() {
        if (_gameMgr.GameState == GameState.Running) {
            // IMPROVE select LeadShipCaptain here for now as Data must be initialized first
            Flagship = RandomExtended<ShipItem>.Choice(Ships);
            __GetFleetUnderway();
        }
    }

    protected override void OnDataChanged() {
        base.OnDataChanged();
        InitializeAutoPilot();
    }

    private void OnFlagshipChanged() {
        Data.FlagshipData = Flagship.Data;
    }

    public void OnCoursePlotCompleted(bool isSuccessful, Vector3 Destination) {
        if (isSuccessful) {
            if (__engageAutoPilotOnCourseSuccess) {
                _autoPilot.Engage();
                __engageAutoPilotOnCourseSuccess = false;
            }
            else {
                D.Log("Course plotted, awaiting engagement order.");
            }
        }
        else {
            __PlotRandomCourseAndEngageAutoPilot();
        }
    }

    public void __GetFleetUnderway() {
        __PlotRandomCourseAndEngageAutoPilot();
        //ChangeFleetHeading(UnityEngine.Random.onUnitSphere);
        //ChangeFleetSpeed(2.0F);
    }

    private void __PlotRandomCourseAndEngageAutoPilot() {
        _autoPilot.PlotCourse(UnityEngine.Random.onUnitSphere * 200F);
        __engageAutoPilotOnCourseSuccess = true;
    }

    public bool ChangeFleetHeading(Vector3 newHeading, bool isManualOverride = true) {
        if (DebugSettings.Instance.StopShipMovement) {
            _autoPilot.Disengage();
            return false;
        }
        if (isManualOverride) {
            _autoPilot.Disengage();
        }
        if (Mathfx.Approx(newHeading, Data.RequestedHeading, .01F)) {
            D.Warn("Duplicate ChangeHeading Command to {0} on {1}.", newHeading, Data.Name);
            return false;
        }
        D.Log("Fleet Requested Heading was {0}, now {1}.", Data.RequestedHeading, newHeading);
        foreach (var ship in Ships) {
            ship.ChangeHeading(newHeading);
        }
        return true;
    }

    public bool ChangeFleetSpeed(float newSpeed, bool isManualOverride = true) {
        if (DebugSettings.Instance.StopShipMovement) {
            _autoPilot.Disengage();
            return false;
        }
        if (isManualOverride) {
            _autoPilot.Disengage();
        }
        if (Mathfx.Approx(newSpeed, Data.RequestedSpeed, .01F)) {
            D.Warn("Duplicate ChangeSpeed Command to {0} on {1}.", newSpeed, Data.Name);
            return false;
        }
        D.Log("Fleet Requested Speed was {0}, now {1}.", Data.RequestedSpeed, newSpeed);
        foreach (var ship in Ships) {
            ship.ChangeSpeed(newSpeed);
        }
        return true;
    }

    private void OnLastFleetShipDestroyedChanged() {
        D.Log("{0} acknowledging {1} has been lost.", this.GetType().Name, LastFleetShipDestroyed.name);
        ProcessShipRemoval(LastFleetShipDestroyed);
    }

    private void InitializeAutoPilot() {
        //_autoPilot = gameObject.AddComponent<FleetAutoPilot>();
        _autoPilot = new FleetAutoPilot(this, gameObject.GetSafeMonoBehaviourComponent<Seeker>());
        _subscribers.Add(_autoPilot.SubscribeToPropertyChanged<FleetAutoPilot, bool>(ap => ap.IsFinalDestinationReached, OnFinalDestinationReachedChanged));
    }

    private void ProcessShipRemoval(ShipItem ship) {
        RemoveShip(ship);
        if (Ships.Count > Constants.Zero) {
            if (ship == Flagship) {
                // LeadShip has died
                Flagship = SelectBestShip();
            }
            return;
        }
        // Fleet knows when to die
    }

    private void RemoveShip(ShipItem ship) {
        bool isRemoved = Ships.Remove(ship);
        isRemoved = isRemoved && Data.RemoveShip(ship.Data);
        D.Assert(isRemoved, "{0} not found.".Inject(ship.Data.Name));
    }

    private ShipItem SelectBestShip() {
        return Ships.MaxBy(s => s.Data.Health);
    }

    protected override void Cleanup() {
        base.Cleanup();
        Data.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

