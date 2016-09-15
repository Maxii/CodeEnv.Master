// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NoPlayer.cs
// A perpetually neutral, non-human player for use with Items that have no owner.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// A perpetually neutral, non-human player for use with Items that have no owner.
    /// </summary>
    public class NoPlayer : Player {

        public NoPlayer() : base(new SpeciesStat(), new LeaderStat(name: "None"), IQ.None, TeamID.None, GameColor.White) { }

        public override void SetRelationsWith(Player player, DiplomaticRelationship relation) {
            throw new NotImplementedException("SetRelationswith() is not implemented in {0}.".Inject(Name));
        }

        internal override void SetRelationsWith_Internal(Player player, DiplomaticRelationship newRelationship) {
            throw new NotImplementedException("SetRelationswith() is not implemented in {0}.".Inject(Name));
        }

        public override DiplomaticRelationship GetCurrentRelations(Player player) {
            return DiplomaticRelationship.None;
        }

        public override DiplomaticRelationship GetPriorRelations(Player player) {
            throw new NotImplementedException("GetPriorRelations() is not implemented in {0}.".Inject(Name));
        }

        public override IEnumerable<Player> GetOtherPlayersWithRelationship(params DiplomaticRelationship[] relations) {
            throw new NotImplementedException("GetOtherPlayersWithRelationship() is not implemented in {0}.".Inject(Name));
        }

        public override void HandleMetNewPlayer(Player newlyMetPlayer) {
            throw new NotImplementedException("HandleMetNewPlayer() is not implemented in {0}.".Inject(Name));
        }

        internal override void HandleMetNewPlayer_Internal(Player newlyMetPlayer) {
            throw new NotImplementedException("HandleMetNewPlayer() is not implemented in {0}.".Inject(Name));
        }

        public override bool IsKnown(Player player) {
            throw new NotImplementedException("isKnown() is not implemented in {0}.".Inject(Name));
        }

        public override void SetInitialRelationship(Player unmetPlayer, DiplomaticRelationship initialRelationship = DiplomaticRelationship.Neutral) {
            throw new NotImplementedException("SetInitialRelationship() is not implemented in {0}.".Inject(Name));
        }

        internal override void SetInitialRelationship_Internal(Player unmetPlayer, DiplomaticRelationship initialRelationship) {
            throw new NotImplementedException("SetInitialRelationship_Internal() is not implemented in {0}.".Inject(Name));
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

