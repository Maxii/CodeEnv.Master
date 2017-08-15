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

        private IDictionary<Player, IDictionary<string, StarbaseCmdDesign>> _starbaseCmdDesignsLookup;
        private IDictionary<Player, IDictionary<string, FleetCmdDesign>> _fleetCmdDesignsLookup;
        private IDictionary<Player, IDictionary<string, SettlementCmdDesign>> _settlementCmdDesignsLookup;

        private IDictionary<Player, IDictionary<string, ShipDesign>> _shipDesignsLookup;
        private IDictionary<Player, IDictionary<string, FacilityDesign>> _facilityDesignsLookup;

        private IDictionary<Player, HashSet<string>> _designNamesInUseLookup;

        private Player _userPlayer;

        public PlayerDesigns(IEnumerable<Player> allPlayers) {
            InitializeValuesAndReferences(allPlayers);
        }

        private void InitializeValuesAndReferences(IEnumerable<Player> allPlayers) {
            int playerCount = allPlayers.Count();
            _shipDesignsLookup = new Dictionary<Player, IDictionary<string, ShipDesign>>(playerCount);
            _facilityDesignsLookup = new Dictionary<Player, IDictionary<string, FacilityDesign>>(playerCount);
            _starbaseCmdDesignsLookup = new Dictionary<Player, IDictionary<string, StarbaseCmdDesign>>(playerCount);
            _fleetCmdDesignsLookup = new Dictionary<Player, IDictionary<string, FleetCmdDesign>>(playerCount);
            _settlementCmdDesignsLookup = new Dictionary<Player, IDictionary<string, SettlementCmdDesign>>(playerCount);

            _designNamesInUseLookup = new Dictionary<Player, HashSet<string>>(playerCount);

            allPlayers.ForAll(p => {
                _shipDesignsLookup.Add(p, new Dictionary<string, ShipDesign>());
                _facilityDesignsLookup.Add(p, new Dictionary<string, FacilityDesign>());
                _starbaseCmdDesignsLookup.Add(p, new Dictionary<string, StarbaseCmdDesign>());
                _fleetCmdDesignsLookup.Add(p, new Dictionary<string, FleetCmdDesign>());
                _settlementCmdDesignsLookup.Add(p, new Dictionary<string, SettlementCmdDesign>());

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
            var designsByName = _shipDesignsLookup[player];
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} {2} for {3}.", GetType().Name, design.GetType().Name, designName, player);
        }

        public void Add(FacilityDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup[player].Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add {1}'s DesignName {2} as it is already present.", DebugName, player.DebugName, designName);
                return;
            }
            var designsByName = _facilityDesignsLookup[player];
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} {2} for {3}.", GetType().Name, design.GetType().Name, designName, player);
        }

        public void Add(StarbaseCmdDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup[player].Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add {1}'s DesignName {2} as it is already present.", DebugName, player.DebugName, designName);
                return;
            }
            var designsByName = _starbaseCmdDesignsLookup[player];
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} {2} for {3}.", GetType().Name, design.GetType().Name, designName, player);
        }

        public void Add(FleetCmdDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup[player].Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add {1}'s DesignName {2} as it is already present.", DebugName, player.DebugName, designName);
                return;
            }
            var designsByName = _fleetCmdDesignsLookup[player];
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} {2} for {3}.", GetType().Name, design.GetType().Name, designName, player);
        }

        public void Add(SettlementCmdDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup[player].Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add {1}'s DesignName {2} as it is already present.", DebugName, player.DebugName, designName);
                return;
            }
            var designsByName = _settlementCmdDesignsLookup[player];
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} {2} for {3}.", GetType().Name, design.GetType().Name, designName, player);
        }

        #endregion

        #region Obsolete Design

        public void ObsoleteShipDesign(Player player, string designName) {
            var designsByName = _shipDesignsLookup[player];
            ShipDesign design;
            if (!designsByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(ShipDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteUserShipDesign(string designName) {
            ObsoleteShipDesign(_userPlayer, designName);
        }

        public void ObsoleteFacilityDesign(Player player, string designName) {
            var designsByName = _facilityDesignsLookup[player];
            FacilityDesign design;
            if (!designsByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(FacilityDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteUserFacilityDesign(string designName) {
            ObsoleteFacilityDesign(_userPlayer, designName);
        }

        public void ObsoleteStarbaseCmdDesign(Player player, string designName) {
            var designsByName = _starbaseCmdDesignsLookup[player];
            StarbaseCmdDesign design;
            if (!designsByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(StarbaseCmdDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteUserStarbaseCmdDesign(string designName) {
            ObsoleteStarbaseCmdDesign(_userPlayer, designName);
        }

        public void ObsoleteSettlementCmdDesign(Player player, string designName) {
            var designsByName = _settlementCmdDesignsLookup[player];
            SettlementCmdDesign design;
            if (!designsByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(SettlementCmdDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteUserSettlementCmdDesign(string designName) {
            ObsoleteSettlementCmdDesign(_userPlayer, designName);
        }

        public void ObsoleteFleetCmdDesign(Player player, string designName) {
            var designsByName = _fleetCmdDesignsLookup[player];
            FleetCmdDesign design;
            if (!designsByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(FleetCmdDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitDesign.SourceAndStatus.Player_Obsolete;
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
            var designsPresent = _shipDesignsLookup[design.Player].Values.Where(des => des.Status != AUnitDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (GameUtility.IsDesignContentEqual(design, presentDesign)) {
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
        public bool IsDesignPresent(FacilityDesign design, out string designName) {
            var designsPresent = _facilityDesignsLookup[design.Player].Values.Where(des => des.Status != AUnitDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (GameUtility.IsDesignContentEqual(design, presentDesign)) {
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
        public bool IsDesignPresent(FleetCmdDesign design, out string designName) {
            var designsPresent = _fleetCmdDesignsLookup[design.Player].Values.Where(des => des.Status != AUnitDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (GameUtility.IsDesignContentEqual(design, presentDesign)) {
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
            var designsPresent = _starbaseCmdDesignsLookup[design.Player].Values.Where(des => des.Status != AUnitDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (GameUtility.IsDesignContentEqual(design, presentDesign)) {
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
            var designsPresent = _settlementCmdDesignsLookup[design.Player].Values.Where(des => des.Status != AUnitDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (GameUtility.IsDesignContentEqual(design, presentDesign)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        #endregion

        #region Get Design

        public IEnumerable<ShipDesign> GetAllUserShipDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _shipDesignsLookup[_userPlayer].Values.Where(des => des.Status == AUnitDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitDesign.SourceAndStatus.Player_Obsolete);
            }
            return _shipDesignsLookup[_userPlayer].Values.Where(des => des.Status == AUnitDesign.SourceAndStatus.Player_Current);
        }

        public ShipDesign GetShipDesign(Player player, string designName) {
            var designsByName = _shipDesignsLookup[player];
            if (!designsByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(ShipDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            return designsByName[designName];
        }

        public ShipDesign GetUserShipDesign(string designName) {
            return GetShipDesign(_userPlayer, designName);
        }

        public bool TryGetShipDesign(Player player, string designName, out ShipDesign design) {
            design = null;
            IDictionary<string, ShipDesign> designsByName;
            if (_shipDesignsLookup.TryGetValue(player, out designsByName)) {
                if (designsByName.TryGetValue(designName, out design)) {
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<FacilityDesign> GetAllUserFacilityDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _facilityDesignsLookup[_userPlayer].Values.Where(des => des.Status == AUnitDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitDesign.SourceAndStatus.Player_Obsolete);
            }
            return _facilityDesignsLookup[_userPlayer].Values.Where(des => des.Status == AUnitDesign.SourceAndStatus.Player_Current);
        }

        public FacilityDesign GetFacilityDesign(Player player, string designName) {
            var designsByName = _facilityDesignsLookup[player];
            if (!designsByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present for {3}. DesignNames: {4}.", GetType().Name, typeof(FacilityDesign).Name, designName, player, designsByName.Keys.Concatenate());
            }
            return designsByName[designName];
        }

        public FacilityDesign GetUserFacilityDesign(string designName) {
            return GetFacilityDesign(_userPlayer, designName);
        }

        public bool TryGetFacilityDesign(Player player, string designName, out FacilityDesign design) {
            design = null;
            IDictionary<string, FacilityDesign> designsByName;
            if (_facilityDesignsLookup.TryGetValue(player, out designsByName)) {
                if (designsByName.TryGetValue(designName, out design)) {
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<StarbaseCmdDesign> GetAllUserStarbaseCmdDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _starbaseCmdDesignsLookup[_userPlayer].Values.Where(des => des.Status == AUnitDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitDesign.SourceAndStatus.Player_Obsolete);
            }
            return _starbaseCmdDesignsLookup[_userPlayer].Values.Where(des => des.Status == AUnitDesign.SourceAndStatus.Player_Current);
        }

        public StarbaseCmdDesign GetStarbaseCmdDesign(Player player, string designName) {
            var designsByName = _starbaseCmdDesignsLookup[player];
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
            if (_starbaseCmdDesignsLookup.TryGetValue(player, out designsByName)) {
                if (designsByName.TryGetValue(designName, out design)) {
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<FleetCmdDesign> GetAllUserFleetCmdDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _fleetCmdDesignsLookup[_userPlayer].Values.Where(des => des.Status == AUnitDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitDesign.SourceAndStatus.Player_Obsolete);
            }
            return _fleetCmdDesignsLookup[_userPlayer].Values.Where(des => des.Status == AUnitDesign.SourceAndStatus.Player_Current);
        }

        public FleetCmdDesign GetFleetCmdDesign(Player player, string designName) {
            var designsByName = _fleetCmdDesignsLookup[player];
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
            if (_fleetCmdDesignsLookup.TryGetValue(player, out designsByName)) {
                if (designsByName.TryGetValue(designName, out design)) {
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<SettlementCmdDesign> GetAllUserSettlementCmdDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _settlementCmdDesignsLookup[_userPlayer].Values.Where(des => des.Status == AUnitDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitDesign.SourceAndStatus.Player_Obsolete);
            }
            return _settlementCmdDesignsLookup[_userPlayer].Values.Where(des => des.Status == AUnitDesign.SourceAndStatus.Player_Current);
        }

        public SettlementCmdDesign GetSettlementCmdDesign(Player player, string designName) {
            var designsByName = _settlementCmdDesignsLookup[player];
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
            if (_settlementCmdDesignsLookup.TryGetValue(player, out designsByName)) {
                if (designsByName.TryGetValue(designName, out design)) {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Debug

        [System.Obsolete]
        public AElementDesign __GetUserElementDesign(string designName) {
            ShipDesign shipDesign;
            if (TryGetShipDesign(_userPlayer, designName, out shipDesign)) {
                return shipDesign;
            }
            else {
                FacilityDesign facilityDesign;
                bool isDesignFound = TryGetFacilityDesign(_userPlayer, designName, out facilityDesign);
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

