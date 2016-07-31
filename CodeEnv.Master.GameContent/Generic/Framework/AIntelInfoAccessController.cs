// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntelInfoAccessController.cs
// Abstract base class that controls access to Item info determined by IntelCoverage.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Abstract base class that controls access to Item info determined by IntelCoverage.
    /// </summary>
    public abstract class AIntelInfoAccessController : AInfoAccessController {

        public AIntelInfoAccessController(AIntelItemData data) : base(data) { }

        public sealed override bool HasAccessToInfo(Player player, ItemInfoID infoID) {
            D.Assert(player != TempGameValues.NoPlayer, "{0}: NoPlayer used to attempt access to {1}.{2}.", _data.FullName, typeof(ItemInfoID).Name, infoID.GetValueName());
            D.Assert(infoID != ItemInfoID.None);

            var coverage = (_data as AIntelItemData).GetIntelCoverage(player);
            return HasAccessToInfo(coverage, infoID, player);
        }

        private bool HasAccessToInfo(IntelCoverage coverage, ItemInfoID infoID, Player player) {
            switch (coverage) {
                case IntelCoverage.Comprehensive:
                    if (HasAccessToInfo_Comprehensive(infoID, player)) {
                        return true;
                    }
                    goto case IntelCoverage.Broad;
                case IntelCoverage.Broad:
                    if (HasAccessToInfo_Broad(infoID, player)) {
                        return true;
                    }
                    goto case IntelCoverage.Essential;
                case IntelCoverage.Essential:
                    if (HasAccessToInfo_Essential(infoID, player)) {
                        return true;
                    }
                    goto case IntelCoverage.Basic;
                case IntelCoverage.Basic:
                    if (HasAccessToInfo_Basic(infoID, player)) {
                        return true;
                    }
                    goto case IntelCoverage.None;
                case IntelCoverage.None:
                    return false;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(coverage));
            }
        }

        /// <summary>
        /// Returns <c>true</c> if <c>Player</c> with IntelCoverage.Comprehensive should have access to the info identified by infoID,
        /// <c>false</c> otherwise. If false, the next lower IntelCoverage level will be tested until true is returned or Basic has been evaluated.
        /// </summary>
        /// <param name="infoID">The information identifier.</param>
        /// <param name="player">The player requesting the access. 
        /// Only used when this InfoAccessCntlr needs to consult with another to make a decision.</param>
        /// <returns></returns>
        protected abstract bool HasAccessToInfo_Comprehensive(ItemInfoID infoID, Player player);

        /// <summary>
        /// Returns <c>true</c> if <c>Player</c> with IntelCoverage.Broad should have access to the info identified by infoID,
        /// <c>false</c> otherwise. If false, the next lower IntelCoverage level will be tested until true is returned or Basic has been evaluated.
        /// </summary>
        /// <param name="infoID">The information identifier.</param>
        /// <param name="player">The player requesting the access. 
        /// Only used when this InfoAccessCntlr needs to consult with another to make a decision.</param>
        /// <returns></returns>
        protected abstract bool HasAccessToInfo_Broad(ItemInfoID infoID, Player player);

        /// <summary>
        /// Returns <c>true</c> if <c>Player</c> with IntelCoverage.Essential should have access to the info identified by infoID,
        /// <c>false</c> otherwise. If false, the next lower IntelCoverage level will be tested until true is returned or Basic has been evaluated.
        /// </summary>
        /// <param name="infoID">The information identifier.</param>
        /// <param name="player">The player requesting the access. 
        /// Only used when this InfoAccessCntlr needs to consult with another to make a decision.</param>
        /// <returns></returns>
        protected abstract bool HasAccessToInfo_Essential(ItemInfoID infoID, Player player);

        /// <summary>
        /// Returns <c>true</c> if <c>Player</c> with IntelCoverage.Basic should have access to the info identified by infoID,
        /// <c>false</c> otherwise.
        /// </summary>
        /// <param name="infoID">The information identifier.</param>
        /// <param name="player">The player requesting the access. 
        /// Only used when this InfoAccessCntlr needs to consult with another to make a decision.</param>
        /// <returns></returns>
        protected abstract bool HasAccessToInfo_Basic(ItemInfoID infoID, Player player);


    }
}

