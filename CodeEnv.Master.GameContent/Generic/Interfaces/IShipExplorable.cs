// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipExplorable.cs
// Interface for Items that individual ships can be ordered to explore.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Items that individual ships can be ordered to explore.
    /// Includes UCenter, Stars and Planets.
    /// </summary>
    public interface IShipExplorable : IExplorable, IShipCloseOrbitable {

        /// <summary>
        /// Tells the item the player has fully explored it.
        /// </summary>
        /// <param name="player">The player.</param>
        void RecordExplorationCompletedBy(Player player);

    }
}

