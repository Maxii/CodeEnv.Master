// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MultiSelectionManager.cs
// Singleton. Selection Manager that keeps track of what is selected in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Singleton. Selection Manager that keeps track of what is selected in the game. 
    /// Allows multiple selections of the same type at the same time.
    /// <remarks>Reverted to use of original SelectionManager as it became clear that the ability to 'select' (aka chose)
    /// multiple instances of an item should only occur in the UnitHud by choosing multiple icons representing Units or a
    /// Unit's elements.</remarks>
    /// </summary>
    [Obsolete]
    public class MultiSelectionManager : AGenericSingleton<MultiSelectionManager>, IDisposable {

        private HashSet<ISelectable> _currentSelections;
        private ISFXManager _sfxMgr;
        private IInputManager _inputMgr;
        private IGameInputHelper _inputHelper;

        private MultiSelectionManager() {
            Initialize();
        }

        protected sealed override void Initialize() {
            //D.Log("{0}.Initialize() called.", DebugName);
            _inputMgr = GameReferences.InputManager;
            _sfxMgr = GameReferences.SFXManager;
            _inputHelper = GameReferences.InputHelper;
            _currentSelections = new HashSet<ISelectable>();
            Subscribe();
        }

        private void Subscribe() {
            _inputMgr.unconsumedPress += UnconsumedPressEventHandler;
        }

        /// <summary>
        /// Returns <c>true</c> if there is only a single item selected, <c>false</c> otherwise.
        /// </summary>
        /// <param name="selection">The returned selection.</param>
        /// <returns></returns>
        public bool TryGetSingleSelection(out ISelectable selection) {
            if (_currentSelections.Count == Constants.One) {
                selection = _currentSelections.First();
                return true;
            }
            selection = null;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if there are one or more items selected, <c>false</c> otherwise.
        /// </summary>
        /// <param name="selections">The returned selections.</param>
        /// <returns></returns>
        public bool TryGetSelections(out IEnumerable<ISelectable> selections) {
            selections = _currentSelections;
            return selections.Any();
        }

        /// <summary>
        /// Assigns a single selection, deselecting any other current selections.
        /// </summary>
        /// <param name="selection">The selection.</param>
        public void AssignSingleSelection(ISelectable selection) {
            D.AssertNotNull(selection);

            bool toAdd = true;
            ISelectable except = null;
            if (_currentSelections.Contains(selection)) {
                toAdd = false;
                except = selection;
            }

            UnselectAllSelections(except);
            if (toAdd) {
                bool isAdded = _currentSelections.Add(selection);
                D.Assert(isAdded);
            }
            selection.SetSelected(toSelect: true, showSelectedInHud: true);
        }

        /// <summary>
        /// Returns <c>true</c> if this SelectionManager is allowed to add the provided selection to the
        /// set of items presently selected, if any, <c>false</c> otherwise.
        /// <remarks>A selection can be added if there are no current selections or it is the same type as those already present.</remarks>
        /// <remarks>Throws an error if called without the multi-selection Cntl key held down or if the selection is already present.</remarks>
        /// </summary>
        /// <param name="selection">The provided selection.</param>
        /// <returns>
        /// </returns>
        public bool CanAddMultiSelection(ISelectable selection) {
            D.AssertNotNull(selection);
            D.Assert(_inputHelper.IsAnyKeyHeldDown(KeyCode.LeftControl, KeyCode.RightControl));
            D.Assert(!_currentSelections.Contains(selection));
            if (!_currentSelections.Any()) {
                return true;
            }
            return _currentSelections.First().GetType() == selection.GetType();
        }

        /// <summary>
        /// Adds the provided selection. 
        /// <remarks>Use in conjunction with CanAddMultiSelection as an error will be thrown if not allowed to add.</remarks>
        /// <remarks>Throws an error if called without the multi-selection Cntl key held down.</remarks>
        /// </summary>
        /// <param name="selection">The selection.</param>
        public void AddMultiSelection(ISelectable selection) {
            D.Assert(CanAddMultiSelection(selection));
            bool isAdded = _currentSelections.Add(selection);
            D.Assert(isAdded);

            bool showSelectedInHud = _currentSelections.Count == Constants.One;
            selection.SetSelected(toSelect: true, showSelectedInHud: showSelectedInHud);
            if (!showSelectedInHud) {
                selection.AssignedHud.Hide();
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the provided selection was removed from the set of current selections, <c>false</c> otherwise.
        /// </summary>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public void RemoveSelection(ISelectable selection) {
            D.AssertNotNull(selection);
            bool isRemoved = _currentSelections.Remove(selection);
            D.Assert(isRemoved);
            _sfxMgr.PlaySFX(SfxClipID.UnSelect);

            selection.SetSelected(toSelect: false, showSelectedInHud: false);

            if (_currentSelections.Count == Constants.One) {
                // With a single item still selected, it should show in the InteractableHUD
                _currentSelections.First().SetSelected(toSelect: true, showSelectedInHud: true);
            }
            else {
                selection.AssignedHud.Hide();
            }
        }

        private void UnselectAllSelections(ISelectable except = null) {
            if (_currentSelections.Any()) {
                IHudWindow assignedHud = null;
                if (except == null) {
                    foreach (var selection in _currentSelections) {
                        selection.SetSelected(toSelect: false, showSelectedInHud: false);
                        assignedHud = selection.AssignedHud;
                    }
                    _currentSelections.Clear();
                    _sfxMgr.PlaySFX(SfxClipID.UnSelect);
                }
                else {
                    bool anyRemoved = false;
                    var copy = new HashSet<ISelectable>(_currentSelections);
                    foreach (var selection in copy) {
                        assignedHud = selection.AssignedHud;
                        if (selection == except) {
                            continue;
                        }
                        _currentSelections.Remove(selection);
                        selection.SetSelected(toSelect: false, showSelectedInHud: false);
                        anyRemoved = true;
                    }
                    if (anyRemoved) {
                        _sfxMgr.PlaySFX(SfxClipID.UnSelect);
                    }
                }
                assignedHud.Hide();
            }
        }

        #region Events and Property Change Handlers

        private void UnconsumedPressEventHandler(object sender, EventArgs e) {
            if (GameReferences.InputHelper.IsLeftMouseButton) {
                //D.Log("{0} is de-selecting any current selection due to an unconsumed press.", DebugName);
                UnselectAllSelections();
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

