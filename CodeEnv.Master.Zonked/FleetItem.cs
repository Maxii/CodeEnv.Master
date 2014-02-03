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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all fleets in the game.
/// </summary>
public class FleetItem : AItemModel {

    public new FleetData Data {
        get { return base.Data as FleetData; }
        set { base.Data = value; }
    }

    private ShipModel _flagship;
    public ShipModel Flagship {
        get { return _flagship; }
        set { SetProperty<ShipModel>(ref _flagship, value, "Flagship", OnFlagshipChanged); }
    }

    private ShipModel _lastFleetShipDestroyed;
    /// <summary>
    /// The most recent member of this fleet that has been destroyed. Can be 
    /// null if no ships belonging to this fleet have been destroyed.
    /// </summary>
    public ShipModel LastFleetShipDestroyed {
        get { return _lastFleetShipDestroyed; }
        set { SetProperty<ShipModel>(ref _lastFleetShipDestroyed, value, "LastFleetShipDestroyed", OnLastFleetShipDestroyedChanged); }
    }

    public IList<ShipModel> Ships { get; private set; }
    public FleetNavigator AutoPilot { get; private set; }
    public FleetStateMachine StateMachine { get; private set; }

    private GameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        Ships = new List<ShipModel>();
    }

    protected override void Start() {
        base.Start();
        Initialize();
    }

    private void Initialize() {
        InitializeAutoPilot();
        InitializeStateMachine();
    }

    private void InitializeAutoPilot() {
        ShipNavigator = new FleetNavigator(this, gameObject.GetSafeMonoBehaviourComponent<Seeker>());
        ShipNavigator.onDestinationReached += OnDestinationReached;
    }

    private void InitializeStateMachine() {
        StateMachine = new FleetStateMachine(this);
    }

    protected override void Subscribe() {
        base.Subscribe();
        _gameMgr = GameManager.Instance;
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    }

    protected override void Update() {
        base.Update();
        StateMachine.Update();
    }

    private void OnDestinationReached() {
        D.Log("{0} AutoPilot Destination {1} reached.", Data.Name, ShipNavigator.Destination);
        //__PlotRandomCourseAndEngageAutoPilot();
        StateMachine.CurrentState = FleetState.Idling;
    }

    private void OnGameStateChanged() {
        //D.Log("FleetItem.OnGameStateChanged event recieved. GameState = {0}.", _gameMgr.CurrentState);
        if (_gameMgr.CurrentState == GameState.Running) {
            // IMPROVE select LeadShipCaptain here for now as Data must be initialized first
            Flagship = RandomExtended<ShipModel>.Choice(Ships);
            __GetFleetUnderway();
        }
    }

    private void OnFlagshipChanged() {
        Data.FlagshipData = Flagship.Data;
    }

    public void __GetFleetUnderway() {
        StateMachine.CurrentState = FleetState.GoIntercept;
        //__PlotRandomCourseAndEngageAutoPilot();
        //ChangeFleetHeading(UnityEngine.Random.onUnitSphere);
        //ChangeFleetSpeed(2.0F);
    }

    public void __PlotRandomCourseAndEngageAutoPilot() {
        ShipNavigator.PlotCourse(UnityEngine.Random.onUnitSphere * 200F);
    }

    public bool ChangeFleetHeading(Vector3 newHeading, bool isManualOverride = true) {
        if (DebugSettings.Instance.StopShipMovement) {
            ShipNavigator.Disengage();
            return false;
        }
        if (isManualOverride) {
            ShipNavigator.Disengage();
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
            ShipNavigator.Disengage();
            return false;
        }
        if (isManualOverride) {
            ShipNavigator.Disengage();
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
        RemoveShip(LastFleetShipDestroyed);
    }


    /// <summary>
    /// Adds the ship to this fleet including parenting if needed.
    /// </summary>
    /// <param name="ship">The ship.</param>
    public void AddShip(ShipModel ship) {
        Ships.Add(ship);
        Data.AddShip(ship.Data);
        Transform parentFleetTransform = gameObject.GetSafeMonoBehaviourComponentInParents<FleetUnitCreator>().transform;
        if (ship.transform.parent != parentFleetTransform) {
            ship.transform.parent = parentFleetTransform;   // local position, rotation and scale are auto adjusted to keep ship unchanged in worldspace
        }
        // TODO consider changing flagship
    }

    public void RemoveShip(ShipModel ship) {
        bool isRemoved = Ships.Remove(ship);
        isRemoved = isRemoved && Data.RemoveShip(ship.Data);
        D.Assert(isRemoved, "{0} not found.".Inject(ship.Data.Name));
        if (Ships.Count > Constants.Zero) {
            if (ship == Flagship) {
                // LeadShip has died
                Flagship = SelectBestShip();
            }
            return;
        }
        // Fleet knows when to die
    }

    protected override void Die() {
        StateMachine.CurrentState = FleetState.Dead;
    }

    private ShipModel SelectBestShip() {
        return Ships.MaxBy(s => s.Data.Health);
    }

    protected override void Cleanup() {
        base.Cleanup();
        Data.Dispose();
    }

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

