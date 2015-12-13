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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Effects Manager for Unit Commands.
    /// </summary>
    public class UnitCmdEffectsManager : MortalEffectsManager {

        public UnitCmdEffectsManager(IEffectsClient effectsClient)
            : base(effectsClient) { }

        public override void StartEffect(EffectID effectID) {
            if (effectID == EffectID.Dying) {
                // turn off the Cmd's icon and highlight around the HQ Element
                _effectsClient.DisplayMgr.EnableDisplay(toEnable: false, isDead: true);

                // no effects for now 
            }
            base.StartEffect(effectID); // currently just calls HandleEffectFinished
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

