// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameInputHelper.cs
//  Singleton helper class for determining the state of Mouse controls
// using Ngui's default mouse input values. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Singleton helper class for determining the state of Mouse controls
/// using Ngui's default mouse input values. These input values are 
/// different than Unitys.
/// </summary>
public class GameInputHelper : AGenericSingleton<GameInputHelper>, IGameInputHelper {

    private GameInputHelper() {
        Initialize();
    }

    protected override void Initialize() {
        // TODO do any initialization here
    }

    /// <summary>
    /// Gets the NguiMouseButton that is being used to generate the current event.
    /// Valid only within an Ngui UICamera-generated event.
    /// </summary>
    /// <returns></returns>
    public NguiMouseButton GetMouseButton() {
        int currentMouseButton = UICamera.currentTouchID;
        ValidateCurrentTouchID(currentMouseButton);
        return (NguiMouseButton)currentMouseButton;
    }

    /// <summary>
    /// Tests whether the designated mouse button is the current button that is being
    /// used to generate the current event. Valid only within an Ngui UICamera-generated event.
    /// </summary>
    /// <param name="mouseButton">The mouse button.</param>
    /// <returns>
    ///   <c>true</c> if the mouseButton is the one that was used to generate the current event; otherwise, <c>false</c>.
    /// </returns>
    public bool IsMouseButton(NguiMouseButton mouseButton) {
        return mouseButton == GetMouseButton();
    }

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
    public bool IsLeftMouseButton() {
        return IsMouseButton(NguiMouseButton.Left);
    }

    /// <summary>
    /// Tests whether the right mouse button is the current button that is being
    /// used to generate this event. Valid only within an Ngui UICamera-generated event.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if the right mouseButton is the one that was used to generate the current event; otherwise, <c>false</c>.
    /// </returns>
    public bool IsRightMouseButton() {
        return IsMouseButton(NguiMouseButton.Right);
    }

    /// <summary>
    /// Tests whether the middle mouse button is the current button that is being
    /// used to generate this event. Valid only within an Ngui UICamera-generated event.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if the middle mouseButton is the one that was used to generate the current event; otherwise, <c>false</c>.
    /// </returns>
    public bool IsMiddleMouseButton() {
        return IsMouseButton(NguiMouseButton.Middle);
    }

    /// <summary>
    /// Detects whether a mouse button is being held down across multiple frames.
    /// </summary>
    /// <param name="mouseButton">The mouse button.</param>
    /// <returns>
    ///   <c>true</c> if the mouse button is being held down; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsMouseButtonDown(NguiMouseButton mouseButton) {
        return Input.GetMouseButton(mouseButton.ToUnityMouseButton());
    }

    /// <summary>
    /// Detects whether any mouse button is being held down across multiple frames.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if any mouse button is being held down; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsAnyMouseButtonDown() {
        return IsMouseButtonDown(NguiMouseButton.Left) || IsMouseButtonDown(NguiMouseButton.Right) || IsMouseButtonDown(NguiMouseButton.Middle);
    }

    /// <summary>
    /// Detects whether any mouse button besides the one provided is being held down across multiple frames.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if any other mouse button is being held down; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsAnyMouseButtonDownBesides(NguiMouseButton mouseButton) {
        foreach (NguiMouseButton button in Enums<NguiMouseButton>.GetValues().Except(NguiMouseButton.None)) {
            if (button != mouseButton) {
                if (IsMouseButtonDown(button)) {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsAnyKeyOrMouseButtonDown() {
        return Input.anyKey;
    }

    public static bool IsHorizontalMouseMovement(out float value) {
        value = Input.GetAxis(UnityConstants.MouseAxisName_Horizontal);
        D.Log("Mouse Horizontal Movement value = {0:0.0000}.", value);
        return value != 0F; // No floating point equality issues as value is smoothed by Unity
    }

    public static bool IsVerticalMouseMovement(out float value) {
        value = Input.GetAxis(UnityConstants.MouseAxisName_Vertical);
        D.Log("Mouse Vertical Movement value = {0:0.0000}.", value);
        return value != 0F; // No floating point equality issues as value is smoothed by Unity
    }

    public static bool IsKeyHeldDown(KeyCode key) {
        return Input.GetKey(key);
    }

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
    public static bool IsKeyDown(KeyCode key) {
        return Input.GetKeyDown(key);
    }

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

    static bool _isNotifying = false;

    /// <summary>
    /// Generic notification function. Used in place of SendMessage to shorten the code and allow for more than one receiver.
    /// Derived from Ngui's UICamera.Notify().
    /// </summary>
    static public void Notify(GameObject go, string funcName, object obj) {
        if (_isNotifying) {
            D.Error("Notify called when not yet finished from previous call.");
            return;
        }
        _isNotifying = true;

        if (NGUITools.GetActive(go)) {
            go.SendMessage(funcName, obj, SendMessageOptions.DontRequireReceiver);
        }
        _isNotifying = false;
    }


}


