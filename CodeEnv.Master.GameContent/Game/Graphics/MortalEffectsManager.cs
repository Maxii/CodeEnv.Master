﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MortalEffectsManager.cs
// EffectsManager for MortalItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// EffectsManager for MortalItems.
    /// </summary>
    public class MortalEffectsManager : EffectsManager {

        public MortalEffectsManager(IEffectsClient effectsClient) // IMPROVE this could be IMortalEffectsClient w/Position, Radius and DisplayMgr
            : base(effectsClient) { }

        public override void StartEffect(EffectID effectID) {
            if (effectID == EffectID.Dying) {
                _effectsClient.DisplayMgr.EnableDisplay(toEnable: false, isDead: true);

                // separate explosionSFXGo from ItemGo so destruction of ItemGo does not destroy explosionSFX before it is completed
                GameObject explosionSFXGo = _generalFactory.MakeAutoDestruct3DAudioSFXInstance("ExplosionSFX", _effectsClient.Position);
                References.SFXManager.PlaySFX(explosionSFXGo, SfxGroupID.Explosions);

                var explosion = _generalFactory.MakeExplosionInstance(_effectsClient.Radius, _effectsClient.Position);
                explosion.Play(withChildren: true);
                WaitJobUtility.WaitForParticleSystemCompletion(explosion, includeChildren: true, onWaitFinished: delegate {
                    _effectsClient.HandleEffectFinished(effectID);
                });
                return;
            }
            base.StartEffect(effectID); // currently just calls HandleEffectFinished
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

