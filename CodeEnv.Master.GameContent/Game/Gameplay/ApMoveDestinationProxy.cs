// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApMoveDestinationProxy.cs
// Proxy used by a Ship's AutoPilot to navigate to an IShipNavigable destination.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Proxy used by a Ship's AutoPilot to navigate to an IShipNavigable destination.
    /// </summary>
    public class ApMoveDestinationProxy : AApDestinationProxy {

        public Vector3 __DestinationOffset { get; private set; }

        public float ObstacleCheckRayLength {
            get {
                // Note: Outer boundary of avoidableObstacleZone, if present, will usually be InnerRadius of Proxy
                // Warning: If InnerRadius is used here, the obstacleZone around the destination could be detected while 
                // within the 'HasArrived' window of the destination, thereby generating a detour just before "HasArrived".
                float rayLength = Vector3.Distance(Position, _ship.Position) - OuterRadius;
                if (rayLength < Constants.ZeroF) {
                    // 1.24.17 Physics.Raycast() using rayLength <= Zero results in false, aka no ray cast attempted
                    return Constants.ZeroF;
                }
                return rayLength;
            }
        }

        public ApMoveDestinationProxy(IShipNavigable dest, IShip ship, float innerRadius, float outerRadius)
            : this(dest, ship, Vector3.zero, innerRadius, outerRadius) {
        }


        public ApMoveDestinationProxy(IShipNavigable dest, IShip ship, Vector3 destOffset, float innerRadius, float outerRadius)
            : base(dest, ship, innerRadius, outerRadius) {
            __DestinationOffset = destOffset;
            Position = Destination.Position + destOffset;
        }

        public void ResetOffset() {
            __DestinationOffset = Vector3.zero;
            Position = Destination.Position;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

