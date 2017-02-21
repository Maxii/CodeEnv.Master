// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyPathfindingGraph.cs
// My implementation of the AStar Graph Generator using the points generated from SectorGrid's GridFramework. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace Pathfinding {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.GameContent;
    using Serialization;
    using UnityEngine;

    /// <summary>
    /// My implementation of the AStar Graph Generator using the points generated from SectorGrid's GridFramework. 
    /// WARNING: These graphs ARE NOT MonoBehaviours, in spite of the authors usage of Awake().
    /// </summary>
    [JsonOptIn]
    public class MyPathfindingGraph : NavGraph, IUpdatableGraph {

        // 6.13.16 AStarPro 3.8.2 no longer requires a tag mask
        //public static read-only int OpenSpaceTagMask = 1 << Topography.OpenSpace.AStarTagValue();   // x0001 
        //public static read-only int NebulaTagMask = 1 << Topography.Nebula.AStarTagValue();         // x0010 
        //public static read-only int DeepNebulaTagMask = 1 << Topography.DeepNebula.AStarTagValue(); // x0100 
        //public static read-only int SystemTagMask = 1 << Topography.System.AStarTagValue();         // x1000 

        /// <summary>
        /// The size of the grid of Sectors this Pathfinding system will scan for waypoint interconnection.
        /// </summary>
        [System.Obsolete]
        private static readonly IntVector3 __MaxAllowedSectorGridSizeToScan = new IntVector3(4, 4, 4);  // limit to divisible by 2

        private static readonly Int3[] ThreeDNeighbours = {
            new Int3(-1,  0, -1),
            new Int3(0,  0, -1),
            new Int3(1,  0, -1),

            new Int3(-1,  0,  0),
            new Int3(0,  0,  0),
            new Int3(1,  0,  0),

            new Int3(-1,  0,  1),
            new Int3(0,  0,  1),
            new Int3(1,  0,  1),


            new Int3(-1, -1, -1),
            new Int3(0, -1, -1),
            new Int3(1, -1, -1),

            new Int3(-1, -1,  0),
            new Int3(0, -1,  0),
            new Int3(1, -1,  0),

            new Int3(-1, -1,  1),
            new Int3(0, -1,  1),
            new Int3(1, -1,  1),


            new Int3(-1,  1, -1),
            new Int3(0,  1, -1),
            new Int3(1,  1, -1),

            new Int3(-1,  1,  0),
            new Int3(0,  1,  0),
            new Int3(1,  1,  0),

            new Int3(-1,  1,  1),
            new Int3(0,  1,  1),
            new Int3(1,  1,  1),
        };

        /** Max distance for a connection to be valid.
         * The value 0 (zero) will be read as infinity and thus all nodes not restricted by
         * other constraints will be added as connections.
         *
         * A negative value will disable any neighbors to be added.
         * It will completely stop the connection processing to be done, so it can save you processing
         * power if you don't these connections.
         */
        [JsonMember]
        public float maxDistance;

        [JsonMember]
        public bool autoLinkNodes = true;

        /** Optimizes the graph for sparse graphs.
         *
         * This can reduce calculation times for both scanning and for normal path requests by huge amounts.
         *
         * You should enable this when your #maxDistance and/or #limits variables are set relatively low compared to the world
         * size. It reduces the number of node-node checks that need to be done during scan, and can also optimize getting the nearest node from the graph (such as when querying for a path).
         *
         * Try enabling and disabling this option, check the scan times logged when you scan the graph to see if your graph is suited for this optimization
         * or if it makes it slower.
         *
         * The gain of using this optimization increases with larger graphs, the default scan algorithm is brute force and requires O(n^2) checks, 
         * this optimization along with a graph suited for it, requires only O(n) checks during scan.
         *
         * \note
         * When you have this enabled, you will not be able to move nodes around using scripting unless you recalculate the lookup structure at the same time.
         * \see RebuildNodeLookup
         *
         * \astarpro
         */
        [JsonMember]
        public bool optimizeForSparseGraph;

        /** All nodes in this graph.
         * Note that only the first #nodeCount will be non-null.
         *
         * You can also use the GetNodes method to get all nodes.
         */
        public PointNode[] nodes;

        /** Number of nodes in this graph.
         *
         * \warning Do not edit directly
         */
        public int nodeCount;

        private Dictionary<Int3, PointNode> _nodeLookup;
        private Int3 _minLookup;
        private Int3 _maxLookup;
        private Int3 _lookupCellSize;

        public override int CountNodes() {
            return nodeCount;
        }

        public override void GetNodes(GraphNodeDelegateCancelable del) {
            if (nodes == null) { return; }
            for (int i = 0; i < nodeCount && del(nodes[i]); i++) { }
        }

        public override NNInfo GetNearest(Vector3 position, NNConstraint constraint, GraphNode hint) {
            return GetNearestForce(position, constraint);
        }

        public override NNInfo GetNearestForce(Vector3 position, NNConstraint constraint) {

            if (nodes == null) return new NNInfo();

            float maxDistSqr = constraint.constrainDistance ? AstarPath.active.maxNearestNodeDistanceSqr : float.PositiveInfinity;

            float minDist = float.PositiveInfinity;
            GraphNode minNode = null;

            float minConstDist = float.PositiveInfinity;
            GraphNode minConstNode = null;

            if (optimizeForSparseGraph) {
                Int3 lookupStart = WorldToLookupSpace((Int3)position);

                Int3 size = lookupStart - _minLookup;

                int mw = 0;
                mw = System.Math.Max(mw, System.Math.Abs(size.x));
                mw = System.Math.Max(mw, System.Math.Abs(size.y));
                mw = System.Math.Max(mw, System.Math.Abs(size.z));

                size = lookupStart - _maxLookup;
                mw = System.Math.Max(mw, System.Math.Abs(size.x));
                mw = System.Math.Max(mw, System.Math.Abs(size.y));
                mw = System.Math.Max(mw, System.Math.Abs(size.z));

                PointNode node;

                if (_nodeLookup.TryGetValue(lookupStart, out node)) {
                    while (node != null) {
                        float dist = (position - (Vector3)node.position).sqrMagnitude;
                        if (dist < minDist) { minDist = dist; minNode = node; }
                        if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) { minConstDist = dist; minConstNode = node; }

                        node = node.next;
                    }
                }

                for (int w = 1; w <= mw; w++) {
                    if (w >= 20) {
                        Debug.LogWarning("Aborting GetNearest call at maximum distance because it has iterated too many times.\n" +
                            "If you get this regularly, check your settings for PointGraph -> <b>Optimize For Sparse Graph</b> and " +
                            "PointGraph -> <b>Optimize For 2D</b>.\nThis happens when the closest node was very far away (20*link distance between nodes). " +
                            "When optimizing for sparse graphs, getting the nearest node from far away positions is <b>very slow</b>.\n");
                        break;
                    }

                    if (_lookupCellSize.y == 0) {
                        Int3 reference = lookupStart + new Int3(-w, 0, -w);

                        for (int x = 0; x <= 2 * w; x++) {
                            if (_nodeLookup.TryGetValue(reference + new Int3(x, 0, 0), out node)) {
                                while (node != null) {
                                    float dist = (position - (Vector3)node.position).sqrMagnitude;
                                    if (dist < minDist) { minDist = dist; minNode = node; }
                                    if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) { minConstDist = dist; minConstNode = node; }

                                    node = node.next;
                                }
                            }
                            if (_nodeLookup.TryGetValue(reference + new Int3(x, 0, 2 * w), out node)) {
                                while (node != null) {
                                    float dist = (position - (Vector3)node.position).sqrMagnitude;
                                    if (dist < minDist) { minDist = dist; minNode = node; }
                                    if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) { minConstDist = dist; minConstNode = node; }

                                    node = node.next;
                                }
                            }
                        }

                        for (int x = 1; x < 2 * w; x++) {
                            if (_nodeLookup.TryGetValue(reference + new Int3(0, 0, x), out node)) {
                                while (node != null) {
                                    float dist = (position - (Vector3)node.position).sqrMagnitude;
                                    if (dist < minDist) { minDist = dist; minNode = node; }
                                    if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) { minConstDist = dist; minConstNode = node; }

                                    node = node.next;
                                }
                            }
                            if (_nodeLookup.TryGetValue(reference + new Int3(2 * w, 0, x), out node)) {
                                while (node != null) {
                                    float dist = (position - (Vector3)node.position).sqrMagnitude;
                                    if (dist < minDist) { minDist = dist; minNode = node; }
                                    if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) { minConstDist = dist; minConstNode = node; }

                                    node = node.next;
                                }
                            }
                        }
                    }
                    else {
                        Int3 reference = lookupStart + new Int3(-w, -w, -w);

                        for (int x = 0; x <= 2 * w; x++) {
                            for (int y = 0; y <= 2 * w; y++) {
                                if (_nodeLookup.TryGetValue(reference + new Int3(x, y, 0), out node)) {
                                    while (node != null) {
                                        float dist = (position - (Vector3)node.position).sqrMagnitude;
                                        if (dist < minDist) { minDist = dist; minNode = node; }
                                        if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) { minConstDist = dist; minConstNode = node; }

                                        node = node.next;
                                    }
                                }
                                if (_nodeLookup.TryGetValue(reference + new Int3(x, y, 2 * w), out node)) {
                                    while (node != null) {
                                        float dist = (position - (Vector3)node.position).sqrMagnitude;
                                        if (dist < minDist) { minDist = dist; minNode = node; }
                                        if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) { minConstDist = dist; minConstNode = node; }

                                        node = node.next;
                                    }
                                }
                            }
                        }

                        for (int x = 1; x < 2 * w; x++) {
                            for (int y = 0; y <= 2 * w; y++) {
                                if (_nodeLookup.TryGetValue(reference + new Int3(0, y, x), out node)) {
                                    while (node != null) {
                                        float dist = (position - (Vector3)node.position).sqrMagnitude;
                                        if (dist < minDist) { minDist = dist; minNode = node; }
                                        if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) { minConstDist = dist; minConstNode = node; }

                                        node = node.next;
                                    }
                                }
                                if (_nodeLookup.TryGetValue(reference + new Int3(2 * w, y, x), out node)) {
                                    while (node != null) {
                                        float dist = (position - (Vector3)node.position).sqrMagnitude;
                                        if (dist < minDist) { minDist = dist; minNode = node; }
                                        if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) { minConstDist = dist; minConstNode = node; }

                                        node = node.next;
                                    }
                                }
                            }
                        }

                        for (int x = 1; x < 2 * w; x++) {
                            for (int y = 1; y < 2 * w; y++) {
                                if (_nodeLookup.TryGetValue(reference + new Int3(x, 0, y), out node)) {
                                    while (node != null) {
                                        float dist = (position - (Vector3)node.position).sqrMagnitude;
                                        if (dist < minDist) { minDist = dist; minNode = node; }
                                        if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) { minConstDist = dist; minConstNode = node; }

                                        node = node.next;
                                    }
                                }
                                if (_nodeLookup.TryGetValue(reference + new Int3(x, 2 * w, y), out node)) {
                                    while (node != null) {
                                        float dist = (position - (Vector3)node.position).sqrMagnitude;
                                        if (dist < minDist) { minDist = dist; minNode = node; }
                                        if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) { minConstDist = dist; minConstNode = node; }

                                        node = node.next;
                                    }
                                }
                            }
                        }
                    }

                    if (minConstNode != null) {
                        // Only search one more layer
                        mw = System.Math.Min(mw, w + 1);
                    }
                }
            }
            else {

                for (int i = 0; i < nodeCount; i++) {
                    PointNode node = nodes[i];
                    float dist = (position - (Vector3)node.position).sqrMagnitude;

                    if (dist < minDist) {
                        minDist = dist;
                        minNode = node;
                    }

                    if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node))) {
                        minConstDist = dist;
                        minConstNode = node;
                    }
                }
            }

            var nnInfo = new NNInfo(minNode);

            nnInfo.constrainedNode = minConstNode;

            if (minConstNode != null) {
                nnInfo.constClampedPosition = (Vector3)minConstNode.position;
            }
            else if (minNode != null) {
                nnInfo.constrainedNode = minNode;
                nnInfo.constClampedPosition = (Vector3)minNode.position;
            }

            #region Debugging

            //D.Log("Constraint: GraphMask: {0}, ConstrainArea: {1}, Area: {2}, ConstrainWalkability: {3}, \nWalkable: {4}, ConstrainTags: {5}, Tags: {6}, ConstrainDistance: {7}.",
            //    constraint.graphMask, constraint.constrainArea, constraint.area, constraint.constrainWalkability, constraint.walkable,
            //    constraint.constrainTags, constraint.tags, constraint.constrainDistance);

            //if (minConstNode != null) {
            //    D.Log("Constraint criteria met. Closest Node is at {0}, {1} from {2}. \nNodeConstrainDistance = {3}, DistanceConstraint = {4}.",
            //        nnInfo.constClampedPosition, Vector3.Distance(nnInfo.constClampedPosition, position), position,
            //        constraint.constrainDistance, Mathf.Sqrt(maxDistSqr));
            //}
            //else {
            //    D.Log("Constraint criteria NOT met. Closest Node is at {0}, {1} from {2}. \nNodeConstrainDistance = {3}, DistanceConstraint = {4}.",
            //        nnInfo.clampedPosition, Vector3.Distance(nnInfo.clampedPosition, position), position,
            //        constraint.constrainDistance, Mathf.Sqrt(maxDistSqr));
            //}

            #endregion

            return nnInfo;
        }

        /* Per Aren: 8.16.16 SparseGraph Algorithm: In your version what it did was to divide the world into cells with a size of 
         * maxNodeDistance(I think...), and then it inserted all nodes into those cells. If multiple nodes were in the same cell, 
         * they would be stored as a linked list(using the .next field). When searching for a node, it would search the cells 
         * in an outward pattern. When connecting them, it could just check a few cells around each node.

           In the newer version I rewrote it to use a self balancing KD-tree instead. That will also perform a lot better when 
           searching for the nearest node to a point which is far away from all nodes (previously it would have to search a lot of empty cells).
        */

        /** Rebuilds the lookup structure for nodes.
         *
         * This is used when #optimizeForSparseGraph is enabled.
         *
         * You should call this method every time you move a node in the graph manually and
         * you are using #optimizeForSparseGraph, otherwise pathfinding might not work correctly.
         *
         * \astarpro
         */
        public void RebuildNodeLookup() {
            if (!optimizeForSparseGraph) { return; }

            if (maxDistance == 0) {
                _lookupCellSize = (Int3)Vector3.zero;
            }
            else {
                _lookupCellSize.x = Mathf.CeilToInt(Int3.Precision * maxDistance);
                _lookupCellSize.y = Mathf.CeilToInt(Int3.Precision * maxDistance);
                _lookupCellSize.z = Mathf.CeilToInt(Int3.Precision * maxDistance);
            }

            if (_nodeLookup == null) _nodeLookup = new Dictionary<Int3, PointNode>();

            _nodeLookup.Clear();

            for (int i = 0; i < nodeCount; i++) {
                PointNode node = nodes[i];
                node.next = null;   // 8.16.16 My addition with Aren's approval
                AddToLookup(node);
            }
        }

        public void AddToLookup(PointNode node) {
            if (_nodeLookup == null) { return; }

            Int3 lookupPosition = WorldToLookupSpace(node.position);

            if (_nodeLookup.Count == 0) {
                _minLookup = lookupPosition;
                _maxLookup = lookupPosition;
            }
            else {
                _minLookup = new Int3(System.Math.Min(_minLookup.x, lookupPosition.x), System.Math.Min(_minLookup.y, lookupPosition.y), System.Math.Min(_minLookup.z, lookupPosition.z));
                _maxLookup = new Int3(System.Math.Max(_minLookup.x, lookupPosition.x), System.Math.Max(_minLookup.y, lookupPosition.y), System.Math.Max(_minLookup.z, lookupPosition.z));
            }

            // Does not cover all cases, but at least some of them
            if (node.next != null) {
                throw new System.Exception("This node has already been added to the lookup structure.");
            }

            PointNode linkedListRoot;
            if (_nodeLookup.TryGetValue(lookupPosition, out linkedListRoot)) {
                // Insert in between
                node.next = linkedListRoot.next;
                linkedListRoot.next = node;
            }
            else {
                _nodeLookup[lookupPosition] = node;
            }
        }

        private Int3 WorldToLookupSpace(Int3 position) {
            Int3 lookupPosition = Int3.zero;

            lookupPosition.x = _lookupCellSize.x != 0 ? position.x / _lookupCellSize.x : 0;
            lookupPosition.y = _lookupCellSize.y != 0 ? position.y / _lookupCellSize.y : 0;
            lookupPosition.z = _lookupCellSize.z != 0 ? position.z / _lookupCellSize.z : 0;

            return lookupPosition;
        }

        #region My Work

        private string DebugName { get { return GetType().Name; } }

        /// <summary>
        /// The distance between nodes to use when adding new nodes when you want the nodes to connect to their neighbors.
        /// <remarks>This value is a small amount less than the maxDistance value used to validate node connections.</remarks>
        /// </summary>
        private float _nodeSeparationDistance;

        /// <summary>
        /// The approach nodes that were made to surround a starbase.
        /// <remarks>Used to make these nodes unwalkable when/if the starbase is removed.</remarks>
        /// </summary>
        private IDictionary<StarbaseCmdItem, IList<PointNode>> _starbaseApproachNodes = new Dictionary<StarbaseCmdItem, IList<PointNode>>();

        /// <summary>
        /// The sector navigation nodes that were made unwalkable when the starbase was added.
        /// <remarks>Used to make these sector navigation nodes walkable again when/if the starbase is removed.</remarks>
        /// </summary>
        private IDictionary<StarbaseCmdItem, IList<PointNode>> _sectorNavNodesMadeUnwalkableByStarbase = new Dictionary<StarbaseCmdItem, IList<PointNode>>();

        public override void ScanInternal(OnScanStatus statusCallback) {
            var walkableOpenSpaceWaypoints = GenerateWalkableOpenSpaceWaypoints();
            var walkableSystemWaypoints = GenerateWalkableInteriorSystemWaypoints();

            int nextNodeIndex = Constants.Zero;
            AddNodes(walkableOpenSpaceWaypoints, Topography.OpenSpace.AStarTagValue(), ref nextNodeIndex);  // OpenSpaceTagMask
            AddNodes(walkableSystemWaypoints, Topography.System.AStarTagValue(), ref nextNodeIndex);    // SystemTagMask

            MakeConnections();
        }

        private IList<Vector3> GenerateWalkableOpenSpaceWaypoints() {
#pragma warning disable 0219
            System.DateTime startTime = Utility.SystemTime;
#pragma warning restore 0219
            GameManager gameMgr = GameManager.Instance;
            List<Vector3> walkableOpenSpaceWaypoints = new List<Vector3>();

            // Generate System approach waypoints surrounding systems, deriving maxNodeDistance
            List<Vector3> systemApproachWaypoints = new List<Vector3>();
            float distanceBetweenSystemApproachWaypoints = Constants.ZeroF;         // 174.7
            float maxNodeDistance = Constants.ZeroF;    // 282.6 // max distance allowed before waypoints are skipped
            var allSystems = gameMgr.GameKnowledge.Systems;
            bool hasSystems = allSystems.Any();
            if (hasSystems) {
                foreach (SystemItem system in allSystems) { // 6.15.16 replaced box with icosahedron whose edges are all the same length
                    float previousDistanceBetweenWaypoints = distanceBetweenSystemApproachWaypoints;
                    float previousMaxNodeDistance = maxNodeDistance;
                    float systemApproachWaypointsInscribedSphereRadius = system.Radius * SystemItem.RadiusMultiplierForApproachWaypointsInscribedSphere;
                    var aSystemApproachWaypoints = MyMath.CalcVerticesOfIcosahedronSurroundingInscribedSphere(system.Position,
                        systemApproachWaypointsInscribedSphereRadius, out distanceBetweenSystemApproachWaypoints, out maxNodeDistance);
                    if (previousMaxNodeDistance > Constants.ZeroF) {
                        if (!Mathfx.Approx(previousDistanceBetweenWaypoints, distanceBetweenSystemApproachWaypoints, .1F)) {
                            D.Error("{0} != {1}.", previousDistanceBetweenWaypoints, distanceBetweenSystemApproachWaypoints);
                        }
                        if (!Mathfx.Approx(previousMaxNodeDistance, maxNodeDistance, .1F)) {
                            D.Error("{0} != {1}.", previousMaxNodeDistance, maxNodeDistance);
                        }
                    }
                    systemApproachWaypoints.AddRange(aSystemApproachWaypoints);
                }
            }
            else {
                // HACK for no systems
                distanceBetweenSystemApproachWaypoints = 174.7F;
                maxNodeDistance = 282.6F;
            }
            //D.Log("Added {0} SystemApproachWaypoints. Icosahedron EdgeLength = {1:0.#}, MaxAllowedNodeDistance = {2:0.#}.",
            //    systemApproachWaypoints.Count, edgeLength, maxNodeDistance);

            _nodeSeparationDistance = Mathf.Floor(maxNodeDistance) - 7F;       // HACK 275F
            float proposedMaxDistance = _nodeSeparationDistance + 5F;                 // HACK 280F
            if (!Mathfx.Approx(maxDistance, proposedMaxDistance, 1F)) {
                D.Warn("{0}: Changing MaxNodeSeparationDistance, aka 'MaxDistance' from {1:0.#} to {2:0.#}.", GetType().Name, maxDistance, proposedMaxDistance);
                maxDistance = proposedMaxDistance;
            }

            //D.Log("{0} took {1:0.##} secs generating {2} SystemApproachWaypoints for {3} Systems.",
            //    DebugName, (Utility.SystemTime - startTime).TotalSeconds, systemApproachWaypoints.Count, allSystems.Count);
            //startTime = Utility.SystemTime;

            // populate all space with sector navigation waypoints separated by nodeSeparationDistance
            IList<Sector> allSectors = SectorGrid.Instance.Sectors.ToList();
            //D.Log("{0}: Sectors to scan = {1}.", DebugName, allSectors.Count);
            List<Vector3> sectorNavWaypoints = new List<Vector3>(allSectors.Count * 37);
            float distanceToCorners = TempGameValues.SectorDiagonalLength / 2F; // 1039.2
            float distanceToFaces = TempGameValues.SectorSideLength / 2F;   // 600
            int outsideOfUniverseWaypointCount = Constants.Zero;

            float universeRadiusSqrd = gameMgr.GameSettings.UniverseSize.Radius() * gameMgr.GameSettings.UniverseSize.Radius();
            //D.Log("{0}: Distance to Sector Corners = {1:0.#}.", DebugName, distanceToCorners);
            IList<Vector3> aSectorWaypoints = new List<Vector3>(37);
            List<Vector3> aSectorCornerWaypoints = new List<Vector3>(24);
            List<Vector3> aSectorFaceWaypoints = new List<Vector3>(12);
            foreach (var sector in allSectors) {
                aSectorWaypoints.Clear();
                aSectorWaypoints.Add(sector.Position);

                float distanceFromCenter = _nodeSeparationDistance;
                while (distanceFromCenter < distanceToCorners) {    // 275, 550, 825
                    // propagate sector navWaypoints outward along direction to corners    // 24 per sector
                    aSectorCornerWaypoints.Clear();
                    aSectorCornerWaypoints.AddRange(MyMath.CalcVerticesOfInscribedCubeInsideSphere(sector.Position, distanceFromCenter));
                    foreach (var aCornerWaypoint in aSectorCornerWaypoints) {
                        if (IsInsideUniverseBoundaries(aCornerWaypoint, universeRadiusSqrd)) {
                            aSectorWaypoints.Add(aCornerWaypoint);
                        }
                        else {
                            outsideOfUniverseWaypointCount++;
                        }
                    }

                    // propagate sector navWaypoints outward along direction to face centers    // 12 per sector 
                    if (distanceFromCenter < distanceToFaces) {   // 275, 550 
                        aSectorFaceWaypoints.Clear();
                        aSectorFaceWaypoints.AddRange(MyMath.CalcCubeFaceCentersAroundPoint(sector.Position, distanceFromCenter));
                        foreach (var aFaceWaypoint in aSectorFaceWaypoints) {
                            if (IsInsideUniverseBoundaries(aFaceWaypoint, universeRadiusSqrd)) {
                                aSectorWaypoints.Add(aFaceWaypoint);
                            }
                            else {
                                outsideOfUniverseWaypointCount++;
                            }
                        }
                    }
                    distanceFromCenter += _nodeSeparationDistance;
                }
                sectorNavWaypoints.AddRange(aSectorWaypoints);
            }
#pragma warning disable 0219
            int sectorNavWaypointCount = sectorNavWaypoints.Count;
#pragma warning restore 0219
            //D.Log("{0} took {1:0.##} secs generating {2} SectorNavWaypoints for {3} sectors.",
            //    DebugName, (Utility.SystemTime - startTime).TotalSeconds, sectorNavWaypointCount, allSectors.Count);
            //startTime = Utility.SystemTime;

            //D.Log("{0} filtered out {1} waypoints which were outside of the universe.", DebugName, outsideOfUniverseWaypointCount);

            //TODO Validate that sectors outside waypoint is within nodeSeparationDistance of neighboring sectors outside waypoint
            // With Sector Radius of 600 and NodeSeparationDistance of 275, expect sector to sector waypoint distance ~ 90

            // Remove SectorNavWaypoints present inside sphere containing SystemApproachWaypoints    
            // https://en.wikipedia.org/wiki/Regular_icosahedron#Dimensions
            float systemApproachWaypointsCircumscribedSphereRadius = distanceBetweenSystemApproachWaypoints * 0.9510565163F;
            float radiusOfSphereContainingSystemApproachWaypoints = systemApproachWaypointsCircumscribedSphereRadius + 1F;

            if (hasSystems) {
                foreach (var system in allSystems) {
                    List<Vector3> tmpSectorNavWaypoints = new List<Vector3>(sectorNavWaypoints.Count);
                    foreach (var sectorNavWaypoint in sectorNavWaypoints) {
                        if (!MyMath.IsPointOnOrInsideSphere(system.Position, radiusOfSphereContainingSystemApproachWaypoints, sectorNavWaypoint)) {
                            tmpSectorNavWaypoints.Add(sectorNavWaypoint);
                        }
                    }
                    sectorNavWaypoints = tmpSectorNavWaypoints;
                }
            }
            //D.Log("{0} took {1:0.##} secs removing {2} SectorNavWaypoints from {3} Systems.",
            //    DebugName, (Utility.SystemTime - startTime).TotalSeconds, sectorNavWaypointCount - sectorNavWaypoints.Count, allSystems.Count);
            //startTime = Utility.SystemTime;
            sectorNavWaypointCount = sectorNavWaypoints.Count;

            float uCenterWaypointsInscribedSphereRadius = Constants.ZeroF;
            IEnumerable<Vector3> universeCenterWaypoints = Enumerable.Empty<Vector3>();
            var universeCenter = UniverseFolder.Instance.GetComponentInChildren<UniverseCenterItem>();
            if (universeCenter != null) {
                float distanceBetweenUCenterWaypoints;
                uCenterWaypointsInscribedSphereRadius = universeCenter.Data.CloseOrbitOuterRadius * UniverseCenterItem.RadiusMultiplierForWaypointInscribedSphere;
                universeCenterWaypoints = MyMath.CalcVerticesOfIcosahedronSurroundingInscribedSphere(universeCenter.Position, uCenterWaypointsInscribedSphereRadius, out distanceBetweenUCenterWaypoints);
                if (distanceBetweenUCenterWaypoints > _nodeSeparationDistance) {
                    D.Error("{0} should be <= {1}.", distanceBetweenUCenterWaypoints, _nodeSeparationDistance);
                }
                //D.Log("{0}: Distance between UCenterWaypoints = {1:0.#}.", DebugName, distanceBetweenUCenterWaypoints);

                // remove SectorNavigationWaypoints present inside sphere containing UniverseCenterWaypoints
                float uCenterWaypointsCircumscribedSphereRadius = distanceBetweenUCenterWaypoints * 0.9510565163F;
                float radiusOfSphereContainingUCenterWaypoints = uCenterWaypointsCircumscribedSphereRadius + 1F;
                List<Vector3> tmpSectorNavWaypoints = new List<Vector3>(sectorNavWaypoints.Count);
                foreach (var sectorNavWaypoint in sectorNavWaypoints) {
                    if (!MyMath.IsPointOnOrInsideSphere(universeCenter.Position, radiusOfSphereContainingUCenterWaypoints, sectorNavWaypoint)) {
                        tmpSectorNavWaypoints.Add(sectorNavWaypoint);
                    }
                }
                sectorNavWaypoints = tmpSectorNavWaypoints;
            }

            //D.Log("{0} took {1:0.##} secs removing {2} SectorNavWaypoints around UniverseCenter.",
            //    DebugName, (Utility.SystemTime - startTime).TotalSeconds, sectorNavWaypointCount - sectorNavWaypoints.Count);
            //startTime = Utility.SystemTime;

            walkableOpenSpaceWaypoints.AddRange(sectorNavWaypoints);
            walkableOpenSpaceWaypoints.AddRange(universeCenterWaypoints);
            walkableOpenSpaceWaypoints.AddRange(systemApproachWaypoints);

            //D.Log("{0} took {1:0.##} secs generating {2} WalkableOpenSpaceWaypoints for {3} Sectors.",
            //    DebugName, (Utility.SystemTime - startTime).TotalSeconds, walkableOpenSpaceWaypoints.Count, allSectors.Count);

            return walkableOpenSpaceWaypoints;
        }

        private IList<Vector3> GenerateWalkableInteriorSystemWaypoints() {
            D.AssertNotDefault(_nodeSeparationDistance);   // method should follow GenerateWalkableOpenSpaceWaypoints
            List<Vector3> allSystemInteriorWaypoints = new List<Vector3>();
            var systems = GameManager.Instance.GameKnowledge.Systems;
            if (systems.Any()) {
                systems.ForAll(sys => {
                    var aSystemInteriorWaypoints = MyMath.CalcVerticesOfInscribedCubeInsideSphere(sys.Position, sys.Radius * SystemItem.InteriorWaypointDistanceMultiplier);
                    allSystemInteriorWaypoints.AddRange(aSystemInteriorWaypoints);
                });
            }
            return allSystemInteriorWaypoints;
        }

        /// <summary>
        /// Adds walkable nodes derived from the provided waypoints to the graph's collection of nodes,
        /// returning the added nodes.
        /// </summary>
        /// <param name="waypoints">The waypoints.</param>
        /// <param name="tag">The tag for the nodes.</param>
        /// <param name="nextNodeIndex">Index of the next node.</param>
        /// <returns></returns>
        private IList<PointNode> AddNodes(IList<Vector3> waypoints, uint tag, ref int nextNodeIndex) {
            CheckAndAdjustNodesSize(waypoints.Count);

            IList<PointNode> nodesAdded = new List<PointNode>(waypoints.Count);

            int indexAfterLastNode = nextNodeIndex + waypoints.Count;
            for (int index = nextNodeIndex; index < indexAfterLastNode; index++) {
                PointNode node = new PointNode(active);
                node.SetPosition((Int3)waypoints[index - nextNodeIndex]);
                node.Walkable = true;
                node.GraphIndex = graphIndex;
                node.Tag = tag;
                nodes[index] = node;

                nodesAdded.Add(node);

                nodeCount++;
                AddToLookup(node);
            }
            D.AssertEqual(nodes.Length, nodeCount);
            nextNodeIndex = indexAfterLastNode;
            return nodesAdded;
        }

        private void CheckAndAdjustNodesSize(int additionalNodes) {
            if (nodes == null || nodeCount == nodes.Length) {
                //var startTime = Utility.SystemTime;
                var nds = new PointNode[nodes != null ? nodes.Length + additionalNodes : additionalNodes];
                for (int i = 0; i < nodeCount; i++) {
                    nds[i] = nodes[i];
                }
                nodes = nds;
                //D.Log("{0}.CheckAndAdjustNodesSize({1}) took {2:0.00} seconds.", DebugName, additionalNodes, (Utility.SystemTime - startTime).TotalSeconds);
            }
        }

        private void MakeConnections() {
#pragma warning disable 0219
            System.DateTime startTime = Utility.SystemTime;
#pragma warning restore 0219

            if (optimizeForSparseGraph) {
                RebuildNodeLookup();
            }

            if (maxDistance >= 0) {
                //To avoid too many allocations, these lists are reused for each node
                var connections = new List<PointNode>(3);
                var costs = new List<uint>(3);

                //Loop through all nodes and add connections to other nodes
                int connectionCount = 0;
                int invalidConnectionCount = 0;
                for (int i = 0; i < nodeCount; i++) {    // 8.16.16 changed from nodes.Length
                    connections.Clear();
                    costs.Clear();

                    PointNode node = nodes[i];

                    if (optimizeForSparseGraph) {
                        Int3 lookupPosition = WorldToLookupSpace(node.position);

                        int l = _lookupCellSize.y == 0 ? 9 : ThreeDNeighbours.Length;

                        for (int j = 0; j < l; j++) {
                            Int3 neighborNodeLookupPosition = lookupPosition + ThreeDNeighbours[j]; // 8.17.16 was called 'n_p'

                            PointNode other;
                            if (_nodeLookup.TryGetValue(neighborNodeLookupPosition, out other)) {
                                while (other != null) {
                                    float dist;
                                    if (IsValidConnection(node, other, out dist)) {
                                        connections.Add(other);
                                        /** \todo Is this equal to .costMagnitude */
                                        costs.Add((uint)Mathf.RoundToInt(dist * Int3.FloatPrecision));
                                    }
                                    else {
                                        invalidConnectionCount++;
                                    }
                                    other = other.next;
                                }
                            }
                        }
                    }
                    else {
                        // Only brute force is available in the free version
                        for (int j = 0; j < nodeCount; j++) {    // 8.16.16 changed from nodes.Length
                            if (i == j) {
                                continue;
                            }

                            PointNode other = nodes[j];

                            float dist;
                            if (IsValidConnection(node, other, out dist)) {
                                connections.Add(other);
                                /** \todo Is this equal to .costMagnitude */
                                costs.Add((uint)Mathf.RoundToInt(dist * Int3.FloatPrecision));
                            }
                            else {
                                invalidConnectionCount++;
                            }
                        }
                    }
                    node.connections = connections.ToArray();
                    connectionCount += connections.Count;
                    node.connectionCosts = costs.ToArray();
                }

                int totalConnectionsAttempted = connectionCount + invalidConnectionCount;
                int connectionCountChg = connectionCount - __previousConnectionsCount;
                __previousConnectionsCount = connectionCount;

                if (connectionCountChg == Constants.Zero) {
                    D.Warn("{0}.MakeConnections() took {1:0.##} secs generating {2}/{3} valid pathfinding connections, but no connections (net) added or removed?",
                        DebugName, (Utility.SystemTime - startTime).TotalSeconds, connectionCount, totalConnectionsAttempted);
                }
                else {
                    // (net) refers to the net of added and removed. I can't separate them using this approach to making connections
                    //string connectionCountChgText = connectionCountChg < 0 ? "removed" : "added";
                    //D.Log("{0}.MakeConnections() took {1:0.##} secs generating {2}/{3} valid pathfinding connections. {4} connections (net) were {5}.",
                    //    DebugName, (Utility.SystemTime - startTime).TotalSeconds, connectionCount, totalConnectionsAttempted, Mathf.Abs(connectionCountChg), connectionCountChgText);
                }
            }
        }

        /// <summary>
        /// Updates the graph during runtime adding approach waypoints for this starbase,
        /// and makes any waypoints located inside the new approach waypoints unwalkable, then reconnects.
        /// </summary>
        /// <param name="baseCmd">The Starbase command.</param>
        /// <param name="sectorID">The sector ID where the Starbase is located. Note that the StarbaseCmdItem
        /// itself does not know its sectorID until FinalInitialize.</param>
        public void AddToGraph(StarbaseCmdItem baseCmd, IntVector3 sectorID) {
            //D.Log("{0}.AddToGraph({1}) called.", DebugName, baseCmd.DebugName);
            // Note: active.IsAnyGraphUpdatesQueued is never true except when using UpdateGraphs(). I've replaced UpdateGraphs(GUO) with WorkItems

            // forceCompletion is set by AstarPath internally 
            var handleStarbaseAddedWorkItem = new AstarPath.AstarWorkItem(update: (forceCompletion) => {
                active.QueueWorkItemFloodFill();
                HandleStarbaseAdded(baseCmd, sectorID);
                return true;
            });
            active.AddWorkItem(handleStarbaseAddedWorkItem);

            // Note: 8.17.16 no current way to remove a work item once added. Otherwise, if I got another call to this
            // method with IsAnyGraphUpdatesQueued = true, I'd remove the previous queued MakeConnections and replace 
            // with a new one at the end. Currently, if I get another call while queued, MakeConnections will run twice.

            var makeConnectionsWorkItem = new AstarPath.AstarWorkItem(update: (forceCompletion) => {
                MakeConnections();
                //D.Log("{0} has completed making node connections for {1}.", DebugName, baseCmd.DebugName);
                return true;
            });
            active.AddWorkItem(makeConnectionsWorkItem);
        }

        /// <summary>
        /// Updates the graph during runtime removing the approach waypoints (marking them as unwalkable) for this starbase, 
        /// and makes any waypoints walkable that were previously made unwalkable when the base was added, then reconnects.
        /// </summary>
        /// <param name="baseCmd">The Starbase command.</param>
        public void RemoveFromGraph(StarbaseCmdItem baseCmd) {
            //D.Log("{0}.RemoveFromGraph({1}) called.", DebugName, baseCmd.DebugName);
            // Note: active.IsAnyGraphUpdatesQueued is never true except when using UpdateGraphs(). 
            // I've replaced UpdateGraphs(GUO) with WorkItems

            // forceCompletion is set by AstarPath internally 
            var handleStarbaseBeingRemovedWorkItem = new AstarPath.AstarWorkItem(update: (forceCompletion) => {
                active.QueueWorkItemFloodFill();
                HandleStarbaseRemoved(baseCmd);
                return true;
            });
            active.AddWorkItem(handleStarbaseBeingRemovedWorkItem);

            // Note: 8.17.16 no current way to remove a work item once added. Otherwise, if I got another call to this
            // method with IsAnyGraphUpdatesQueued = true, I'd remove the previous queued MakeConnections and replace 
            // with a new one at the end. Currently, if I get another call while queued, MakeConnections will run twice.

            var makeConnectionsWorkItem = new AstarPath.AstarWorkItem(update: (forceCompletion) => {
                MakeConnections();
                //D.Log("{0} has completed making node connections for {1}.", DebugName, baseCmd.DebugName);
                return true;
            });
            active.AddWorkItem(makeConnectionsWorkItem);
        }

        // NOTE: For now, no Add/Remove(Settlement). Settlements aren't likely to be on top of existing waypoints, and,
        // surrounding them with waypoints makes no sense if I allow them to orbit

        private void HandleStarbaseAdded(StarbaseCmdItem starbaseCmd, IntVector3 sectorID) {
            D.Assert(!_sectorNavNodesMadeUnwalkableByStarbase.ContainsKey(starbaseCmd), DebugName);
            D.Assert(!_starbaseApproachNodes.ContainsKey(starbaseCmd), DebugName);

            // Make Sector nav nodes (inside where inner approach nodes will go) unwalkable 
            IList<PointNode> nds = new List<PointNode>();
            float radiusOfInnerApproachWaypointsInscribedSphere = starbaseCmd.CloseOrbitOuterRadius * StarbaseCmdItem.RadiusMultiplierForApproachWaypointsInscribedSphere;
            for (int i = 0; i < nodeCount; i++) {
                if (MyMath.IsPointOnOrInsideSphere(starbaseCmd.Position, radiusOfInnerApproachWaypointsInscribedSphere, (Vector3)nodes[i].position)) {
                    nodes[i].Walkable = false;
                    nds.Add(nodes[i]);
                }
            }
            _sectorNavNodesMadeUnwalkableByStarbase.Add(starbaseCmd, nds);
            if (nds.Count > Constants.Zero) {
                D.Log("{0} has completed making {1} sector nav nodes unwalkable as a result of {2}'s addition.", DebugName, nds.Count, starbaseCmd.DebugName);
            }

            // Note: There may be unwalkable approach nodes from a previously removed starbase at the same locations, but
            // I am choosing to ignore them rather than try to make them walkable again. Can't currently remove nodes once added

            // Now add the walkable inner approach waypoints for this starbase
            float vertexDistanceFromStarbase;
            List<Vector3> approachWaypoints = MyMath.CalcVerticesOfCubeSurroundingInscribedSphere(starbaseCmd.Position, radiusOfInnerApproachWaypointsInscribedSphere, out vertexDistanceFromStarbase);
            //D.Log("{0}: {1}'s Inner Approach Node distance = {2:0.##}.", DebugName, starbaseCmd.DebugName, vertexDistanceFromStarbase);
            if (vertexDistanceFromStarbase > maxDistance) {
                D.Warn("{0}: {1}'s Inner Approach Node distance {2:0.##} > MaxDistance {3:0.##}.", DebugName, starbaseCmd.DebugName, vertexDistanceFromStarbase, maxDistance);
            }

            // ... and then the walkable outer approach waypoints
            IList<Vector3> approachWaypointsToRemove = new List<Vector3>(); // avoids modifying approachWaypoints while iterating
            approachWaypoints.AddRange(MyMath.CalcVerticesOfInscribedCubeInsideSphere(starbaseCmd.Position, _nodeSeparationDistance));

            float universeRadiusSqrd = GameManager.Instance.GameSettings.UniverseSize.Radius() * GameManager.Instance.GameSettings.UniverseSize.Radius();
            foreach (var waypoint in approachWaypoints) {
                if (!IsInsideUniverseBoundaries(waypoint, universeRadiusSqrd)) {
                    D.Warn("{0} is excluding {1}'s proposed approach waypoint that is outside the universe.", DebugName, starbaseCmd.DebugName);
                    approachWaypointsToRemove.Add(waypoint);
                }
            }

            foreach (var waypoint in approachWaypointsToRemove) {
                approachWaypoints.Remove(waypoint);
            }
            approachWaypointsToRemove.Clear();

            ISystem systemInSector;
            bool doesSectorContainSystem = GameManager.Instance.GameKnowledge.TryGetSystem(sectorID, out systemInSector);
            if (doesSectorContainSystem) {
                foreach (var waypoint in approachWaypoints) {
                    if (MyMath.IsPointOnOrInsideSphere(systemInSector.Position, systemInSector.Radius, waypoint)) {
                        // FIXME warn for now as I should preclude locating a Starbase close to a system
                        D.Warn("{0} is excluding {1}'s proposed approach waypoint that is inside adjacent System {2}.", DebugName, starbaseCmd.DebugName, systemInSector.DebugName);
                        approachWaypointsToRemove.Add(waypoint);
                    }
                }
            }

            foreach (var waypoint in approachWaypointsToRemove) {
                approachWaypoints.Remove(waypoint);
            }

            int nextNodeIndex = nodeCount;
            nds = AddNodes(approachWaypoints, Topography.OpenSpace.AStarTagValue(), ref nextNodeIndex);
            //D.Log("{0} has completed adding {1}'s {2} approach nodes.", DebugName, starbaseCmd.DebugName, nds.Count);
            _starbaseApproachNodes.Add(starbaseCmd, nds);
        }

        [Obsolete]
        private void ChangeWalkabilityOfNodesInsideApproachNodes(StarbaseCmdItem starbaseCmd, bool isBaseBeingAdded) {
            IList<PointNode> nodesMadeUnwalkable;
            if (isBaseBeingAdded) {
                // starbase is being added so make Sector nav nodes inside approach nodes unwalkable
                nodesMadeUnwalkable = new List<PointNode>();
                float radiusOfApproachWaypointsInscribedSphere = starbaseCmd.CloseOrbitOuterRadius * StarbaseCmdItem.RadiusMultiplierForApproachWaypointsInscribedSphere;
                Vector3 sphereCenter = starbaseCmd.Position;
                float sphereRadius = radiusOfApproachWaypointsInscribedSphere;
                for (int i = 0; i < nodeCount; i++) {
                    if (MyMath.IsPointOnOrInsideSphere(sphereCenter, sphereRadius, (Vector3)nodes[i].position)) {
                        nodes[i].Walkable = false;
                        nodesMadeUnwalkable.Add(nodes[i]);
                    }
                }
                D.Assert(!_sectorNavNodesMadeUnwalkableByStarbase.ContainsKey(starbaseCmd), DebugName);
                _sectorNavNodesMadeUnwalkableByStarbase.Add(starbaseCmd, nodesMadeUnwalkable);
            }
            else {
                // starbase is being removed so make Sector nav nodes previously made unwalkable, walkable again
                D.Assert(_sectorNavNodesMadeUnwalkableByStarbase.ContainsKey(starbaseCmd), DebugName);
                nodesMadeUnwalkable = _sectorNavNodesMadeUnwalkableByStarbase[starbaseCmd];
                foreach (var unwalkableNode in nodesMadeUnwalkable) {
                    unwalkableNode.Walkable = true;
                }
                _sectorNavNodesMadeUnwalkableByStarbase.Remove(starbaseCmd);
            }
            D.Log("{0} has completed making {1} nodes inside {2}'s approach nodes {3}.", DebugName, nodesMadeUnwalkable.Count, starbaseCmd.DebugName, isBaseBeingAdded ? "unwalkable" : "walkable");
        }

        [Obsolete]
        private void AddStarbaseApproachNodes(StarbaseCmdItem starbaseCmd) {
            // Note: There may be unwalkable approach nodes from a previously removed starbase at the same locations, but
            // I am choosing to ignore them rather than try to make them walkable again. Can't currently remove nodes once added
            float radiusOfApproachWaypointsInscribedSphere = starbaseCmd.CloseOrbitOuterRadius * StarbaseCmdItem.RadiusMultiplierForApproachWaypointsInscribedSphere;

            Vector3 sphereCenter = starbaseCmd.Position;
            float sphereRadius = radiusOfApproachWaypointsInscribedSphere;
            float vertexDistanceFromStarbase;
            var approachWaypoints = MyMath.CalcVerticesOfCubeSurroundingInscribedSphere(sphereCenter, sphereRadius, out vertexDistanceFromStarbase);
            D.Log("{0}: {1}'s Approach Node distance = {2:0.##}.", DebugName, starbaseCmd.DebugName, vertexDistanceFromStarbase);
            if (vertexDistanceFromStarbase > maxDistance) {
                D.Warn("{0}: {1}'s Approach Node distance {2:0.##} > MaxDistance {3:0.##}.", DebugName, starbaseCmd.DebugName, vertexDistanceFromStarbase, maxDistance);
            }

            float universeRadiusSqrd = GameManager.Instance.GameSettings.UniverseSize.Radius() * GameManager.Instance.GameSettings.UniverseSize.Radius();
            foreach (var waypoint in approachWaypoints) {
                if (!IsInsideUniverseBoundaries(waypoint, universeRadiusSqrd)) {
                    D.Warn("{0} is excluding {1}'s proposed approach waypoint that is outside the universe.", DebugName, starbaseCmd.DebugName);
                    approachWaypoints.Remove(waypoint);
                }
            }

            int nextNodeIndex = nodeCount;
            IList<PointNode> approachNodesAdded = AddNodes(approachWaypoints, Topography.OpenSpace.AStarTagValue(), ref nextNodeIndex);
            //D.Log("{0} has completed adding {1} approach nodes for {2}.", DebugName, approachWaypoints.Count, starbaseCmd.DebugName);

            D.Assert(!_starbaseApproachNodes.ContainsKey(starbaseCmd), DebugName);
            _starbaseApproachNodes.Add(starbaseCmd, approachNodesAdded);
        }

        private void HandleStarbaseRemoved(StarbaseCmdItem starbaseCmd) {
            // Make existing Starbase approach nodes unwalkable
            D.Assert(_starbaseApproachNodes.ContainsKey(starbaseCmd), DebugName);
            IList<PointNode> nds = _starbaseApproachNodes[starbaseCmd];
            foreach (var approachNode in nds) {
                approachNode.Walkable = false;
            }
            _starbaseApproachNodes.Remove(starbaseCmd);
            D.Log("{0} has completed making {1} approach nodes unwalkable as a result of {2}'s removal.", DebugName, nds.Count, starbaseCmd.DebugName);

            // Now make Sector nav nodes previously made unwalkable, walkable again
            D.Assert(_sectorNavNodesMadeUnwalkableByStarbase.ContainsKey(starbaseCmd), DebugName);
            nds = _sectorNavNodesMadeUnwalkableByStarbase[starbaseCmd];
            foreach (var unwalkableSectorNavNode in nds) {
                unwalkableSectorNavNode.Walkable = true;
            }
            _sectorNavNodesMadeUnwalkableByStarbase.Remove(starbaseCmd);
            D.Log("{0} has completed making {1} sector nav nodes walkable again as a result of {2}'s removal.", DebugName, nds.Count, starbaseCmd.DebugName);
        }

        private bool IsInsideUniverseBoundaries(Vector3 point, float universeRadiusSqrd) {
            return Vector3.SqrMagnitude(point - GameConstants.UniverseOrigin) < universeRadiusSqrd;
        }

        #endregion

        /** Returns if the connection between \a a and \a b is valid.
 * Checks for obstructions using raycasts (if enabled) and checks for height differences.\n
 * As a bonus, it outputs the distance between the nodes too if the connection is valid
 */
        private bool IsValidConnection(PointNode a, PointNode b, out float dist) {
            dist = Constants.ZeroF;

            if (a.Walkable && b.Walkable) {
                var dir = (Vector3)(a.position - b.position);

                dist = dir.magnitude;
                if (maxDistance == 0 || dist < maxDistance) {
                    return true;
                }
            }
            return false;
        }

        /** Add a node with the specified type to the graph at the specified position.
 *
 * \param position The node will be set to this position.
 * \note Vector3 can be casted to Int3 using (Int3)myVector.
 *
 * \note This needs to be called when it is safe to update nodes, which is
 * - when scanning
 * - during a graph update
 * - inside a callback registered using AstarPath.RegisterSafeUpdate
 *
 * \see AstarPath.RegisterSafeUpdate
 */
        [Obsolete]  // 8.17.16 use my AddNodes(waypoints)
        public PointNode AddNode(Int3 position) {
            if (nodes == null || nodeCount == nodes.Length) {
                var nds = new PointNode[nodes != null ? System.Math.Max(nodes.Length + 4, nodes.Length * 2) : 4];
                for (int i = 0; i < nodeCount; i++) nds[i] = nodes[i];
                nodes = nds;
            }

            PointNode node = new PointNode(active);

            node.SetPosition(position);
            node.GraphIndex = graphIndex;
            node.Walkable = true;

            nodes[nodeCount] = node;
            nodeCount++;

            AddToLookup(node);

            return node;
        }

        public override void PostDeserialization() {
            RebuildNodeLookup();
        }

        public override void RelocateNodes(Matrix4x4 oldMatrix, Matrix4x4 newMatrix) {
            base.RelocateNodes(oldMatrix, newMatrix);
            RebuildNodeLookup();
        }

