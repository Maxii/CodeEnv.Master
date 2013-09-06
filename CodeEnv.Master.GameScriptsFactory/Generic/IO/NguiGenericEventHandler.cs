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

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Class that catches all Ngui events independant of whether they are consumed
/// by other Gui and Game elements.
/// </summary>
public class NguiGenericEventHandler : AMonoBehaviourBaseSingleton<NguiGenericEventHandler> {

    // Note: Bug - UICamera.isDragging returns false on OnPress(false) when the drag started and ended over the same object.

    public bool LogEvents = false;

    private GameInput _gameInput;

    protected override void Start() {
        base.Start();
        _gameInput = CameraControl.gameInput;
        AssignEventHandler();
    }

    private void AssignEventHandler() {
        UICamera.genericEventHandler = gameObject;
    }

    // In general, determine WHEN (and where if somewhere else besides GameInput)
    // to send events here. Determine what to do with those events at the destination.

    void OnHover(bool isOver) {
        if (IsEventFromGuiLayer()) {
            return;
        }
        WriteMessage(isOver.ToString());
    }

    void OnPress(bool isDown) {
        if (IsEventFromGuiLayer()) {
            return;
        }
        WriteMessage(isDown.ToString());
        if (!isDown && UICamera.isDragging) {
            // Deletes any GameInput drag values accumulated from drags using buttons that the camera
            // doesn't use. FIXME Unfortunately, it won't be called if the wrong button drag started and ended
            // over the same object because UICamera.isDragging returns false (bug?) in that circumstance.
            // FIXME Also won't be called if the drag ends over the Gui layer because GuiLayer events are ignored.
            _gameInput.OnDragEnd();
        }
    }

    void OnSelect(bool selected) {
        if (IsEventFromGuiLayer()) {
            return;
        }
        WriteMessage(selected.ToString());
    }

    void OnClick() {
        if (IsEventFromGuiLayer()) {
            return;
        }
        WriteMessage();
    }

    void OnDoubleClick() {
        if (IsEventFromGuiLayer()) {
            return;
        }
        WriteMessage();
    }

    void OnDrag(Vector2 delta) {
        if (IsEventFromGuiLayer()) {
            return;
        }
        WriteMessage(delta.ToString());
        // turn off the click notification that would normally occur once the drag is complete
        UICamera.currentTouch.clickNotification = UICamera.ClickNotification.None;
        _gameInput.RecordDrag(delta);
    }

    void OnDrop(GameObject go) {
        if (IsEventFromGuiLayer()) {
            return;
        }
        WriteMessage(go.name);
    }

    void OnInput(string text) {
        if (IsEventFromGuiLayer()) {
            return;
        }
        WriteMessage(text);
    }

    void OnTooltip(bool toShow) {
        if (IsEventFromGuiLayer()) {
            return;
        }
        WriteMessage(toShow.ToString());
    }

    void OnScroll(float delta) {
        if (IsEventFromGuiLayer() || GameInputHelper.IsAnyKeyOrMouseButtonDown()) {
            return;
        }
        WriteMessage(delta.ToString());
        _gameInput.RecordScrollWheelMovement(delta);
    }

    void OnKey(KeyCode key) {
        // don't test for event on gui layer as I want all arrow key events to 
        // go to the camera whether the mouse is on the gui or not
        if (GameInputHelper.IsAnyMouseButtonDown()) {
            return;
        }
        WriteMessage();
        _gameInput.RecordKey(key);
    }

    private void WriteMessage(string arg = "") {
        if (LogEvents) {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            NguiMouseButton? button = Enums<NguiMouseButton>.CastOrNull(UICamera.currentTouchID);
            string touchID = (button ?? NguiMouseButton.None).GetName();
            string gameObjectHit = UICamera.hoveredObject.name;
            string camera = UICamera.currentCamera.name;
            string screenPosition = UICamera.lastTouchPosition.ToString();
            UICamera.lastHit = new RaycastHit();    // clears any gameobject that was hit. Otherwise it is cached until the next hit
            string msg = @"{0}.{1}({2}) event. MouseButton = {3}, GameObject hit = {4}, Camera = {5}, ScreenPosition = {6}."
                .Inject(this.GetType().Name, stackFrame.GetMethod().Name, arg, touchID, gameObjectHit, camera, screenPosition);
            Debug.Log(msg);
        }
    }

    /// <summary>
    /// Tests whether the event is on the Gui Layer and returns true if it is.
    /// </summary>
    /// <returns></returns>
    private bool IsEventFromGuiLayer() {
        bool isOnGuiLayer = UICamera.hoveredObject.layer == (int)Layers.Gui;
        return isOnGuiLayer;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

