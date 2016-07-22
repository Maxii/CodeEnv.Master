// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: __UniverseInitializer.cs
// Initializes Data for all items in the universe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Initializes Data for all items in the universe.
/// </summary>
public class __UniverseInitializer : AMonoSingleton<__UniverseInitializer> {

    protected override bool IsRootGameObject { get { return true; } }

    public UniverseCenterItem UniverseCenter { get; private set; }

    private GameManager _gameMgr;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameMgr = GameManager.Instance;
        Subscribe();
    }

    private void Subscribe() {
        _gameMgr.gameStateChanged += GameStateChangedEventHandler;
    }

    #region Event and Property Change Handlers

    private void GameStateChangedEventHandler(object sender, EventArgs e) {
        LogEvent();
        GameState gameState = _gameMgr.CurrentState;
        if (gameState == GameState.BuildAndDeploySystems) {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.BuildAndDeploySystems, isReady: false);
            InitializeUniverseCenter();
            // SystemCreators build, initialize and deploy their system during this state, then they allow the state to progress
        }
        if (gameState == GameState.Running) {
            EnableOtherWhenRunning();
            BeginOperations();
        }
    }

    #endregion

    private void InitializeUniverseCenter() {
        UniverseCenter = GetComponentInChildren<UniverseCenterItem>();
        if (UniverseCenter != null) {
            float radius = TempGameValues.UniverseCenterRadius;
            float lowOrbitRadius = radius + 5F;
            CameraFocusableStat cameraStat = __MakeCameraStat(radius, lowOrbitRadius);
            UniverseCenter.Name = "UniverseCenter";
            UniverseCenterData data = new UniverseCenterData(UniverseCenter, cameraStat, radius, lowOrbitRadius);
            UniverseCenter.Data = data;
            // UC will be enabled when CommenceOperations() called
        }
        _gameMgr.RecordGameStateProgressionReadiness(this, GameState.BuildAndDeploySystems, isReady: true);
    }

    private void EnableOtherWhenRunning() {
        if (UniverseCenter != null) {
            // CameraLosChangedListener is enabled in Item.InitializeOnFirstDiscernibleToUser()
        }
    }

    private void BeginOperations() {
        if (UniverseCenter != null) {
            UniverseCenter.CommenceOperations();
        }
    }

    private CameraFocusableStat __MakeCameraStat(float radius, float lowOrbitRadius) {
        float minViewDistance = radius + 1F;
        float highOrbitRadius = lowOrbitRadius + TempGameValues.ShipCloseOrbitSlotDepth;
        float optViewDistance = highOrbitRadius + 1F;
        return new CameraFocusableStat(minViewDistance, optViewDistance, fov: 80F);
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _gameMgr.gameStateChanged -= GameStateChangedEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

