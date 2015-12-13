// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EffectsManager.cs
// EffectsManager for Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// EffectsManager for Items.
    /// </summary>
    public class EffectsManager {

        protected IEffectsClient _effectsClient;
        protected IGeneralFactory _generalFactory;

        public EffectsManager(IEffectsClient effectsClient) {
            _effectsClient = effectsClient;
            _generalFactory = References.GeneralFactory;
        }

        /// <summary>
        /// Starts the effect(s) associated with <c>effectID</c>. This default
        /// version does nothing except complete the handshake by replying
        /// to the client with OnEffectFinished().
        /// </summary>
        /// <param name="effectID">The effect identifier.</param>
        public virtual void StartEffect(EffectID effectID) {
            _effectsClient.HandleEffectFinished(effectID);
        }

        /// <summary>
        /// Stops the effect(s) associated with <c>effectID</c>. 
        /// </summary>
        /// <param name="effectID">The effect identifier.</param>
        public virtual void StopEffect(EffectID effectID) {
            //TODO This default version does nothing.
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

