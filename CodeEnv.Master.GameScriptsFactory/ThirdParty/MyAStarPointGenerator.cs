// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyAStarPointGenerator.cs
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
    public class MyAStarPointGenerator : PointGraph {

        public IList<Vector3> GraphLocations { get; private set; }

        public IList<Vector3> ObstacleLocations { get; private set; }

        private UniverseCenterView _universeCenterView;

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

            if (nnInfo.constrainedNode == null) {
                float closestDistance;
                Node closestNode = __FindClosestNode(position, out closestDistance);
                D.Warn("Can't find node close enough to {0}. ClosestNode is {1} away.", position, closestDistance);
            }
            else {
                D.Log("Closest Node is at {0}, {1} from {2}.", (Vector3)nnInfo.constrainedNode.position, Mathf.Sqrt(minDist), position);
            }

            //D.Log("GetNearest() constraint.Suitable = {0}.", constraint.Suitable(nnInfo.node));
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
            ObstacleLocations = FindObstacleLocations();
            GraphLocations = InitializeGraphLocations();

            nodes = CreateNodes(GraphLocations.Count);
            for (int i = 0; i < nodes.Length; i++) {
                nodes[i].position = (Int3)GraphLocations[i];
                nodes[i].walkable = true;
            }
            D.Log("{0} pathfinding nodes created.", nodes.Length);

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
                        else if (dist <= maxDistance) {
                            invalidConnectionCount++;
                            //D.Log("Connection {0} to {1} invalid.", node.position * Int3.PrecisionFactor, other.position * Int3.PrecisionFactor);
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

        private IList<Vector3> InitializeGraphLocations() {
            SectorGrid sectorGrid = SectorGrid.Instance;
            var corners = sectorGrid.SectorCorners;
            if (corners == null) {
                // AstarPath has an option to automatically call Scan() on Awake which can be too early
                D.Warn("SectorGrid not yet initialized.");
                return new List<Vector3>();
            }
            var sectorCenters = sectorGrid.SectorCenters;
            IEnumerable<Vector3> graphLocations = corners.Union(sectorCenters);

            var vector3EqualityComparer = new Vector3EqualityComparer();
            graphLocations = graphLocations.Except(ObstacleLocations, vector3EqualityComparer);

            float percentDistanceAroundObstacles = TempGameValues.PathGraphPointPercentDistanceAroundObstacles;  // 0.1F
            float percentDistanceAroundSectorCenters = 0.5F;

            IEnumerable<Vector3> pointsAroundObstacles = Enumerable.Empty<Vector3>();
            foreach (var obstacleLoc in ObstacleLocations) {
                if (sectorCenters.Contains<Vector3>(obstacleLoc, vector3EqualityComparer)) {
                    pointsAroundObstacles = pointsAroundObstacles.Union(sectorGrid.CalcBoxVerticesAroundCenter(obstacleLoc, percentDistanceAroundObstacles));
                }
                else if (_universeCenterView != null && _universeCenterView.transform.position.IsSame(obstacleLoc)) {
                    var radius = _universeCenterView.Radius;    // allows UniverseCenter to be quite large
                    var absoluteDistance = radius + percentDistanceAroundObstacles * ((0.5F * TempGameValues.SectorSideLength) - radius);
                    pointsAroundObstacles = pointsAroundObstacles.Union(UnityUtility.CalcBoxVerticesAroundPoint(obstacleLoc, absoluteDistance));
                }
                else {
                    D.Warn("Obstacle at {0} not anticipated.", obstacleLoc);
                }
            }
            graphLocations = graphLocations.Union(pointsAroundObstacles);

            IEnumerable<Vector3> interiorSectorPoints = Enumerable.Empty<Vector3>();
            foreach (var sectorCenter in sectorCenters) {
                interiorSectorPoints = interiorSectorPoints.Union(sectorGrid.CalcBoxVerticesAroundCenter(sectorCenter, percentDistanceAroundSectorCenters));
            }
            graphLocations = graphLocations.Union(interiorSectorPoints);

            return graphLocations.ToList();
        }

        private IList<Vector3> FindObstacleLocations() {
            _universeCenterView = Universe.Instance.Folder.gameObject.GetSafeMonoBehaviourComponentInChildren<UniverseCenterView>();
            var obstacleLocations = Universe.Instance.Folder.gameObject.GetSafeMonoBehaviourComponentsInChildren<SystemCreator>()
                .Select(sm => sm.transform.position).ToList();
            var starbaseObstacleLocations = Universe.Instance.Folder.gameObject.GetSafeMonoBehaviourComponentsInChildren<StarbaseUnitCreator>()
                .Select(sbc => sbc.transform.position);
            if (!starbaseObstacleLocations.IsNullOrEmpty()) {
                obstacleLocations = obstacleLocations.Concat(starbaseObstacleLocations).ToList();
            }

            if (_universeCenterView != null) {   // allows me to deactivate UniverseCenter
                obstacleLocations.Add(_universeCenterView.transform.position);
            }
            D.Log("{0} obstacle locations found.", obstacleLocations.Count);
            return obstacleLocations;
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