#if ASTAR_NO_JSON
        public override void SerializeSettings (GraphSerializationContext ctx) {
            base.SerializeSettings(ctx);

            ctx.SerializeUnityObject(root);
            ctx.writer.Write(searchTag ?? "");
            ctx.writer.Write(maxDistance);
            ctx.SerializeVector3(limits);
            ctx.writer.Write(raycast);
            ctx.writer.Write(use2DPhysics);
            ctx.writer.Write(thickRaycast);
            ctx.writer.Write(thickRaycastRadius);
            ctx.writer.Write(recursive);
            ctx.writer.Write(autoLinkNodes);
            ctx.writer.Write((int)mask);
            ctx.writer.Write(optimizeForSparseGraph);
            ctx.writer.Write(optimizeFor2D);
        }

        public override void DeserializeSettings (GraphSerializationContext ctx) {
            base.DeserializeSettings(ctx);

            root = ctx.DeserializeUnityObject() as Transform;
            searchTag = ctx.reader.ReadString();
            maxDistance = ctx.reader.ReadSingle();
            limits = ctx.DeserializeVector3();
            raycast = ctx.reader.ReadBoolean();
            use2DPhysics = ctx.reader.ReadBoolean();
            thickRaycast = ctx.reader.ReadBoolean();
            thickRaycastRadius = ctx.reader.ReadSingle();
            recursive = ctx.reader.ReadBoolean();
            autoLinkNodes = ctx.reader.ReadBoolean();
            mask = (LayerMask)ctx.reader.ReadInt32();
            optimizeForSparseGraph = ctx.reader.ReadBoolean();
            optimizeFor2D = ctx.reader.ReadBoolean();
        }
