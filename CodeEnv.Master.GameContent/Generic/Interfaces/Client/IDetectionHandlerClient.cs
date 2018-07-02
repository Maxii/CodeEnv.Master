// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDetectionHandlerClient.cs
// Interface for clients of DetectionHandlers.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for clients of DetectionHandlers.
    /// </summary>
    public interface IDetectionHandlerClient : IDebugable {

        Player Owner { get; }

        IntelCoverage GetIntelCoverage(Player player);

        /// <summary>
        /// Sets the Intel coverage for this player. 
        /// <remarks>Convenience method for clients who don't care whether the value was accepted or not.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="newCoverage">The new coverage.</param>
        void SetIntelCoverage(Player player, IntelCoverage newCoverage);

        /// <summary>
        /// Attempts to change the IntelCoverage of this Item to newCoverage. Returns <c>true</c> if the coverage
        /// was changed, false if not. In both cases, resultingCoverage will reflect the currentCoverage, changed or not.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="newCoverage">The new coverage.</param>
        /// <param name="resultingCoverage">The resulting coverage.</param>
        /// <returns></returns>
        bool TryChangeIntelCoverage(Player player, IntelCoverage newCoverage, out IntelCoverage resultingCoverage);
    }
}

