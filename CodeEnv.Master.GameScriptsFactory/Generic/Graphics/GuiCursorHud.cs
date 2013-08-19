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

using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// HUD that follows the Cursor on the screen.
/// </summary>
public sealed class GuiCursorHud : AGuiHud<GuiCursorHud>, IGuiCursorHud {

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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IGuiCursorHud Members

    public void Set(GuiCursorHudText guiCursorHudText) {
        Set(guiCursorHudText.GetText());
    }

    #endregion
}

