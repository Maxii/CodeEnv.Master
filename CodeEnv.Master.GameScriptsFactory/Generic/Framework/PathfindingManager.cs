// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PathfindingManager.cs
// The singleton manager for the AStar Pathfinding system. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using Pathfinding;
using UnityEditor;
using UnityEngine;

// NOTE: Can't move this to GameScriptAssembly as it requires loose AStar scripts in Unity when compiled

/// <summary>
/// The singleton manager for the AStar Pathfinding system. 
/// </summary>
public class PathfindingManager : AMonoSingleton<PathfindingManager> {

    /// <summary>
    /// Occurs when [graph update completed].
    /// 8.16.16 Does not currently fire as I'm using WorkItems in place of GraphUpdate
    /// TODO fleets should re-plot their paths when this occurs.
    /// </summary>
    public event EventHandler graphUpdateCompleted;

    [Tooltip("Check to exit editor play mode before Pathfinding scanning begins")]
    [SerializeField]
    private bool _abortScan = false;

    private string DebugName { get { return GetType().Name; } }

    private MyNGPathfindingGraph _graph;
    public MyNGPathfindingGraph Graph {
        get { return _graph; }
        private set { SetProperty<MyNGPathfindingGraph>(ref _graph, value, "Graph"); }
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
        D.Assert(!_astarPath.scanOnStartup);
        __ValidateTagNames();
        Subscribe();
    }

    /// <summary>
    /// Validates the AStar tag names.
    /// <remarks>I can't programmatically set TagNames, so I'll validate the values I've typed into the editor.</remarks>
    /// </summary>
    private void __ValidateTagNames() {
        string[] tagNames = _astarPath.GetTagNames();
        D.AssertEqual(Topography.OpenSpace.GetValueName(), tagNames[(int)Topography.OpenSpace.AStarTagValue()]);
        D.AssertEqual(Topography.Nebula.GetValueName(), tagNames[(int)Topography.Nebula.AStarTagValue()]);
        D.AssertEqual(Topography.DeepNebula.GetValueName(), tagNames[(int)Topography.DeepNebula.AStarTagValue()]);
        D.AssertEqual(Topography.System.GetValueName(), tagNames[(int)Topography.System.AStarTagValue()]);
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
            if (_abortScan) {
                D.LogBold("{0} is aborting scan and exiting Editor.Play.", DebugName);
                // 7.11.18 ConstructSectors/DeployingSystemCreators can still take a lot of time on larger universes
                // Gigantic ~ 50/200 seconds, Enormous ~ 20/80 seconds
                EditorApplication.isPlaying = false;
                return;
            }
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.GeneratingPathGraphs, isReady: false);
            //D.Log("{0} calling AstarPath.Scan on Frame {1}.", DebugName, Time.frameCount);
            _astarPath.Scan();
        }
    }

    private void GraphScansCompletedEventHandler(AstarPath astarPath) {
        Graph = astarPath.graphs[0] as MyNGPathfindingGraph;
        //D.Log("{0} AstarPath.Scan has completed on Frame {1}.", DebugName, Time.frameCount);
        _gameMgr.RecordGameStateProgressionReadiness(this, GameState.GeneratingPathGraphs, isReady: true);
        // WARNING: I must not directly cause the game state to change as the other subscribers to GameStateChanged may not have been called yet. 
        // This GraphScansCompletedEvent occurs while we are still processing OnGameStateChanged.
    }

    /// <summary>
    /// Event handler for completed GraphObjectUpdates during runtime.
    /// <remarks>8.17.16 not currently using UpdateGraph(GUO) in MyPathfindingGraph. I've replaced
    /// it with the use of multiple work items which accomplishes the same thing but doesn't generate this callback.</remarks>
    /// </summary>
    /// <param name="astarPath">The AStar path.</param>
    private void GraphUpdateCompletedEventHandler(AstarPath astarPath) {
        D.AssertEqual(Graph, astarPath.graphs[0]);
        OnGraphUpdateCompleted();
        //__ReportUpdateDuration();
        //__ReportNodeCountAndDuration();
        D.Warn("{0}.GraphUpdateCompletedEventHandler() called. 3.7.17 Not using it.", DebugName);
    }

    private void OnGraphUpdateCompleted() {
        if (graphUpdateCompleted != null) {
            graphUpdateCompleted(this, EventArgs.Empty);
        }
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
        return DebugName;
    }

    #region Debug

    //private void __ReportUpdateDuration() {
    //    D.LogBold("{0} took {1:0.##} secs updating graph for the addition/removal of one or more StarBases.",
    //        DebugName, (Utility.SystemTime - Graph.__GraphUpdateStartTime).TotalSeconds);
    //    Graph.__GraphUpdateStartTime = default(System.DateTime);
    //}

    //private void __ReportNodeCountAndDuration() {
    //    System.DateTime nodeCountStartTime = Utility.SystemTime;

    //    int connectionCount = Constants.Zero;
    //    int walkableNodeCount = Constants.Zero;
    //    Graph.GetNodes(delegate (GraphNode node) {   // while return true, passes each node to this anonymous method
    //        if (node.Walkable) {
    //            walkableNodeCount++;
    //            connectionCount += (node as PointNode).connections.Length;
    //        }
    //        return true;    // pass the next node
    //    });

    //    D.Log("{0}: Counting graph update nodes took {1:0.##} secs. WalkableNodeCount = {2}, ConnectionCount = {3}.",
    //        DebugName, (Utility.SystemTime - nodeCountStartTime).TotalSeconds, walkableNodeCount, connectionCount);
    //}

    #endregion

}

