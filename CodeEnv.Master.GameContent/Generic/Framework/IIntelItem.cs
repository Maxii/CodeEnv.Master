// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IIntelItem.cs
// Interface for easy access to all items that carry knowledge of a player's IntelCoverage on the item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for easy access to all items that carry knowledge of a player's IntelCoverage on the item.
    /// </summary>
    public interface IIntelItem : IDiscernibleItem {

        IntelCoverage GetIntelCoverage(Player player);

        bool SetIntelCoverage(Player player, IntelCoverage coverage);

    }
}

