// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrbitSimulator.cs
// Simulates orbiting around an immobile parent of any children of the simulator.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Simulates orbiting around an immobile parent of any children of the simulator.
/// </summary>
public class OrbitSimulator : AMonoBase, IOrbitSimulator {

    protected const string DebugNameFormat = "{0}.{1}";

    #region Static Spread UpdateOrbit Load Fields

    /// <summary>
    /// The number of frames over which to spread the UpdateOrbit() load.
    /// </summary>
    private const int FramesToSpreadUpdate = 4;

    /// <summary>
    /// The frameCount value that most recently completed execution of its UpdateOrbit() load.
    /// </summary>
    private static int _frameUpdated;

    /// <summary>
    /// The next index of _allOrbitSims that should have UpdateOrbit executed.
    /// </summary>
    private static int _nextSimUpdateIndex;

    /// <summary>
    /// All the OrbitSimulators currently present in the scene whether enabled or not.
    /// <remarks>Used List rather than HashSet to gain guaranteed iteration order.</remarks>
    /// </summary>
    private static IList<OrbitSimulator> _allOrbitSims = new List<OrbitSimulator>();

    /// <summary>
    /// Allows a one time static subscription to events from this class.
    /// </summary>
    private static bool _isStaticallySubscribed;

    #endregion

    /// <summary>
    /// The relative orbit speed of the object around the location. A value of 1 means
    /// an orbit will take one OrbitPeriod.
    /// </summary>
    [SerializeField]
    private float _relativeOrbitRate = 1.0F;

    private string _debugName;
    public virtual string DebugName {
        get {
            if (_debugName == null) {
                _debugName = DebugNameFormat.Inject(OrbitData.OrbitedItem.name, GetType().Name);
            }
            return _debugName;
        }
    }

    private bool _isActivated;
    /// <summary>
    /// Control for activating this OrbitSimulator. Activating the simulator does not necessarily
    /// cause the simulator to rotate as it may be set by the OrbitData to not rotate.
    /// <remarks>This has nothing to do with the active property of a GameObject.</remarks>
    /// </summary>
    public bool IsActivated {
        get { return _isActivated; }
        set { SetProperty<bool>(ref _isActivated, value, "IsActivated", IsActivatedPropChangedHandler); }
    }

    private Rigidbody _orbitRigidbody;
    public Rigidbody OrbitRigidbody {
        get {
            if (_orbitRigidbody == null) {
                _orbitRigidbody = gameObject.AddMissingComponent<Rigidbody>();
                _orbitRigidbody.useGravity = false;
                _orbitRigidbody.isKinematic = true;
            }
            return _orbitRigidbody;
        }
    }

    public int OrbitSlotIndex { get { return OrbitData.SlotIndex; } }

    private OrbitData _orbitData;
    public OrbitData OrbitData {
        get { return _orbitData; }
        set {
            D.AssertNull(_orbitData);   // one time only
            _orbitData = value;
            OrbitDataPropSetHandler();
        }
    }

    /// <summary>
    /// The speed of travel in units per hour of the OrbitingItem located at a radius of OrbitData.MeanRadius
    /// from the OrbitedItem. This value is always relative to the body being orbited.
    /// <remarks>The speed of a planet around a system is relative to an unmoving system, so this value
    /// is the speed the planet is traveling in the universe. Conversely, the speed of a moon around a planet
    /// is relative to the moving planet, so the value returned for the moon does not account for the 
    /// speed of the planet.</remarks>
    /// </summary>
    public float RelativeOrbitSpeed { get; private set; }

    /// <summary>
    /// The axis of orbit in local space.
    /// </summary>
    protected Vector3 _axisOfOrbit = Vector3.up;

    /// <summary>
    /// The rate this OrbitSimulator orbits around the orbited object in degrees per hour.
    /// </summary>
    protected float _orbitRateInDegreesPerHour;
    protected GameTime _gameTime;

    private IList<IDisposable> _subscriptions;
    private IGameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameReferences.GameManager;
        _gameTime = GameTime.Instance;
        Subscribe();
        enabled = false;

