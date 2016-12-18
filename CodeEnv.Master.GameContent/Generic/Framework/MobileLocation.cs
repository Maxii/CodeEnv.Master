// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MobileLocation.cs
// An INavigableTarget wrapping a mobile location in world space.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An INavigable target wrapping a mobile location in world space.
    /// </summary>
    public class MobileLocation : IFleetNavigable, IShipNavigable {

        private Reference<Vector3> _movingPosition;

        public MobileLocation(Reference<Vector3> movingPosition) {
            _movingPosition = movingPosition;
        }

        public override string ToString() {
            return DebugName;
        }

        #region INavigable Members

        public string Name { get { return DebugName; } }

        private string _debugName;
        public string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = "{0}[{1}]".Inject(GetType().Name, Position);
                }
                return _debugName;
            }
        }

        public Vector3 Position { get { return _movingPosition.Value; } }

        public bool IsMobile { get { return true; } }

        #endregion

        #region IShipNavigable Members

        public AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
            return new AutoPilotDestinationProxy(this, tgtOffset, Constants.ZeroF, TempGameValues.WaypointCloseEnoughDistance);
        }

        #endregion

        #region IFleetNavigable Members

        public Topography Topography { get { return References.GameManager.GameKnowledge.GetSpaceTopography(Position); } }

        public float GetObstacleCheckRayLength(Vector3 fleetPosition) {
            return Vector3.Distance(fleetPosition, Position);
        }


        #endregion

    }
}

