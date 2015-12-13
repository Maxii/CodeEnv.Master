// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyCtxPickHandler.cs
// My modified CtxPickHandler that avoids opening the menu when dragging over the same object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using UnityEngine;
using CodeEnv.Master.GameContent;

/// <summary>
/// My modified CtxPickHandler that avoids opening the menu when dragging over the same object. 
/// </summary>
public class MyCtxPickHandler : MonoBehaviour {

    #region Public Variables

    /// <summary>
    /// The layer mask which will be used to filter ray casts for the pick handler.
    /// If this is 0 then the default ray cast filtering will be used.
    /// </summary>
    public int pickLayers = 0;  // My change to default to the Default layer

    /// <summary>
    /// The mouse button used to trigger the display of the context menu.
    /// Used only for Windows/Mac standalone and Web player.
    /// </summary>
    public int menuButton = 1;  // My change to default to the right mouse button

    /// <summary>
    /// If this flag is clear then this object will make itself the fall through
    /// event receiver for the NGUI UICamera on startup. Otherwise mouse/touch
    /// events will need to come from your code.
    /// </summary>
    public bool dontUseFallThrough = true;  // My change to default to NOT making this the Ngui fallthrough event handler

    #endregion

    #region Private Variables

    private CtxObject tracking;
    // private CtxObject lastTracked = null; // my change: lastTracked not used

    #endregion

    #region Event Handling

    void Start() {
        // We rely on NGUI to send us the events it doesn't handle. This
        // allows us to avoid having to check UI hits before processing
        // an event. However, if other code is using the fall through then
        // other arrangements will need to be made.
        if (!dontUseFallThrough)
            UICamera.fallThrough = gameObject;
    }

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
                //if (lastTracked != null) {    // my change: lastTracked not used
                //    //lastTracked.HideMenu();
                //    lastTracked = null;
                //}

                tracking = Pick(Input.mousePosition);
            }
        }
        else {
            if (tracking) {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WEBPLAYER
                if (Input.GetMouseButtonUp(menuButton))
#endif
                {
                    CtxObject picked = Pick(Input.mousePosition);
                    if (tracking == picked) {
                        // if (!GameInput.IsDragging) { // My addition to avoid opening menu when dragging over same object
                        tracking.ShowMenu();
                        //lastTracked = tracking;   // my change: lastTracked not used
                        //}
                    }
                }

                tracking = null;
            }
        }
    }

    #endregion

    #region Private Functions

    CtxObject Pick(Vector3 mousePos) {
        Camera cam = camera;
        if (cam == null)
            cam = Camera.main;  // my change from deprecated mainCamera

        Ray ray = cam.ScreenPointToRay(mousePos);

        RaycastHit hit = new RaycastHit();
        int layerMask = (pickLayers != 0) ? pickLayers : Physics.kDefaultRaycastLayers;

        if (Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask)) {
            return hit.collider.gameObject.GetComponent<CtxObject>();
        }

        return null;
    }

    #endregion

}

