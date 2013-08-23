// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NguiFallthruEventHandler.cs
// Class that catches all Ngui events that are not consumed by other Gui and 
// Game elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Class that catches all Ngui events that are not consumed by other Gui and 
/// Game elements. Note: A gameobject just needs a collider to consume all Ngui
/// events sent its way. It does not need to implement an event's handler to consume it.
/// </summary>
public class NguiFallthruEventHandler : AMonoBehaviourBase {

    void Start() {
        InitializeOnStart();
    }

    protected virtual void InitializeOnStart() {
        UICamera.fallThrough = gameObject;
    }

    protected void OnHover(bool isOver) {
        WriteMessage(isOver.ToString());
    }

    protected void OnPress(bool isDown) {
        WriteMessage(isDown.ToString());
    }

    protected void OnSelect(bool selected) {
        WriteMessage(selected.ToString());
    }

    protected void OnClick() {
        WriteMessage();
    }

    protected void OnDoubleClick() {
        WriteMessage();
    }

    protected void OnDrag(Vector2 delta) {
        WriteMessage(delta.ToString());
    }

    protected void OnDrop(GameObject go) {
        WriteMessage(go.name);
    }

    protected void OnInput(string text) {
        WriteMessage(text);
    }

    protected void OnTooltip(bool toShow) {
        WriteMessage(toShow.ToString());
    }

    protected void OnScroll(float delta) {
        WriteMessage(delta.ToString());
    }

    protected void OnKey(KeyCode key) {
        WriteMessage(key.ToString());
    }

    private void WriteMessage(string arg = "") {
        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
        NguiMouseButton? button = Enums<NguiMouseButton>.CastOrNull(UICamera.currentTouchID);
        string touchID = (button ?? NguiMouseButton.None).GetName();
        string gameObjectHit = UICamera.hoveredObject.name;
        string camera = UICamera.currentCamera.name;
        string screenPosition = UICamera.lastTouchPosition.ToString();
        UICamera.lastHit = new RaycastHit();    // clears any gameobject that was hit. Otherwise it is cached until the next hit
        string msg = @"{0}.{1}({2}) event. MouseButton = {3}, GameObject hit = {4}, Camera = {5}, ScreenPosition = {6}."
            .Inject(this.GetType().Name, stackFrame.GetMethod().Name, arg, touchID, gameObjectHit, camera, screenPosition);
        WriteMessageToConsole(msg);
    }

    protected virtual void WriteMessageToConsole(string msg) {
        D.Warn(msg);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

