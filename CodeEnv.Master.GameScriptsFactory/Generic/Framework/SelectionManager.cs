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

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Singleton. Selection Manager that keeps track of what is selected in the game. Only one item can be selected at a time.
/// </summary>
public class SelectionManager : AMonoBehaviourBaseSingleton<SelectionManager>, IDisposable {

    public ISelectable CurrentSelection { get; private set; }

    private GameEventManager _eventMgr;

    protected override void Awake() {
        base.Awake();
        _eventMgr = GameEventManager.Instance;
        Subscribe();
    }

    private void Subscribe() {
        _eventMgr.AddListener<SelectionEvent>(this, OnNewSelection);
        _eventMgr.AddListener<GameItemDestroyedEvent>(this, OnGameItemDestroyed);
    }

    private void OnNewSelection(SelectionEvent e) {
        ISelectable newSelection = e.Source as ISelectable;
        D.Assert(newSelection != null, "{0} received from {1} that is not {2}.".Inject(typeof(SelectionEvent).Name, e.Source.GetType().Name, typeof(ISelectable).Name));
        if (CurrentSelection != null) {
            CurrentSelection.IsSelected = false;
        }
        CurrentSelection = newSelection;
    }

    private void OnGameItemDestroyed(GameItemDestroyedEvent e) {
        ISelectable selectable = e.Source as ISelectable;
        if (selectable != null) {
            if (selectable == CurrentSelection) {
                // the current selection is being destroyed
                D.Assert(selectable.IsSelected, "{0} should be selected!".Inject(e.Source.GetType().Name));
                // selectable.IsSelected = false;   // no need as being destroyed. Also, SelectionReadout needs to test for IsSelected
                CurrentSelection = null;
            }
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Unsubscribe() {
        _eventMgr.RemoveListener<SelectionEvent>(this, OnNewSelection);
        _eventMgr.RemoveListener<GameItemDestroyedEvent>(this, OnGameItemDestroyed);
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
            Unsubscribe();
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

