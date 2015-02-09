// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipItem.cs
// Interface for all items that are ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    ///  Interface for all items that are ships.
    /// </summary>
    public interface IShipItem : IElementItem {

        /// <summary>
        /// Parents the ship to the provided orbiter's transform.
        /// The ship retains existing position, rotation, scale and layer.
        /// </summary>
        /// <param name="orbiter">The orbiter.</param>
        void AttachTo(IOrbiterForShips orbiter);

        /// <summary>
        /// Reattaches the ship's transform to the fleet container it came from.
        /// </summary>
        void ReattachToParentFleetContainer();

        void OnTopographicBoundaryTransition(Topography newTopography);

    }
}

