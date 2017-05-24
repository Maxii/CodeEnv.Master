// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitCmdEffectsManager.cs
// Effects Manager for Unit Commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Effects Manager for Unit Commands.
    /// </summary>
    public class UnitCmdEffectsManager : MortalEffectsManager {

        public UnitCmdEffectsManager(IEffectsMgrClient effectsClient)
            : base(effectsClient) { }

        public override void StartEffect(EffectSequenceID effectSeqID) {
            if (effectSeqID == EffectSequenceID.Dying) {
                // no effects for now 
            }
            base.StartEffect(effectSeqID); // currently just calls HandleEffectFinished
        }

    }
}

