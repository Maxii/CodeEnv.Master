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
public class __UniverseInitializer : AMonoBase {

    private UniverseCenterItem _universeCenter;

    private GameManager _gameMgr;
    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        Subscribe();
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
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
        _universeCenter = gameObject.GetSafeMonoBehaviourComponentInChildren<UniverseCenterItem>();
        if (_universeCenter != null) {
            UniverseCenterData data = new UniverseCenterData(_universeCenter.Transform, "UniverseCenter", 100000000F);
            _universeCenter.Data = data;
            _universeCenter.enabled = true;
        }
        UnityUtility.WaitOneToExecute(onWaitFinished: delegate {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.BuildAndDeploySystems, isReady: true);
        });
    }

    private void EnableOtherWhenRunning() {
        if (_universeCenter != null) {
            // CameraLosChangedListener is enabled in Item.InitializeViewMembersOnDiscernible
        }
    }

    private void BeginOperations() {
        if (_universeCenter != null) {
            _universeCenter.CommenceOperations();
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

