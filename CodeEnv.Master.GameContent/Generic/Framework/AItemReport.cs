// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemReport.cs
// Abstract class for Reports that support Items with no PlayerIntel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Abstract class for Reports that support Items with no PlayerIntel.
    /// </summary>
    public abstract class AItemReport {

        public string Name { get; protected set; }

        /// <summary>
        /// The owner of the item this report is about. Can be null if the player
        /// that requested the report (Player) does not have sufficient IntelCoverage
        /// on the item to know the owner.
        /// </summary>
        public Player Owner { get; protected set; }

        public Vector3? Position { get; protected set; }

        /// <summary>
        /// The player that requested the report.
        /// </summary>
        public Player Player { get; private set; }

        /// <summary>
        /// The item the report is about.
        /// </summary>
        public IItem Item { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AItemReport" /> class.
        /// </summary>
        /// <param name="player">The player requesting the report.</param>
        /// <param name="item">The item the report is about.</param>
        public AItemReport(Player player, IItem item) {
            Player = player;
            Item = item;
        }

        /// <summary>
        /// Assigns the values to this report. Some values may be acquired from
        /// data, others from other Constructor parameters provided by the derived
        /// class. The derived class is responsible for calling this method once all
        /// Constructor parameters needed by it have been recorded.
        /// </summary>
        /// <param name="data">The data.</param>
        protected abstract void AssignValues(AItemData data);
        // can't call from this Constructor as derived class parameters not yet recorded
    }
}

