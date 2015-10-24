// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayersDesigns.cs
// Wrapper that holds a collection of ElementDesigns organized by the owner of the design and the design's name.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper that holds a collection of ElementDesigns organized by the owner of the design and the design's name.
    /// </summary>
    public class PlayersDesigns {

        private IDictionary<Player, IDictionary<string, ShipDesign>> _shipDesignsByPlayer;
        private IDictionary<Player, IDictionary<string, FacilityDesign>> _facilityDesignsByPlayer;
        private Player _userPlayer;

        public PlayersDesigns(IEnumerable<Player> allPlayers) {
            int playerCount = allPlayers.Count();
            _shipDesignsByPlayer = new Dictionary<Player, IDictionary<string, ShipDesign>>(playerCount);
            _facilityDesignsByPlayer = new Dictionary<Player, IDictionary<string, FacilityDesign>>(playerCount);
            allPlayers.ForAll(p => {
                _shipDesignsByPlayer.Add(p, new Dictionary<string, ShipDesign>());
                _facilityDesignsByPlayer.Add(p, new Dictionary<string, FacilityDesign>());
                if (p.IsUser) {
                    _userPlayer = p;
                }
            });
        }

        public void Add(ShipDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            var designsByName = _shipDesignsByPlayer[player];
            D.Assert(!designsByName.ContainsKey(designName));
            designsByName.Add(designName, design);
        }

        public void Add(FacilityDesign design) {
            Player player = design.Player;
            string designName = design.DesignName;
            var designsByName = _facilityDesignsByPlayer[player];
            D.Assert(!designsByName.ContainsKey(designName));
            designsByName.Add(designName, design);
        }

        public ShipDesign GetShipDesign(Player player, string designName) {
            var designsByName = _shipDesignsByPlayer[player];
            D.Assert(designsByName.ContainsKey(designName), "No design named {0} present for {1}.".Inject(designName, player.LeaderName));
            return designsByName[designName];
        }

        public ShipDesign GetUserShipDesign(string designName) {
            return GetShipDesign(_userPlayer, designName);
        }

        public FacilityDesign GetFacilityDesign(Player player, string designName) {
            var designsByName = _facilityDesignsByPlayer[player];
            D.Assert(designsByName.ContainsKey(designName), "No design named {0} present for {1}.".Inject(designName, player.LeaderName));
            return designsByName[designName];
        }

        public FacilityDesign GetUserFacilityDesign(string designName) {
            return GetFacilityDesign(_userPlayer, designName);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

