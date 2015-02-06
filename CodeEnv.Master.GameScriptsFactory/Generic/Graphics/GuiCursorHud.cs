// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCursorHud.cs
// Singleton HUD that displays Item data where instructed, typically next to the cursor.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton HUD that displays Item data where instructed, typically
/// next to the cursor.
/// </summary>
public class GuiCursorHud : AHud<GuiCursorHud>, IGuiHud {

    /// <summary>
    /// The location in ViewPort space (0-1.0, 0-1.0) of the transform of this Hud.
    /// </summary>
    private Vector2 _viewPortLocation;
    private Vector2 _labelOffset = new Vector2(15F, 0f);    // Transform.localPosition is in pixels when Ngui.UIRoot is in PixelPerfect mode

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeHudManagers();
        UpdateRate = FrameUpdateFrequency.Frequent;
    }

    private void InitializeHudManagers() {
        //AGuiHudPublisher.GuiCursorHud = Instance;
        //GuiHudPublisher<SectorData>.TextFactory = SectorGuiHudTextFactory.Instance;
        //GuiHudPublisher<ShipData>.TextFactory = ShipGuiHudTextFactory.Instance;
        //GuiHudPublisher<FleetCmdData>.TextFactory = FleetGuiHudTextFactory.Instance;
        //GuiHudPublisher<StarData>.TextFactory = StarGuiHudTextFactory.Instance;
        //GuiHudPublisher<SettlementCmdData>.TextFactory = SettlementGuiHudTextFactory.Instance;
        //GuiHudPublisher<FacilityData>.TextFactory = FacilityGuiHudTextFactory.Instance;
        //GuiHudPublisher<StarbaseCmdData>.TextFactory = StarbaseGuiHudTextFactory.Instance;
        //GuiHudPublisher<UniverseCenterData>.TextFactory = UniverseCenterGuiHudTextFactory.Instance;
        //GuiHudPublisher<PlanetData>.TextFactory = PlanetGuiHudTextFactory.Instance;
        //GuiHudPublisher<MoonData>.TextFactory = MoonGuiHudTextFactory.Instance;
        //GuiHudPublisher<SystemData>.TextFactory = SystemGuiHudTextFactory.Instance;
        //GuiHudPublisher<ItemData>.TextFactory = GuiHudTextFactory.Instance;

        AHudManager.CursorHud = Instance;
    }

    /// <summary>
    /// Position the HUD on the screen at this world <c>position</c>.
    /// IMPROVE Currently called indirectly by HudPublisher whenever the text changes.
    /// Is this sufficient, or does it need to be refreshed on Update?
    /// </summary>
    /// <param name="position">The position.</param>
    private void PositionHud(Vector3 position) {
        _viewPortLocation = Camera.main.WorldToViewportPoint(position);
        Vector3 hudPosition = uiCamera.ViewportToWorldPoint(_viewPortLocation);
        hudPosition.z = 0F;
        _transform.position = hudPosition;
    }

    /// <summary>
    /// Positions the label so its text is always on the screen. Derived from UITooltip.
    /// </summary>
    private void PositionLabel() {

        /* Per Aren, UILabel.printedSize returns the size of the label in pixels. This value is not the final size on the screen, 
                * but is rather in NGUI's virtual pixels. If the UIRoot is set to PixelPerfect, then this value will match the size in screen pixels. 
                * If the UIRoot is FixedSize, then you need to scale the printedSize by UIRoot.pixelSizeAdjustment.
                */
        Vector2 textSizeInPixels = _label.printedSize * UIRoot.GetPixelSizeAdjustment(gameObject);

        // UNCLEAR how camera orthographicSize should fit here if at all
        // Assumes the scale of the various text-related transforms is never anything but 1.0

        // adjust the pixel size of the text for any padding from the background    // Per Aren, Sliced Sprites generally have a border
        if (_labelBackground != null) {
            Vector4 border = _labelBackground.border;
            // border: x = left, y = bottom, z = right, w = top
            textSizeInPixels.x += border.x + border.z;
            textSizeInPixels.y += border.y + border.w;
        }

        // Calculate the on-screen viewport size of the HUD text
        Vector2 textViewportSize = new Vector2(textSizeInPixels.x / Screen.width, textSizeInPixels.y / Screen.height);
        //D.Log("TextViewportSize = {0}, TextSizeInPixels = {1}.", textViewportSize, textSizeInPixels);


        UIWidget.Pivot pivot = UIWidget.Pivot.Left; // place on right side
        Vector2 labelOffset = _labelOffset;
        Vector2 labelViewportOffset = new Vector2(_labelOffset.x / Screen.width, _labelOffset.y / Screen.height);
        // IMPROVE labelOffset and border values overlap as labelOffset affects the label location, not the background location
        if (_viewPortLocation.x + textViewportSize.x + labelViewportOffset.x < 1.0F) {
            // fits on the right
            if (_viewPortLocation.y - textViewportSize.y + labelViewportOffset.y > Constants.ZeroF) {
                // fits bottom right
                pivot = UIWidget.Pivot.TopLeft;
            }
            else if (_viewPortLocation.y + textViewportSize.y + labelViewportOffset.y < 1.0F) {
                // fits top right
                pivot = UIWidget.Pivot.BottomLeft;
            }
        }
        else {
            pivot = UIWidget.Pivot.Right;
            labelOffset = -labelOffset;
            labelViewportOffset = -labelViewportOffset;
            if (_viewPortLocation.x - textViewportSize.x + labelViewportOffset.x > Constants.ZeroF) {
                // fits on the left
                if (_viewPortLocation.y - textViewportSize.y + labelViewportOffset.y > Constants.ZeroF) {
                    // fits bottom left
                    pivot = UIWidget.Pivot.TopRight;
                }
                else if (_viewPortLocation.y + textViewportSize.y + labelViewportOffset.y < 1.0F) {
                    // fits top left
                    pivot = UIWidget.Pivot.BottomRight;
                }
            }
        }

        SetLabelPivot(pivot);
        SetLabelOffset(labelOffset);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IGuiHud Members

    /// <summary>
    /// Populate the HUD with text pulled from LabelText. This method only needs to be called
    /// when the content of labelText changes.
    /// </summary>
    /// <param name="labelText">The label text.</param>
    /// <param name="position">The position of the GameObject this HUD info represents.</param>
    public void Set(ALabelText labelText, Vector3 position) {
        if (Utility.CheckForContent(_label.text) && !labelText.IsChanged) {
            // the hud already has text and this new submission has no changes so no reason to proceed
            D.Warn("{0} attempted to update its text when not needed./nHud content: [{1}].", GetType().Name, _label.text);
            return;
        }
        PositionHud(position);
        Set(labelText.GetText());
        PositionLabel();
    }

    public void Set(string text, Vector3 position) {
        PositionHud(position);
        Set(text);
        PositionLabel();
    }

    #endregion

}

