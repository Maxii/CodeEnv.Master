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
        /// Gets the drag associated with this topography.
        /// </summary>
        /// <param name="topography">The topography.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetDrag(this Topography topography) {
            switch (topography) {
                case Topography.OpenSpace:
                    return TempGameValues.InterstellerDrag; // TODO .001F
                case Topography.System:
                case Topography.Nebula:
                    return TempGameValues.SystemDrag;  // TODO .01F
                case Topography.DeepNebula:
                    return 1F;  // TODO .1F?
                case Topography.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(topography));
            }

        }

    }
}

