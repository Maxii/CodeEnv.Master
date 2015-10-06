// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerDesigns.cs
// Wrapper that holds a collection of ElementDesigns organized by the owner of the design and the design's name.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper that holds a collection of ElementDesigns organized by the owner of the design and the design's name.
    /// </summary>
    public class PlayerDesigns {

        private IDictionary<Player, IDictionary<string, ShipDesign>> _shipDesignsByPlayer;

        private IDictionary<Player, IDictionary<string, FacilityDesign>> _facilityDesignsByPlayer;

        public PlayerDesigns() {
            Initialize();
        }

        private void Initialize() {
            _shipDesignsByPlayer = new Dictionary<Player, IDictionary<string, ShipDesign>>();
            _facilityDesignsByPlayer = new Dictionary<Player, IDictionary<string, FacilityDesign>>();
        }

        public void Add(Player player) {
            D.Assert(!_shipDesignsByPlayer.ContainsKey(player));
            D.Assert(!_facilityDesignsByPlayer.ContainsKey(player));
            _shipDesignsByPlayer.Add(player, new Dictionary<string, ShipDesign>());
            _facilityDesignsByPlayer.Add(player, new Dictionary<string, FacilityDesign>());
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

        public FacilityDesign GetFacilityDesign(Player player, string designName) {
            var designsByName = _facilityDesignsByPlayer[player];
            D.Assert(designsByName.ContainsKey(designName), "No design named {0} present for {1}.".Inject(designName, player.LeaderName));
            return designsByName[designName];
        }

        public void Clear() {
            _shipDesignsByPlayer.Clear();
            _facilityDesignsByPlayer.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

