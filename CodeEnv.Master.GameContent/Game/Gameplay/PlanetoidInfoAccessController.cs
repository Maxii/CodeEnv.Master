// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidInfoAccessController.cs
// Controls access to Planetoid info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Planetoid info.
    /// <remarks>Handles access based on Item IntelCoverage for the player. Access based on whether a technology has been discovered
    /// is handled by PlayerAIMgr. Separated this way (vs all incorporated here) to allow Reports to withhold awareness of the
    /// existence of a Resource that hasn't been researched yet. If all filtering was incorporated here, the report </remarks>
    /// </summary>
    public class PlanetoidInfoAccessController : AIntelInfoAccessController {

        public PlanetoidInfoAccessController(PlanetoidData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.CurrentHitPoints:
                case ItemInfoID.Health:
                    // 10.13.17 Removed Resources as Report now handles Resources without AccessCntlr
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.MaxHitPoints:
                case ItemInfoID.Defense:
                case ItemInfoID.Mass:
                case ItemInfoID.Capacity:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Category:
                case ItemInfoID.Owner:
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
                case ItemInfoID.OrbitalSpeed:
                    return true;
                default:
                    return false;
            }
        }

    }
}

