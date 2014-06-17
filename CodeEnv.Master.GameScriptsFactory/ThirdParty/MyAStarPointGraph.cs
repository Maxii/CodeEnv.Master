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

//#define DEBUG_LOG
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

        //public IList<Vector3> GraphWaypoints { get; private set; }

        public IDictionary<SpaceTopography, IList<Vector3>> GraphWaypointsLookupByTag { get; private set; }

        //private UniverseCenterView _universeCenterView;

        /// <summary>
        /// This will be called on the same time as Awake on the gameObject which the AstarPath script is attached to. (remember, not in the editor)
        /// Use this for any initialization code which can't be placed in Scan
        /// </summary>
        //public override void Awake() {
        //    base.Awake();
        //}

        // IMPROVE not really necessary. I just override this NavGraph method to add the debug line at the bottom. Otherwise its identical
        public override NNInfo GetNearest(Vector3 position, NNConstraint constraint, Node hint) {

            if (nodes == null) {
                return new NNInfo();
            }

            float maxDistSqr = constraint.constrainDistance ? AstarPath.active.maxNearestNodeDistanceSqr : float.PositiveInfinity;

            float minDist = float.PositiveInfinity;
            Node minNode = null;

            float minConstDist = float.PositiveInfinity;
            Node minConstNode = null;

            for (int i = 0; i < nodes.Length; i++) {

                Node node = nodes[i];
                float dist = (position - (Vector3)node.position).sqrMagnitude;

                if (dist < minDist) {
                    minDist = dist;
                    minNode = node;
                }

                if (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node)) {
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

            if (minConstNode != null) {
                D.Log("Closest Node is at {0}, {1} from {2}.", nnInfo.constClampedPosition, Vector3.Distance(nnInfo.constClampedPosition, position), position);
                D.Log("Constraints on Node: ConstrainDistance = {0}, DistanceConstraint = {1}.", constraint.constrainDistance, Mathf.Sqrt(maxDistSqr));
            }
            else {
                D.Log("Closest Node is at {0}, {1} from {2}. Penalty = {3}.", nnInfo.clampedPosition, Vector3.Distance(nnInfo.clampedPosition, position), position, minNode.penalty);
                D.Log("Constraints on Node: ConstrainDistance = {0}, DistanceConstraint = {1}.", constraint.constrainDistance, Mathf.Sqrt(maxDistSqr));
            }

            #endregion

            return nnInfo;
        }

        private Node __FindClosestNode(Vector3 position, out float closestDistance) {
            closestDistance = float.PositiveInfinity;
            Node closestNode = null;
            for (int i = 0; i < nodes.Length; i++) {

                Node node = nodes[i];
                float dist = (position - (Vector3)node.position).magnitude;

                if (dist < closestDistance) {
                    closestDistance = dist;
                    closestNode = node;
                }
            }
            return closestNode;
        }

        public override void Scan() {
            GraphWaypointsLookupByTag = ConstructGraphWaypoints();

            nodes = PopulateNodes();

            if (maxDistance >= 0) {
                //To avoid too many allocations, these lists are reused for each node
                List<Node> connections = new List<Node>(3);
                List<int> costs = new List<int>(3);

                //Loop through all nodes and add connections to other nodes
                int connectionCount = 0;
                int invalidConnectionCount = 0;
                for (int i = 0; i < nodes.Length; i++) {
                    connections.Clear();
                    costs.Clear();

                    Node node = nodes[i];
                    for (int j = 0; j < nodes.Length; j++) {
                        if (i == j) { continue; }
                        Node other = nodes[j];

                        float dist = 0;
                        if (IsValidConnection(node, other, out dist)) {
                            connections.Add(other);
                            costs.Add(Mathf.RoundToInt(dist * Int3.FloatPrecision));
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

            var universeCenter = Universe.Instance.Folder.GetComponentInChildren<UniverseCenterModel>();
            if (universeCenter != null) {
                var pointsAroundUniverseCenter = UnityUtility.CalcVerticesOfInscribedBoxInsideSphere(universeCenter.Position, universeCenter.OrbitDistance);
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

            var starbaseCreators = Universe.Instance.Folder.GetComponentsInChildren<StarbaseUnitCreator>();
            if (starbaseCreators.Any()) {
                var starbaseLocations = starbaseCreators.Select(sbc => sbc.transform.position);
                // can't create any points around a starbase at this time as the creators hasn't instantiated any Starbase prefabs yet
                openSpaceWaypoints = openSpaceWaypoints.Except(starbaseLocations, UnityUtility.Vector3EqualityComparer);
            }

            return new Dictionary<SpaceTopography, IList<Vector3>>() {
                { SpaceTopography.OpenSpace, openSpaceWaypoints.ToList() },
                { SpaceTopography.Nebula, new List<Vector3>() },        // TODO
                { SpaceTopography.DeepNebula, new List<Vector3>() },    // TODO
                { SpaceTopography.System, allWaypointsInsideSystems.ToList() }
                };
        }

        /// <summary>
        /// Creates and populates a nodes array with waypoint positions, walkability and penalty tags
        /// and returns it.
        /// </summary>
        /// <returns></returns>
        private Node[] PopulateNodes() {
            int waypointCount = GraphWaypointsLookupByTag.Values.Sum(list => list.Count);
            Node[] populatedNodes = CreateNodes(waypointCount); // assigns initial penalty of 0 to each node
            D.Log("{0} pathfinding nodes will be created.", waypointCount);
            D.Assert(waypointCount == populatedNodes.Length);

            // initialize nodes that will be tagged OpenSpace
            var waypoints = GraphWaypointsLookupByTag[SpaceTopography.OpenSpace];
            D.Log("Creating {0} pathfinding nodes with tag {1}.", waypoints.Count, SpaceTopography.OpenSpace.GetName());
            int tagMask = openSpaceTagMask;
            //D.Log("{0} tag mask = {1}.", PathfindingTags.OpenSpace.GetName(), StringExtensions.GetBinaryString(tagMask));
            for (int i = 0; i < waypoints.Count; i++) {
                populatedNodes[i].position = (Int3)waypoints[i];
                populatedNodes[i].walkable = true;
                populatedNodes[i].tags = tagMask;
            }
            int nextNodeIndex = waypoints.Count;

            // initialize nodes that will be tagged System
            waypoints = GraphWaypointsLookupByTag[SpaceTopography.System];
            D.Log("Creating {0} pathfinding nodes with tag {1}.", waypoints.Count, SpaceTopography.System.GetName());
            tagMask = systemTagMask;
            //D.Log("{0} tag mask = {1}.", PathfindingTags.System.GetName(), StringExtensions.GetBinaryString(tagMask));
            for (int i = nextNodeIndex; i < nextNodeIndex + waypoints.Count; i++) {
                populatedNodes[i].position = (Int3)waypoints[i - nextNodeIndex];
                populatedNodes[i].walkable = true;
                populatedNodes[i].tags = tagMask;
            }
            nextNodeIndex += waypoints.Count;
            D.Assert(waypointCount == nextNodeIndex);
            // TODO initialize nodes that will be tagged Nebula and DeepNebula
            return populatedNodes;
        }

        /// <summary>
        ///  This will be called on the same time as OnDisable on the gameObject which the AstarPath script is attached to (remember, not in the editor)
        /// Use for any cleanup code such as cleaning up static variables which otherwise might prevent resources from being collected
        /// Use by creating a function overriding this one in a graph class, but always call base.OnDestroy () in that function.
        /// </summary>
        //public override void OnDestroy() {
        //    base.OnDestroy();
        //}

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

