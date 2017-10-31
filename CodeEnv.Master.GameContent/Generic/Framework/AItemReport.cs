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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Reports that support Items with no PlayerIntel.
    /// </summary>
    public abstract class AItemReport {

        private const string DebugNameFormat = "{0}.{1}";

        public virtual string DebugName { get { return DebugNameFormat.Inject(Name, GetType().Name); } }

        /// <summary>
        /// Debug. The position of the Item for reporting the camera distance.
        /// </summary>
        public Reference<Vector3> __PositionForCameraDistance { get; private set; }

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
        public IOwnerItem_Ltd Item { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AItemReport" /> class.
        /// </summary>
        /// <param name="data">The Item's data.</param>
        /// <param name="player">The player requesting the report.</param>
        public AItemReport(AItemData data, Player player) {
            Player = player;
            Item = data.Item as IOwnerItem_Ltd;
            __PositionForCameraDistance = new Reference<Vector3>(() => Item.Position);
            // 10.12.17 Moved AssignValues(data) to AIntelItemReport constructor to make sure IntelCoverage is initialized
        }

        protected abstract void AssignValues(AItemData data);

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

