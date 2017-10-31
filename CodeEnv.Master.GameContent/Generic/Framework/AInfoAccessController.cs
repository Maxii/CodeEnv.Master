// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AInfoAccessController.cs
// Abstract base class that controls other player's access to Item info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using Common.LocalResources;

    /// <summary>
    /// Abstract base class that controls other player's access to Item info.
    /// </summary>
    public abstract class AInfoAccessController {

        private const string DebugNameFormat = "{0}.{1}";

        public string DebugName { get { return DebugNameFormat.Inject(_data.DebugName, GetType().Name); } }

        protected bool ShowDebugLog { get { return _data.ShowDebugLog; } }

        protected AItemData _data;
        private IGameManager _gameMgr;

        public AInfoAccessController(AItemData data) {
            _data = data;
            _gameMgr = GameReferences.GameManager;
        }

        /// <summary>
        /// Returns <c>true</c> if the provided Player has the proper IntelCoverage required to access 
        /// the info indicated by ItemInfoID for this Item.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="infoID">The information identifier.</param>
        /// <returns></returns>
        public abstract bool HasIntelCoverageReqdToAccess(Player player, ItemInfoID infoID);

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

