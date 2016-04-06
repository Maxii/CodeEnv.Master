// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrbitalPlaneInputEventRouter.cs
// Routes Ngui mouse input events received by this orbital plane to either
// its parent SystemItem or an object currently occluded by the plane's collider. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Routes Ngui mouse input events received by this orbital plane to either
/// its parent SystemItem or an object currently occluded by the plane's collider. 
/// </summary>
public class OrbitalPlaneInputEventRouter : AMonoBase {

    private const string NameFormat = "{0}.{1}";

    public string Name { get { return NameFormat.Inject(_systemItemGo.name, GetType().Name); } }

    private bool IsSpawnOccludedObjectChecksJobRunning { get { return _spawnOccludedObjectChecksJob != null && _spawnOccludedObjectChecksJob.IsRunning; } }

    /// <summary>
    /// Flag indicating that this orbital plane has already received OnHover(true) without an intervening OnHover(false). 
    /// Used to filter out the duplicate OnHover(true) events which I don't expect Ngui to remedy.
    /// <remarks>Ngui sends a duplicate OnHover(true) everytime the gameObject is clicked. According to ArenMook, 
    /// this is intended to regain some unknown OnHover state that was overridden by the click. The original 
    /// design was based on the erroneous understanding that between every true there will always be a false.</remarks>
    /// </summary>
    private bool _isAlreadyHovering = false;
    private GameObject _systemItemGo;
    private GameInputHelper _inputHelper;
    private InputManager _inputMgr;
    private GameManager _gameMgr;
    private IList<IDisposable> _subscriptions;

    protected override void Awake() {
        base.Awake();
        _inputHelper = GameInputHelper.Instance;
        _inputMgr = InputManager.Instance;
        _gameMgr = GameManager.Instance;
        _systemItemGo = gameObject.GetSingleComponentInParents<SystemItem>().gameObject;
        Subscribe();
    }

