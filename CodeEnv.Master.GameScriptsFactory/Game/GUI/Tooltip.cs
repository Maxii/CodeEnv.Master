// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Tooltip.cs
// Singleton. My advanced Tooltip supporting multiple customized Tooltip Elements
// including the UITooltip-equivalent TextTooltipElement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. My advanced Tooltip supporting multiple customized Tooltip Elements
/// including the UITooltip-equivalent TextTooltipElement.
/// </summary>
public class Tooltip : AHud<Tooltip> {

    private Camera _uiCamera;

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.Tooltip = Instance;
    }

    protected override void AcquireReferences() {
        base.AcquireReferences();
        _uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
    }

    #endregion

    protected override void PositionPopup() {
        // Since the screen can be of different than expected size, we want to convert mouse coordinates to view space, then convert that to world position.

        var mouseScreenSpacePosition = Input.mousePosition; // mouse position in pixel coordinates (0,0) bottomLeft to (screenWidth, screenHeight) topRight
        //D.Log("Mouse.ScreenPositionInPixels = {0}, ScreenWidth = {1}, ScreenHeight = {2}.", mouseScreenSpacePosition, Screen.width, Screen.height);

        float mouseViewportSpacePositionX = Mathf.Clamp01(mouseScreenSpacePosition.x / Screen.width);
        float mouseViewportSpacePositionY = Mathf.Clamp01(mouseScreenSpacePosition.y / Screen.height);
        Vector3 mouseViewportSpacePosition = new Vector3(mouseViewportSpacePositionX, mouseViewportSpacePositionY);

        // The maximum on-screen size of the tooltip window
        Vector2 maxTooltipWindowViewportSpaceSize = new Vector2((float)_backgroundWidget.width / Screen.width, (float)_backgroundWidget.height / Screen.height);

        // Limit the tooltip to always be visible
        float maxTooltipViewportSpacePositionX = 1F - maxTooltipWindowViewportSpaceSize.x;
        float maxTooltipViewportSpacePositionY = maxTooltipWindowViewportSpaceSize.y;
        float tooltipViewportSpacePositionX = Mathf.Min(mouseViewportSpacePosition.x, maxTooltipViewportSpacePositionX);
        float tooltipViewportSpacePositionY = Mathf.Max(mouseViewportSpacePosition.y, maxTooltipViewportSpacePositionY);

        Vector3 tooltipViewportSpacePosition = new Vector3(tooltipViewportSpacePositionX, tooltipViewportSpacePositionY);
        //D.Log("{0}.ViewportSpacePosition = {1}.", GetType().Name, tooltipViewportSpacePosition);

        // Position the tooltip in world space
        Vector3 tooltipWorldSpacePosition = _uiCamera.ViewportToWorldPoint(tooltipViewportSpacePosition);
        transform.position = tooltipWorldSpacePosition;
    }

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        References.Tooltip = null;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

