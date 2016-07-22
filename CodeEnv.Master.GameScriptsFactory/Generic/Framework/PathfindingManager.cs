// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PathfindingManager.cs
//  The singleton manager for the AStar Pathfinding system. 
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
using Pathfinding;
using UnityEngine;

// NOTE: Can't move this to GameScriptAssembly as it requires loose AStar scripts in Unity when compiled

/// <summary>
/// The singleton manager for the AStar Pathfinding system. 
/// </summary>
public class PathfindingManager : AMonoSingleton<PathfindingManager> {

    private MyPathfindingGraph _graph;
    public MyPathfindingGraph Graph {
        get { return _graph; }
        private set { SetProperty<MyPathfindingGraph>(ref _graph, value, "Graph"); }
    }

    private AstarPath _astarPath;
    private GameManager _gameMgr;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameMgr = GameManager.Instance;
        if (AstarPath.active == null) {
            // this Awake was called before AstarPath.Awake so AstarPath has not initialized yet
            AstarPath.OnAwakeSettings += AstarPathOnAwakeEventHandler;  // event raised on AstarPath.Awake(), right after AstarPath.active is set
        }
        else {
            // AstarPath Awake has already been called 
            InitializeAstarPath();
        }
    }

    private void InitializeAstarPath() {
        LogEvent();
        _astarPath = AstarPath.active;
        // As we can't reliably know which Awake() will be called first, scanOnStartup must be set to false in inspector to avoid initial scan
        D.Assert(_astarPath.scanOnStartup == false);
        __ValidateTagNames();
        Subscribe();
    }

    /// <summary>
    /// Validates the AStar tag names.
    /// <remarks>I can't programmatically set TagNames, so I'll validate the values I've typed into the editor.</remarks>
    /// </summary>
    private void __ValidateTagNames() {
        string[] tagNames = _astarPath.GetTagNames();
        D.Assert(tagNames[(int)Topography.OpenSpace.AStarTagValue()] == Topography.OpenSpace.GetValueName());
        D.Assert(tagNames[(int)Topography.Nebula.AStarTagValue()] == Topography.Nebula.GetValueName());
        D.Assert(tagNames[(int)Topography.DeepNebula.AStarTagValue()] == Topography.DeepNebula.GetValueName());
        D.Assert(tagNames[(int)Topography.System.AStarTagValue()] == Topography.System.GetValueName());
    }

    private void Subscribe() {
        _gameMgr.gameStateChanged += GameStateChangedEventHandler;
        AstarPath.OnLatePostScan += GraphScansCompletedEventHandler;
        AstarPath.OnGraphsUpdated += GraphUpdateCompletedEventHandler;
    }

    #region Event and Property Change Handler

    private void AstarPathOnAwakeEventHandler() {
        InitializeAstarPath();
        AstarPath.OnAwakeSettings -= AstarPathOnAwakeEventHandler;
    }

    private void GameStateChangedEventHandler(object sender, EventArgs e) {
        if (_gameMgr.CurrentState == GameState.GeneratingPathGraphs) {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.GeneratingPathGraphs, isReady: false);
            _astarPath.Scan();
        }
    }

    private void GraphScansCompletedEventHandler(AstarPath astarPath) {
        Graph = astarPath.graphs[0] as MyPathfindingGraph;  // as MyAStarPointGraph
        _gameMgr.RecordGameStateProgressionReadiness(this, GameState.GeneratingPathGraphs, isReady: true);
        // WARNING: I must not directly cause the game state to change as the other subscribers to GameStateChanged may not have been called yet. 
        // This GraphScansCompletedEvent occurs while we are still processing OnGameStateChanged.
    }

    /// <summary>
    /// Event handler for completed GraphObjectUpdates during runtime.
    /// </summary>
    /// <param name="astarPath">The astar path.</param>
    private void GraphUpdateCompletedEventHandler(AstarPath astarPath) {
        D.Assert(astarPath.graphs[0] == Graph);
        int connectionCount = Constants.Zero;
        int walkableNodeCount = Constants.Zero;
        Graph.GetNodes(delegate (GraphNode node) {   // while return true, passes each node to this anonymous method
            if (node.Walkable) {
                walkableNodeCount++;
                connectionCount += (node as PointNode).connections.Length;
            }
            return true;    // pass the next node
        });
        D.Log("{0} after graph update: WalkableNodeCount = {1}, ConnectionCount = {2}.", GetType().Name, walkableNodeCount, connectionCount);
    }

    #endregion

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _gameMgr.gameStateChanged -= GameStateChangedEventHandler;
        AstarPath.OnLatePostScan -= GraphScansCompletedEventHandler;
        AstarPath.OnGraphsUpdated -= GraphUpdateCompletedEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

