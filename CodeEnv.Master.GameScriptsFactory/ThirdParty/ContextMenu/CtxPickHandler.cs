// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CtxPickHandler.cs
// A component which drives the picking process for the context menu objects in a scene.
// Derived from Troy Heere's Contextual with permission. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// A component which drives the picking process for the context menu objects
/// in a scene. Attach this component to the camera game object that is used
/// to render the objects for which you want context menus.
/// </summary>
/// <remarks>Derived from Troy Heere's Contextual with permission.</remarks>
public class CtxPickHandler : AMonoBase {

    /// <summary>
    /// The layer mask which will be used to filter ray casts for the pick handler.
    /// If this is 0 then the default ray cast filtering will be used.
    /// </summary>
    public int pickLayers = 1;

    /// <summary>
    /// The mouse button used to trigger the display of the context menu.
    /// Used only for Windows/Mac standalone and Web player. For typical Windows
    /// RightClick behaviour, set to 1.
    /// </summary>
    public int menuButton = 1;

    /// <summary>
    /// If this flag is clear then this object will make itself the fall through
    /// event receiver for the NGUI UICamera on startup. Otherwise mouse/touch
    /// events will need to come from your code.
    /// </summary>
    public bool dontUseFallThrough = false;

    protected override void Start() {
        base.Start();
        // We rely on NGUI to send us the events it doesn't handle. This
        // allows us to avoid having to check UI hits before processing
        // an event. However, if other code is using the fall through then
        // other arrangements will need to be made.
        if (!dontUseFallThrough) {
            UICamera.fallThrough = gameObject;
        }
    }

    private CtxObject _tracking;
    private CtxObject _lastTracked = null;


    #region Event and Property Change Handlers

    /// <summary>
    /// Handles the OnPress event. Normally this will come through NGUI as
    /// by default this component will register itself with UICamera as the
    /// fallthrough event handler. However, if you don't want this to
    /// be the fallthrough handler then set the dontUseFallThrough flag and
    /// call this function directly from your own event handlers.
    /// </summary>
    public void OnPress(bool isPressed) {
        if (isPressed) {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WEBPLAYER
            // For mouse platforms we go through the additional step of filtering
            // out the events that don't match our specified mouse button.
            if (Input.GetMouseButtonDown(menuButton))
#endif
            {
                if (_lastTracked != null) {
                    //_lastTracked.HideMenu();
                    _lastTracked = null;
                }

                _tracking = Pick(Input.mousePosition);
            }
        }
        else {
            if (_tracking) {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WEBPLAYER
                if (Input.GetMouseButtonUp(menuButton))
#endif
                {
                    CtxObject picked = Pick(Input.mousePosition);
                    if (_tracking == picked) {
                        _tracking.ShowMenu();
                        _lastTracked = _tracking;
                    }
                }
                _tracking = null;
            }
        }
    }

    #endregion

    private CtxObject Pick(Vector3 mousePos) {
        Camera cam = GetComponent<Camera>();
        if (cam == null) {
            cam = Camera.main;
        }

        Ray ray = cam.ScreenPointToRay(mousePos);

        RaycastHit hit = new RaycastHit();
        int layerMask = (pickLayers != 0) ? pickLayers : Physics.DefaultRaycastLayers;

        if (Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask)) {
            return hit.collider.gameObject.GetComponent<CtxObject>();
        }
        return null;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}
