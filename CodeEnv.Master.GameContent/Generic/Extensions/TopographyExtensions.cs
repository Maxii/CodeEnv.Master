// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TopographyExtensions.cs
// Extensions for Topography.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Extensions for Topography.
    /// </summary>
    public static class TopographyExtensions {

        /// <summary>
        /// Gets the density of matter in this <c>topography</c> relative to Topography.OpenSpace.
        /// The density of a <c>Topography</c> affects the drag of ships and projectiles moving through it.
        /// </summary>
        /// <param name="topography">The topography.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetRelativeDensity(this Topography topography) {
            switch (topography) {
                case Topography.OpenSpace:
                    return 1F;
                case Topography.System:
                case Topography.Nebula:
                    return 5F;
                case Topography.DeepNebula: // TODO
                case Topography.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(topography));
            }

        }

    }
}