        _allOrbitSims.Add(this);
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
        SubscribeStaticallyOnce();
    }

    /// <summary>
    /// Subscribes this class using static event handler(s) to instance events exactly one time.
    /// </summary>
    private void SubscribeStaticallyOnce() {
        if (!_isStaticallySubscribed) {
            //D.Log("{0} is subscribing statically to {1}'s sceneLoading event.", typeof(OrbitSimulator).Name, _gameMgr.GetType().Name);
            _gameMgr.sceneLoading += SceneLoadingEventHandler;
            _isStaticallySubscribed = true;
        }
    }

    private float InitializeOrbitSpeed() {
        float orbitSpeedInUnitsPerHour = (2F * Mathf.PI * OrbitData.MeanRadius) / (OrbitData.OrbitPeriod.TotalInHours / _relativeOrbitRate);
        if (!(this is ShipCloseOrbitSimulator)) {
            if (orbitSpeedInUnitsPerHour > TempGameValues.__MaxPlanetoidOrbitSpeed) {
                D.Warn("{0} orbitSpeed {1:0.0000} > max {2:0.0000}.", DebugName, orbitSpeedInUnitsPerHour, TempGameValues.__MaxPlanetoidOrbitSpeed);
            }
        }
        return orbitSpeedInUnitsPerHour;
    }

    private void Update() {
#if UNITY_EDITOR
        if (_frameUpdated != Time.frameCount || !Application.isPlaying) {
#else
        if(_frameUpdated != Time.frameCount) {
#endif
            // Spreads the UpdateOrbit() load over a designated number of frames - FramesToSpreadUpdate

            // This Simulator is the first to have its Update called in the new frame indicated by Time.frameCount.
            // As a result, it gets to call UpdateOrbit() for the Simulators that are next in line to run.

            _frameUpdated = Time.frameCount;

            int allSimCount = _allOrbitSims.Count;
            if (_nextSimUpdateIndex >= allSimCount) {
                _nextSimUpdateIndex = Constants.Zero;
            }

            int simQtyToUpdate = Mathf.CeilToInt(allSimCount / (float)FramesToSpreadUpdate);
            int lastSimUpdateIndex = _nextSimUpdateIndex + simQtyToUpdate - 1;
            if (lastSimUpdateIndex > allSimCount - 1) {
                lastSimUpdateIndex = allSimCount - 1;
            }

            float deltaTime = _gameTime.DeltaTime * FramesToSpreadUpdate;
            for (int i = _nextSimUpdateIndex; i <= lastSimUpdateIndex; i++) {
                var sim = _allOrbitSims[i];
                if (sim.enabled) {
                    sim.UpdateOrbit(deltaTime);
                }
            }
            //D.LogBold("{0} updated OrbitSimulator Indices {1} - {2} during Frame {3}. AllSimCount = {4}.",
            //DebugName, _nextSimUpdateIndex, lastSimUpdateIndex, _frameUpdated, allSimCount);
            _nextSimUpdateIndex = lastSimUpdateIndex + 1;
        }
    }

    /// <summary>
    /// Updates the rotation of this object around its axis of orbit (it is coincident with the position of the object being orbited)
    /// to simulate the orbit of this object's child around the object orbited. The visual speed of the orbit varies with game speed.
    /// OPTIMIZE Consider calling this centrally every x updates.
    /// </summary>
    /// <param name="deltaTimeSinceLastUpdate">The delta time since last update.</param>
    protected virtual void UpdateOrbit(float deltaTimeSinceLastUpdate) {
        float degreesToRotate = _orbitRateInDegreesPerHour * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTimeSinceLastUpdate;
        transform.Rotate(_axisOfOrbit, degreesToRotate, relativeTo: Space.Self);
    }

    #region Event and Property Change Handlers

    private void IsActivatedPropChangedHandler() {
        D.Assert(_gameMgr.IsRunning);
        AssessEnabled();
    }

    private void IsPausedPropChangedHandler() {
        AssessEnabled();
    }

    private void OrbitDataPropSetHandler() {
        _orbitRateInDegreesPerHour = _relativeOrbitRate * Constants.DegreesPerOrbit / (float)OrbitData.OrbitPeriod.TotalInHours;
        RelativeOrbitSpeed = InitializeOrbitSpeed();
    }

    private static void SceneLoadingEventHandler(object sender, EventArgs e) {
        CleanupStaticMembers();
    }

    #endregion

    private void AssessEnabled() {
        enabled = OrbitData.ToOrbit && IsActivated && !_gameMgr.IsPaused;
    }

    protected override void Cleanup() {
        Unsubscribe();
        _allOrbitSims.Remove(this);
        if (IsApplicationQuiting) {
            UnsubscribeStaticallyOnceOnQuit();
        }
    }

    /// <summary>
    /// Cleans up static members of this class whose value should not persist across scenes.
    /// UNCLEAR This is called whether the scene loaded is from a saved game or a new game. 
    /// Should static values be reset on a scene change from a saved game? 1) do the static members
    /// retain their value after deserialization, and/or 2) can static members even be serialized? 
    /// </summary>
    private static void CleanupStaticMembers() {
        //D.Log("{0}'s static CleanupStaticMembers() called.", typeof(OrbitSimulator).Name);
        _nextSimUpdateIndex = Constants.Zero;
        _allOrbitSims.Clear();
    }

    /// <summary>
    /// Unsubscribes this class from all events that use a static event handler on Quit.
    /// </summary>
    private void UnsubscribeStaticallyOnceOnQuit() {
        if (_isStaticallySubscribed) {
            //D.Log("{0} is unsubscribing statically to {1}.", typeof(OrbitSimulator).Name, _gameMgr.GetType().Name);
            _gameMgr.sceneLoaded -= SceneLoadingEventHandler;
            _isStaticallySubscribed = false;
        }
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(d => d.Dispose());
        _subscriptions.Clear();
    }

    public override string ToString() {
        return DebugName;
    }


}

