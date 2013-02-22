// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPauseButton.cs
// Custom Gui button control for the main User Pause Button.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using UnityEngine;

/// <summary>
/// Custom Gui button control for the main User Pause Button.
/// </summary>
public class GuiPauseButton : GuiPauseResumeOnClick, IDisposable {

#pragma warning disable
    [SerializeField]
    private string Warning = "Do not change GuiPauseCommand public variable.";
#pragma warning restore

    private UILabel pauseButtonLabel;

    protected override void Initialize() {
        base.Initialize();
        pauseButtonLabel = button.GetComponentInChildren<UILabel>();
        UpdateButtonLabel();
        eventMgr.AddListener<PauseGameEvent>(OnPauseGame);
        tooltip = "Pause or resume the game.";
    }

    // real game pause and resumption events, not just gui pause events which may or may not result in a pause or resumption
    private void OnPauseGame(PauseGameEvent e) {
        pauseCommand = e.PauseCmd == PauseGameCommand.Pause ? GuiPauseCommand.UserPause : GuiPauseCommand.UserResume;
        UpdateButtonLabel();
    }

    protected override void OnButtonClick(GameObject sender) {
        // toggle the pauseCommand so the base class sends the correct GuiPauseCommand in the GuiPauseEvent
        pauseCommand = (pauseCommand == GuiPauseCommand.UserPause) ? GuiPauseCommand.UserResume : GuiPauseCommand.UserPause;
        base.OnButtonClick(sender);
    }

    private void UpdateButtonLabel() {
        pauseButtonLabel.text = (pauseCommand == GuiPauseCommand.UserPause) ? UIMessages.ResumeButtonLabel : UIMessages.PauseButtonLabel;
    }

    #region IDisposable
    [NonSerialized]
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
            eventMgr.RemoveListener<PauseGameEvent>(OnPauseGame);
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


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