#endif

        public override void SerializeExtraInfo(GraphSerializationContext ctx) {
            // Serialize node data

            if (nodes == null) ctx.writer.Write(-1);

            // Length prefixed array of nodes
            ctx.writer.Write(nodeCount);
            for (int i = 0; i < nodeCount; i++) {
                // -1 indicates a null field
                if (nodes[i] == null) ctx.writer.Write(-1);
                else {
                    ctx.writer.Write(0);
                    nodes[i].SerializeNode(ctx);
                }
            }
        }

        public override void DeserializeExtraInfo(GraphSerializationContext ctx) {
            int count = ctx.reader.ReadInt32();

            if (count == -1) {
                nodes = null;
                return;
            }

            nodes = new PointNode[count];
            nodeCount = count;

            for (int i = 0; i < nodes.Length; i++) {
                if (ctx.reader.ReadInt32() == -1) continue;
                nodes[i] = new PointNode(active);
                nodes[i].DeserializeNode(ctx);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        private int __previousConnectionsCount = Constants.Zero;

        /// <summary>
        /// Gets the sectors this pathfinding system is allowed to scan for waypoint interconnection. 
        /// This method reduces the total sector count to a manageable value so the scan time is not onerous. 
        /// This allows SectorGrid to build out a large number of sectors for testing without requiring the
        /// Pathfinding system to make interconnections between all the waypoints in all sectors.
        /// </summary>
        /// <returns></returns>
        [System.Obsolete]
        private IList<Sector> __GetAllowedSectorsToScan() {
            IList<Sector> sectorsToScan = new List<Sector>();

            int maxIndexX = __MaxAllowedSectorGridSizeToScan.x / 2;
            int maxIndexY = __MaxAllowedSectorGridSizeToScan.y / 2;
            int maxIndexZ = __MaxAllowedSectorGridSizeToScan.z / 2;
            var allSectors = SectorGrid.Instance.Sectors;
            allSectors.ForAll(s => {
                var index = s.SectorID;
                if (Mathf.Abs(index.x) <= maxIndexX) {
                    if (Mathf.Abs(index.y) <= maxIndexY) {
                        if (Mathf.Abs(index.z) <= maxIndexZ) {
                            //D.Log("{0} adding Sector {1} to scan.", DebugName, s);
                            sectorsToScan.Add(s);
                        }
                    }
                }
            });
            //D.Log("{0}: Total Sector Count = {1}, Sectors to scan = {2}.", DebugName, allSectors.Count, sectorsToScan.Count);
            return sectorsToScan;
        }

        //private System.DateTime __GraphUpdateStartTime { get; set; }

        #endregion

        #region IUpdatableGraph Members

        public GraphUpdateThreading CanUpdateAsync(GraphUpdateObject o) {
            return GraphUpdateThreading.UnityThread;
        }

        public void UpdateAreaInit(GraphUpdateObject o) { }

        /** Updates an area in the list graph.
         * Recalculates possibly affected connections, i.e all connection lines passing trough the bounds of the \a GUO will be recalculated
         * \astarpro */
        public void UpdateArea(GraphUpdateObject guo) {
            throw new NotImplementedException();    // I'm not using GraphUpdateObjects for graph updates so shouldn't be called
        }

        #endregion

        #region GUO Approach Archive

        //private IDictionary<StarbaseCmdItem, GraphUpdateObject> _starbaseGuoLookup = new Dictionary<StarbaseCmdItem, GraphUpdateObject>();

        //public void AddToGraph(StarbaseCmdItem baseCmd) {
        //if (active.IsAnyGraphUpdatesQueued) {
        //    D.Log("{0}.AddToGraph({1}) was called with graph updates already queued.", GetType().Name, baseCmd.DebugName);
        //}
        //else {
        //    D.Log("{0}.AddToGraph({1}) called.", GetType().Name, baseCmd.DebugName);
        //}

        // forceCompletion is set by AstarPath internally 
        //var addApproachNodesWorkItem = new AstarPath.AstarWorkItem(update: (forceCompletion) => {
        //    active.QueueWorkItemFloodFill();
        //    AddStarbaseApproachNodes(baseCmd);
        //    MakeConnections();  // Graph Updates using a Guo are only for modifying existing nodes
        //    return true;
        //});
        //active.AddWorkItem(addApproachNodesWorkItem);

        // GraphUpdateObject approach 
        //float baseRadius = baseCmd.CloseOrbitOuterRadius;
        //Vector3 baseUnwalkableAreaSize = Vector3.one * baseRadius;
        //Bounds baseUnwalkableBounds = new Bounds(baseCmd.Position, baseUnwalkableAreaSize);
        //if (IsAnyNodeInsideBounds(baseUnwalkableBounds)) {
        //    // one or more nodes are within the base's bounds so update the graph and store the Guo

        //    if (!active.IsAnyGraphUpdatesQueued) {
        //        // start the timer if this is the first update in the queue. Avoids overriding start time with any consecutive update calls
        //        __GraphUpdateStartTime = Utility.SystemTime;
        //    }

        //    GraphUpdateObject baseUnwalkableGuo = new GraphUpdateObject(baseUnwalkableBounds) {
        //        modifyWalkability = true,   // default is false
        //        setWalkability = false,     // default is false
        //        updatePhysics = true,       // default is true. Should have been called refreshConnections as that is what it does
        //        trackChangedNodes = true,   // default is false, Enables RevertFromBackup
        //        requiresFloodFill = false    // default is true
        //    };
        //    active.UpdateGraphs(baseUnwalkableGuo);
        //    _starbaseGuoLookup.Add(baseCmd, baseUnwalkableGuo);
        //}
        // else no node within the base's bounds so no reason to update the graph or store the Guo
        //}

        //public void RemoveFromGraph(StarbaseCmdItem baseCmd) {
        //    D.Log("{0}.RemoveFromGraph({1}) called.", GetType().Name, baseCmd.DebugName);
        //    if (active.IsAnyGraphUpdatesQueued) {
        //        D.Log("{0}.RemoveFromGraph({1}) was called with graph updates already queued.", GetType().Name, baseCmd.DebugName);
        //    }

        //    GraphUpdateObject guo;
        //    if (_starbaseGuoLookup.TryGetValue(baseCmd, out guo)) {
        //        // the Guo is stored, so the base had one or more nodes within its bounds
        //        D.Assert(IsAnyNodeInsideBounds(guo.bounds));

        //        if (!active.IsAnyGraphUpdatesQueued) {
        //            // start the timer if this is the first update in the queue. Avoids overriding start time with any consecutive update calls
        //            __GraphUpdateStartTime = Utility.SystemTime;
        //        }

        //        D.LogBold("{0}: RevertFromBackup() about to be called upon removal of {1}.", GetType().Name, baseCmd.DebugName);
        //        guo.RevertFromBackup();

        //        // Notes: RevertFromBackup() instantly reverses the node changes made when StarBase was added, but it DOES NOT re-establish 
        //        // connections! Currently, I'm only changing node walkability so using RevertFromBackup() is overkill. I could just as easily
        //        // set guo.setWalkability = true here and UpdateGraphs() without using RevertFromBackup(). If I leave the Guo settings as is
        //        // after RevertFromBackup(), during UpdateGraphs(guo) it will revert the reversions, so I tell it to not modify walkability again. 
        //        // As I'm getting rid of the Guo after this, there is no point in tracking changes.
        //        guo.modifyWalkability = false;
        //        guo.trackChangedNodes = false;

        //        active.UpdateGraphs(guo);
        //        _starbaseGuoLookup.Remove(baseCmd);
        //    }
        //    // else no Guo was stored so there were no nodes within the base's bounds
        //}


        //public void UpdateArea(GraphUpdateObject guo) {
        //    if (nodes == null) {
        //        return;
        //    }
        //    D.Log("{0}.UpdateArea(GraphUpdateObject) called.", GetType().Name);

        //    for (int i = 0; i < nodeCount; i++) {
        //        if (guo.bounds.Contains((Vector3)nodes[i].position)) {
        //            //D.Log("{0}: Bounds node walkability before apply = {1}.", GetType().Name, nodes[i].Walkable);
        //            guo.WillUpdateNode(nodes[i]);
        //            guo.Apply(nodes[i]);
        //            //D.Log("{0}: Bounds node walkability after apply = {1}.", GetType().Name, nodes[i].Walkable);
        //        }
        //    }

        //    // Make connection changes
        //    if (guo.updatePhysics) {
        //        MakeConnectionChanges(guo);
        //    }
        //}

        /// <summary>
        /// Makes the connection changes needed by guo using the original 'connection intersects bounds' algorithm.
        /// </summary>
        /// <param name="guo">The guo.</param>
        //private void MakeConnectionChanges(GraphUpdateObject guo) {
        //    System.DateTime startTime = Utility.SystemTime;
        //    //Use a copy of the bounding box, we should not change the GUO's bounding box since it might be used for other graph updates
        //    Bounds bounds = guo.bounds;

        //    //Create two temporary arrays used for holding new connections and costs
        //    List<GraphNode> tmp_arr = Pathfinding.Util.ListPool<GraphNode>.Claim();
        //    List<uint> tmp_arr2 = Pathfinding.Util.ListPool<uint>.Claim();

        //    int connectionsAdded = Constants.Zero;
        //    int connectionsRemoved = Constants.Zero;

        //    for (int i = 0; i < nodeCount; i++) {
        //        PointNode node = nodes[i];
        //        var a = (Vector3)node.position;

        //        List<GraphNode> conn = null;
        //        List<uint> costs = null;

        //        for (int j = 0; j < nodeCount; j++) {
        //            if (j == i) continue;

        //            var b = (Vector3)nodes[j].position;
        //            if (VectorMath.SegmentIntersectsBounds(bounds, a, b)) {
        //                float dist;
        //                PointNode other = nodes[j];
        //                bool contains = node.ContainsConnection(other);
        //                bool validConnection = IsValidConnection(node, other, out dist);

        //                if (!contains && validConnection) {
        //                    // A new connection should be added

        //                    if (conn == null) {
        //                        tmp_arr.Clear();
        //                        tmp_arr2.Clear();
        //                        conn = tmp_arr;
        //                        costs = tmp_arr2;
        //                        conn.AddRange(node.connections);
        //                        costs.AddRange(node.connectionCosts);
        //                    }

        //                    uint cost = (uint)Mathf.RoundToInt(dist * Int3.FloatPrecision);
        //                    conn.Add(other);
        //                    connectionsAdded++;
        //                    costs.Add(cost);
        //                }
        //                else if (contains && !validConnection) {
        //                    // A connection should be removed

        //                    if (conn == null) {
        //                        tmp_arr.Clear();
        //                        tmp_arr2.Clear();
        //                        conn = tmp_arr;
        //                        costs = tmp_arr2;
        //                        conn.AddRange(node.connections);
        //                        costs.AddRange(node.connectionCosts);
        //                    }

        //                    int p = conn.IndexOf(other);

        //                    //Shouldn't have to check for it, but who knows what might go wrong
        //                    if (p != -1) {
        //                        conn.RemoveAt(p);
        //                        connectionsRemoved++;
        //                        costs.RemoveAt(p);
        //                    }
        //                }
        //            }
        //        }

        //        // Save the new connections if any were changed
        //        if (conn != null) {
        //            node.connections = conn.ToArray();
        //            node.connectionCosts = costs.ToArray();
        //        }
        //    }

        //    // Release buffers back to the pool
        //    Pathfinding.Util.ListPool<GraphNode>.Release(tmp_arr);
        //    Pathfinding.Util.ListPool<uint>.Release(tmp_arr2);

        //    // Connections are single direction so each node pair will have 2
        //    if (connectionsAdded == Constants.Zero && connectionsRemoved == Constants.Zero) {
        //        D.Warn("{0}.UpdateArea() occurred with no connections added or removed?", GetType().Name);
        //    }
        //    else {
        //        D.Log("{0}.UpdateArea() occurred. ConnectionsAdded: {1}, ConnectionsRemoved: {2}.", GetType().Name, connectionsAdded, connectionsRemoved);
        //    }

        //    D.LogBold("{0}.UpdateArea() took {1:0.00} seconds to process {2} nodes.", GetType().Name, (Utility.SystemTime - startTime).TotalSeconds, nodeCount);
        //}

        /// <summary>
        /// Determines whether there is any node inside the provided guoBounds.
        /// </summary>
        /// <param name="guoBounds">The guo bounds.</param>
        /// <returns>
        ///   <c>true</c> if [is any node inside bounds] [the specified guo bounds]; otherwise, <c>false</c>.
        /// </returns>
        //private bool IsAnyNodeInsideBounds(Bounds guoBounds) {
        //    //var startTime = Utility.SystemTime;
        //    bool isNodeInside = false;
        //    for (int i = 0; i < nodeCount; i++) {
        //        if (guoBounds.Contains((Vector3)nodes[i].position)) {
        //            isNodeInside = true;
        //            break;
        //        }
        //    }
        //    //D.Log("{0}.IsAnyNodeInsideBounds() took {1:0.00} seconds.", GetType().Name, (Utility.SystemTime - startTime).TotalSeconds);
        //    return isNodeInside;
        //}



        #endregion

    }
}

