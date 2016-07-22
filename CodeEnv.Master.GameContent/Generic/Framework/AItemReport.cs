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

        /// <summary>
        /// Debug. The position of the Item for reporting the camera distance.
        /// </summary>
        public Vector3 __PositionForCameraDistance { get; private set; }

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
        public IItem_Ltd Item { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AItemReport" /> class.
        /// </summary>
        /// <param name="player">The player requesting the report.</param>
        /// <param name="item">The item the report is about.</param>
        public AItemReport(AItemData data, Player player, IItem_Ltd item) {
            Player = player;
            Item = item;
            __PositionForCameraDistance = item.Position;
            AssignValues(data);
        }

        protected abstract void AssignValues(AItemData data);

    }
}

