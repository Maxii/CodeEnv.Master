// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayersDesigns.cs
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
    public class PlayersDesigns {

        public string DebugName { get { return GetType().Name; } }

        private IDictionary<Player, IDictionary<string, StarbaseCmdDesign>> _starbaseCmdDesignsLookup;
        private IDictionary<Player, IDictionary<string, FleetCmdDesign>> _fleetCmdDesignsLookup;
        private IDictionary<Player, IDictionary<string, SettlementCmdDesign>> _settlementCmdDesignsLookup;

        private IDictionary<Player, IDictionary<string, ShipDesign>> _shipDesignsLookup;
        private IDictionary<Player, IDictionary<string, FacilityDesign>> _facilityDesignsLookup;
        private Player _userPlayer;

        public PlayersDesigns(IEnumerable<Player> allPlayers) {
            int playerCount = allPlayers.Count();
            _shipDesignsLookup = new Dictionary<Player, IDictionary<string, ShipDesign>>(playerCount);
            _facilityDesignsLookup = new Dictionary<Player, IDictionary<string, FacilityDesign>>(playerCount);
            _starbaseCmdDesignsLookup = new Dictionary<Player, IDictionary<string, StarbaseCmdDesign>>(playerCount);
            _fleetCmdDesignsLookup = new Dictionary<Player, IDictionary<string, FleetCmdDesign>>(playerCount);
            _settlementCmdDesignsLookup = new Dictionary<Player, IDictionary<string, SettlementCmdDesign>>(playerCount);
            allPlayers.ForAll(p => {
                _shipDesignsLookup.Add(p, new Dictionary<string, ShipDesign>());
                _facilityDesignsLookup.Add(p, new Dictionary<string, FacilityDesign>());
                _starbaseCmdDesignsLookup.Add(p, new Dictionary<string, StarbaseCmdDesign>());
                _fleetCmdDesignsLookup.Add(p, new Dictionary<string, FleetCmdDesign>());
                _settlementCmdDesignsLookup.Add(p, new Dictionary<string, SettlementCmdDesign>());
                if (p.IsUser) {
                    _userPlayer = p;
                }
            });
        }

        public void Add(ShipDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            var designsByName = _shipDesignsLookup[player];
            D.Assert(!designsByName.ContainsKey(designName));
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} {2} for {3}.", GetType().Name, design.GetType().Name, designName, player);
        }

        public void Add(FacilityDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            var designsByName = _facilityDesignsLookup[player];
            D.Assert(!designsByName.ContainsKey(designName));
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} {2} for {3}.", GetType().Name, design.GetType().Name, designName, player);
        }

        public void Add(StarbaseCmdDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            var designsByName = _starbaseCmdDesignsLookup[player];
            D.Assert(!designsByName.ContainsKey(designName));
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} {2} for {3}.", GetType().Name, design.GetType().Name, designName, player);
        }

        public void Add(FleetCmdDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            var designsByName = _fleetCmdDesignsLookup[player];
            D.Assert(!designsByName.ContainsKey(designName));
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} {2} for {3}.", GetType().Name, design.GetType().Name, designName, player);
        }

        public void Add(SettlementCmdDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            var designsByName = _settlementCmdDesignsLookup[player];
            D.Assert(!designsByName.ContainsKey(designName));
            designsByName.Add(designName, design);
            //D.Log("{0} added {1} {2} for {3}.", GetType().Name, design.GetType().Name, designName, player);
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

        public override string ToString() {
            return DebugName;
        }

    }
}

