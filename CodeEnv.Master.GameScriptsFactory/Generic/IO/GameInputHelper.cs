// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameInputHelper.cs
// Singleton helper class for determining the state of Mouse controls
// using Ngui's default mouse input values. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton helper class for determining the state of Mouse controls using Ngui's 
/// default mouse input values. These input values are different than Unitys.
/// </summary>
public class GameInputHelper : AGenericSingleton<GameInputHelper>, IGameInputHelper, IDisposable {

    private GameInputHelper() {
        Initialize();
    }

    protected override void Initialize() { }

    /// <summary>
    /// Gets the NguiMouseButton that is being used to generate the current event.
    /// Valid only within an Ngui UICamera-generated event.
    /// </summary>
    /// <returns></returns>
    public NguiMouseButton CurrentMouseButton {
        get {
            int currentMouseButton = UICamera.currentTouchID;
            ValidateCurrentTouchID(currentMouseButton);
            return (NguiMouseButton)currentMouseButton;
        }
    }

    /// <summary>
    /// Tests whether the designated mouse button is the current button that is being
    /// used to generate the current event. Valid only within an Ngui UICamera-generated event.
    /// </summary>
    /// <param name="mouseButton">The mouse button.</param>
    /// <returns>
    ///   <c>true</c> if the mouseButton is the one that was used to generate the current event; otherwise, <c>false</c>.
    /// </returns>
    public bool IsMouseButton(NguiMouseButton mouseButton) { return mouseButton == CurrentMouseButton; }

    [System.Diagnostics.Conditional("DEBUG")]
    private void ValidateCurrentTouchID(int currentTouchID) {
        Arguments.ValidateForRange(currentTouchID, -3, -1);
    }

    /// <summary>
    /// Tests whether the left mouse button is the current button that is being
    /// used to generate this event. Valid only within an Ngui UICamera-generated event.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if the left mouseButton is the one that was used to generate the current event; otherwise, <c>false</c>.
    /// </returns>
    public bool IsLeftMouseButton { get { return IsMouseButton(NguiMouseButton.Left); } }

    /// <summary>
    /// Tests whether the right mouse button is the current button that is being
    /// used to generate this event. Valid only within an Ngui UICamera-generated event.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if the right mouseButton is the one that was used to generate the current event; otherwise, <c>false</c>.
    /// </returns>
    public bool IsRightMouseButton { get { return IsMouseButton(NguiMouseButton.Right); } }

    /// <summary>
    /// Tests whether the middle mouse button is the current button that is being
    /// used to generate this event. Valid only within an Ngui UICamera-generated event.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if the middle mouseButton is the one that was used to generate the current event; otherwise, <c>false</c>.
    /// </returns>
    public bool IsMiddleMouseButton { get { return IsMouseButton(NguiMouseButton.Middle); } }

    /// <summary>
    /// Detects whether a mouse button is being held down across multiple frames.
    /// </summary>
    /// <param name="mouseButton">The mouse button.</param>
    /// <returns>
    ///   <c>true</c> if the mouse button is being held down; otherwise, <c>false</c>.
    /// </returns>
    public bool IsMouseButtonDown(NguiMouseButton mouseButton) {
        return Input.GetMouseButton(mouseButton.ToUnityMouseButton());
    }

    /// <summary>
    /// Detects whether any mouse button is being held down across multiple frames.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if any mouse button is being held down; otherwise, <c>false</c>.
    /// </returns>
    public bool IsAnyMouseButtonDown {
        get {
            return IsMouseButtonDown(NguiMouseButton.Left) ||
                    IsMouseButtonDown(NguiMouseButton.Right) ||
                    IsMouseButtonDown(NguiMouseButton.Middle);
        }
    }

    /// <summary>
    /// Detects whether any mouse button besides the one provided is being held down across multiple frames.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if any other mouse button is being held down; otherwise, <c>false</c>.
    /// </returns>
    public bool IsAnyMouseButtonDownBesides(NguiMouseButton mouseButton) {
        foreach (NguiMouseButton button in Enums<NguiMouseButton>.GetValues().Except(NguiMouseButton.None)) {
            if (button != mouseButton) {
                if (IsMouseButtonDown(button)) {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsAnyKeyOrMouseButtonDown { get { return Input.anyKey; } }

    public bool IsHorizontalMouseMovement(out float value) {
        value = Input.GetAxis(UnityConstants.MouseAxisName_Horizontal);
        //D.Log("Mouse Horizontal Movement value = {0:0.0000}.", value);
        return value != 0F; // No floating point equality issues as value is smoothed by Unity
    }

    public bool IsVerticalMouseMovement(out float value) {
        value = Input.GetAxis(UnityConstants.MouseAxisName_Vertical);
        //D.Log("Mouse Vertical Movement value = {0:0.0000}.", value);
        return value != 0F; // No floating point equality issues as value is smoothed by Unity
    }

    public bool IsKeyHeldDown(KeyCode key) { return Input.GetKey(key); }

    /// <summary>
    /// Determines whether any of the specified keys are being held down.
    /// </summary>
    /// <param name="keyHeldDown">The key held down.</param>
    /// <param name="keys">The keys.</param>
    /// <returns></returns>
    public bool TryIsKeyHeldDown(out KeyCode keyHeldDown, params KeyCode[] keys) {
        keyHeldDown = KeyCode.None;
        foreach (var key in keys) {
            if (IsKeyHeldDown(key)) {
                keyHeldDown = key;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines whether this key has been pressed down during this frame.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public bool IsKeyDown(KeyCode key) { return Input.GetKeyDown(key); }

    /// <summary>
    /// Determines whether any of the specified keys were pressed down this frame.
    /// </summary>
    /// <param name="keyDown">The key down.</param>
    /// <param name="keys">The keys.</param>
    /// <returns></returns>
    public bool TryIsKeyDown(out KeyCode keyDown, params KeyCode[] keys) {
        keyDown = KeyCode.None;
        foreach (var key in keys) {
            if (IsKeyDown(key)) {
                keyDown = key;
                return true;
            }
        }
        return false;
    }

    private bool _isNotifying;
    private GameObject __previousGo;

    /// <summary>
    /// Generic notification function. Used in place of SendMessage to shorten the code and allow for more than one receiver.
    /// Derived from Ngui's UICamera.Notify() as sometimes UICamera.Notify was busy sending a previous message.
    /// </summary>
    /// <param name="go">The GameObject to notify.</param>
    /// <param name="methodName">Name of the method to call.</param>
    /// <param name="obj">Optional parameter associated with the method.</param>
    public void Notify(GameObject go, string methodName, object obj = null) {
        if (_isNotifying) {
            D.Error("Notify called when not yet finished from previous call. \nPreviousGO = {0}, NewGO = {1}.", __previousGo.name, go.name);
            return; // This should not happen. See http://answers.unity3d.com/questions/672269/is-sendmessage-immediate-or-not.html
        }
        _isNotifying = true;

        if (NGUITools.GetActive(go)) {
            __previousGo = go;
            go.SendMessage(methodName, obj, SendMessageOptions.DontRequireReceiver);
        }
        _isNotifying = false;
    }

    private void Cleanup() {
        OnDispose();
        // other cleanup here including any tracking Gui2D elements
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
    private bool _alreadyDisposed = false;
    protected bool _isDisposing = false;

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
        if (_alreadyDisposed) {
            return;
        }

        _isDisposing = true;
        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        _alreadyDisposed = true;
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


