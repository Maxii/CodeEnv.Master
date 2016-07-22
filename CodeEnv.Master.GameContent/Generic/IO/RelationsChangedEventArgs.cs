// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RelationsChangedEventArgs.cs
// EventArgs for a Diplomatic Relationship change.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// EventArgs for a Diplomatic Relationship change.
    /// <remarks>The other player involved in the change is acquired from the sender of the event.</remarks>
    /// </summary>
    public class RelationsChangedEventArgs : EventArgs {

        public Player EffectedPlayer { get; private set; }

        public DiplomaticRelationship NewRelationship { get; private set; }

        public DiplomaticRelationship PriorRelationship { get; private set; }

        public RelationsChangedEventArgs(Player effectedPlayer, DiplomaticRelationship priorRelationship, DiplomaticRelationship newRelationship) {
            EffectedPlayer = effectedPlayer;
            PriorRelationship = priorRelationship;
            NewRelationship = newRelationship;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

