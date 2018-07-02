// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectionManager.cs
// Singleton. Selection Manager that keeps track of what single Item is selected in the game. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Singleton. Selection Manager that keeps track of what single Item the User has selected in the game. 
    /// <remarks>8.2.17 Replaces deprecated MultiSelectionManager. The only circumstance multiple items can now be concurrently 'chosen' 
    /// is from the UnitHud - one or more listed user-owned Units can be chosen as can one or more of a single Unit's listed elements.</remarks>
    /// </summary>
    public class SelectionManager : AGenericSingleton<SelectionManager>, IDisposable {

        private ISelectable _currentSelection;
        /// <summary>
        /// The currently selected Item which can be null if no Item is selected.
        /// <remarks>Usage: An Item is selected by setting its IsSelected property to true. That Item then automatically populates
        /// this property with a reference to itself. An Item's deselection is initiated by assigning this CurrentSelection 
        /// property to some other value, including null. This manager then automatically DeSelect()s any previously selected Item.</remarks>
        /// </summary>
        public ISelectable CurrentSelection {
            get { return _currentSelection; }
            set { SetProperty<ISelectable>(ref _currentSelection, value, "CurrentSelection", CurrentSelectionPropChangedHandler, CurrentSelectionPropChangingHandler); }
        }

        private ISFXManager _sfxMgr;
        private IInputManager _inputMgr;
        private IGameInputHelper _inputHelper;
        private IGameManager _gameMgr;

        private SelectionManager() {
            Initialize();
        }

        protected sealed override void Initialize() {
            //D.Log("{0}.Initialize() called.", DebugName);
            _inputMgr = GameReferences.InputManager;
            _sfxMgr = GameReferences.SFXManager;
            _inputHelper = GameReferences.InputHelper;
            _gameMgr = GameReferences.GameManager;
            Subscribe();
        }

        private void Subscribe() {
            _inputMgr.unconsumedPress += UnconsumedPressEventHandler;
        }

        #region Events and Property Change Handlers

        private void CurrentSelectionPropChangingHandler(ISelectable incomingSelection) {
            if (CurrentSelection != null) {
                CurrentSelection.IsSelected = false;    // 8.2.17 ISelectables now auto hide the HUD they show in when DeSelect()ed
                if (incomingSelection != null) {
                    // 6.20.18 Request unpause to negate the additional pause request about to occur in ChangedHandler...
                    _gameMgr.RequestPauseStateChange(toPause: false);
                }
                // ...else ChangedHandler will request unpause so also doing it here would request undesirable double unpause 
            }
        }

        private void CurrentSelectionPropChangedHandler() {
            if (CurrentSelection != null) {
                _sfxMgr.PlaySFX(SfxClipID.Select);
                _gameMgr.RequestPauseStateChange(toPause: true);    // UNCLEAR why does a user selection of an item cause a pause
            }
            else {
                // 1.1.18 Occurs from UnconsumedPress and a SelectedItem losing its owner
                _sfxMgr.PlaySFX(SfxClipID.UnSelect);
                _gameMgr.RequestPauseStateChange(toPause: false);
            }
        }

        private void UnconsumedPressEventHandler(object sender, EventArgs e) {
            if (_inputHelper.IsLeftMouseButton) {
                //D.Log("{0} is deselecting any current selection due to an unconsumed press.", DebugName);
                if (_inputHelper.IsOverUI) {
                    // 1.1.18 Shouldn't happen as UI should block ability to select
                    D.Error("{0} has blocked deselecting any current selection as the click was over the UI.", DebugName);
                    return;
                }
                CurrentSelection = null;
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


