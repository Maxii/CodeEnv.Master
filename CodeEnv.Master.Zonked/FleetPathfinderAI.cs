// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetPathfinderAI.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using Pathfinding;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public class FleetPathfinderAI : AMonoBase {

    public Vector3 pathDestination;

    private Vector3 _pathStart;

    private Path _path;
    public Path Path {
        get { return _path; }
        private set { SetProperty<Path>(ref _path, value, "Path", OnPathChanged); }
    }

    private int _currentWaypoint;
    private float _arrivedAtWaypointDistanceSqrd = 25F;

    private Seeker _seeker;
    private FleetCmdModel _fleet;
    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        _seeker = gameObject.GetSafeMonoBehaviourComponent<Seeker>();
        _fleet = gameObject.GetSafeMonoBehaviourComponent<FleetCmdModel>();
        Subscribe();
    }

    protected override void Start() {
        base.Start();
        _pathStart = _transform.position;
        D.Log("Path start = {0}, target = {1}.", _pathStart, pathDestination);
        Debug.DrawLine(_pathStart, pathDestination, Color.yellow, 20F, false);
        //Path path = new Path(startPosition, targetPosition, null);    // Path is now abstract
        //Path path = PathPool<ABPath>.GetPath();   // don't know how to assign start and target points
        Path path = ABPath.Construct(_pathStart, pathDestination, null);

        // Node qualifying constraint instance that checks that nodes are walkable, and within the seeker-specified
        // max search distance. Tags and area testing are turned off, primarily because I don't yet understand them
        NNConstraint constraint = new NNConstraint();
        constraint.constrainTags = false;
        path.nnConstraint = constraint;

        _seeker.StartPath(path);

        // this simple default version uses a constraint that has tags enabled which made finding close nodes problematic
        //_seeker.StartPath(startPosition, targetPosition); 
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        // _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.GameState, OnGameStateChanged));
        _seeker.pathCallback += OnPathCreated;
    }

    private void OnPathCreated(Path path) {
        if (path.error) {
            D.Error("{0} error generating path from {1} to {2}. {3}.", _transform.name, _pathStart, pathDestination, path.errorLog);
            return;
        }
        Path = path;
        _currentWaypoint = 0;

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

        _seeker.pathCallback -= OnPathCreated;
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

