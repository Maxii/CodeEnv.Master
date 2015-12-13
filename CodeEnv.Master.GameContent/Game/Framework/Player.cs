// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Player.cs
// Instantiable base class for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Instantiable base class for a player.
    /// </summary>
    public class Player : APropertyChangeTracking {

        private bool _isActive = true;
        public bool IsActive {  // accomodates an AIPlayer being eliminated in the game
            get { return _isActive; }
            set { SetProperty<bool>(ref _isActive, value, "IsActive"); }
        }

        /// <summary>
        /// Indicates whether this Player is the human user playing the game.
        /// </summary>
        public bool IsUser { get; private set; }

        public IQ IQ { get; private set; }

        public GameColor Color { get; private set; }

        public string LeaderName { get { return IsUser ? PlayerPrefsManager.Instance.Username : _leaderStat.Name; } }

        public AtlasID LeaderImageAtlasID { get { return _leaderStat.ImageAtlasID; } }

        public string LeaderImageFilename { get { return _leaderStat.ImageFilename; } }


        public Species Species { get { return _speciesStat.Species; } }

        public string SpeciesName { get { return Species.GetValueName(); } }

        public string SpeciesName_Plural { get { return _speciesStat.Name_Plural; } }

        public string SpeciesDescription { get { return _speciesStat.Description; } }

        public AtlasID SpeciesImageAtlasID { get { return _speciesStat.ImageAtlasID; } }

        public string SpeciesImageFilename { get { return _speciesStat.ImageFilename; } }


        public float SensorRangeMultiplier { get { return _speciesStat.SensorRangeMultiplier; } }

        public float WeaponRangeMultiplier { get { return _speciesStat.WeaponRangeMultiplier; } }

        public float CountermeasureRangeMultiplier { get { return _speciesStat.ActiveCountermeasureRangeMultiplier; } }

        public float WeaponReloadPeriodMultiplier { get { return _speciesStat.WeaponReloadPeriodMultiplier; } }

        public float CountermeasureReloadPeriodMultiplier { get { return _speciesStat.CountermeasureReloadPeriodMultiplier; } }

        private IDictionary<Player, DiplomaticRelationship> _diplomaticRelationship = new Dictionary<Player, DiplomaticRelationship>();
        private LeaderStat _leaderStat;
        private SpeciesStat _speciesStat;

        /// <summary>
        /// Initializes a new instance of the <see cref="Player"/> class.
        /// </summary>
        /// <param name="speciesStat">The species stat.</param>
        /// <param name="leaderStat">The leader stat.</param>
        /// <param name="iq">The iq.</param>
        /// <param name="color">The color.</param>
        /// <param name="isUser">if set to <c>true</c> [is user].</param>
        public Player(SpeciesStat speciesStat, LeaderStat leaderStat, IQ iq, GameColor color, bool isUser = false) {
            _speciesStat = speciesStat;
            _leaderStat = leaderStat;
            IQ = iq;
            Color = color;
            IsUser = isUser;
            _diplomaticRelationship[this] = DiplomaticRelationship.Self;    // assigning relations this way allows NoPlayer to make SetRelations illegal
        }

        public DiplomaticRelationship GetRelations(Player player) {
            if (!_diplomaticRelationship.ContainsKey(player)) {
                return DiplomaticRelationship.None;
            }
            return _diplomaticRelationship[player];
        }

        public virtual void SetRelations(Player player, DiplomaticRelationship relationship) {
            if (player == this) {
                D.Assert(relationship == DiplomaticRelationship.Self);
            }
            _diplomaticRelationship[player] = relationship;
            //TODO send DiploRelationsChange event
        }

        public bool IsRelationship(Player player, params DiplomaticRelationship[] relations) {
            return GetRelations(player).EqualsAnyOf(relations);
        }

        public bool IsEnemyOf(Player player) {
            return IsRelationship(player, DiplomaticRelationship.War, DiplomaticRelationship.ColdWar);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

