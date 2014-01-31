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

    private SystemCreator[] _systemCreators;
    private UniverseCenterItem _universeCenter;
    private Stack<SettlementCreator> _settlementCreators;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        Subscribe();
        RegisterGameStateProgressionReadiness(GameState.DeployingSystems, isReady: false);
        RegisterGameStateProgressionReadiness(GameState.DeployingSettlements, isReady: false);
        AcquireObjectsPresentInScene();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    }

    // UNCLEAR how useful this is in the cases here
    private void RegisterGameStateProgressionReadiness(GameState stateToNotMoveBeyondUntilReady, bool isReady) {
        GameEventManager.Instance.Raise(new ElementReadyEvent(this, stateToNotMoveBeyondUntilReady, isReady));
    }

    private void AcquireObjectsPresentInScene() {
        _systemCreators = gameObject.GetSafeMonoBehaviourComponentsInChildren<SystemCreator>();
        _settlementCreators = new Stack<SettlementCreator>(gameObject.GetSafeMonoBehaviourComponentsInChildren<SettlementCreator>());
        _universeCenter = gameObject.GetSafeMonoBehaviourComponentInChildren<UniverseCenterItem>();
    }

    private void OnGameStateChanged() {
        if (_gameMgr.CurrentState == GameState.DeployingSystems) {
            InitializeUniverseCenter();
            RegisterGameStateProgressionReadiness(GameState.DeployingSystems, isReady: true);
        }
        if (_gameMgr.CurrentState == GameState.DeployingSettlements) {
            AssignSettlementsToSystems();   // must occur after SystemData has been set...
            RegisterGameStateProgressionReadiness(GameState.DeployingSettlements, isReady: true);
        }

        if (_gameMgr.CurrentState == GameState.RunningCountdown_1) {
            //InitializeUniverseCenterPlayerIntel();    // no longer needed as UniverseCenter.PlayerIntel.Coverage is fixed to Comprehensive
        }
    }

    private void InitializeUniverseCenter() {
        if (_universeCenter) {
            Data data = new Data("UniverseCenter");

            _universeCenter.Data = data;
            _universeCenter.enabled = true;
            _universeCenter.gameObject.GetSafeMonoBehaviourComponent<UniverseCenterView>().enabled = true;
        }
    }

    private void AssignSettlementsToSystems() {
        foreach (var sysCreator in _systemCreators) {
            if (!_settlementCreators.IsNullOrEmpty()) {
                var settlementCreator = _settlementCreators.Pop();
                sysCreator.gameObject.GetComponentInChildren<SystemItem>().AssignSettlement(settlementCreator);
            }
        }

        if (!_settlementCreators.IsNullOrEmpty()) {
            foreach (var settlementCreator in _settlementCreators) {
                settlementCreator.gameObject.SetActive(false);
            }
        }
    }


    // *****************************************************************************************************************************************************************


    /// <summary>
    /// PlayerIntelLevel changes immediately propogate through COs and Ships so initialize this last in case the change pulls Data.
    /// </summary>
    //private void InitializeUniverseCenterPlayerIntel() {
    //    if (_universeCenter != null) {  // allows me to deactivate it
    //        //_universeCenter.gameObject.GetSafeInterface<IViewable>().PlayerIntel = new Intel(IntelScope.Comprehensive, IntelSource.InfoNet);
    //        _universeCenter.gameObject.GetSafeInterface<IViewable>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
    //    }
    //}

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

