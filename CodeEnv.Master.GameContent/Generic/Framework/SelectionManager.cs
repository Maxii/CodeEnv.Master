// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectionManager.cs
// Singleton. Selection Manager that keeps track of what is selected in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.GameContent;

    /// <summary>
    /// Singleton. Selection Manager that keeps track of what is selected in the game. Only one item can be selected at a time.
    /// </summary>
    public class SelectionManager : AGenericSingleton<SelectionManager>, IDisposable {

        private ISelectable _currentSelection;
        public ISelectable CurrentSelection {
            get { return _currentSelection; }
            set { SetProperty<ISelectable>(ref _currentSelection, value, "CurrentSelection", CurrentSelectionPropChangedHandler, CurrentSelectionPropChangingHandler); }
        }

        private ISFXManager _sfxMgr;
        private IInputManager _inputMgr;

        private SelectionManager() {
            Initialize();
        }

        protected sealed override void Initialize() {
            //D.Log("{0}.Initialize() called.", DebugName);
            _inputMgr = GameReferences.InputManager;
            _sfxMgr = GameReferences.SFXManager;
            Subscribe();
        }

        private void Subscribe() {
            _inputMgr.unconsumedPress += UnconsumedPressEventHandler;
        }

        #region Events and Property Change Handlers

        private void UnconsumedPressEventHandler(object sender, EventArgs e) {
            if (GameReferences.InputHelper.IsLeftMouseButton) {
                CurrentSelection = null;
            }
        }

        private void CurrentSelectionPropChangingHandler(ISelectable newSelection) {
            if (CurrentSelection != null) {
                CurrentSelection.IsSelected = false;
            }
        }

        private void CurrentSelectionPropChangedHandler() {
            if (CurrentSelection != null) {
                _sfxMgr.PlaySFX(SfxClipID.Select);
            }
            else {
                _sfxMgr.PlaySFX(SfxClipID.Select);  //TODO play a different sound indicating selection cleared
                // Note: Hide() handled centrally here as ISelectable's don't know whether another item has been selected
                GameReferences.SelectedItemHudWindow.Hide();
            }
        }

        #endregion

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            if (GameUtility.CheckNotNullOrAlreadyDestroyed(_inputMgr)) {
                _inputMgr.unconsumedPress -= UnconsumedPressEventHandler;
            }
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
                CallOnDispose();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion


    }
}

