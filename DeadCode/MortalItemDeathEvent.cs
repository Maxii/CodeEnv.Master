// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MortalItemDeathEvent.cs
// Event indicating a MortalItem in the game has died. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

using CodeEnv.Master.Common;

/// <summary>
/// Event indicating a MortalItem in the game has died. 
/// </summary>
    public class MortalItemDeathEvent : AGameEvent {

        public IMortalModel ItemModel { get; private set; }

        public MortalItemDeathEvent(object source, IMortalModel itemModel)
            : base(source) {
            ItemModel = itemModel;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

