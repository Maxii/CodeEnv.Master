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
    using CodeEnv.Master.Common;

    /// <summary>
    /// A perpetually neutral, non-human player for use with Items that have no owner.
    /// </summary>
    public class NoPlayer : Player {

        public NoPlayer() : base(new Race(Species.None, GameColor.White), IQ.None) { }

        public override void SetRelations(Player player, DiplomaticRelationship relation) {
            throw new NotImplementedException("SetRelations() is not implemented in {0}.".Inject(GetType().Name));
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