    // MeshCollider is initialized by SystemItem

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_inputMgr.SubscribeToPropertyChanged<InputManager, GameInputMode>(im => im.InputMode, InputModePropChangedHandler));
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
    }

    #region Event and Property Change Handlers

    private void InputModePropChangedHandler() {
        var inputMode = _inputMgr.InputMode;
        switch (inputMode) {
            case GameInputMode.NoInput:
            case GameInputMode.PartialPopup:
            case GameInputMode.FullPopup:
                EnableOnHoverCheckingForOccludedObjects(false);
                break;
            case GameInputMode.Lobby:
            case GameInputMode.Normal:
                // do nothing
                break;
            case GameInputMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(inputMode));
        }
    }

    private void IsPausedPropChangedHandler() {
        PauseJobs(_gameMgr.IsPaused);
    }

    private void MouseMoveWhileOnHoverCheckingForOccludedObjectsEventHandler(Vector2 moveDelta) {
        D.Assert(_inputMgr.InputMode == GameInputMode.Normal);
        CheckForOccludedObjectAndProcessOnHoverNotifications();
    }

    // Note: No need to filter occlusion checking with IsDiscernible within these events 
    // as IsDiscernible turns the collider on and off which accomplishes the same thing. In the case of 
    // OnHover, if hovering and IsDiscernible changes to false, the Ngui event system
    // will send out an OnHover(false) when it sees the collider disappear.

    private void HoverEventHandler(bool isOver) {
        //D.Log("{0}.OnHover({1}) received.", Name, isOver);
        if (AssessOnHoverEvent(isOver)) {
            _inputHelper.Notify(_systemItemGo, "OnHover", isOver);
        }
    }

    private void ClickEventHandler() {
        //D.Log("{0}.OnClick() received.", Name);
        if (_inputMgr.InputMode == GameInputMode.PartialPopup) {
            // Click events immediately follow PressRelease events which can open a ContextMenu. If the menu is opened,
            // it changes InputMode to PartialPopup which eliminates subsequent mouse events by setting the EventDispatcher's
            // eventReceiverMask to Zero. However, the Click event for this collider is already queued to send, so the 
            // eventReceiverMask has no affect on this particular event. If allowed to check for an occluded object, 
            // TryCheckForOccludedObject's Assert will fail.
            //D.Log("{0}.OnClick() received while in {1}.{2}.", Name, typeof(GameInputMode).Name, _inputMgr.InputMode.GetValueName());
            return;
        }
        GameObject occludedObject;
        if (TryCheckForOccludedObject(out occludedObject)) {
            _inputHelper.Notify(occludedObject, "OnClick");
            return;
        }
        _inputHelper.Notify(_systemItemGo, "OnClick");
    }

    private void DoubleClickEventHandler() {
        //D.Log("{0}.OnDoubleClick() received.", Name);
        D.Warn(_inputMgr.InputMode == GameInputMode.PartialPopup, "{0}.OnDoubleClick() received while in {1}.{2}.",
            Name, typeof(GameInputMode).Name, _inputMgr.InputMode.GetValueName());   // OPTIMIZE I haven't seen this occur so far
        GameObject occludedObject;
        if (TryCheckForOccludedObject(out occludedObject)) {
            _inputHelper.Notify(occludedObject, "OnDoubleClick");
            return;
        }
        _inputHelper.Notify(_systemItemGo, "OnDoubleClick");
    }

    private void PressEventHandler(bool isDown) {
        //D.Log("{0}.OnPress({1}) received.", Name, isDown);
        GameObject occludedObject;
        if (TryCheckForOccludedObject(out occludedObject)) {
            _inputHelper.Notify(occludedObject, "OnPress", isDown);
            return;
        }
        _inputHelper.Notify(_systemItemGo, "OnPress", isDown);
    }

    void OnHover(bool isOver) {
        HoverEventHandler(isOver);
    }

    void OnClick() {
        ClickEventHandler();
    }

    void OnDoubleClick() {
        DoubleClickEventHandler();
    }

    void OnPress(bool isDown) {
        PressEventHandler(isDown);
    }

    #endregion

    #region Occluded Object Checking

    /* The Ngui event system generates events for the first object with a collider
     *  under the mouse that its Raycast encounters. As this System OrbitalPlane's
     *  collider hides the collider of any portion of an object that is behind it, some 
     *  or all of an object can effectively be occluded from detection by the event 
     *  system. The TryCheckForOccludedObject() method tests for and returns the 
     *  occluded object found, if any. If an object is found, it is notified of the event.
     *  
     * This is sufficient for all event types except OnHover. The Ngui Event system  
     * generates OnHover events when it detects a different object under the mouse. This
     * means OnHover events are typically generated twice per object - once when the object
     * is first detected under the mouse [OnHover(true)], and once when the same object
     * is no longer detected under the mouse [OnHover(false)]. Because of this, an object
     * that is occluded cannot be detected because the object under the mouse doesn't 
     * change. The workaround implemented here is to continuously spawn occluded object
     * checks while the mouse is over the OrbitalPlane collider. To make this approach sufficiently 
     * responsive to the user, two different mechanisms spawn these checks - mouse movement 
     * and an elapsed time approach. The elapsed time approach is needed because some 
     * occluded objects move on their own (eg - fleets and planetoids) and can move under 
     * (or away from) the mouse by themselves without any required user mouse movement.
     *
     * Note: An additional OnHover(true) event is also generated each time the mouse is clicked.
     * According to Ngui's ArenMook, this is done to regain the proper state that was overwritten 
     * by OnClick. I think the state he is referring too is within Ngui, but whether true or not
     * this behaviour is intended so he isn't going to change it. That duplicate OnHover(true)
     * creates difficulties as I use OnHover(isOver) to switch the OnHoverOccludedObjectChecking
     * System on and off. Accordingly, I've had to filter out those duplicate OnHover(true)s 
     * using the field _isAlreadyHovering.
     */

    /// <summary>
    /// Checks if this orbitalPlane's collider is occluding another object behind it.
    /// </summary>
    /// <param name="occludedObject">The occluded object that was found or null if no object was found.</param>
    /// <returns><c>true</c> if an occluded object was found, else <c>false</c>.</returns>
    private bool TryCheckForOccludedObject(out GameObject occludedObject) {
        Layers orbitalPlaneLayer = (Layers)gameObject.layer;
        D.Assert(orbitalPlaneLayer == Layers.SystemOrbitalPlane, "{0} Layer {1} should be {2}.".Inject(GetType().Name, orbitalPlaneLayer.GetValueName(), Layers.SystemOrbitalPlane.GetValueName()));

        // UNCLEAR: For some unknown reason, using UICamera.Raycast() here can find the orbitalPlaneMesh (on the orbitalPlaneLayer) itself as
        // the occludedObject, even though the mask says it shouldn't be able too. That is, UICamera.hoveredObject returns the orbitalPlaneMesh.
        // This only occurs when a new game is created (new GameScene) from within the game. 
        // The alternative approach of using my own Raycast does not exhibit this problem.
        //if (UICamera.Raycast(Input.mousePosition)) {
        //    // A spawned check from OnHover can occur before SystemView.OnHover(false) is called to turn off spawning.
        //    // This occurs when the mouse moves between the orbitalPlane and a UI element, resulting in hoveredObject returning the UI element
        //    if (!UICamera.isOverUI) {
        //        occludedObject = UICamera.hoveredObject;
        //        D.Log("{0}.{1} found occluded object {2}, eventRcvrMask = {3}.", FullName, GetType().Name, occludedObject.name, eventDispatcher.eventReceiverMask.MaskToString());
        //        isObjectOccluded = true;
        //    }
        //}
        //eventDispatcher.eventReceiverMask = savedMask;

        D.Assert(_inputMgr.InputMode == GameInputMode.Normal, "{0}: {1} = {2}.",
            Name, typeof(GameInputMode).Name, _inputMgr.InputMode.GetValueName());  // Occlusion check should only occur during Normal InputMode

        var maskWithoutOrbitalPlaneLayer = InputManager.WorldEventDispatcherMask_NormalInput.RemoveFromMask(orbitalPlaneLayer);
        bool isObjectOccluded = false;
        occludedObject = null;
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);    // using UICamera.currentRay doesn't always get a hit over Star when it should
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, maskWithoutOrbitalPlaneLayer)) {
            if (!UICamera.isOverUI) {
                occludedObject = hit.collider.gameObject;
                //D.Log("{0}.{1} found occluded object {2}. \nMask = {3}.", transform.name, GetType().Name, occludedObject.name, maskWithoutOrbitalPlaneLayer.MaskToString());
                isObjectOccluded = true;
            }
        }
        return isObjectOccluded;
    }

    #region OnHover Occluded Object Checking Workaround

    /// <summary>
    /// The occluded object recorded during the last check. Can be null.
    /// </summary>
    private GameObject _currentOccludedObject;

    /// <summary>
    /// Coroutine Job that spawns OnHoverOccludedObjectChecks over time. Used to 
    /// detect occluded objects when the occluded object can move itself to/from
    /// under the mouse without any required mouse motion.
    /// </summary>
    private Job _spawnOccludedObjectChecksJob;

    /// <summary>
    /// Assesses the OnHover events received from the Ngui Event System by this OrbitalPlane.
    /// Returns true if the event should be executed by this class, false if not.
    /// </summary>
    /// <param name="isOver">if set to <c>true</c> [is over].</param>
    /// <returns></returns>
    private bool AssessOnHoverEvent(bool isOver) {
        if (_isAlreadyHovering && isOver) {
            // duplicate isOver = true so ignore
            //D.Log("{0} received duplicate OnHover(true). Ignoring.", Name);
            return false;
        }
        _isAlreadyHovering = isOver;

        if (isOver) {
            D.Assert(_inputMgr.InputMode == GameInputMode.Normal, "{0} received OnHover(true) during {1}.{2}.",
                Name, typeof(GameInputMode).Name, _inputMgr.InputMode.GetValueName());
        }


        EnableOnHoverCheckingForOccludedObjects(isOver);
        bool toExecuteNormalOnHover = false;
        if (isOver) {
            if (_currentOccludedObject == null) {
                // just arrived over the orbitalPlane with no occluded object beneath the mouse
                toExecuteNormalOnHover = true;
            }
        }
        else {
            // leaving the orbitalPlane
            toExecuteNormalOnHover = true;
            if (_currentOccludedObject != null) {
                // there is an occludedObject underneath the mouse as we leave the orbitalPlane
                _inputHelper.Notify(_currentOccludedObject, "OnHover", false);
                _currentOccludedObject = null;
            }
        }
        return toExecuteNormalOnHover;
    }

    #region CheckForOccludedObjectAndProcessOnHoverNotifications Archive
    //private void CheckForOccludedObjectAndProcessOnHoverNotifications() {
    //    GameObject newOccludedObject;
    //    TryCheckForOccludedObject(out newOccludedObject);

    //    // now process any required notifications to said objects
    //    if (newOccludedObject == null) {
    //        // new state is not occluded
    //        if (_currentOccludedObject != null) {
    //            // occluded -> notOccluded transition
    //            _inputHelper.Notify(_currentOccludedObject, "OnHover", false);
    //            ExecuteThisOnHoverContent(true);
    //            _currentOccludedObject = newOccludedObject;   // null
    //        }
    //        // notOccluded -> notOccluded transition: do nothing as System already knows hovered = true
    //    }
    //    else {
    //        // new state is occluded
    //        if (_currentOccludedObject != null) {
    //            // occluded -> occluded transition
    //            //D.Log("CurrentOccludedObject: {0}, NewOccludedObject: {1}.", _currentOccludedObject.name, newOccludedObject.name);
    //            if (newOccludedObject != _currentOccludedObject) {
    //                // occluded -> different occluded transition
    //                _inputHelper.Notify(_currentOccludedObject, "OnHover", false);
    //                _inputHelper.Notify(newOccludedObject, "OnHover", true);
    //                // IMPROVE use UICamera.Notify approach separating the old and new notify in time
    //                _currentOccludedObject = newOccludedObject;
    //            }
    //            // occluded -> same occluded transition: do nothing
    //        }
    //        else {
    //            // notOccluded -> occluded transtion
    //            // also handles offSystem -> occluded transition with unneeded ProcessSystemViewOnHover(false)
    //            ExecuteThisOnHoverContent(false);
    //            _inputHelper.Notify(newOccludedObject, "OnHover", true);
    //            _currentOccludedObject = newOccludedObject;
    //        }
    //    }
    //}
    #endregion

    /// <summary>
    /// The OnHover version of CheckForOccludedObject() which also executes any
    /// required OnHover notifications.
    /// </summary>
    private void CheckForOccludedObjectAndProcessOnHoverNotifications() {
        GameObject newOccludedObject;
        if (TryCheckForOccludedObject(out newOccludedObject)) {
            D.Assert(newOccludedObject != null);

            // new state is occluded
            if (_currentOccludedObject != null) {
                // occluded -> occluded transition
                //D.Log("CurrentOccludedObject: {0}, NewOccludedObject: {1}.", _currentOccludedObject.name, newOccludedObject.name);
                if (newOccludedObject != _currentOccludedObject) {
                    // occluded -> different occluded transition
                    _inputHelper.Notify(_currentOccludedObject, "OnHover", false);
                    _inputHelper.Notify(newOccludedObject, "OnHover", true);
                    // IMPROVE use UICamera.Notify approach separating the old and new notify in time?
                }
                // occluded -> same occluded transition: do nothing
            }
            else {
                // notOccluded -> occluded transtion
                // also handles offSystem -> occluded transition with unneeded ExecuteThisOnHoverContent(false)
                _inputHelper.Notify(_systemItemGo, "OnHover", false);
                _inputHelper.Notify(newOccludedObject, "OnHover", true);
            }
        }
        else {
            // new state is not occluded
            if (_currentOccludedObject != null) {
                // occluded -> notOccluded transition
                _inputHelper.Notify(_currentOccludedObject, "OnHover", false);
                _inputHelper.Notify(_systemItemGo, "OnHover", true);
            }
            // notOccluded -> notOccluded transition: do nothing as System already knows hovered = true
        }
        _currentOccludedObject = newOccludedObject; // can be object=null, object=object, null=object or null=null
    }

    /// <summary>
    /// Debug flag indicating whether this instance has subscribed to the mouse move delegate.
    /// Used as a double check to Assert that multiple subscriptions aren't occuring from
    /// this instance as there isn't another way to tell. Note: this delegate is static and
    /// therefore can be subscribed to by each instance so checking for number of subscriptions in
    /// the InvocationList won't work.
    /// </summary>
    private bool __isSubscribedToMouseMove = false;

    /// <summary>
    ///  Enables continuous checking for occluded objects over time (currently every second)
    ///  and each time the mouse moves. Used to work around the fact that UICamera only sends 
    ///  OnHover events when the object under the mouse changes. This way, the check for an occluded 
    ///  object continues to occur even when the underlying object (this orbitalPlane collider) remains
    ///  under the mouse.
    /// </summary>
    /// <param name="toEnable">if set to <c>true</c> enable checking. If false, disables it.</param>
    private void EnableOnHoverCheckingForOccludedObjects(bool toEnable) {
        //D.Log("{0}.EnableOnHoverCheckingForOccludedObjects({1}) called.", Name, toEnable);
        if (toEnable) {
            CheckForOccludedObjectAndProcessOnHoverNotifications();

            if (!IsSpawnOccludedObjectChecksJobRunning) {
                _spawnOccludedObjectChecksJob = new Job(SpawnOnHoverOccludedObjectChecks(), toStart: true);
            }

            D.Assert(!__isSubscribedToMouseMove);
            // C# GOTCHA! You can subscribe to a delegate using a Lambda expression...
            //UICamera.onMouseMove += (moveDelta) => MouseMoveWhileOnHoverCheckingForOccludedObjectsEventHandler();
            UICamera.onMouseMove += MouseMoveWhileOnHoverCheckingForOccludedObjectsEventHandler;
        }
        else {
            if (IsSpawnOccludedObjectChecksJobRunning) {
                _spawnOccludedObjectChecksJob.Kill();
            }

            // ... but you CAN'T unsubscribe from a delegate using a Lambda expression! There is no error, it just doesn't work!
            //UICamera.onMouseMove -= (moveDelta) => MouseMoveWhileOnHoverCheckingForOccludedObjectsEventHandler();
            UICamera.onMouseMove -= MouseMoveWhileOnHoverCheckingForOccludedObjectsEventHandler;

            //if(UICamera.onMouseMove != null) {
            //    D.Log("{0}: UICamera.onMouseMove invocation list length = {1}.", Name, UICamera.onMouseMove.GetInvocationList().Length);
            //}
        }
        __isSubscribedToMouseMove = toEnable;
    }

    /// <summary>
    /// Coroutine that spawns occluded object checks for the OnHover event.
    /// </summary>
    /// <returns></returns>
    private IEnumerator SpawnOnHoverOccludedObjectChecks() {
        while (true) {
            CheckForOccludedObjectAndProcessOnHoverNotifications();
            yield return new WaitForSeconds(1F);
        }
    }

    #endregion

    #endregion

    private void PauseJobs(bool toPause) {
        if (IsSpawnOccludedObjectChecksJobRunning) {
            // spawn job gets paused as its sole purpose is to show occluded objects that move on their own
            _spawnOccludedObjectChecksJob.IsPaused = toPause;
        }
    }

    protected override void Cleanup() {
        EnableOnHoverCheckingForOccludedObjects(false);
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(d => d.Dispose());
        _subscriptions.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

