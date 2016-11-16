// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShield.cs
// Interface for access to a Shield[Monitor].
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for access to a Shield[Monitor].
    /// </summary>
    public interface IShield : IDebugable {

        /// <summary>
        /// The owner of this Shield.
        /// <remarks>No reason to hinder access to Owner info as this interface value
        /// is only used by the equipment associated with the monitor to access range and 
        /// reload period multipliers that need to adjust when the Monitor's Item owner changes.</remarks>
        /// </summary>
        Player Owner { get; }

        void InitializeRangeDistance();


    }
}

