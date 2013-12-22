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
public class NguiGenericEventHandler : AMonoBaseSingleton<NguiGenericEventHandler> {

    // Note: Bug - UICamera.isDragging returns false on OnPress(false) when the drag started and ended over the same object.
    // still true after new Drag events???

    public bool LogEvents = false;

    private GameInput _gameInput;
    //private PlayerViews _playerViews;

    protected override void Awake() {
        base.Awake();
        if (Camera.main == null) {
            // no main camera so no one to talk too
            Destroy(gameObject);
            return;
        }
        _gameInput = GameInput.Instance;
        //_playerViews = PlayerViews.Instance;
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
        // my attempt to apply the unconsumed approach to hover failed as Ngui spams these calls
    }

    void OnPress(bool isDown) {
        if (IsEventFrom(Layers.Gui2D)) {
            return; // FIXME OnDragEnd won't be considered if the drag ends over the Gui layer because GuiLayer events are ignored.
        }
        WriteMessage(isDown.ToString());

        //if (UICamera.hoveredObject == gameObject) {
        //    // the target of the press is this generic event handler, so this press wasn't consumed by another gameobject
        //    _gameInput.RecordUnconsumedPress(isDown);
        //}
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

        if (UICamera.hoveredObject == gameObject) {
            // the target of the click is this generic event handler, so this click wasn't consumed by another gameobject
            _gameInput.RecordUnconsumedClick();
        }
    }

    void OnDoubleClick() {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage();
    }

    void OnDragStart() {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage();
        // turn off the click notification that would normally occur once the drag is complete
        UICamera.currentTouch.clickNotification = UICamera.ClickNotification.None;
        _gameInput.IsDragging = true;
    }

    void OnDrag(Vector2 delta) {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage(delta.ToString());

        _gameInput.RecordDrag(delta);
    }

    void OnDragOver(GameObject draggedObject) {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage(draggedObject.name);
    }

    void OnDragOut(GameObject draggedObject) {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage(draggedObject.name);
    }

    void OnDragEnd() {
        if (IsEventFrom(Layers.Gui2D)) {
            return;
        }
        WriteMessage();
        _gameInput.NotifyDragEnded();
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

