// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AStarPathManager.cs
//  The manager for the AStar Pathfinding system. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The manager for the AStar Pathfinding system. 
/// </summary>
public class AStarPathManager : AMonoBase, IDisposable {

    private IList<IDisposable> _subscribers;
    private AstarPath _astarPath;

    protected override void Awake() {
        base.Awake();
        InitializeAstarPath();
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
        AstarPath.OnLatePostScan += OnGraphScansCompleted;
    }

    private void InitializeAstarPath() {
        _astarPath = AstarPath.active;
        _astarPath.scanOnStartup = false;
        // IMPROVE max distance from a worldspace position to a node from my experimentation
        // assuming points surrounding obstacles are set at 0.05 grids away (0.5 * 0.1) from obstacle center
        // and interior sector points are 0.25 grids away from sector center (0.5 * 0.5)
        _astarPath.maxNearestNodeDistance = 600F;
        // TODO other settings
    }

    private void OnGameStateChanged() {
        if (GameManager.Instance.CurrentState == GameState.GeneratingPathGraphs) {
            AstarPath.active.Scan();
        }
    }

    private void OnGraphScansCompleted(AstarPath astarPath) {
        //if (GameManager.Instance.GameState == GameState.GeneratingPathGraphs) {
        //GameManager.Instance.__ProgressToRunning();
        // TODO need to tell GameManager that it is OK to progress the game state
        // WARNING: I must not directly cause the game state to change as the other subscribers to 
        // GameStateChanged may not have been called yet. This GraphScansCompletedEvent occurs 
        // while we are still processing OnGameStateChanged
        //}
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
        AstarPath.OnLatePostScan -= OnGraphScansCompleted;
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

