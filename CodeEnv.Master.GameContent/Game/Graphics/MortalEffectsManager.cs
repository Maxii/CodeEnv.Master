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
            //D.Log("{0}.{1}.StartEffect({2}) called.", _effectsClient.FullName, typeof(MortalEffectsManager).Name, effectID.GetValueName());
            if (effectID == EffectID.Dying) {
                _effectsClient.DisplayMgr.EnableDisplay(toEnable: false, isDead: true);

                // separate explosionSFXGo from ItemGo so destruction of ItemGo does not destroy explosionSFX before it is completed
                GameObject explosionSFXGo = _generalFactory.MakeAutoDestruct3DAudioSFXInstance("ExplosionSFX", _effectsClient.Position);
                References.SFXManager.PlaySFX(explosionSFXGo, SfxGroupID.Explosions);

                IExplosion_Pooled explosion = _generalFactory.SpawnExplosionInstance(_effectsClient.Position);
                explosion.explosionFinishedOneShot += (source, args) => {
                    _effectsClient.HandleEffectFinished(effectID);
                };
                explosion.Play(_effectsClient.Radius);
                return;
            }
            base.StartEffect(effectID); // currently just calls HandleEffectFinished
        }
        //public override void StartEffect(EffectID effectID) {
        //    //D.Log("{0}.{1}.StartEffect({2}) called.", _effectsClient.FullName, typeof(MortalEffectsManager).Name, effectID.GetValueName());
        //    if (effectID == EffectID.Dying) {
        //        _effectsClient.DisplayMgr.EnableDisplay(toEnable: false, isDead: true);

        //        // separate explosionSFXGo from ItemGo so destruction of ItemGo does not destroy explosionSFX before it is completed
        //        GameObject explosionSFXGo = _generalFactory.MakeAutoDestruct3DAudioSFXInstance("ExplosionSFX", _effectsClient.Position);
        //        References.SFXManager.PlaySFX(explosionSFXGo, SfxGroupID.Explosions);

        //        var explosion = _generalFactory.MakeAutoDestructExplosionInstance(_effectsClient.Radius, _effectsClient.Position);
        //        explosion.Play(withChildren: true);
        //        WaitJobUtility.WaitForParticleSystemCompletion(explosion, includeChildren: true, waitFinished: delegate {
        //            //D.Log("{0}.{1} explosion particle system has completed.", _effectsClient.FullName, GetType().Name);
        //            _effectsClient.HandleEffectFinished(effectID);
        //        });
        //        return;
        //    }
        //    base.StartEffect(effectID); // currently just calls HandleEffectFinished
        //}

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

