// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarInfoAccessController.cs
// Controls access to Star info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Star info.
    /// </summary>
    public class StarInfoAccessController : AIntelInfoAccessController {

        public StarInfoAccessController(StarData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Resources:
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Capacity:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Owner:
                case ItemInfoID.Category:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Basic(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Name:
                case ItemInfoID.Position:
                case ItemInfoID.SectorID:
                    return true;
                default:
                    return false;
            }
        }

    }
}

