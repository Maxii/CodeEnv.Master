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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

//namespace CodeEnv.Master.GameContent {

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
        set { SetProperty<ISelectable>(ref _currentSelection, value, "CurrentSelection", null, OnSelectionChanging); }
    }

    private IList<IDisposable> _subscribers;

    private SelectionManager() {
        Initialize();
    }

    protected override void Initialize() {
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameInput.Instance.SubscribeToPropertyChanged<GameInput, UnconsumedMouseButtonClick>(gi => gi.UnconsumedClick, OnUnconsumedMouseButtonClickChanged));
    }

    private void OnUnconsumedMouseButtonClickChanged() {
        if (GameInput.Instance.UnconsumedClick.MouseButton == NguiMouseButton.Left) {
            CurrentSelection = null;
        }
    }

    private void OnSelectionChanging(ISelectable newSelection) {
        if (CurrentSelection != null) {
            CurrentSelection.IsSelected = false;
        }
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion


}
//}

