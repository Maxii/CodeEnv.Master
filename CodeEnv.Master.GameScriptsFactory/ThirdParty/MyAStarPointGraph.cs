// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyAStarPointGraph.cs
// My implementation of the AStar PointGraph Generator using the points generated from SectorGrid's GridFramework. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace Pathfinding {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// My implementation of the AStar PointGraph Generator using the 
    /// points generated from SectorGrid's GridFramework. 
    /// WARNING: These graphs ARE NOT MonoBehaviours, inspite of the authors usage of Awake().
    /// </summary>
    public class MyAStarPointGraph : PointGraph {

        public static int openSpaceTagMask = 1 << (int)SpaceTopography.OpenSpace;   // x0001
        public static int nebulaTagMask = 1 << (int)SpaceTopography.Nebula;         // x0010
        public static int deepNebulaTagMask = 1 << (int)SpaceTopography.DeepNebula; // x0100
        public static int systemTagMask = 1 << (int)SpaceTopography.System;         // x1000

        #region Archived

        /// <summary>
        /// This will be called on the same time as Awake on the gameObject which the AstarPath script is attached to. (remember, not in the editor)
        /// Use this for any initialization code which can't be placed in Scan
        /// </summary>
        //public override void Awake() {
        //    base.Awake();
        //}

        /// <summary>
        ///  This will be called on the same time as OnDisable on the gameObject which the AstarPath script is attached to (remember, not in the editor)
        /// Use for any cleanup code such as cleaning up static variables which otherwise might prevent resources from being collected
        /// Use by creating a function overriding this one in a graph class, but always call base.OnDestroy () in that function.
        /// </summary>
        //public override void OnDestroy() {
        //    base.OnDestroy();
        //}

        #endregion

        public override NNInfo GetNearestForce(Vector3 position, NNConstraint constraint) {
            if (nodes == null) return new NNInfo();

            float maxDistSqr = constraint.constrainDistance ? AstarPath.active.maxNearestNodeDistanceSqr : float.PositiveInfinity;

            float minDist = float.PositiveInfinity;
            GraphNode minNode = null;

            float minConstDist = float.PositiveInfinity;
            GraphNode minConstNode = null;

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

            NNInfo nnInfo = new NNInfo(minNode);

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
            //    D.Log("Constaint criteria met. Closest Node is at {0}, {1} from {2}. \nNodeConstrainDistance = {3}, DistanceConstraint = {4}.",
            //        nnInfo.constClampedPosition, Vector3.Distance(nnInfo.constClampedPosition, position), position,
            //        constraint.constrainDistance, Mathf.Sqrt(maxDistSqr));
            //}
            //else {
            //    D.Log("Constraint critieria NOT met. Closest Node is at {0}, {1} from {2}. \nNodeConstrainDistance = {3}, DistanceConstraint = {4}.",
            //        nnInfo.clampedPosition, Vector3.Distance(nnInfo.clampedPosition, position), position,
            //        constraint.constrainDistance, Mathf.Sqrt(maxDistSqr));
            //}

            #endregion

            return nnInfo;
        }

        // Scan() has been deprecated, replaced by ScanInternal. // IMPROVE Docs recommend using AstarPath.Scan() 
        public override void ScanInternal(OnScanStatus statusCallback) {
            // ********************************************************************
            // NOTE: removed all code that derived nodes from GameObjects and Tags
            // ********************************************************************
            IDictionary<SpaceTopography, IList<Vector3>> graphWaypointsLookupByTag = ConstructGraphWaypoints();
            PopulateNodes(graphWaypointsLookupByTag);
            // ********************************************************************
            D.Log("{0} Pathfinding nodes.", nodeCount);

            if (maxDistance >= 0) {
                //To avoid too many allocations, these lists are reused for each node
                List<PointNode> connections = new List<PointNode>(3);
                List<uint> costs = new List<uint>(3);

                //Loop through all nodes and add connections to other nodes
                int connectionCount = 0;
                int invalidConnectionCount = 0;
                for (int i = 0; i < nodes.Length; i++) {
                    connections.Clear();
                    costs.Clear();

                    PointNode node = nodes[i];

                    // Only brute force is available in the free version
                    for (int j = 0; j < nodes.Length; j++) {
                        if (i == j) continue;

                        PointNode other = nodes[j];

                        float dist = 0;
                        if (IsValidConnection(node, other, out dist)) {
                            connections.Add(other);
                            /** \todo Is this equal to .costMagnitude */
                            costs.Add((uint)Mathf.RoundToInt(dist * Int3.FloatPrecision));
                        }
                        else if (dist <= maxDistance) { // maxDistance is currently 700
                            invalidConnectionCount++;
                            D.Warn("Connection from Node at {0} to Node at {1} is invalid.", (Vector3)node.position, (Vector3)other.position);
                        }
                    }
                    node.connections = connections.ToArray();
                    connectionCount += connections.Count;
                    node.connectionCosts = costs.ToArray();
                }
                int totalConnectionsAttempted = connectionCount + invalidConnectionCount;
                D.Log("{0}/{1} valid connections.", connectionCount, totalConnectionsAttempted);
            }
        }

        /// <summary>
        /// Pre-runtime construction of waypoints for the point graph.
        /// </summary>
        /// <returns></returns>
        private IDictionary<SpaceTopography, IList<Vector3>> ConstructGraphWaypoints() {
            var sectors = SectorGrid.Instance.AllSectors;
            var sectorCenters = SectorGrid.Instance.SectorCenters;
            D.Assert(sectorCenters != null, "{0} not yet initialized.".Inject(typeof(SectorGrid).Name));  // AstarPath has an option to automatically call Scan() on Awake which can be too early
            IEnumerable<Vector3> openSpaceWaypoints = new List<Vector3>(sectorCenters);

            // The 8 vertices of the box inscribed inside the sector's spherical radius
            // Note: these navigational waypoints are clearly inside the boudary of the sector so they can be weighted with the sectors movement penalty
            // This avoids the weighting ambiguity that exists when navigational waypoints are equidistant between sectors, aka shared between sectors
            IEnumerable<Vector3> interiorSectorPoints = Enumerable.Empty<Vector3>();
            sectors.ForAll(s => {
                interiorSectorPoints = interiorSectorPoints.Union(SectorGrid.GenerateVerticesOfBoxAroundCenter(s.SectorIndex, s.Radius));
            });
            openSpaceWaypoints = openSpaceWaypoints.Union(interiorSectorPoints);

            var universeCenter = UniverseFolder.Instance.Folder.GetComponentInChildren<UniverseCenterItem>();
            if (universeCenter != null) {
                var pointsAroundUniverseCenter = UnityUtility.CalcVerticesOfInscribedBoxInsideSphere(universeCenter.Position, universeCenter.ShipOrbitSlot.OuterRadius);
                openSpaceWaypoints = openSpaceWaypoints.Except(new List<Vector3>() { universeCenter.Position }, UnityUtility.Vector3EqualityComparer);
                openSpaceWaypoints = openSpaceWaypoints.Union(pointsAroundUniverseCenter, UnityUtility.Vector3EqualityComparer);
            }

            IEnumerable<Vector3> allWaypointsInsideSystems = Enumerable.Empty<Vector3>();

            var systems = SystemCreator.AllSystems;
            if (systems.Any()) {
                var systemPositions = systems.Select(sys => sys.Position);
                var allWaypointsPeripheralToSystems = Enumerable.Empty<Vector3>();
                systems.ForAll(sys => {
                    var waypointsPeripheralToSystem = UnityUtility.CalcVerticesOfInscribedBoxInsideSphere(sys.Position, sys.Radius);
                    allWaypointsPeripheralToSystems = allWaypointsPeripheralToSystems.Union(waypointsPeripheralToSystem);

                    var waypointsInsideSystem = UnityUtility.CalcVerticesOfInscribedBoxInsideSphere(sys.Position, sys.Radius / 2F);
                    //D.Log("{0} interior waypoints generated = {1}.", sys.FullName, waypointsInsideSystem.Concatenate());
                    allWaypointsInsideSystems = allWaypointsInsideSystems.Union(waypointsInsideSystem, UnityUtility.Vector3EqualityComparer);
                });

                openSpaceWaypoints = openSpaceWaypoints.Except(systemPositions, UnityUtility.Vector3EqualityComparer);
                openSpaceWaypoints = openSpaceWaypoints.Union(allWaypointsPeripheralToSystems, UnityUtility.Vector3EqualityComparer);
            }

            // Starbase-related waypoints are now added or removed when they are created or destroyed - UpdateGraph()
            //var starbaseCreators = Universe.Instance.Folder.GetComponentsInChildren<StarbaseUnitCreator>();
            //if (starbaseCreators.Any()) {
            //    var starbaseLocations = starbaseCreators.Select(sbc => sbc.transform.position);
            //    // can't create any points around a starbase at this time as the creators hasn't instantiated any Starbase prefabs yet
            //    openSpaceWaypoints = openSpaceWaypoints.Except(starbaseLocations, UnityUtility.Vector3EqualityComparer);
            //}

            return new Dictionary<SpaceTopography, IList<Vector3>>() {
                { SpaceTopography.OpenSpace, openSpaceWaypoints.ToList() },
                { SpaceTopography.Nebula, new List<Vector3>() },        // TODO
                { SpaceTopography.DeepNebula, new List<Vector3>() },    // TODO
                { SpaceTopography.System, allWaypointsInsideSystems.ToList() }
                };
        }


        /// <summary>
        /// Creates a PointNode array populated with waypoint positions, walkability and penalty tags.
        /// </summary>
        /// <param name="graphWaypointsLookupByTag">The graph waypoints lookup by tag.</param>
        private void PopulateNodes(IDictionary<SpaceTopography, IList<Vector3>> graphWaypointsLookupByTag) {
            int waypointCount = graphWaypointsLookupByTag.Values.Sum(list => list.Count);

            // WARNING: The approach used below to populate nodes is the only one that worked. I tried making
            // a separate nodes array, populating it manually, then assigning it to nodes as well as using AddNode().
            // In both cases, the Editor showed 0 total nodes even though they were really built. The approach below
            // is copied directly from that approach used in PointGraph.ScanInternal when deriving nodes from gameobjects

            nodes = new PointNode[waypointCount];
            nodeCount = waypointCount;
            for (int i = 0; i < nodeCount; i++) {
                nodes[i] = new PointNode(active);
            }

            // initialize nodes that will be tagged OpenSpace
            var waypoints = graphWaypointsLookupByTag[SpaceTopography.OpenSpace];
            //D.Log("Creating {0} pathfinding nodes with tag {1}.", waypoints.Count, SpaceTopography.OpenSpace.GetName());
            //D.Log("{0} tag mask = {1}.", SpaceTopography.OpenSpace.GetName(), StringExtensions.GetBinaryString(openSpaceTagMask));
            for (int i = 0; i < waypoints.Count; i++) {
                nodes[i].SetPosition((Int3)waypoints[i]);
                nodes[i].Walkable = true;
                nodes[i].Tag = (uint)openSpaceTagMask;
            }
            int nextNodeIndex = waypoints.Count;

            // initialize nodes that will be tagged System
            waypoints = graphWaypointsLookupByTag[SpaceTopography.System];
            //D.Log("Creating {0} pathfinding nodes with tag {1}.", waypoints.Count, SpaceTopography.System.GetName());
            //D.Log("{0} tag mask = {1}.", SpaceTopography.System.GetName(), StringExtensions.GetBinaryString(systemTagMask));
            for (int i = nextNodeIndex; i < nextNodeIndex + waypoints.Count; i++) {
                nodes[i].SetPosition((Int3)waypoints[i - nextNodeIndex]);
                nodes[i].Walkable = true;
                nodes[i].Tag = (uint)systemTagMask;
            }
            nextNodeIndex += waypoints.Count;
            D.Assert(waypointCount == nextNodeIndex);
            // TODO initialize nodes that will be tagged Nebula and DeepNebula
        }


        //private IDictionary<StarbaseCmdModel, List<GraphUpdateObject>> _starbaseGuos = new Dictionary<StarbaseCmdModel, List<GraphUpdateObject>>();
        // FIXME check all dictionaries for mutable keys and replace with a GuidID

        /// <summary>
        /// Updates the graph during runtime adding or removing the waypoints associated with this starbase.
        /// The method determines which based on whether the starbase has previously been recorded.
        /// </summary>
        /// <param name="baseCmd">The Starbase command.</param>
        public void UpdateGraph(StarbaseCommandItem baseCmd) {
            throw new System.NotImplementedException("{0}.UpdateGraph(StarbaseCommandItem) is awaiting upgrade to AstarPath.Pro.".Inject(GetType().Name));
            // *****************************************************************************
            // TODO Aren: "PointGraphs implement GraphUpdateObject.Apply() only in Pro"
            // GraphUpdateObject.RevertFromBackup() implementation was forgotten by devs
            // Currently, using this generates Nodes around the starbase, but without connections
            // As a result, those nodes are in another GraphNode.area which leads to a course plotting error
            // *****************************************************************************

            //List<GraphUpdateObject> guos = null;
            //if (_starbaseGuos.TryGetValue(baseCmd, out guos)) {
            //    // this base is being removed
            //    guos.ForAll(guo => guo.RevertFromBackup()); // reverses the node changes made when base was added    // UNDONE not yet supported
            //    _starbaseGuos.Remove(baseCmd);
            //}
            //else {
            //    // base is being added
            //    ShipOrbitSlot baseShipOrbitSlot = baseCmd.ShipOrbitSlot;
            //    D.Assert(baseShipOrbitSlot != null, "{0}.ShipOrbitSlot is not set.".Inject(baseCmd.FullName));

            //    //OrbitalSlot baseShipOrbitSlot = baseCmd.ShipOrbitSlot;
            //    //D.Assert(baseShipOrbitSlot != default(OrbitalSlot), "{0}.ShipOrbitSlot is not set.".Inject(baseCmd.FullName));
            //    Vector3 basePosition = baseCmd.Position;

            //    guos = new List<GraphUpdateObject>(9);

            //    // create keepoutZone GUO that makes any existing nodes in the keepoutZone unwalkable
            //    float baseKeepoutZoneRadius = baseShipOrbitSlot.InnerRadius;
            //    Vector3 keepoutZoneSize = Vector3.one * 2F * baseKeepoutZoneRadius;
            //    Bounds keepoutZoneBounds = new Bounds(basePosition, keepoutZoneSize);
            //    //D.Log("KeepoutZoneBounds {0} contains {1}: {2}.", keepoutZoneBounds, basePosition, keepoutZoneBounds.Contains(basePosition));
            //    GraphUpdateObject keepoutZoneGuo = new GraphUpdateObject(keepoutZoneBounds) {
            //        modifyWalkability = true,
            //        setWalkability = false,
            //        updatePhysics = true,    // default
            //        trackChangedNodes = true
            //    };
            //    guos.Add(keepoutZoneGuo);

            //    // create new waypoint nodes that surround the starbase  
            //    //D.Log("{0} node count before adding {1}.", nodeCount, baseCmd.FullName);
            //    float surroundingAreaWaypointRadius = baseShipOrbitSlot.OuterRadius * 3F;
            //    var waypoints = UnityUtility.CalcVerticesOfInscribedBoxInsideSphere(basePosition, surroundingAreaWaypointRadius);
            //    AstarPath.active.AddWorkItem(new AstarPath.AstarWorkItem(delegate() {
            //        waypoints.ForAll(w => AddNode((Int3)w));
            //    }, null));

            //    // create surrounding waypoint GUOs that flesh out the newly made waypoint nodes 
            //    IList<GraphUpdateObject> surroundWaypointGuos = new List<GraphUpdateObject>(8);
            //    for (int i = 0; i < 8; i++) {
            //        var waypointBounds = new Bounds(waypoints[i], Vector3.one);
            //        surroundWaypointGuos.Add(new GraphUpdateObject(waypointBounds) {
            //            modifyWalkability = true,
            //            setWalkability = true,
            //            modifyTag = true,
            //            setTag = openSpaceTagMask,
            //            updatePhysics = true,   // default
            //            trackChangedNodes = true
            //        });
            //    }
            //    guos.AddRange(surroundWaypointGuos);    // guos must be List (not IList) to support .AddRange()

            //    // Note: GraphUpdateObject internally queues another AstarWorkItem when UpdateGraphs(guo) is called
            //    guos.ForAll(guo => AstarPath.active.UpdateGraphs(guo));
            //    _starbaseGuos.Add(baseCmd, guos);
            //}
        }

        // NOTE: For now, no UpdateGraph(Settlement). Settlements aren't likely to be on top of existing waypoints, and,
        // surrounding them with waypoints makes no sense if I allow them to orbit


        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

