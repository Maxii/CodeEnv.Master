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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// HUD that follows the Cursor on the screen.
/// </summary>
public class GuiCursorHud : AHud<GuiCursorHud>, IGuiHud, IDisposable {

    /// <summary>
    /// The location in ViewPort space (0-1.0, 0-1.0) of the transform of this Hud.
    /// </summary>
    private Vector2 _viewPortLocation;
    private Vector2 _labelOffset = new Vector2(15F, 0f);    // Transform.localPosition is in pixels when Ngui.UIRoot is in PixelPerfect mode

    private IList<IDisposable> _subscribers;
    private GameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        InitializeHudPublishers();
        _gameMgr = GameManager.Instance;
        Subscribe();
        UpdateRate = FrameUpdateFrequency.Frequent;
    }

    private void InitializeHudPublishers() {
        AGuiHudPublisher.GuiCursorHud = Instance;
        GuiHudPublisher<SectorData>.TextFactory = SectorGuiHudTextFactory.Instance;
        GuiHudPublisher<ShipData>.TextFactory = ShipGuiHudTextFactory.Instance;
        GuiHudPublisher<FleetCmdData>.TextFactory = FleetGuiHudTextFactory.Instance;
        GuiHudPublisher<StarData>.TextFactory = StarGuiHudTextFactory.Instance;
        GuiHudPublisher<SettlementCmdData>.TextFactory = SettlementGuiHudTextFactory.Instance;
        GuiHudPublisher<FacilityData>.TextFactory = FacilityGuiHudTextFactory.Instance;
        GuiHudPublisher<StarbaseCmdData>.TextFactory = StarbaseGuiHudTextFactory.Instance;
        GuiHudPublisher<UniverseCenterData>.TextFactory = UniverseCenterGuiHudTextFactory.Instance;
        GuiHudPublisher<PlanetData>.TextFactory = PlanetGuiHudTextFactory.Instance;
        GuiHudPublisher<MoonData>.TextFactory = MoonGuiHudTextFactory.Instance;
        GuiHudPublisher<SystemData>.TextFactory = SystemGuiHudTextFactory.Instance;
        //GuiHudPublisher<ItemData>.TextFactory = GuiHudTextFactory.Instance;
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
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

    // No reason to track cursor movement as UpdateHudPosition() is called anytime the text changes
    //protected override void OccasionalUpdate() {
    //    base.OccasionalUpdate();
    //    UpdateHudPosition();
    //}

    /// <summary>
    /// Move the HUD to track the cursor.
    /// </summary>
    //[System.Obsolete]
    //protected override void UpdateHudPosition() {
    //    if (NGUITools.GetActive(_label.gameObject)) {
    //        Vector3 cursorPosition = Input.mousePosition;

    //        if (uiCamera != null) {
    //            // Since the screen can be of different than expected size, we want to convert
    //            // mouse coordinates to view space, then convert that to world position.
    //            cursorPosition.x = Mathf.Clamp01(cursorPosition.x / Screen.width);
    //            cursorPosition.y = Mathf.Clamp01(cursorPosition.y / Screen.height);
    //            _transform.position = uiCamera.ViewportToWorldPoint(cursorPosition);
    //            _viewPortLocation = cursorPosition;
    //        }
    //        // Not needed as uiCamera, if not set in Editor, is found in AHud<>
    //        //else {
    //        //    // Simple calculation that assumes that the camera is of fixed size
    //        //    cursorPosition.x -= Screen.width * 0.5f;
    //        //    cursorPosition.y -= Screen.height * 0.5f;
    //        //}
    //    }
    //}

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

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IGuiHud Members

    public void Set(GuiHudText guiCursorHudText, Vector3 position) {
        if (!_label.text.IsNullOrEmpty() && !guiCursorHudText.IsDirty) {
            // the existing text hasn't been cleared and this new text has not changed so no reason to set it again
            // D.Warn("{0} is not dirty!", typeof(GuiHudText));
            return;
        }

        _viewPortLocation = Camera.main.WorldToViewportPoint(position);
        Vector3 hudPosition = uiCamera.ViewportToWorldPoint(_viewPortLocation);
        hudPosition.z = 0F;
        _transform.position = hudPosition;

        string text = guiCursorHudText.GetText().ToString();
        Set(text);
        PositionLabel();
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

