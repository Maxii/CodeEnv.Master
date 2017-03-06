// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApStrafeDestinationProxy.cs
// Proxy used by a Ship's AutoPilot to navigate to and strafe an IShipAttackable target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Proxy used by a Ship's AutoPilot to navigate to and strafe an IShipAttackable target.
    /// </summary>
    public class ApStrafeDestinationProxy : ApMoveDestinationProxy {

        public new IShipAttackable Destination { get { return base.Destination as IShipAttackable; } }

        public ApStrafeDestinationProxy(IShipNavigable destination, IShip ship, float innerRadius, float outerRadius)
            : base(destination, ship, innerRadius, outerRadius) {
            RefreshStrafePosition();
        }

        public void RefreshStrafePosition() {
            Vector3 destToShipVector = _ship.Position - Position;
            Vector3 planeNormal = destToShipVector.normalized;
            Vector3 shipFacing = _ship.CurrentHeading;
            Vector3 ptAlongShipFacing = _ship.Position + shipFacing;
            Vector3 closestPtOnPlaneToPtAlongShipFacing = Math3D.ProjectPointOnPlane(planeNormal, Position, ptAlongShipFacing);
            Vector3 destToStrafeTgtDirection = (closestPtOnPlaneToPtAlongShipFacing - Position).normalized;
            float destToStrafeTgtDistance = (Destination as IMortalItem).Radius;
            Position = Destination.Position + destToStrafeTgtDirection * destToStrafeTgtDistance;
        }

        public ApMoveDestinationProxy GenerateBeginRunWaypoint() {
            var directionToShipFromTgt = (_ship.Position - Position).normalized;
            var waypointVector = _ship.CurrentHeading + directionToShipFromTgt;   // HACK
            Vector3 waypointDirectionFromTgt = waypointVector != Vector3.zero ? waypointVector.normalized : directionToShipFromTgt;
            float waypointDistanceFromTgt = OuterRadius * 2F;
            var waypointPosition = Position + waypointDirectionFromTgt * waypointDistanceFromTgt;
            var waypoint = new StationaryLocation(waypointPosition);
            return waypoint.GetApMoveTgtProxy(Vector3.zero, Constants.ZeroF, _ship);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

