// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FocusableItemCameraStat.cs
// Camera stat for ICameraFocusable Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Camera stat for ICameraFocusable Items.
    /// </summary>
    public class FocusableItemCameraStat : AItemCameraStat {

        public float OptimalViewingDistance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FocusableItemCameraStat" />.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="optViewDistance">The opt view distance.</param>
        /// <param name="fov">The field of view.</param>
        public FocusableItemCameraStat(float minViewDistance, float optViewDistance, float fov)
            : base(minViewDistance, fov) {
            D.Assert(optViewDistance >= MinimumViewingDistance);
            OptimalViewingDistance = optViewDistance;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

