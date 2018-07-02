// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IIntelItem.cs
// Interface for easy access to MonoBehaviours that are AIntelItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for easy access to IItems that support IntelCoverage (including Sector).
    /// </summary>
    public interface IIntelItem : IOwnerItem {

        IntelCoverage UserIntelCoverage { get; }

        IntelCoverage GetIntelCoverage(Player player);

        /// <summary>
        /// Sets the Intel coverage for this player. 
        /// <remarks>Convenience method for clients who don't care whether the value was accepted or not.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="newCoverage">The new coverage.</param>
        void SetIntelCoverage(Player player, IntelCoverage newCoverage);

        bool __IsPlayerEntitledToComprehensiveRelationship(Player player);

    }
}

