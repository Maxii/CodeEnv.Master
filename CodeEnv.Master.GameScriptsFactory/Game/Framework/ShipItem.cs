// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipItem.cs
// The data-holding class for all ships in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all ships in the game.
/// </summary>
public class ShipItem : Item {

    public new ShipData Data {
        get { return base.Data as ShipData; }
        set { base.Data = value; }
    }

    public Navigator Navigator { get; private set; }

    private FleetItem _fleet;

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        GameObject parentFleet = _transform.parent.gameObject;
        _fleet = parentFleet.GetSafeMonoBehaviourComponentInChildren<FleetItem>();
    }

    protected override void OnDataChanged() {
        base.OnDataChanged();
        __InitializeNavigator();
    }

    public void ChangeHeading(Vector3 newHeading) {
        if (Navigator.ChangeHeading(newHeading)) {
            StartCoroutine(Navigator.ExecuteHeadingChange());
        }
        // else TODO
    }

    public void ChangeSpeed(float newRequestedSpeed) {
        if (Navigator.ChangeSpeed(newRequestedSpeed)) {
            StartCoroutine(Navigator.ExecuteSpeedChange());
        }
        // else TODO
    }

    public void __SimulateAttacked() {
        if (!DebugSettings.Instance.MakePlayerInvincible) {
            __OnHit(UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints + 1F));
        }
    }

    #region Velocity Debugger

    void Update() {
        //__CompareVelocity();
    }

    private Vector3 __lastPosition;
    private float __lastTime;
    private void __CompareVelocity() {
        Vector3 currentPosition = _transform.position;
        float distanceTraveled = Vector3.Distance(currentPosition, __lastPosition);
        __lastPosition = currentPosition;

        float currentTime = GameTime.RealTime_Game;
        float elapsedTime = currentTime - __lastTime;
        __lastTime = currentTime;
        float calcVelocity = distanceTraveled / elapsedTime;
        D.Log("Rigidbody.velocity = {0}, ShipData.currentSpeed = {1}, Calculated Velocity = {2}.",
            rigidbody.velocity.magnitude, Data.CurrentSpeed, calcVelocity);
    }

    #endregion

    private void __InitializeNavigator() {
        Navigator = new Navigator(_transform, Data);
    }

    protected override void Die() {
        _fleet.LastFleetShipDestroyed = this;
        // let fleetCmd process the loss before the destroyed ship starts processing its state changes
        base.Die();
    }

    private void __OnHit(float damage) {
        Data.CurrentHitPoints = Data.CurrentHitPoints - damage;
    }

    protected override void Cleanup() {
        base.Cleanup();
        Navigator.Dispose();
        Data.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

