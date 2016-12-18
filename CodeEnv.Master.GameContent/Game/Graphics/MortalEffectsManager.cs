// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// EffectsManager for MortalItems.
    /// </summary>
    public class MortalEffectsManager : EffectsManager {

        private bool IsExplosionEffectPlaying { get { return _explosionEffect != null && _explosionEffect.IsPlaying; } }

        private IEffect _explosionEffect;

        public MortalEffectsManager(IEffectsMgrClient effectsClient) // IMPROVE this could be IMortalEffectsClient w/Position, Radius and DisplayMgr
            : base(effectsClient) { }

        public override void StartEffect(EffectSequenceID effectSeqID) {
            //D.Log("{0}.{1}.StartEffect({2}) called.", _effectsClient.DebugName, typeof(MortalEffectsManager).Name, effectSeqID.GetValueName());
            if (effectSeqID == EffectSequenceID.Dying) {
                // separate explosionSFXGo from ItemGo so destruction of ItemGo does not destroy explosionSFX before it is completed
                GameObject explosionSFXGo = _generalFactory.MakeAutoDestruct3DAudioSFXInstance("ExplosionSFX", _effectsClient.Position);
                References.SFXManager.PlaySFX(explosionSFXGo, SfxGroupID.Explosions);

                _explosionEffect = _myPoolMgr.Spawn(EffectID.Explosion, _effectsClient.Position);
                _explosionEffect.effectFinishedOneShot += (source, args) => {
                    _effectsClient.HandleEffectSequenceFinished(effectSeqID);
                };
                _explosionEffect.Play(_effectsClient.Radius);
                return;
            }
            base.StartEffect(effectSeqID); // currently just calls HandleEffectFinished
        }

        #region Event and Prop Change Handlers

        protected override void IsPausedPropChangedHandler() {
            base.IsPausedPropChangedHandler();
            if (IsExplosionEffectPlaying) {
                _explosionEffect.IsPaused = _gameMgr.IsPaused;
            }
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

