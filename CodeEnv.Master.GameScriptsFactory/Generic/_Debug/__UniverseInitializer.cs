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
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Initializes Data for all items in the universe.
/// </summary>
public class __UniverseInitializer : AMonoBase, IDisposable {

    private UniverseCenterModel _universeCenter;

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
            RegisterReadinessForGameStateProgression(GameState.BuildAndDeploySystems, isReady: false);
            InitializeUniverseCenter();
            // SystemCreators build, initialize and deploy their system during this state, then they allow the state to progress
        }
        if (gameState == GameState.Running) {
            EnableOtherWhenRunning();
        }
    }

    private void InitializeUniverseCenter() {
        _universeCenter = gameObject.GetSafeMonoBehaviourComponentInChildren<UniverseCenterModel>();
        if (_universeCenter != null) {
            float minimumShipOrbitDistance = _universeCenter.Radius * TempGameValues.KeepoutRadiusMultiplier;
            float maximumShipOrbitDistance = minimumShipOrbitDistance + TempGameValues.DefaultShipOrbitSlotDepth;
            UniverseCenterData data = new UniverseCenterData("UniverseCenter") {
                ShipOrbitSlot = new OrbitalSlot(minimumShipOrbitDistance, maximumShipOrbitDistance)
            };
            _universeCenter.Data = data;
            _universeCenter.enabled = true;
            _universeCenter.gameObject.GetSafeMonoBehaviourComponent<UniverseCenterView>().enabled = true;
        }
        UnityUtility.WaitOneToExecute(onWaitFinished: delegate {
            RegisterReadinessForGameStateProgression(GameState.BuildAndDeploySystems, isReady: true);
        });
    }

    private void EnableOtherWhenRunning() {
        D.Assert(GameStatus.Instance.IsRunning);
        if (_universeCenter != null) {
            _universeCenter.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().enabled = true;
            //_universeCenter.gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>().enabled = true;    // doesn't appear to be needed
        }
    }

    private void RegisterReadinessForGameStateProgression(GameState stateToNotProgressBeyondUntilReady, bool isReady) {
        GameEventManager.Instance.Raise(new ElementReadyEvent(this, stateToNotProgressBeyondUntilReady, isReady));
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}

