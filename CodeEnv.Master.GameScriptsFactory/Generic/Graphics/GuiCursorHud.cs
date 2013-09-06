// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCursorHud.cs
// HUD that follows the Cursor on the screen.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// HUD that follows the Cursor on the screen.
/// </summary>
public class GuiCursorHud : AHud<GuiCursorHud>, IGuiHud, IDisposable {

    private IList<IDisposable> _subscribers;
    private GameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, PauseState>(gm => gm.PauseState, OnPauseStateChanged));
    }

    private void OnPauseStateChanged() {
        switch (_gameMgr.PauseState) {
            case PauseState.Paused:
            case PauseState.NotPaused:
                EnableDisplay(true);
                break;
            case PauseState.GuiAutoPaused:
                EnableDisplay(false);
                break;
            case PauseState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_gameMgr.PauseState));
        }
    }

    private void EnableDisplay(bool toEnable) {
        if (!toEnable) {
            Clear();
            NGUITools.SetActive(_label.gameObject, false);
        }
        _isDisplayEnabled = toEnable;
    }


    /// <summary>
    /// Move the HUD to track the cursor.
    /// </summary>
    protected override void UpdatePosition() {
        base.UpdatePosition();
        if (NGUITools.GetActive(_label.gameObject)) {
            Vector3 cursorPosition = Input.mousePosition;

            if (uiCamera != null) {
                // Since the screen can be of different than expected size, we want to convert
                // mouse coordinates to view space, then convert that to world position.
                cursorPosition.x = Mathf.Clamp01(cursorPosition.x / Screen.width);
                cursorPosition.y = Mathf.Clamp01(cursorPosition.y / Screen.height);
                _transform.position = uiCamera.ViewportToWorldPoint(cursorPosition);
                // OPTIMIZE why not just use uiCamera.ScreenToWorldPoint(cursorPosition)?

                // For pixel-perfect results
                if (uiCamera.isOrthoGraphic) {
                    _transform.localPosition = NGUIMath.ApplyHalfPixelOffset(_transform.localPosition, _transform.localScale);
                }
            }
            else {
                // Simple calculation that assumes that the camera is of fixed size
                cursorPosition.x -= Screen.width * 0.5f;
                cursorPosition.y -= Screen.height * 0.5f;
                _transform.localPosition = NGUIMath.ApplyHalfPixelOffset(cursorPosition, _transform.localScale);
            }
        }
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IGuiCursorHud Members

    public void Set(GuiHudText guiCursorHudText) {
        Set(guiCursorHudText.GetText());
    }

    #endregion

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

