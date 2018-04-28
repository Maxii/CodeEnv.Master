// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TooltipHudWindow.cs
//  Singleton. My advanced Tooltip window supporting multiple customized Forms
// including the UITooltip-equivalent TextForm.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Singleton. My advanced Tooltip window supporting multiple customized Forms
/// including the UITooltip-equivalent TextForm.
/// </summary>
public class TooltipHudWindow : AHudWindow<TooltipHudWindow>, ITooltipHudWindow {

    private Camera _uiCamera;

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        GameReferences.TooltipHudWindow = Instance;
    }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
        if (!_panel.widgetsAreStatic) {
            D.Warn("{0}: Can't UIPanel.widgetsAreStatic = true?", DebugName);
        }
    }

    #endregion

    public void Show(StringBuilder sb) {
        Show(sb.ToString());
    }

    public void Show(ColoredStringBuilder csb) {
        Show(csb.ToString());
    }

    public void Show(string text) {
        if (text.IsNullOrEmpty()) {
            return;
        }
        var form = PrepareForm(FormID.TextHud);
        (form as TextForm).Text = text;
        ShowForm(form);
    }

    public void Show(ResourceID resourceID) {
        var form = PrepareForm(FormID.ResourceTooltip);
        (form as ResourceTooltipForm).ResourceID = resourceID;
        ShowForm(form);
    }

    public void Show(AImageStat stat) {
        D.Warn("{0} has not yet implemented Show({1}).", DebugName, stat.DebugName);
    }

    /// <summary>
    /// Positions this tooltip window so it doesn't interfere with the widget the mouse is hovering over.
    /// </summary>
    protected override void PositionWindow() {
        var hoveredObject = UICamera.hoveredObject;
        if (hoveredObject != null) {

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
            var hoveredWidget = hoveredObject.GetComponent<UIWidget>();
            Profiler.EndSample();

            if (hoveredWidget != null) {
                var hoveredWidgetViewportPosition = _uiCamera.WorldToViewportPoint(hoveredWidget.worldCenter);
                //D.Log("{0}: Viewport position of Hovered Widget = {1}.", DebugName, hoveredWidgetViewportPosition);

                WidgetCorner cornerToAlignTo = PickCornerOfHoveredWidgetToAlignTooltip(hoveredWidgetViewportPosition);
                //D.Log("{0}: Hovered Widget corner to align to = {1}.", DebugName, cornerToAlignTo.GetValueName());
                Vector3 tooltipPosition = CalcTooltipWorldPosition(hoveredWidget, cornerToAlignTo);
                //D.Log("{0}: new world position: {1}.", DebugName, tooltipPosition);
                transform.position = tooltipPosition;
            }
        }
    }

    /// <summary>
    /// Returns the corner of the hovered widget that should be used to align the tooltip.
    /// The algorithm used selects the corner with the most screen space available to show the tooltip.
    /// </summary>
    /// <param name="hoveredWidgetViewportPosition">The hovered widget's position in viewport space (0..1, 0..1).</param>
    /// <returns></returns>
    private WidgetCorner PickCornerOfHoveredWidgetToAlignTooltip(Vector2 hoveredWidgetViewportPosition) {
        if (hoveredWidgetViewportPosition.x < 0.5F) {
            if (hoveredWidgetViewportPosition.y > 0.5F) {
                return WidgetCorner.BottomRight;    // widget located in top left quadrant of screen
            }
            return WidgetCorner.TopRight;   // widget located in bottom left quadrant of screen
        }
        else {
            if (hoveredWidgetViewportPosition.y > 0.5F) {
                return WidgetCorner.BottomLeft; // widget located in top right quadrant of screen
            }
            return WidgetCorner.TopLeft;    // widget located in bottom right quadrant of screen
        }
    }

    /// <summary>
    /// Calculates the world-space position the tooltip should move too.
    /// </summary>
    /// <param name="hoveredWidget">The widget the mouse is hovering over.</param>
    /// <param name="hoveredWidgetAlignmentCorner">The hovered widget corner chosen to align the tooltip too .</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private Vector3 CalcTooltipWorldPosition(UIWidget hoveredWidget, WidgetCorner hoveredWidgetAlignmentCorner) {
        Vector2 tooltipCornerToCenterViewportOffset;
        Vector2 tooltipViewportSize = new Vector2(_backgroundWidget.width / (float)Screen.width, _backgroundWidget.height / (float)Screen.height);

        switch (hoveredWidgetAlignmentCorner) {
            case WidgetCorner.BottomLeft:
                // the tooltip background widget's TopRight corner should align with the hovered widget's BottomLeft corner
                tooltipCornerToCenterViewportOffset = new Vector2(-tooltipViewportSize.x / 2, -tooltipViewportSize.y / 2);
                break;

            case WidgetCorner.TopLeft:
                // the tooltip background widget's BottomRight corner should align with the hovered widget's TopLeft corner
                tooltipCornerToCenterViewportOffset = new Vector2(-tooltipViewportSize.x / 2, tooltipViewportSize.y / 2);
                break;
            case WidgetCorner.TopRight:
                // the tooltip background widget's BottomLeft corner should align with the hovered widget's TopRight corner
                tooltipCornerToCenterViewportOffset = new Vector2(tooltipViewportSize.x / 2, tooltipViewportSize.y / 2);
                break;
            case WidgetCorner.BottomRight:
                // the tooltip background widget's TopLeft corner should align with the hovered widget's BottomRight corner
                tooltipCornerToCenterViewportOffset = new Vector2(tooltipViewportSize.x / 2, -tooltipViewportSize.y / 2);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hoveredWidgetAlignmentCorner));
        }
        //D.Log("{0}: BackgroundWidgetCornerToCenterOffset = {1}.", DebugName, tooltipCornerToCenterViewportOffset);

        Vector2 hoveredWidgetCornerViewportPosition = GetAlignmentCornerViewportPosition(hoveredWidget, hoveredWidgetAlignmentCorner);
        Vector2 desiredTooltipViewportPosition = hoveredWidgetCornerViewportPosition + tooltipCornerToCenterViewportOffset;
        return _uiCamera.ViewportToWorldPoint(desiredTooltipViewportPosition);
    }

    /// <summary>
    /// Gets the position in viewport space of the provided alignment corner for the hovered widget.
    /// </summary>
    /// <param name="hoveredWidget">The hovered widget.</param>
    /// <param name="alignmentCorner">The alignment corner.</param>
    /// <returns></returns>
    private Vector2 GetAlignmentCornerViewportPosition(UIWidget hoveredWidget, WidgetCorner alignmentCorner) {
        var cornerWorldPosition = hoveredWidget.worldCorners[(int)alignmentCorner];
        return _uiCamera.WorldToViewportPoint(cornerWorldPosition);
    }

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        GameReferences.TooltipHudWindow = null;
    }

    #endregion


    #region Nested Classes

    private enum WidgetCorner {
        BottomLeft = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 3
    }

    #endregion

    #region Keep Tooltip on Screen Archive

    //protected override void PositionWindow() {
    //    // Since the screen can be of different than expected size, we want to convert mouse coordinates to view space, then convert that to world position.

    //    var mouseScreenSpacePosition = Input.mousePosition; // mouse position in pixel coordinates (0,0) bottomLeft to (screenWidth, screenHeight) topRight
    //    //D.Log("Mouse.ScreenPositionInPixels = {0}, ScreenWidth = {1}, ScreenHeight = {2}.", mouseScreenSpacePosition, Screen.width, Screen.height);

    //    float mouseViewportSpacePositionX = Mathf.Clamp01(mouseScreenSpacePosition.x / Screen.width);
    //    float mouseViewportSpacePositionY = Mathf.Clamp01(mouseScreenSpacePosition.y / Screen.height);
    //    Vector3 mouseViewportSpacePosition = new Vector3(mouseViewportSpacePositionX, mouseViewportSpacePositionY);

    //    // The maximum on-screen size of the tooltip window
    //    Vector2 maxTooltipWindowViewportSpaceSize = new Vector2((float)_backgroundWidget.width / Screen.width, (float)_backgroundWidget.height / Screen.height);

    //    // Limit the tooltip to always be visible
    //    float maxTooltipViewportSpacePositionX = 1F - maxTooltipWindowViewportSpaceSize.x;
    //    float maxTooltipViewportSpacePositionY = maxTooltipWindowViewportSpaceSize.y;
    //    float tooltipViewportSpacePositionX = Mathf.Min(mouseViewportSpacePosition.x, maxTooltipViewportSpacePositionX);
    //    float tooltipViewportSpacePositionY = Mathf.Max(mouseViewportSpacePosition.y, maxTooltipViewportSpacePositionY);

    //    Vector3 tooltipViewportSpacePosition = new Vector3(tooltipViewportSpacePositionX, tooltipViewportSpacePositionY);
    //    //D.Log("{0}.ViewportSpacePosition = {1}.", GetType().Name, tooltipViewportSpacePosition);

    //    // Position the tooltip in world space
    //    Vector3 tooltipWorldSpacePosition = _uiCamera.ViewportToWorldPoint(tooltipViewportSpacePosition);
    //    transform.position = tooltipWorldSpacePosition;

    //    var widget = gameObject.GetComponentsInImmediateChildren<UIWidget>().First();
    //    D.Log("Tooltip local position = {0}, local center = {1}.", transform.localPosition, widget.localCenter);
    //    D.Log("Local center after TransformPoint = {0}.", widget.transform.TransformPoint(widget.localCenter));
    //}

    #endregion

}

