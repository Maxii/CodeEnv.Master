// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipCaptain.cs
// Manages the operation of a ship within a fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages the operation of a ship within a fleet.
/// </summary>
public class ShipCaptain : FollowableItem {

    public new ShipData Data {
        get { return base.Data as ShipData; }
        set { base.Data = value; }
    }

    public Navigator Navigator { get; private set; }

    private ShipGraphics _shipGraphics;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _shipGraphics = gameObject.GetSafeMonoBehaviourComponent<ShipGraphics>();
        UpdateRate = UpdateFrequency.Infrequent;
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
        __InitializeNavigator();
        HumanPlayerIntelLevel = IntelLevel.ShortRangeSensors;
        HudPublisher.SetOptionalUpdateKeys(GuiCursorHudLineKeys.Speed);
    }

    private void __InitializeNavigator() {
        Navigator = new Navigator(_transform, Data);
    }

    public void ChangeHeading(Vector3 newHeading) {
        if (Data.RequestedHeading != newHeading) {
            Navigator.ChangeHeading(newHeading);
        }
    }

    public void ChangeSpeed(float newRequestedSpeed) {
        Logger.Log("Current Requested Speed = {0}, New Requested Speed = {1}.", Data.RequestedSpeed, newRequestedSpeed);
        if (Data.RequestedSpeed != newRequestedSpeed) {
            Navigator.ChangeSpeed(newRequestedSpeed);
        }
    }

    void Update() {
        if (ToUpdate()) {
            bool isTurnUnderway = Navigator.TryProcessHeadingChange((int)UpdateRate);   // IMPROVE isTurnUnderway useful as a field?
        }
    }

    void FixedUpdate() {
        Navigator.ApplyThrust();
    }

    void OnDestroy() {
        Navigator.Dispose();
        Data.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public override bool IsTargetable {
        get {
            return _shipGraphics.IsShipShowing;
        }
    }

    #endregion
}

