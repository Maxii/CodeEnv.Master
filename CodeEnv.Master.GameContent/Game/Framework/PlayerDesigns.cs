// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerDesigns.cs
// Holds a collection of UnitDesigns organized by the owner of the design and the design's name.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Holds a collection of UnitDesigns organized by the owner of the design and the design's name.
    /// </summary>
    public class PlayerDesigns {

        public string DebugName { get { return GetType().Name; } }

        private IDictionary<Player, IDictionary<string, StarbaseCmdDesign>> _starbaseCmdDesignLookupByName;
        private IDictionary<Player, IDictionary<string, FleetCmdDesign>> _fleetCmdDesignLookupByName;
        private IDictionary<Player, IDictionary<string, SettlementCmdDesign>> _settlementCmdDesignLookupByName;

        private IDictionary<Player, IDictionary<string, ShipDesign>> _shipDesignLookupByName;
        private IDictionary<Player, IDictionary<string, FacilityDesign>> _facilityDesignLookupByName;

        private IDictionary<Player, IDictionary<ShipHullCategory, IList<ShipDesign>>> _shipDesignsLookupByHull;
        private IDictionary<Player, IDictionary<FacilityHullCategory, IList<FacilityDesign>>> _facilityDesignsLookupByHull;

        private IDictionary<Player, HashSet<string>> _designNamesInUseLookup;

        private Player _userPlayer;

        public PlayerDesigns(IEnumerable<Player> allPlayers) {
            InitializeValuesAndReferences(allPlayers);
        }

        private void InitializeValuesAndReferences(IEnumerable<Player> allPlayers) {
            int playerCount = allPlayers.Count();
            _shipDesignLookupByName = new Dictionary<Player, IDictionary<string, ShipDesign>>(playerCount);
            _facilityDesignLookupByName = new Dictionary<Player, IDictionary<string, FacilityDesign>>(playerCount);
            _starbaseCmdDesignLookupByName = new Dictionary<Player, IDictionary<string, StarbaseCmdDesign>>(playerCount);
            _fleetCmdDesignLookupByName = new Dictionary<Player, IDictionary<string, FleetCmdDesign>>(playerCount);
            _settlementCmdDesignLookupByName = new Dictionary<Player, IDictionary<string, SettlementCmdDesign>>(playerCount);

            _shipDesignsLookupByHull = new Dictionary<Player, IDictionary<ShipHullCategory, IList<ShipDesign>>>(playerCount);
            _facilityDesignsLookupByHull = new Dictionary<Player, IDictionary<FacilityHullCategory, IList<FacilityDesign>>>(playerCount);

            _designNamesInUseLookup = new Dictionary<Player, HashSet<string>>(playerCount);

            allPlayers.ForAll(p => {
                _shipDesignLookupByName.Add(p, new Dictionary<string, ShipDesign>());
                _facilityDesignLookupByName.Add(p, new Dictionary<string, FacilityDesign>());
                _starbaseCmdDesignLookupByName.Add(p, new Dictionary<string, StarbaseCmdDesign>());
                _fleetCmdDesignLookupByName.Add(p, new Dictionary<string, FleetCmdDesign>());
                _settlementCmdDesignLookupByName.Add(p, new Dictionary<string, SettlementCmdDesign>());

                _shipDesignsLookupByHull.Add(p, new Dictionary<ShipHullCategory, IList<ShipDesign>>());
                var shipHullCats = Enums<ShipHullCategory>.GetValues(excludeDefault: true);
                foreach (var hull in shipHullCats) {
                    _shipDesignsLookupByHull[p].Add(hull, new List<ShipDesign>());
                }
                _facilityDesignsLookupByHull.Add(p, new Dictionary<FacilityHullCategory, IList<FacilityDesign>>());
                var facHullCats = Enums<FacilityHullCategory>.GetValues(excludeDefault: true);
                foreach (var hull in facHullCats) {
                    _facilityDesignsLookupByHull[p].Add(hull, new List<FacilityDesign>());
                }

                _designNamesInUseLookup.Add(p, new HashSet<string>());

                if (p.IsUser) {
                    _userPlayer = p;
                }
            });
        }

        #region Add Design

        public void Add(ShipDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup[player].Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add {1}'s DesignName {2} as it is already present.", DebugName, player.DebugName, designName);
                return;
            }
            var designsByName = _shipDesignLookupByName[player];
            designsByName.Add(designName, design);

            _shipDesignsLookupByHull[player][design.HullCategory].Add(design);
            //D.Log("{0} added {1} for {2}.", DebugName, design.DebugName, player.DebugName);
        }

        public void Add(FacilityDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup[player].Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add {1}'s DesignName {2} as it is already present.", DebugName, player.DebugName, designName);
                return;
            }
            var designsByName = _facilityDesignLookupByName[player];
            designsByName.Add(designName, design);

            _facilityDesignsLookupByHull[player][design.HullCategory].Add(design);
            ////if (player.IsUser) {
            //D.Log("{0} added {1} for {2}.", DebugName, design.DebugName, player.DebugName);
            ////}
        }

        public void Add(StarbaseCmdDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup[player].Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add {1}'s DesignName {2} as it is already present.", DebugName, player.DebugName, designName);
                return;
            }
            var designsByName = _starbaseCmdDesignLookupByName[player];
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} for {2}.", DebugName, design.DebugName, player.DebugName);
        }

        public void Add(FleetCmdDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup[player].Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add {1}'s DesignName {2} as it is already present.", DebugName, player.DebugName, designName);
                return;
            }
            var designsByName = _fleetCmdDesignLookupByName[player];
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} for {2}.", DebugName, design.DebugName, player.DebugName);
        }

        public void Add(SettlementCmdDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup[player].Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add {1}'s DesignName {2} as it is already present.", DebugName, player.DebugName, designName);
                return;
            }
            var designsByName = _settlementCmdDesignLookupByName[player];
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} for {2}.", DebugName, design.DebugName, player.DebugName);
        }

        #endregion

        #region Obsolete Design

        public void ObsoleteShipDesign(Player player, string designName) {
            var designsByName = _shipDesignLookupByName[player];
            ShipDesign design;
            if (!designsByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(ShipDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitMemberDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteUserShipDesign(string designName) {
            ObsoleteShipDesign(_userPlayer, designName);
        }

        public void ObsoleteFacilityDesign(Player player, string designName) {
            var designsByName = _facilityDesignLookupByName[player];
            FacilityDesign design;
            if (!designsByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(FacilityDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitMemberDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteUserFacilityDesign(string designName) {
            ObsoleteFacilityDesign(_userPlayer, designName);
        }

        public void ObsoleteStarbaseCmdDesign(Player player, string designName) {
            var designsByName = _starbaseCmdDesignLookupByName[player];
            StarbaseCmdDesign design;
            if (!designsByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(StarbaseCmdDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitMemberDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteUserStarbaseCmdDesign(string designName) {
            ObsoleteStarbaseCmdDesign(_userPlayer, designName);
        }

        public void ObsoleteSettlementCmdDesign(Player player, string designName) {
            var designsByName = _settlementCmdDesignLookupByName[player];
            SettlementCmdDesign design;
            if (!designsByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(SettlementCmdDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitMemberDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteUserSettlementCmdDesign(string designName) {
            ObsoleteSettlementCmdDesign(_userPlayer, designName);
        }

        public void ObsoleteFleetCmdDesign(Player player, string designName) {
            var designsByName = _fleetCmdDesignLookupByName[player];
            FleetCmdDesign design;
            if (!designsByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(FleetCmdDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitMemberDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteUserFleetCmdDesign(string designName) {
            ObsoleteFleetCmdDesign(_userPlayer, designName);
        }

        #endregion

        #region Design Presence

        public bool IsDesignNameInUseByUser(string designName) {
            return _designNamesInUseLookup[_userPlayer].Contains(designName);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in active designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns>
        ///   <c>true</c> if [is design present] [the specified design]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDesignPresent(ShipDesign design, out string designName) {
            var designsPresent = _shipDesignLookupByName[design.Player].Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        public bool AreUpgradeDesignsPresent(Player player, ShipDesign designToUpgrade) {
            IList<ShipDesign> unusedUpgradeDesigns;
            return TryGetUpgradeDesigns(player, designToUpgrade, out unusedUpgradeDesigns);
        }

        public bool AreUserUpgradeDesignsPresent(ShipDesign designToUpgrade) {
            return AreUpgradeDesignsPresent(_userPlayer, designToUpgrade);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in active designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns>
        ///   <c>true</c> if [is design present] [the specified design]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDesignPresent(FacilityDesign design, out string designName) {
            var designsPresent = _facilityDesignLookupByName[design.Player].Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        public bool AreUpgradeDesignsPresent(Player player, FacilityDesign designToUpgrade) {
            IList<FacilityDesign> unusedUpgradeDesigns;
            return TryGetUpgradeDesigns(player, designToUpgrade, out unusedUpgradeDesigns);
        }

        public bool AreUserUpgradeDesignsPresent(FacilityDesign designToUpgrade) {
            return AreUpgradeDesignsPresent(_userPlayer, designToUpgrade);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in active designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns>
        ///   <c>true</c> if [is design present] [the specified design]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDesignPresent(FleetCmdDesign design, out string designName) {
            var designsPresent = _fleetCmdDesignLookupByName[design.Player].Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in active designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns>
        ///   <c>true</c> if [is design present] [the specified design]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDesignPresent(StarbaseCmdDesign design, out string designName) {
            var designsPresent = _starbaseCmdDesignLookupByName[design.Player].Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in active designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns>
        ///   <c>true</c> if [is design present] [the specified design]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDesignPresent(SettlementCmdDesign design, out string designName) {
            var designsPresent = _settlementCmdDesignLookupByName[design.Player].Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        #region Deprecated Design Presence

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool AreUpgradeDesignsPresent(Player player, FleetCmdDesign design) {
            IList<FleetCmdDesign> unusedUpgradeDesigns;
            return TryGetUpgradeDesigns(player, design, out unusedUpgradeDesigns);
        }

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool AreUserUpgradeDesignsPresent(FleetCmdDesign design) {
            return AreUpgradeDesignsPresent(_userPlayer, design);
        }

        [System.Obsolete("Some ships of FleetCmd may not be candidates for a Refit")]
        public bool AreUnitUpgradeDesignsPresent(Player player, FleetCmdData cmdData) {
            var elementDesigns = cmdData.ElementsData.Select(eData => eData.Design);
            foreach (var design in elementDesigns) {
                if (AreUpgradeDesignsPresent(player, design)) {
                    return true;
                }
            }
            return false;
        }

        [System.Obsolete("Some ships of FleetCmd may not be candidates for a Refit")]
        public bool AreUserUnitUpgradeDesignsPresent(FleetCmdData cmdData) {
            return AreUnitUpgradeDesignsPresent(_userPlayer, cmdData);
        }

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool AreUpgradeDesignsPresent(Player player, StarbaseCmdDesign design) {
            IList<StarbaseCmdDesign> unusedUpgradeDesigns;
            return TryGetUpgradeDesigns(player, design, out unusedUpgradeDesigns);
        }

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool AreUserUpgradeDesignsPresent(StarbaseCmdDesign design) {
            return AreUpgradeDesignsPresent(_userPlayer, design);
        }

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool AreUpgradeDesignsPresent(Player player, SettlementCmdDesign design) {
            IList<SettlementCmdDesign> unusedUpgradeDesigns;
            return TryGetUpgradeDesigns(player, design, out unusedUpgradeDesigns);
        }

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool AreUserUpgradeDesignsPresent(SettlementCmdDesign design) {
            return AreUpgradeDesignsPresent(_userPlayer, design);
        }

        [System.Obsolete("Some facilities of BaseCmd may not be candidates for a Refit")]
        public bool AreUnitUpgradeDesignsPresent(Player player, AUnitBaseCmdData cmdData) {
            var elementDesigns = cmdData.ElementsData.Select(eData => eData.Design);
            foreach (var design in elementDesigns) {
                if (AreUpgradeDesignsPresent(player, design)) {
                    return true;
                }
            }
            return false;
        }

        [System.Obsolete("Some facilities of BaseCmd may not be candidates for a Refit")]
        public bool AreUserUnitUpgradeDesignsPresent(AUnitBaseCmdData cmdData) {
            return AreUnitUpgradeDesignsPresent(_userPlayer, cmdData);
        }

        #endregion

        #endregion

        #region Get Design

        public IEnumerable<ShipDesign> GetAllUserShipDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _shipDesignLookupByName[_userPlayer].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.Player_Obsolete);
            }
            return _shipDesignLookupByName[_userPlayer].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
        }

        public ShipDesign GetShipDesign(Player player, string designName) {
            var designsByName = _shipDesignLookupByName[player];
            if (!designsByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(ShipDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            return designsByName[designName];
        }

        public ShipDesign GetUserShipDesign(string designName) {
            return GetShipDesign(_userPlayer, designName);
        }

        public bool TryGetDesign(Player player, string designName, out ShipDesign design) {
            design = null;
            IDictionary<string, ShipDesign> designsByName;
            if (_shipDesignLookupByName.TryGetValue(player, out designsByName)) {
                if (designsByName.TryGetValue(designName, out design)) {
                    return true;
                }
            }
            return false;
        }

        public bool TryGetUserDesign(string designName, out ShipDesign design) {
            return TryGetDesign(_userPlayer, designName, out design);
        }

        public bool TryGetDesigns(Player player, ShipHullCategory hullCategory, out IList<ShipDesign> designs) {
            IDictionary<ShipHullCategory, IList<ShipDesign>> designsByHull;
            if (_shipDesignsLookupByHull.TryGetValue(player, out designsByHull)) {
                if (designsByHull.TryGetValue(hullCategory, out designs)) {
                    return true;
                }
            }
            designs = new List<ShipDesign>(Constants.Zero);
            return false;
        }

        public bool TryGetUserDesigns(ShipHullCategory hullCategory, out IList<ShipDesign> designs) {
            return TryGetDesigns(_userPlayer, hullCategory, out designs);
        }

        public bool TryGetUpgradeDesigns(Player player, ShipDesign designToUpgrade, out IList<ShipDesign> upgradeDesigns) {
            if (player == designToUpgrade.Player) {
                IList<ShipDesign> hullDesigns;
                if (TryGetDesigns(player, designToUpgrade.HullCategory, out hullDesigns)) {
                    var candidateDesigns = hullDesigns.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.Player_Current).Except(designToUpgrade);
                    upgradeDesigns = candidateDesigns.Where(d => d.RefitBenefit > designToUpgrade.RefitBenefit).ToList();
                    bool hasUpgradeDesigns = upgradeDesigns.Any();
                    if (!hasUpgradeDesigns) {
                        //D.Log("{0} has found no upgrade designs better than {1}. Designs considered = {2}.",
                        //    DebugName, designToUpgrade.DebugName, candidateDesigns.Select(d => d.DebugName).Concatenate());
                    }
                    return hasUpgradeDesigns;
                }
            }
            upgradeDesigns = new List<ShipDesign>(Constants.Zero);
            return false;
        }

        public bool TryGetUserUpgradeDesigns(ShipDesign designToUpgrade, out IList<ShipDesign> upgradeDesigns) {
            return TryGetUpgradeDesigns(_userPlayer, designToUpgrade, out upgradeDesigns);
        }

        public IEnumerable<FacilityDesign> GetAllUserFacilityDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _facilityDesignLookupByName[_userPlayer].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.Player_Obsolete);
            }
            return _facilityDesignLookupByName[_userPlayer].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
        }

        public FacilityDesign GetFacilityDesign(Player player, string designName) {
            var designsByName = _facilityDesignLookupByName[player];
            if (!designsByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(FacilityDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            return designsByName[designName];
        }

        public FacilityDesign GetUserFacilityDesign(string designName) {
            return GetFacilityDesign(_userPlayer, designName);
        }

        public bool TryGetDesign(Player player, string designName, out FacilityDesign design) {
            design = null;
            IDictionary<string, FacilityDesign> designsByName;
            if (_facilityDesignLookupByName.TryGetValue(player, out designsByName)) {
                if (designsByName.TryGetValue(designName, out design)) {
                    return true;
                }
            }
            return false;
        }

        public bool TryGetUserDesign(string designName, out FacilityDesign design) {
            return TryGetDesign(_userPlayer, designName, out design);
        }

        public bool TryGetDesigns(Player player, FacilityHullCategory hullCategory, out IList<FacilityDesign> designs) {
            IDictionary<FacilityHullCategory, IList<FacilityDesign>> designsByHull;
            if (_facilityDesignsLookupByHull.TryGetValue(player, out designsByHull)) {
                if (designsByHull.TryGetValue(hullCategory, out designs)) {
                    return true;
                }
            }
            designs = new List<FacilityDesign>(Constants.Zero);
            return false;
        }

        public bool TryGetUserDesigns(FacilityHullCategory hullCategory, out IList<FacilityDesign> designs) {
            return TryGetDesigns(_userPlayer, hullCategory, out designs);
        }

        public bool TryGetUpgradeDesigns(Player player, FacilityDesign designToUpgrade, out IList<FacilityDesign> upgradeDesigns) {
            if (player == designToUpgrade.Player) {
                IList<FacilityDesign> hullDesigns;
                if (TryGetDesigns(player, designToUpgrade.HullCategory, out hullDesigns)) {
                    var candidateDesigns = hullDesigns.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.Player_Current).Except(designToUpgrade);
                    upgradeDesigns = candidateDesigns.Where(d => d.RefitBenefit > designToUpgrade.RefitBenefit).ToList();
                    bool hasUpgradeDesigns = upgradeDesigns.Any();
                    if (!hasUpgradeDesigns) {
                        //D.Log("{0} has found no upgrade designs better than {1}. Designs considered = {2}.",
                        //    DebugName, designToUpgrade.DebugName, candidateDesigns.Select(d => d.DebugName).Concatenate());
                    }
                    return hasUpgradeDesigns;
                }
            }
            upgradeDesigns = new List<FacilityDesign>(0);
            return false;
        }

        public bool TryGetUserUpgradeDesigns(FacilityDesign designToUpgrade, out IList<FacilityDesign> upgradeDesigns) {
            return TryGetUpgradeDesigns(_userPlayer, designToUpgrade, out upgradeDesigns);
        }


        public IEnumerable<StarbaseCmdDesign> GetAllUserStarbaseCmdDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _starbaseCmdDesignLookupByName[_userPlayer].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.Player_Obsolete);
            }
            return _starbaseCmdDesignLookupByName[_userPlayer].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
        }

        public StarbaseCmdDesign GetStarbaseCmdDesign(Player player, string designName) {
            var designsByName = _starbaseCmdDesignLookupByName[player];
            if (!designsByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(StarbaseCmdDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            return designsByName[designName];
        }

        public StarbaseCmdDesign GetUserStarbaseCmdDesign(string designName) {
            return GetStarbaseCmdDesign(_userPlayer, designName);
        }

        public bool TryGetStarbaseCmdDesign(Player player, string designName, out StarbaseCmdDesign design) {
            design = null;
            IDictionary<string, StarbaseCmdDesign> designsByName;
            if (_starbaseCmdDesignLookupByName.TryGetValue(player, out designsByName)) {
                if (designsByName.TryGetValue(designName, out design)) {
                    return true;
                }
            }
            return false;
        }

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool TryGetUpgradeDesigns(Player player, StarbaseCmdDesign designToUpgrade, out IList<StarbaseCmdDesign> upgradeDesigns) {
            if (player == designToUpgrade.Player) {
                var candidateDesigns = _starbaseCmdDesignLookupByName[player].Values.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.Player_Current).Except(designToUpgrade);
                if (candidateDesigns.Any()) {
                    upgradeDesigns = candidateDesigns.Where(d => d.RefitBenefit > designToUpgrade.RefitBenefit).ToList();
                    if (upgradeDesigns.Any()) {
                        return true;
                    }
                }
            }
            upgradeDesigns = new List<StarbaseCmdDesign>(Constants.Zero);
            return false;
        }

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool TryGetUserUpgradeDesigns(StarbaseCmdDesign designToUpgrade, out IList<StarbaseCmdDesign> upgradeDesigns) {
            return TryGetUpgradeDesigns(_userPlayer, designToUpgrade, out upgradeDesigns);
        }

        public IEnumerable<FleetCmdDesign> GetAllUserFleetCmdDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _fleetCmdDesignLookupByName[_userPlayer].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.Player_Obsolete);
            }
            return _fleetCmdDesignLookupByName[_userPlayer].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
        }

        public FleetCmdDesign GetFleetCmdDesign(Player player, string designName) {
            var designsByName = _fleetCmdDesignLookupByName[player];
            if (!designsByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(FleetCmdDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            return designsByName[designName];
        }

        public FleetCmdDesign GetUserFleetCmdDesign(string designName) {
            return GetFleetCmdDesign(_userPlayer, designName);
        }

        public bool TryGetFleetCmdDesign(Player player, string designName, out FleetCmdDesign design) {
            design = null;
            IDictionary<string, FleetCmdDesign> designsByName;
            if (_fleetCmdDesignLookupByName.TryGetValue(player, out designsByName)) {
                if (designsByName.TryGetValue(designName, out design)) {
                    return true;
                }
            }
            return false;
        }

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool TryGetUpgradeDesigns(Player player, FleetCmdDesign designToUpgrade, out IList<FleetCmdDesign> upgradeDesigns) {
            if (player == designToUpgrade.Player) {
                var candidateDesigns = _fleetCmdDesignLookupByName[player].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current).Except(designToUpgrade);
                if (candidateDesigns.Any()) {
                    upgradeDesigns = candidateDesigns.Where(d => d.RefitBenefit > designToUpgrade.RefitBenefit).ToList();
                    if (upgradeDesigns.Any()) {
                        return true;
                    }
                }
            }
            upgradeDesigns = new List<FleetCmdDesign>(Constants.Zero);
            return false;
        }

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool TryGetUserUpgradeDesigns(FleetCmdDesign designToUpgrade, out IList<FleetCmdDesign> upgradeDesigns) {
            return TryGetUpgradeDesigns(_userPlayer, designToUpgrade, out upgradeDesigns);
        }

        public IEnumerable<SettlementCmdDesign> GetAllUserSettlementCmdDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _settlementCmdDesignLookupByName[_userPlayer].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.Player_Obsolete);
            }
            return _settlementCmdDesignLookupByName[_userPlayer].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
        }

        public SettlementCmdDesign GetSettlementCmdDesign(Player player, string designName) {
            var designsByName = _settlementCmdDesignLookupByName[player];
            if (!designsByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(SettlementCmdDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            return designsByName[designName];
        }

        public SettlementCmdDesign GetUserSettlementCmdDesign(string designName) {
            return GetSettlementCmdDesign(_userPlayer, designName);
        }

        public bool TryGetSettlementCmdDesign(Player player, string designName, out SettlementCmdDesign design) {
            design = null;
            IDictionary<string, SettlementCmdDesign> designsByName;
            if (_settlementCmdDesignLookupByName.TryGetValue(player, out designsByName)) {
                if (designsByName.TryGetValue(designName, out design)) {
                    return true;
                }
            }
            return false;
        }

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool TryGetUpgradeDesigns(Player player, SettlementCmdDesign designToUpgrade, out IList<SettlementCmdDesign> upgradeDesigns) {
            if (player == designToUpgrade.Player) {
                var candidateDesigns = _settlementCmdDesignLookupByName[player].Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current).Except(designToUpgrade);
                if (candidateDesigns.Any()) {
                    upgradeDesigns = candidateDesigns.Where(d => d.RefitBenefit > designToUpgrade.RefitBenefit).ToList();
                    if (upgradeDesigns.Any()) {
                        return true;
                    }
                }
            }
            upgradeDesigns = new List<SettlementCmdDesign>(Constants.Zero);
            return false;
        }

        [System.Obsolete("Upgrades for Cmd Designs not yet implemented")]
        public bool TryGetUserUpgradeDesigns(SettlementCmdDesign designToUpgrade, out IList<SettlementCmdDesign> upgradeDesigns) {
            return TryGetUpgradeDesigns(_userPlayer, designToUpgrade, out upgradeDesigns);
        }

        #endregion

        #region Debug

        [System.Obsolete]
        public AUnitElementDesign __GetUserElementDesign(string designName) {
            ShipDesign shipDesign;
            if (TryGetDesign(_userPlayer, designName, out shipDesign)) {
                return shipDesign;
            }
            else {
                FacilityDesign facilityDesign;
                bool isDesignFound = TryGetDesign(_userPlayer, designName, out facilityDesign);
                D.Assert(isDesignFound);
                return facilityDesign;
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

    }
}

