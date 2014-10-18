// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PathfindingLine.cs
// Generates a continuous line along the AStarPathfinding path. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Generates a continuous line along the AStarPathfinding path. 
    /// </summary>
    public class PathfindingLine : A3DVectrosityBase {

        public Reference<Vector3> Destination { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathfindingLine"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pathPoints">All the points along the path.</param>
        /// <param name="destination">The potentially moving destination reference.</param>
        public PathfindingLine(string name, Vector3[] pathPoints, Reference<Vector3> destination)
            : base(name, pathPoints, null, References.DynamicObjectsFolder.Folder, LineType.Continuous, 1F, GameColor.Gray) {
            Destination = destination;
        }

        protected override void Draw3D() {
            int destinationIndex = _line.points3.Length - 1;
            _line.points3[destinationIndex] = Destination.Value;
            base.Draw3D();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

