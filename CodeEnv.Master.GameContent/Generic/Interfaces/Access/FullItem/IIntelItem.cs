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
        /// Sets the intel coverage for this player. Returns <c>true</c> if the <c>newCoverage</c>
        /// was successfully applied, and <c>false</c> if it was rejected due to the inability of
        /// the item to regress its IntelCoverage.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="newCoverage">The new coverage.</param>
        /// <returns></returns>
        bool SetIntelCoverage(Player player, IntelCoverage newCoverage);

    }
}

