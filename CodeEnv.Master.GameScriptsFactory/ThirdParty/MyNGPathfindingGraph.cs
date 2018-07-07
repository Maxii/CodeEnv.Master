// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyNGPathfindingGraph.cs
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
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using Serialization;
    using UnityEngine;

    /// <summary>
    /// My implementation of the AStar Graph Generator using the points generated from SectorGrid's GridFramework. 
    /// WARNING: These graphs ARE NOT MonoBehaviours, in spite of the authors usage of Awake().
    /// </summary>
    [JsonOptIn]
    public class MyNGPathfindingGraph : NavGraph, IUpdatableGraph {

        /// <summary>
        /// The multiplier to apply to the item radius value used when determining the
        /// radius of the inscribed sphere used to generate the item's surrounding waypoints.
        /// </summary>
        private const float UniverseCenterRadiusMultiplierForWaypointInscribedSphere = 2.5F;



        public string DebugName { get { return GetType().Name; } }

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

        /** Use raycasts to check connections */
        [JsonMember]
        public bool raycast;

        /** Use thick raycast */
        [JsonMember]
        public bool thickRaycast;

        /** Thick raycast radius */
        [JsonMember]
        public float thickRaycastRadius = 0.2F;

        /** Layer mask to use for raycast */
        [JsonMember]
        public LayerMask mask;

        /** Optimizes the graph for sparse graphs.
         *
         * This can reduce calculation times for both scanning and for normal path requests by huge amounts.
         * It reduces the number of node-node checks that need to be done during scan, 
         * and can also optimize getting the nearest node from the graph (such as when querying for a path).
         *
         * Try enabling and disabling this option, check the scan times logged when you scan the graph to 
         * see if your graph is suited for this optimization or if it makes it slower.
         *
         * The gain of using this optimization increases with larger graphs, the default scan algorithm 
         * is brute force and requires O(n^2) checks, this optimization along with a graph suited for it, 
         * requires only O(n) checks during scan (assuming the connection distance limits are reasonable).
         *
         * \note
         * When you have this enabled, you will not be able to move nodes around using scripting 
         * unless you recalculate the lookup structure at the same time.
         * \see RebuildNodeLookup
         *
         * \astarpro
         */
        [JsonMember]
        public bool optimizeForSparseGraph = true;

        private PointKDTree _lookupTree = new PointKDTree();

        /** All nodes in this graph.
         * Note that only the first #nodeCount will be non-null.
         *
         * You can also use the GetNodes method to get all nodes.
         */
        public PointNode[] nodes;

        /** Number of nodes in this graph */
        public int nodeCount { get; private set; }

        public override int CountNodes() {
            return nodeCount;
        }

        public override void GetNodes(System.Action<GraphNode> action) {
            if (nodes == null) {
                return;
            }
            var count = nodeCount;
            for (int i = 0; i < count; i++) {
                action(nodes[i]);
            }
        }

        public override NNInfoInternal GetNearest(Vector3 position, NNConstraint constraint, GraphNode hint) {
            return GetNearestForce(position, null);
        }

        public override NNInfoInternal GetNearestForce(Vector3 position, NNConstraint constraint) {
            if (nodes == null)
                return new NNInfoInternal();

            if (optimizeForSparseGraph) {
                return new NNInfoInternal(_lookupTree.GetNearest((Int3)position, constraint));
            }

            float maxDistSqr = constraint == null || constraint.constrainDistance ? AstarPath.active.maxNearestNodeDistanceSqr : float.PositiveInfinity;

            var nnInfo = new NNInfoInternal(null);
            float minDist = float.PositiveInfinity;
            float minConstDist = float.PositiveInfinity;

            for (int i = 0; i < nodeCount; i++) {
                PointNode node = nodes[i];
                float dist = (position - (Vector3)node.position).sqrMagnitude;

                if (dist < minDist) {
                    minDist = dist;
                    nnInfo.node = node;
                }

                if (dist < minConstDist && dist < maxDistSqr && (constraint == null || constraint.Suitable(node))) {
                    minConstDist = dist;
                    nnInfo.constrainedNode = node;
                }
            }

            nnInfo.UpdateInfo();
            return nnInfo;
        }

        /** Add a node to the graph at the specified position.
         * \note Vector3 can be casted to Int3 using (Int3)myVector.
         *
         * \note This needs to be called when it is safe to update nodes, which is
         * - when scanning
         * - during a graph update
         * - inside a callback registered using AstarPath.RegisterSafeUpdate
         */
        public PointNode AddNode(Int3 position) {
            return AddNode(new PointNode(active), position);
        }

        /** Add a node with the specified type to the graph at the specified position.
         *
         * \param node This must be a node created using T(AstarPath.active) right before the call to this method.
         * The node parameter is only there because there is no new(AstarPath) constraint on
         * generic type parameters.
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
        public T AddNode<T>(T node, Int3 position) where T : PointNode {
            if (nodes == null || nodeCount == nodes.Length) {
                var newNodes = new PointNode[nodes != null ? System.Math.Max(nodes.Length + 4, nodes.Length * 2) : 4];
                for (int i = 0; i < nodeCount; i++) {
                    newNodes[i] = nodes[i];
                }
                nodes = newNodes;
            }

            node.SetPosition(position);
            node.GraphIndex = graphIndex;
            node.Walkable = true;

            nodes[nodeCount] = node;
            nodeCount++;

            AddToLookup(node);

            return node;
        }

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
            if (!optimizeForSparseGraph || nodes == null) {
                _lookupTree = new PointKDTree();
            }
            else {
                //D.Log("{0} is rebuilding sparse graph node tree. NodeCount = {1}.", DebugName, nodeCount);
                _lookupTree.Rebuild(nodes, 0, nodeCount);
            }
        }

        private void AddToLookup(PointNode node) {
            _lookupTree.Add(node);
        }


        /******************************************** My 7.23.17 Implementation using AStar 4.0 *******************/

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

        protected override IEnumerable<Progress> ScanInternal() {
            yield return new Progress(0.1F, "Creating OpenSpace Navigation Nodes");
            var walkableOpenSpaceWaypoints = GenerateWalkableOpenSpaceWaypoints();
            AddNodes(walkableOpenSpaceWaypoints, Topography.OpenSpace.AStarTagValue()/*, ref nextNodeIndex*/);  // OpenSpaceTagMask

            yield return new Progress(0.15F, "Creating System Navigation Nodes");
            var walkableSystemWaypoints = GenerateWalkableInteriorSystemWaypoints();
            AddNodes(walkableSystemWaypoints, Topography.System.AStarTagValue()/*, ref nextNodeIndex*/);    // SystemTagMask

            AddNodesFromInitialStarbases();

            if (optimizeForSparseGraph) {
                yield return new Progress(0.2F, "Building node lookup");
                RebuildNodeLookup();
            }

            yield return new Progress(0.3F, "Making Connections");
            MakeConnections();  // IMPROVE with a yield return every 512 nodes processed
            yield return new Progress(1F, "Connections completed");
        }

        private IList<Vector3> GenerateWalkableOpenSpaceWaypoints() {
#pragma warning disable 0219
            System.DateTime localStartTime = Utility.SystemTime;
            System.DateTime walkableOpenSpaceWaypointsStartTime = localStartTime;
#pragma warning restore 0219
            GameManager gameMgr = GameManager.Instance;

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
            //systemApproachWaypoints.Count, distanceBetweenSystemApproachWaypoints, maxNodeDistance);

            _nodeSeparationDistance = Mathf.Floor(maxNodeDistance) - 7F;       // HACK 275F
            float proposedMaxDistance = _nodeSeparationDistance + 5F;                 // HACK 280F
            if (!Mathfx.Approx(maxDistance, proposedMaxDistance, 1F)) {
                D.Warn("{0}: Changing MaxNodeSeparationDistance, aka 'MaxDistance' from {1:0.#} to {2:0.#}.", GetType().Name, maxDistance, proposedMaxDistance);
                maxDistance = proposedMaxDistance;
            }

            //D.Log("{0} took {1:0.##} secs generating {2} SystemApproachWaypoints for {3} Systems.", DebugName,
            //(Utility.SystemTime - localStartTime).TotalSeconds, systemApproachWaypoints.Count, allSystems.Count());
            localStartTime = Utility.SystemTime;

            // populate all space containing ASectors with navigation waypoints separated by nodeSeparationDistance
            IEnumerable<Vector3> allSectorCenterLocations = SectorGrid.Instance.SectorCenters; ////UniverseCellWorldLocations;
            int sectorQty = allSectorCenterLocations.Count();
            D.Log("{0}: ASectors to scan = {1}.", DebugName, sectorQty);
            List<Vector3> allSectorWaypoints = new List<Vector3>(sectorQty * 37);
            float sectorCenterToCornerDistance = TempGameValues.SectorDiagonalLength / 2F; // 1039.2
            float sectorCenterToFaceDistance = TempGameValues.SectorSideLength / 2F;   // 600
            int outsideOfUniverseWaypointCount = Constants.Zero;

            float universeRadius = gameMgr.GameSettings.UniverseSize.Radius();
            float universeRadiusSqrd = universeRadius * universeRadius;
            //D.Log("{0}: Distance from SectorCenter to a Corner = {1:0.#}.", DebugName, sectorCenterToCornerDistance);
            IList<Vector3> aSectorWaypoints = new List<Vector3>(37);
            List<Vector3> aSectorCornerWaypoints = new List<Vector3>(24);
            List<Vector3> aSectorFaceWaypoints = new List<Vector3>(12);
            foreach (var sectorCenterLoc in allSectorCenterLocations) {
                aSectorWaypoints.Clear();
                if (GameUtility.IsLocationContainedInUniverse(sectorCenterLoc, universeRadiusSqrd)) {
                    aSectorWaypoints.Add(sectorCenterLoc);
                }
                else {
                    outsideOfUniverseWaypointCount++;
                }

                float currentDistanceFromSectorCenter = _nodeSeparationDistance;
                while (currentDistanceFromSectorCenter < sectorCenterToCornerDistance) {    // 275, 550, 825
                    // propagate sector navWaypoints outward from center to corners    // up to 24 per cell
                    aSectorCornerWaypoints.Clear();
                    aSectorCornerWaypoints.AddRange(MyMath.CalcVerticesOfInscribedCubeInsideSphere(sectorCenterLoc, currentDistanceFromSectorCenter));
                    foreach (var aCornerWaypoint in aSectorCornerWaypoints) {
                        if (GameUtility.IsLocationContainedInUniverse(aCornerWaypoint, universeRadiusSqrd)) {
                            aSectorWaypoints.Add(aCornerWaypoint);
                        }
                        else {
                            outsideOfUniverseWaypointCount++;
                        }
                    }

                    // propagate sector navWaypoints outward from center to face centers    // up to 12 per cell 
                    if (currentDistanceFromSectorCenter < sectorCenterToFaceDistance) {   // 275, 550 
                        aSectorFaceWaypoints.Clear();
                        aSectorFaceWaypoints.AddRange(MyMath.CalcCubeFaceCenters(sectorCenterLoc, currentDistanceFromSectorCenter));
                        foreach (var aCellFaceWaypoint in aSectorFaceWaypoints) {
                            if (GameUtility.IsLocationContainedInUniverse(aCellFaceWaypoint, universeRadiusSqrd)) {
                                aSectorWaypoints.Add(aCellFaceWaypoint);
                            }
                            else {
                                outsideOfUniverseWaypointCount++;
                            }
                        }
                    }
                    currentDistanceFromSectorCenter += _nodeSeparationDistance;
                }
                allSectorWaypoints.AddRange(aSectorWaypoints);
            }
            //D.Log("{0} took {1:0.##} secs generating {2} SectorWaypoints for {3} sectors.", DebugName, (Utility.SystemTime - localStartTime).TotalSeconds,
            //allSectorWaypoints.Count, sectorQty);
            localStartTime = Utility.SystemTime;
            //D.Log("{0} filtered out {1} waypoints which were outside of the universe.", DebugName, outsideOfUniverseWaypointCount);

            // With Call Radius of 600 and NodeSeparationDistance of 275, expect cell to cell waypoint distance ~ 90
            //TODO Validate that sectors outside waypoint is within nodeSeparationDistance of neighboring sectors outside waypoint

            if (hasSystems) {
                // Remove SectorWaypoints present inside sphere containing SystemApproachWaypoints    
                // https://en.wikipedia.org/wiki/Regular_icosahedron#Dimensions
                float systemApproachWaypointsCircumscribedSphereRadius = distanceBetweenSystemApproachWaypoints * 0.9510565163F;
                float radiusOfSphereContainingSystemApproachWaypoints = systemApproachWaypointsCircumscribedSphereRadius + 1F;
                int sectorWaypointsRemovedBySystemCount = 0;
                List<Vector3> sectorWaypointsRetainedOutsideSystems = new List<Vector3>(allSectorWaypoints.Count);
                foreach (var system in allSystems) {
                    foreach (var sectorWaypoint in allSectorWaypoints) {
                        if (!MyMath.IsPointOnOrInsideSphere(system.Position, radiusOfSphereContainingSystemApproachWaypoints, sectorWaypoint)) {
                            sectorWaypointsRetainedOutsideSystems.Add(sectorWaypoint);
                        }
                        else {
                            sectorWaypointsRemovedBySystemCount++;
                        }
                    }
                }
                allSectorWaypoints = sectorWaypointsRetainedOutsideSystems;
                //D.Log("{0} took {1:0.##} secs removing {2} Sector Waypoints from {3} Systems.", DebugName, (Utility.SystemTime - localStartTime).TotalSeconds,
                //sectorWaypointsRemovedBySystemCount, allSystems.Count());
            }
            localStartTime = Utility.SystemTime;

            IEnumerable<Vector3> universeCenterWaypoints = Enumerable.Empty<Vector3>();
            var universeCenter = GameManager.Instance.UniverseCreator.UniverseCenter;
            if (universeCenter != null) {
                float distanceBetweenUCenterWaypoints;
                float universeCenterWaypointsInscribedSphereRadius = universeCenter.Data.CloseOrbitOuterRadius * UniverseCenterRadiusMultiplierForWaypointInscribedSphere;
                universeCenterWaypoints = MyMath.CalcVerticesOfIcosahedronSurroundingInscribedSphere(universeCenter.Position, universeCenterWaypointsInscribedSphereRadius,
                    out distanceBetweenUCenterWaypoints);
                if (distanceBetweenUCenterWaypoints > _nodeSeparationDistance) {
                    D.Error("{0} should be <= {1}.", distanceBetweenUCenterWaypoints, _nodeSeparationDistance);
                }
                //D.Log("{0}: Distance between UCenterWaypoints = {1:0.#}.", DebugName, distanceBetweenUCenterWaypoints);

                // remove SectorWaypoints present inside sphere containing UniverseCenterWaypoints
                float uCenterWaypointsCircumscribedSphereRadius = distanceBetweenUCenterWaypoints * 0.9510565163F;
                float radiusOfSphereContainingUCenterWaypoints = uCenterWaypointsCircumscribedSphereRadius + 1F;
                List<Vector3> sectorWaypointsRetainedOutsideOfUniverseCenter = new List<Vector3>(allSectorWaypoints.Count);
                int sectorWaypointsRemovedByUniverseCenter = 0;
                foreach (var sectorWaypoint in allSectorWaypoints) {
                    if (!MyMath.IsPointOnOrInsideSphere(universeCenter.Position, radiusOfSphereContainingUCenterWaypoints, sectorWaypoint)) {
                        sectorWaypointsRetainedOutsideOfUniverseCenter.Add(sectorWaypoint);
                    }
                    else {
                        sectorWaypointsRemovedByUniverseCenter++;
                    }
                }
                allSectorWaypoints = sectorWaypointsRetainedOutsideOfUniverseCenter;
                //D.Log("{0} took {1:0.##} secs removing {2} SectorWaypoints around UniverseCenter.", DebugName,
                //(Utility.SystemTime - localStartTime).TotalSeconds, sectorWaypointsRemovedByUniverseCenter);
            }

            List<Vector3> walkableOpenSpaceWaypoints = new List<Vector3>(allSectorWaypoints);
            walkableOpenSpaceWaypoints.AddRange(universeCenterWaypoints);
            walkableOpenSpaceWaypoints.AddRange(systemApproachWaypoints);

            D.Log("{0} took {1:0.##} secs generating {2} WalkableOpenSpaceWaypoints for {3} Sectors.", DebugName,
                (Utility.SystemTime - walkableOpenSpaceWaypointsStartTime).TotalSeconds, walkableOpenSpaceWaypoints.Count, sectorQty);

            localStartTime = Utility.SystemTime;
            __ValidateWaypointsInsideUniverse(walkableOpenSpaceWaypoints, universeRadiusSqrd);
            //D.Log("{0} took {1:0.##} secs validating {2} WalkableOpenSpaceWaypoints are inside the Universe.", DebugName,
            //(Utility.SystemTime - localStartTime).TotalSeconds, walkableOpenSpaceWaypoints.Count);

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

            var startTime = Utility.SystemTime;
            float universeRadius = GameManager.Instance.GameSettings.UniverseSize.Radius();
            float universeRadiusSqrd = universeRadius * universeRadius;
            __ValidateWaypointsInsideUniverse(allSystemInteriorWaypoints, universeRadiusSqrd);
            //D.Log("{0} took {1:0.##} secs validating {2} WalkableInteriorSystemWaypoints are inside the Universe.",
            //DebugName, (Utility.SystemTime - startTime).TotalSeconds, allSystemInteriorWaypoints.Count);

            return allSystemInteriorWaypoints;
        }

        private void AddNodesFromInitialStarbases() {
            var initialStarbases = StarbasesFolder.Instance.GetComponentsInChildren<StarbaseCmdItem>();
            if (initialStarbases.Any()) {
                foreach (var starbase in initialStarbases) {
                    HandleStarbaseAdded(starbase);
                }
            }
            else {
                D.Warn("{0}: No initial starbases?", DebugName);
            }
        }

        private void MakeConnections() {
            if (maxDistance >= 0F) {
                var startTime = Utility.SystemTime;
                // To avoid too many allocations, these lists are reused for each node
                var connections = new List<Connection>();
                var candidateConnections = new List<GraphNode>();

                int connectionCount = 0;
                int invalidConnectionCount = 0;

                // Max possible squared length of a connection between two nodes. WARNING: Needs to be in Int3 units
                // This is used to speed up the calculations by skipping a lot of nodes that do not need to be checked
                long maxPossibleSqrRange = maxDistance == 0F ? long.MaxValue : (long)((maxDistance * Int3.Precision) * (maxDistance * Int3.Precision));

                // Loop through all nodes and add connections to other nodes
                for (int i = 0; i < nodeCount; i++) {

                    connections.Clear();

                    PointNode node = nodes[i];
                    if (optimizeForSparseGraph) {
                        candidateConnections.Clear();
                        _lookupTree.GetInRange(node.position, maxPossibleSqrRange, candidateConnections);
                        for (int j = 0; j < candidateConnections.Count; j++) {
                            var other = candidateConnections[j] as PointNode;
                            float dist;
                            if (other != node && IsValidConnection(node, other, out dist)) {
                                connections.Add(new Connection {
                                    node = other,
                                    /** \todo Is this equal to .costMagnitude */
                                    cost = (uint)Mathf.RoundToInt(dist * Int3.FloatPrecision)
                                });
                                connectionCount++;
                            }
                            else {
                                invalidConnectionCount++;
                            }
                        }
                    }
                    else {
                        // Only brute force is available in the free version
                        for (int j = 0; j < nodeCount; j++) {
                            if (i == j) {
                                continue;
                            }

                            PointNode other = nodes[j];
                            float dist;
                            if (IsValidConnection(node, other, out dist)) {
                                connections.Add(new Connection {
                                    node = other,
                                    /** \todo Is this equal to .costMagnitude */
                                    cost = (uint)Mathf.RoundToInt(dist * Int3.FloatPrecision)
                                });
                                connectionCount++;
                            }
                            else {
                                invalidConnectionCount++;
                            }
                        }
                    }
                    node.connections = connections.ToArray();
                }
                //D.Log("{0} took {1:0.##} secs connecting {2} waypoints. Invalid connections = {3}.", DebugName,
                //(Utility.SystemTime - startTime).TotalSeconds, connectionCount, invalidConnectionCount);
            }
        }

        /// <summary>
        /// Adds walkable nodes derived from the provided waypoints to the graph's collection of nodes,
        /// returning the added nodes.
        /// </summary>
        /// <param name="waypoints">The waypoints.</param>
        /// <param name="tag">The tag for the nodes.</param>
        /// <returns></returns>
        private IList<PointNode> AddNodes(IList<Vector3> waypoints, uint tag) {
            IList<PointNode> nodesAdded = new List<PointNode>(waypoints.Count);
            foreach (var wayPt in waypoints) {
                Int3 position = (Int3)wayPt;
                PointNode addedNode = AddNode(position);
                addedNode.Tag = tag;

                nodesAdded.Add(addedNode);
            }
            return nodesAdded;
        }

        /// <summary>
        /// Adds walkable nodes derived from the provided waypoints to the graph's collection of nodes,
        /// returning the added nodes.
        /// </summary>
        /// <param name="waypoints">The waypoints.</param>
        /// <param name="tag">The tag for the nodes.</param>
        /// <param name="nextNodeIndex">Index of the next node.</param>
        /// <returns></returns>
        [Obsolete]
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

        [Obsolete]
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

        /** Returns if the connection between \a a and \a b is valid.
         * Checks for obstructions using raycasts (if enabled) and checks for height differences.\n
         * As a bonus, it outputs the distance between the nodes too if the connection is valid.
         *
         * \note This is not the same as checking if node a is connected to node b.
         * That should be done using a.ContainsConnection(b)
         */
        private bool IsValidConnection(GraphNode a, GraphNode b, out float dist) {
            dist = 0F;

            if (!a.Walkable || !b.Walkable) {
                //D.Log("{0}: a node is not walkable.", DebugName);
                return false;
            }

            var dir = (Vector3)(b.position - a.position);
            dist = dir.magnitude;
            if (maxDistance == 0F || dist < maxDistance) {
                if (raycast) {
                    if (thickRaycast) {
                        var ray = new Ray((Vector3)a.position, dir);
                        var invertRay = new Ray((Vector3)b.position, -dir);
                        return !Physics.SphereCast(ray, thickRaycastRadius, dist, mask) && !Physics.SphereCast(invertRay, thickRaycastRadius, dist, mask);
                    }
                    else {
                        return !Physics.Linecast((Vector3)a.position, (Vector3)b.position, mask) && !Physics.Linecast((Vector3)b.position, (Vector3)a.position, mask);
                    }
                }
                else {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Updates the graph during runtime adding approach waypoints for this starbase
        /// and makes any waypoints located inside the new approach waypoints unwalkable, then reconnects.
        /// </summary>
        /// <param name="baseCmd">The Starbase command.</param>
        public void AddToGraph(StarbaseCmdItem baseCmd) {
            //D.Log("{0}.AddToGraph({1}) called. Frame = {2}.", DebugName, baseCmd.DebugName, Time.frameCount);
            // Note: active.IsAnyGraphUpdatesQueued is never true except when using UpdateGraphs(). 
            // I've replaced UpdateGraphs(GUO) with WorkItems.

            // ctxProcessor and forceCompletion are assigned by AstarPath internally 
            var handleStarbaseAddedWorkItem = new AstarWorkItem(update: (ctxProcessor, forceCompletion) => {
                ctxProcessor.QueueFloodFill();
                HandleStarbaseAdded(baseCmd);
                // 7.24.17 Per Aron, no need to rebuild lookupTable as AddNode() adds the node to the table. Table does not care about walkable
                return true;
            });
            active.AddWorkItem(handleStarbaseAddedWorkItem);

            // Note: 8.17.16 no current way to remove a work item once added. Otherwise, if I got another call to this
            // method with IsAnyGraphUpdatesQueued = true, I'd remove the previous queued MakeConnections and replace 
            // with a new one at the end. Currently, if I get another call while queued, MakeConnections will run twice.

            var makeConnectionsWorkItem = new AstarWorkItem(update: (forceCompletion) => {
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
            // I've replaced UpdateGraphs(GUO) with WorkItems.

            // forceCompletion is set by AstarPath internally 
            var handleStarbaseBeingRemovedWorkItem = new AstarWorkItem(update: (ctxProcessor, forceCompletion) => {
                ctxProcessor.QueueFloodFill();
                HandleStarbaseRemoved(baseCmd);
                // 7.24.17 Per Aron, no need to rebuild lookupTable as table does not care about walkable
                return true;
            });
            active.AddWorkItem(handleStarbaseBeingRemovedWorkItem);

            // Note: 8.17.16 no current way to remove a work item once added. Otherwise, if I got another call to this
            // method with IsAnyGraphUpdatesQueued = true, I'd remove the previous queued MakeConnections and replace 
            // with a new one at the end. Currently, if I get another call while queued, MakeConnections will run twice.

            var makeConnectionsWorkItem = new AstarWorkItem(update: (forceCompletion) => {
                MakeConnections();
                D.Log("{0} has completed making node connections for {1}.", DebugName, baseCmd.DebugName);
                return true;
            });
            active.AddWorkItem(makeConnectionsWorkItem);
        }

        // NOTE: For now, no Add/Remove(Settlement). Settlements aren't likely to be on top of existing waypoints, and,
        // surrounding them with waypoints makes no sense if I allow them to orbit

        private void HandleStarbaseAdded(StarbaseCmdItem starbaseCmd) {
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

            float universeRadius = GameManager.Instance.GameSettings.UniverseSize.Radius();
            float universeRadiusSqrd = universeRadius * universeRadius;
            foreach (var waypoint in approachWaypoints) {
                if (!GameUtility.IsLocationContainedInUniverse(waypoint, universeRadiusSqrd)) {
                    D.Warn("{0} is excluding {1}'s proposed approach waypoint {2} that is outside the universe?", DebugName, starbaseCmd.DebugName,
                        waypoint);
                    approachWaypointsToRemove.Add(waypoint);
                }
            }

            ISystem systemInSector;
            IntVector3 starbaseSectorID = starbaseCmd.SectorID;
            bool doesSectorContainSystem = GameManager.Instance.GameKnowledge.TryGetSystem(starbaseSectorID, out systemInSector);
            if (doesSectorContainSystem) {
                foreach (var waypoint in approachWaypoints) {
                    if (MyMath.IsPointOnOrInsideSphere(systemInSector.Position, systemInSector.Radius, waypoint)) {
                        // FIXME warn for now as I should preclude locating a Starbase close to a system
                        D.Warn("{0} is excluding {1}'s proposed approach waypoint {2} that is inside adjacent System {3}.", DebugName,
                            starbaseCmd.DebugName, waypoint, systemInSector.DebugName);
                        approachWaypointsToRemove.Add(waypoint);
                    }
                }
            }

            foreach (var waypoint in approachWaypointsToRemove) {
                approachWaypoints.Remove(waypoint);
            }

            nds = AddNodes(approachWaypoints, Topography.OpenSpace.AStarTagValue());
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

        private void HandleStarbaseRemoved(StarbaseCmdItem starbaseCmd) {
            // Make existing Starbase approach nodes unwalkable
            D.Assert(_starbaseApproachNodes.ContainsKey(starbaseCmd), DebugName);
            IList<PointNode> nds = _starbaseApproachNodes[starbaseCmd];
            foreach (var approachNode in nds) {
                approachNode.Walkable = false;
            }
            _starbaseApproachNodes.Remove(starbaseCmd);
            //D.Log("{0} has completed making {1} approach nodes unwalkable as a result of {2}'s removal.", DebugName, nds.Count, starbaseCmd.DebugName);

            // Now make Sector nav nodes previously made unwalkable, walkable again
            D.Assert(_sectorNavNodesMadeUnwalkableByStarbase.ContainsKey(starbaseCmd), DebugName);
            nds = _sectorNavNodesMadeUnwalkableByStarbase[starbaseCmd];
            foreach (var unwalkableSectorNavNode in nds) {
                unwalkableSectorNavNode.Walkable = true;
            }
            _sectorNavNodesMadeUnwalkableByStarbase.Remove(starbaseCmd);
            //D.Log("{0} has completed making {1} sector nav nodes walkable again as a result of {2}'s removal.", DebugName, nds.Count, starbaseCmd.DebugName);
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __ValidateWaypointsInsideUniverse(IEnumerable<Vector3> walkableWaypoints, float universeRadiusSqrd) {
            foreach (var waypoint in walkableWaypoints) {
                D.Assert(GameUtility.IsLocationContainedInUniverse(waypoint, universeRadiusSqrd));
            }
        }

        #endregion

        /***************************************** End of My Implementation *****************************************/

        protected override void PostDeserialization(GraphSerializationContext ctx) {
            RebuildNodeLookup();
        }

        public override void RelocateNodes(Matrix4x4 deltaMatrix) {
            base.RelocateNodes(deltaMatrix);
            RebuildNodeLookup();
        }

        protected override void DeserializeSettingsCompatibility(GraphSerializationContext ctx) {
            base.DeserializeSettingsCompatibility(ctx);

            maxDistance = ctx.reader.ReadSingle();
            raycast = ctx.reader.ReadBoolean();
            thickRaycast = ctx.reader.ReadBoolean();
            thickRaycastRadius = ctx.reader.ReadSingle();
            ctx.reader.ReadBoolean(); // Deprecated field
            mask = (LayerMask)ctx.reader.ReadInt32();
            optimizeForSparseGraph = ctx.reader.ReadBoolean();
            ctx.reader.ReadBoolean(); // Deprecated field
        }

        protected override void SerializeExtraInfo(GraphSerializationContext ctx) {
            // Serialize node data
            if (nodes == null) {
                ctx.writer.Write(-1);
            }

            // Length prefixed array of nodes
            ctx.writer.Write(nodeCount);
            for (int i = 0; i < nodeCount; i++) {
                // -1 indicates a null field
                if (nodes[i] == null) {
                    ctx.writer.Write(-1);
                }
                else {
                    ctx.writer.Write(0);
                    nodes[i].SerializeNode(ctx);
                }
            }
        }

        protected override void DeserializeExtraInfo(GraphSerializationContext ctx) {
            int count = ctx.reader.ReadInt32();

            if (count == -1) {
                nodes = null;
                return;
            }

            nodes = new PointNode[count];
            nodeCount = count;

            for (int i = 0; i < nodes.Length; i++) {
                if (ctx.reader.ReadInt32() == -1) {
                    continue;
                }
                nodes[i] = new PointNode(active);
                nodes[i].DeserializeNode(ctx);
            }
        }

        public override string ToString() {
            return DebugName;
        }

        #region IUpdateableGraph Members

        public GraphUpdateThreading CanUpdateAsync(GraphUpdateObject o) {
            return GraphUpdateThreading.UnityThread;
        }

        public void UpdateAreaInit(GraphUpdateObject o) { }
        public void UpdateAreaPost(GraphUpdateObject o) { }

        /** Updates an area in the list graph.
         * Recalculates possibly affected connections, i.e all connection lines passing trough the bounds of the \a guo will be recalculated
         * \astarpro */
        public void UpdateArea(GraphUpdateObject guo) {
            if (nodes == null) {
                return;
            }

            for (int i = 0; i < nodeCount; i++) {
                if (guo.bounds.Contains((Vector3)nodes[i].position)) {
                    guo.WillUpdateNode(nodes[i]);
                    guo.Apply(nodes[i]);
                }
            }

            if (guo.updatePhysics) {
                //Use a copy of the bounding box, we should not change the GUO's bounding box since it might be used for other graph updates
                Bounds bounds = guo.bounds;

                if (thickRaycast) {
                    //Expand the bounding box to account for the thick raycast
                    bounds.Expand(thickRaycastRadius * 2);
                }

                //Create two temporary arrays used for holding new connections and costs
                List<Connection> tmp_arr = Pathfinding.Util.ListPool<Connection>.Claim();

                for (int i = 0; i < nodeCount; i++) {
                    PointNode node = nodes[i];
                    var nodePos = (Vector3)node.position;

                    List<Connection> conn = null;

                    for (int j = 0; j < nodeCount; j++) {
                        if (j == i)
                            continue;

                        var otherNodePos = (Vector3)nodes[j].position;
                        // Check if this connection intersects the bounding box.
                        // If it does we need to recalculate that connection.
                        if (VectorMath.SegmentIntersectsBounds(bounds, nodePos, otherNodePos)) {
                            float dist;
                            PointNode other = nodes[j];
                            bool contains = node.ContainsConnection(other);
                            bool validConnection = IsValidConnection(node, other, out dist);

                            if (!contains && validConnection) {
                                // A new connection should be added

                                if (conn == null) {
                                    tmp_arr.Clear();
                                    conn = tmp_arr;
                                    conn.AddRange(node.connections);
                                }

                                uint cost = (uint)Mathf.RoundToInt(dist * Int3.FloatPrecision);
                                conn.Add(new Connection { node = other, cost = cost });
                            }
                            else if (contains && !validConnection) {
                                // A connection should be removed

                                if (conn == null) {
                                    tmp_arr.Clear();
                                    conn = tmp_arr;
                                    conn.AddRange(node.connections);
                                }

                                for (int q = 0; q < conn.Count; q++) {
                                    if (conn[q].node == other) {
                                        conn.RemoveAt(q);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // Save the new connections if any were changed
                    if (conn != null) {
                        node.connections = conn.ToArray();
                    }
                }

                // Release buffers back to the pool
                Pathfinding.Util.ListPool<Connection>.Release(tmp_arr);
            }
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

