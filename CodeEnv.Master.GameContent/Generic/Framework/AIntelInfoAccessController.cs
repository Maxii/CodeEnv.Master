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

        public override bool HasAccessToInfo(Player player, AccessControlInfoID infoID) {
            D.Assert(player != TempGameValues.NoPlayer, "{0}: NoPlayer used to attempt access to {1}.{2}.", _data.FullName, typeof(AccessControlInfoID).Name, infoID.GetValueName());
            D.Assert(infoID != AccessControlInfoID.None);

            var coverage = (_data as AIntelItemData).GetIntelCoverage(player);
            return HasAccessToInfo(coverage, infoID);
        }

        private bool HasAccessToInfo(IntelCoverage coverage, AccessControlInfoID infoID) {
            switch (coverage) {
                case IntelCoverage.Comprehensive:
                    if (HasAccessToInfo_Comprehensive(infoID)) {
                        return true;
                    }
                    goto case IntelCoverage.Broad;
                case IntelCoverage.Broad:
                    if (HasAccessToInfo_Broad(infoID)) {
                        return true;
                    }
                    goto case IntelCoverage.Essential;
                case IntelCoverage.Essential:
                    if (HasAccessToInfo_Essential(infoID)) {
                        return true;
                    }
                    goto case IntelCoverage.Basic;
                case IntelCoverage.Basic:
                    if (HasAccessToInfo_Basic(infoID)) {
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
        /// <returns></returns>
        protected abstract bool HasAccessToInfo_Comprehensive(AccessControlInfoID infoID);

        /// <summary>
        /// Returns <c>true</c> if <c>Player</c> with IntelCoverage.Broad should have access to the info identified by infoID, 
        /// <c>false</c> otherwise. If false, the next lower IntelCoverage level will be tested until true is returned or Basic has been evaluated.
        /// </summary>
        /// <param name="infoID">The information identifier.</param>
        /// <returns></returns>
        protected abstract bool HasAccessToInfo_Broad(AccessControlInfoID infoID);

        /// <summary>
        /// Returns <c>true</c> if <c>Player</c> with IntelCoverage.Essential should have access to the info identified by infoID, 
        /// <c>false</c> otherwise. If false, the next lower IntelCoverage level will be tested until true is returned or Basic has been evaluated.
        /// </summary>
        /// <param name="infoID">The information identifier.</param>
        /// <returns></returns>
        protected abstract bool HasAccessToInfo_Essential(AccessControlInfoID infoID);

        /// <summary>
        /// Returns <c>true</c> if <c>Player</c> with IntelCoverage.Basic should have access to the info identified by infoID, 
        /// <c>false</c> otherwise.
        /// </summary>
        /// <param name="infoID">The information identifier.</param>
        /// <returns></returns>
        protected abstract bool HasAccessToInfo_Basic(AccessControlInfoID infoID);


    }
}

