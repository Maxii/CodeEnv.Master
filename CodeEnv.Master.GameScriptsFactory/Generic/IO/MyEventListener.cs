// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyEventListener.cs
// Event Hook class lets you easily add remote event listener functions to an object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Event Hook class lets you easily add remote Event listener methods to a GameObject.
/// Example usage: 
/// Action-based: MyEventListener.Get(gameObject).onClick += MyClickFunction;
/// <remarks>
/// Derived from Ngui's UIEventListener to allow the addition of events and the IMyEventListener interface.
/// </remarks>
/// </summary>
public class MyEventListener : AMonoBase, IMyEventListener {

    /// <summary>
    /// Get or add an event listener to the specified game object.
    /// </summary>
    public static MyEventListener Get(GameObject go) {
        MyEventListener listener = go.GetComponent<MyEventListener>();
        if (listener == null) listener = go.AddComponent<MyEventListener>();
        return listener;
    }

    public event Action<GameObject> onSubmit;
    public event Action<GameObject> onClick;
    public event Action<GameObject> onDoubleClick;
    public event Action<GameObject, bool> onHover;
    public event Action<GameObject, bool> onPress;
    public event Action<GameObject, bool> onSelect;
    public event Action<GameObject, float> onScroll;
    public event Action<GameObject> onDragStart;
    public event Action<GameObject, Vector2> onDrag;
    public event Action<GameObject> onDragOver;
    public event Action<GameObject> onDragOut;
    public event Action<GameObject> onDragEnd;
    public event Action<GameObject, GameObject> onDrop;
    public event Action<GameObject, KeyCode> onKey;
    public event Action<GameObject, bool> onTooltip;

    public event Action<GameObject, Collider> onTriggerEnter;
    public event Action<GameObject, Collider> onTriggerExit;
    public event Action<GameObject, Collision> onCollisionEnter;
    public event Action<GameObject, Collision> onCollisionExit;

    void OnSubmit() { if (onSubmit != null) { onSubmit(gameObject); } }
    void OnClick() { if (onClick != null) { onClick(gameObject); } }
    void OnDoubleClick() { if (onDoubleClick != null) { onDoubleClick(gameObject); } }
    void OnHover(bool isOver) { if (onHover != null) { onHover(gameObject, isOver); } }
    void OnPress(bool isPressed) { if (onPress != null) { onPress(gameObject, isPressed); } }
    void OnSelect(bool selected) { if (onSelect != null) { onSelect(gameObject, selected); } }
    void OnScroll(float delta) { if (onScroll != null) { onScroll(gameObject, delta); } }
    void OnDragStart() { if (onDragStart != null) { onDragStart(gameObject); } }
    void OnDrag(Vector2 delta) { if (onDrag != null) { onDrag(gameObject, delta); } }
    void OnDragOver() { if (onDragOver != null) { onDragOver(gameObject); } }
    void OnDragOut() { if (onDragOut != null) { onDragOut(gameObject); } }
    void OnDragEnd() { if (onDragEnd != null) { onDragEnd(gameObject); } }
    void OnDrop(GameObject go) { if (onDrop != null) { onDrop(gameObject, go); } }
    void OnKey(KeyCode key) { if (onKey != null) { onKey(gameObject, key); } }
    void OnTooltip(bool show) { if (onTooltip != null) { onTooltip(gameObject, show); } }

    protected override void OnTriggerEnter(Collider other) { if (onTriggerEnter != null) { onTriggerEnter(gameObject, other); } }
    protected override void OnTriggerExit(Collider other) { if (onTriggerExit != null) { onTriggerExit(gameObject, other); } }
    protected override void OnCollisionEnter(Collision collision) { if (onCollisionEnter != null) { onCollisionEnter(gameObject, collision); } }
    protected override void OnCollisionExit(Collision collision) { if (onCollisionExit != null) { onCollisionExit(gameObject, collision); } }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

