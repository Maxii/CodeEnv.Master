// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TempGameValues.cs
// Static class of common constants specific to the Unity Engine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    public static class TempGameValues {

        public const int MinFleetTrackingLabelShowDistance = 50;
        public const int MaxFleetTrackingLabelShowDistance = 300;

        public const int MinSystemTrackingLabelShowDistance = 100;
        public const int MaxSystemTrackingLabelShowDistance = 2500;

        /// <summary>
        /// The length in world units of a sector side along any of the axis. As a sector
        /// is a cube, all side lengths are equal.
        /// </summary>
        public const float SectorSideLength = 1200F;

        public static readonly Vector3 SectorSize = new Vector3(SectorSideLength, SectorSideLength, SectorSideLength);

    }
}


