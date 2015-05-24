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
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Initializes Data for all items in the universe.
/// </summary>
public class __UniverseInitializer : AMonoSingleton<__UniverseInitializer> {

    public UniverseCenterItem UniverseCenter { get; private set; }

    private GameManager _gameMgr;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameMgr = GameManager.Instance;
        Subscribe();
    }

    private void Subscribe() {
        _gameMgr.onGameStateChanged += OnGameStateChanged;
    }

    private void OnGameStateChanged() {
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

    private void InitializeUniverseCenter() {
        UniverseCenter = gameObject.GetSafeMonoBehaviourInChildren<UniverseCenterItem>();
        if (UniverseCenter != null) {
            UniverseCenterData data = new UniverseCenterData(UniverseCenter.Transform, "UniverseCenter");
            UniverseCenter.Data = data;
            UniverseCenter.enabled = true;
        }
        UnityUtility.WaitOneToExecute(onWaitFinished: delegate {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.BuildAndDeploySystems, isReady: true);
        });
    }

    private void EnableOtherWhenRunning() {
        if (UniverseCenter != null) {
            // CameraLosChangedListener is enabled in Item.InitializeViewMembersOnDiscernible
        }
    }

    private void BeginOperations() {
        if (UniverseCenter != null) {
            UniverseCenter.CommenceOperations();
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _gameMgr.onGameStateChanged -= OnGameStateChanged;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

