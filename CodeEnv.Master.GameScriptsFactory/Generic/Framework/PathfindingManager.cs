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

    public MyAStarPointGraph Graph { get; private set; }

    private AstarPath _astarPath;
    private GameManager _gameMgr;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameMgr = GameManager.Instance;
        InitializeAstarPath();
        Subscribe();
    }

    private void Subscribe() {
        _gameMgr.onGameStateChanged += OnGameStateChanged;
        AstarPath.OnLatePostScan += OnGraphScansCompleted;
        AstarPath.OnGraphsUpdated += OnGraphRuntimeUpdateCompleted;
    }

    private void InitializeAstarPath() {
        _astarPath = AstarPath.active;
        _astarPath.scanOnStartup = false;
        // IMPROVE 600 seems a good max distance from a worldspace position to a node from my experimentation
        // This distance is used during the construction of a path. MaxDistance is used in the generation of a point graph
        // Assumptions:
        // points surrounding obstacles are set at 0.05 grids away (0.5 * 0.1) from obstacle center
        // interior sector points are 0.25 grids away from sector center (0.5 * 0.5) ~ 520 (0.25 x sectorDiag of 2078)
        //_astarPath.maxNearestNodeDistance = 600F; // trying no constraint for now - controlled by FleetCmdModel.GenerateCourse()
        //_astarPath.logPathResults = PathLog.Heavy;    // Editor controls will work if save change as prefab

        // Can't programmatically set TagNames. They appear to only be setable through the Editor
    }

    private void OnGameStateChanged() {
        if (_gameMgr.CurrentState == GameState.GeneratingPathGraphs) {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.GeneratingPathGraphs, isReady: false);
            AstarPath.active.Scan();
        }
    }

    private void OnGraphScansCompleted(AstarPath astarPath) {
        Graph = astarPath.graphs[0] as MyAStarPointGraph;
        _gameMgr.RecordGameStateProgressionReadiness(this, GameState.GeneratingPathGraphs, isReady: true);
        // WARNING: I must not directly cause the game state to change as the other subscribers to GameStateChanged may not have been called yet. 
        // This GraphScansCompletedEvent occurs while we are still processing OnGameStateChanged.
    }

    private void OnGraphRuntimeUpdateCompleted(AstarPath script) {
        D.Assert(script.graphs[0] == Graph);
        D.Log("{0} node count after graph update.", Graph.nodeCount);
        Graph.GetNodes(delegate(GraphNode node) {   // while return true, passes each node to this anonymous method
            if (!node.Walkable) {
                D.Log("Node {0} is not walkable.", (Vector3)node.position);
                return false;   // no need to pass any more nodes
            }
            return true;    // pass the next node
        });
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _gameMgr.onGameStateChanged -= OnGameStateChanged;
        AstarPath.OnLatePostScan -= OnGraphScansCompleted;
        AstarPath.OnGraphsUpdated -= OnGraphRuntimeUpdateCompleted;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

