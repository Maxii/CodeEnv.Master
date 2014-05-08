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

    private GameManager _gameMgr;
    private IList<IDisposable> _subscribers;

    /// <summary>
    /// Systems that have no settlements. Note: The SystemModel is used 
    /// because the SystemCreator and its top-level gameobject is 
    /// destroyed once the system becomes operational.
    /// </summary>
    private Stack<SystemModel> _systemsWithoutSettlements;

    /// <summary>
    /// Settlements available to be deployed to systems. Note: These are the
    /// top level GameObjects where the SettlementCreator is located. The
    /// Creator's gameObject is used because the SettlementCreator is 
    /// removed once it becomes operational.
    /// </summary>
    private Stack<SettlementCmdModel> _deployableSettlements;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        Subscribe();
        RegisterGameStateProgressionReadiness(GameState.DeployingSystems, isReady: false);
        RegisterGameStateProgressionReadiness(GameState.DeployingSettlements, isReady: false);
        NotifySettlementsToRegisterWhenBuilt(); // SettlementUnitCreators, if active, are currently present in the scene from the beginning
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanging<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanging));
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    }

    private void OnGameStateChanging(GameState enteringGameState) {
        GameState exitingGameState = _gameMgr.CurrentState;
        if (exitingGameState == GameState.DeployingSystems) {
            // all the systemModels have been built so it is now safe to acquire them
            AcquireActiveSystemsInScene();
        }
    }

    private void OnGameStateChanged() {
        GameState gameState = _gameMgr.CurrentState;
        if (gameState == GameState.DeployingSystems) {
            InitializeUniverseCenter();
            // Systems build and initialize themselves during this state
            RegisterGameStateProgressionReadiness(GameState.DeployingSystems, isReady: true);
        }
        if (gameState == GameState.DeployingSettlements) {
            if (_deployableSettlements != null) {   // if null, no settlements have registered to be deployed during startup
                DeploySettlements();    // must occur after SystemData has been set...
            }
            RegisterGameStateProgressionReadiness(GameState.DeployingSettlements, isReady: true);
        }
        //InitializeUniverseCenterPlayerIntel();    // no longer needed as UniverseCenter.PlayerIntel.Coverage is fixed to Comprehensive
    }

    private void AcquireActiveSystemsInScene() {
        var systemModels = gameObject.GetSafeMonoBehaviourComponentsInChildren<SystemModel>();
        _systemsWithoutSettlements = new Stack<SystemModel>(systemModels);
    }

    private void NotifySettlementsToRegisterWhenBuilt() {
        var settlementCreators = gameObject.GetComponentsInChildren<SettlementUnitCreator>();
        settlementCreators.ForAll(creator => creator.onUnitBuildComplete_OneShot += RegisterSettlementForDeployment);
    }

    private void RegisterSettlementForDeployment(SettlementCmdModel settlement) {   // can occur in runtime if settlementCreator setup that way
        _deployableSettlements = _deployableSettlements ?? new Stack<SettlementCmdModel>();
        _deployableSettlements.Push(settlement);
        if (GameStatus.Instance.IsRunning) {
            DeploySettlements();
        }
    }

    private void DeploySettlements() {
        var availableSystemsCount = _systemsWithoutSettlements.Count;
        for (int i = 0; i < availableSystemsCount; i++) {
            if (_deployableSettlements.Count == 0) {
                // out of settlements to deploy
                break;
            }
            SettlementCmdModel settlement = _deployableSettlements.Pop();
            SystemModel system = _systemsWithoutSettlements.Pop();
            system.AssignSettlement(settlement);
        }

        if (_systemsWithoutSettlements.Count == 0) {
            // no more systems without Settlements, so destroy any remaining settlements
            int extraSettlementsCount = _deployableSettlements.Count;
            for (int i = 0; i < extraSettlementsCount; i++) {
                var settlement = _deployableSettlements.Pop();
                GameObject topLevelSettlementGo = settlement.Transform.parent.gameObject;
                D.Log("{0} is destroying leftover Settlement {1}.", GetType().Name, topLevelSettlementGo.name);
                Destroy(topLevelSettlementGo);
            }
        }
    }

    private void InitializeUniverseCenter() {
        var universeCenter = gameObject.GetSafeMonoBehaviourComponentInChildren<UniverseCenterModel>();
        if (universeCenter != null) {
            ItemData data = new ItemData("UniverseCenter");
            universeCenter.Data = data;
            universeCenter.enabled = true;
            universeCenter.gameObject.GetSafeMonoBehaviourComponent<UniverseCenterView>().enabled = true;
        }
    }

    // UNCLEAR how useful this is in the cases here
    private void RegisterGameStateProgressionReadiness(GameState stateToNotMoveBeyondUntilReady, bool isReady) {
        GameEventManager.Instance.Raise(new ElementReadyEvent(this, stateToNotMoveBeyondUntilReady, isReady));
    }

    // *****************************************************************************************************************************************************************

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

