// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AReport.cs
// Abstract base class for Reports associated with an AItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class for Reports associated with an AItem.
    /// </summary>
    [System.Obsolete]
    public abstract class AReport {

        public string Name { get; protected set; }

        /// <summary>
        /// The owner of the item this report is about. Can be null if the player
        /// that requested the report (Player) does not have sufficient IntelCoverage
        /// on the item to know the owner.
        /// </summary>
        public Player Owner { get; protected set; }

        /// <summary>
        /// The player that requested the report.
        /// </summary>
        public Player Player { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AReport"/> class.
        /// </summary>
        /// <param name="player">The player requesting the report.</param>
        public AReport(Player player) {
            Player = player;
        }

    }
}

