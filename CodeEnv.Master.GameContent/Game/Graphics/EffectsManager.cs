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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// EffectsManager for Items.
    /// </summary>
    public class EffectsManager : IDisposable {

        protected IEffectsMgrClient _effectsClient;
        protected IGeneralFactory _generalFactory;
        protected IMyPoolManager _myPoolMgr;
        protected IGameManager _gameMgr;
        protected IList<IDisposable> _subscriptions;

        public EffectsManager(IEffectsMgrClient effectsClient) {
            _effectsClient = effectsClient;
            _generalFactory = References.GeneralFactory;
            _gameMgr = References.GameManager;
            _myPoolMgr = References.MyPoolManager;
            Subscribe();
        }

        protected virtual void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gMgr => gMgr.IsPaused, IsPausedPropChangedHandler));
        }

        /// <summary>
        /// Starts the effect(s) associated with <c>effectID</c>. This default
        /// version does nothing except complete the handshake by replying
        /// to the client with OnEffectFinished().
        /// </summary>
        /// <param name="effectSeqID">The effect identifier.</param>
        public virtual void StartEffect(EffectSequenceID effectSeqID) {
            //D.Log("{0}.{1}.StartEffect({2}) called.", _effectsClient.FullName, typeof(EffectsManager).Name, effectSeqID.GetValueName());
            _effectsClient.HandleEffectSequenceFinished(effectSeqID);
        }

        /// <summary>
        /// Stops the effect(s) associated with <c>effectID</c>. 
        /// </summary>
        /// <param name="effectSeqID">The effect identifier.</param>
        public virtual void StopEffect(EffectSequenceID effectSeqID) {
            //TODO This default version does nothing.
        }

        #region Event and Property Change Handlers

        protected virtual void IsPausedPropChangedHandler() { }

        #endregion

        protected virtual void Cleanup() {
            Unsubscribe();
        }

        protected virtual void Unsubscribe() {
            _subscriptions.ForAll(s => s.Dispose());
            _subscriptions.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable

        private bool _alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {

            Dispose(true);

            // This object is being cleaned up by you explicitly calling Dispose() so take this object off
            // the finalization queue and prevent finalization code from 'disposing' a second time
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isExplicitlyDisposing) {
            if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion

    }
}

