// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NguiGenericEventHandler.cs
// Class that catches all Ngui events independant of whether they are consumed
// by other Gui and Game elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class that catches all Ngui events independant of whether they are consumed
/// by other Gui and Game elements.
/// </summary>
public class NguiGenericEventHandler : AMonoBehaviourBaseSingleton<NguiGenericEventHandler> {

    // Note: Bug - UICamera.isDragging returns false on OnPress(false) when the drag started and ended over the same object.

    public bool LogEvents = false;

    private GameInput _gameInput;
    private PlayerViews _playerViews;

    protected override void Awake() {
        base.Awake();
        if (Camera.main == null) {
            // no main camera so no one to talk too
            Destroy(gameObject);
            return;
        }
        _gameInput = GameInput.Instance;
        _playerViews = PlayerViews.Instance;
        AssignEventHandler();
    }

    private void AssignEventHandler() {
        UICamera.genericEventHandler = gameObject;
    }

    // In general, determine WHEN (and where if somewhere else besides GameInput)
    // to send events here. Determine what to do with those events at the destination.

    /// <summary>
    /// Called when the mouse hovers over a gameobject.
    /// WARNING: Ngui sends out OnHover(false) events to cleanup after OnHover(true), BUT
    /// the hoveredObject is no longer the object that it was with OnHover(true).
    /// Instead, it is the GenericEventHandler. Upshot is that handling OnHover here
    /// is difficult. Ngui makes it easy if it is handled on the gameobject itself.
    /// </summary>
    /// <param name="isOver">if set to <c>true</c> [is over].</param>
    void OnHover(bool isOver) {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage(isOver.ToString());
    }

    void OnPress(bool isDown) {
        if (IsEventFrom(Layers.Gui2D)) {
            return; // FIXME OnDragEnd won't be considered if the drag ends over the Gui layer because GuiLayer events are ignored.
        }
        WriteMessage(isDown.ToString());
        if (!isDown) {
            if (UICamera.isDragging) {
                return; // if press is released and UICamera.isDragging flag is set, then this is a dummy event 
                // sent by Ngui to animate buttons and is NOT the potential end of a drag
            }
            // Deletes any GameInput drag values accumulated from drags that haven't been used by the camera (wrong button, etc.)
            _gameInput.NotifyDragEnded();
        }
    }

    void OnSelect(bool selected) {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage(selected.ToString());
    }

    void OnClick() {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage();
        if (UICamera.hoveredObject == this) {
            _gameInput.OnClickOnNothing();
        }
    }

    void OnDoubleClick() {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage();
    }

    void OnDrag(Vector2 delta) {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage(delta.ToString());
        // turn off the click notification that would normally occur once the drag is complete
        UICamera.currentTouch.clickNotification = UICamera.ClickNotification.None;
        _gameInput.RecordDrag(delta);
    }

    void OnDrop(GameObject go) {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage(go.name);
    }

    void OnInput(string text) {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage(text);
    }

    void OnTooltip(bool toShow) {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage(toShow.ToString());
    }

    void OnScroll(float delta) {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage(delta.ToString());
        _gameInput.RecordScrollWheelMovement(delta);
    }

    void OnKey(KeyCode key) {
        WriteMessage();
        // unused. Sends messages only for arrows and WASD?
    }

    private void WriteMessage(string arg = "") {
        if (LogEvents) {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            NguiMouseButton? button = Enums<NguiMouseButton>.CastOrNull(UICamera.currentTouchID);
            string touchID = (button ?? NguiMouseButton.None).GetName();
            string hoveredObject = UICamera.hoveredObject.name;
            string camera = UICamera.currentCamera.name;
            string screenPosition = UICamera.lastTouchPosition.ToString();
            UICamera.lastHit = new RaycastHit();    // clears any gameobject that was hit. Otherwise it is cached until the next hit
            string msg = @"{0}.{1}({2}) event. MouseButton = {3}, HoveredObject = {4}, Camera = {5}, ScreenPosition = {6}."
                .Inject(this.GetType().Name, stackFrame.GetMethod().Name, arg, touchID, hoveredObject, camera, screenPosition);
            Debug.Log(msg);
        }
    }

    /// <summary>
    /// Tests whether the event is from the provided Layer.
    /// </summary>
    /// <returns></returns>
    private bool IsEventFrom(Layers layer) {
        return UICamera.hoveredObject.layer == (int)layer;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

