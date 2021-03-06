﻿// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

    private const string DebugNameFormat = "{0}.{1}";

    private static float _universeDiameter;

    /// <summary>
    /// The LayerMask to use when raycasting to find non-orbital plane occluded objects behind this orbital plane.
    /// </summary>
    private static LayerMask _occludedObjectDetectionMask = InputManager.WorldEventDispatcherMask_NormalInput.RemoveFromMask(Layers.SystemOrbitalPlane);

    /// <summary>
    /// The LayerMask to use when raycasting to find other systems (orbital plane colliders) behind this system orbital plane.
    /// </summary>
    private static LayerMask _occludedSystemDetectionMask = LayerMaskUtility.CreateInclusiveMask(Layers.SystemOrbitalPlane);

    private string _debugName;
    public string DebugName {
        get {
            if (_debugName == null) {
                _debugName = DebugNameFormat.Inject(System.DebugName, typeof(OrbitalPlaneInputEventRouter).Name);
            }
            return _debugName;
        }
    }

    public SystemItem System { get; private set; }

    private bool ShowDebugLog { get { return System.ShowDebugLog; } }

    /// <summary>
    /// Flag indicating that this orbital plane has already received OnHover(true) without an intervening OnHover(false). 
    /// Used to filter out the duplicate OnHover(true) events which I don't expect Ngui to remedy.
    /// <remarks>Ngui sends a duplicate OnHover(true) every time the gameObject is clicked. According to ArenMook, 
    /// this is intended to regain some unknown OnHover state that was overridden by the click. The original 
    /// design was based on the erroneous understanding that between every true there will always be a false.</remarks>
    /// </summary>
    private bool _isAlreadyHovering = false;
    private GameInputHelper _inputHelper;
    private InputManager _inputMgr;
    private IList<IDisposable> _subscriptions;
    private JobManager _jobMgr;

    protected override void Awake() {
        base.Awake();
        _inputHelper = GameInputHelper.Instance;
        _inputMgr = InputManager.Instance;
        _jobMgr = JobManager.Instance;
        System = gameObject.GetSingleComponentInParents<SystemItem>();

        D.AssertEqual(Layers.SystemOrbitalPlane, (Layers)gameObject.layer, ((Layers)gameObject.layer).GetValueName());
        Subscribe();
    }

    // MeshCollider is initialized by SystemItem

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_inputMgr.SubscribeToPropertyChanged<InputManager, GameInputMode>(im => im.InputMode, InputModePropChangedHandler));
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

    private void MouseMoveWhileOnHoverCheckingForOccludedObjectsEventHandler(Vector2 moveDelta) {
        D.AssertEqual(GameInputMode.Normal, _inputMgr.InputMode);
        CheckForOccludedObjectAndProcessOnHoverNotifications();
    }

    // Note: No need to filter occlusion checking with IsDiscernible within these events 
    // as IsDiscernible turns the collider on and off which accomplishes the same thing. In the case of 
    // OnHover, if hovering and IsDiscernible changes to false, the Ngui event system
    // will send out an OnHover(false) when it sees the collider disappear.

    private void HoverEventHandler(bool isOver) {
        //D.Log(ShowDebugLog, "{0}.OnHover({1}) received.", DebugName, isOver);
        if (AssessOnHoverEvent(isOver)) {
            _inputHelper.Notify(System.gameObject, "OnHover", isOver);
        }
    }

    private void ClickEventHandler() {
        //D.Log(ShowDebugLog, "{0}.OnClick() received.", DebugName);
        if (_inputMgr.InputMode == GameInputMode.PartialPopup) {
            // Click events immediately follow PressRelease events which can open a ContextMenu. If the menu is opened,
            // it changes InputMode to PartialPopup which eliminates subsequent mouse events by setting the EventDispatcher's
            // eventReceiverMask to Zero. However, the Click event for this collider is already queued to send, so the 
            // eventReceiverMask has no affect on this particular event. If allowed to check for an occluded object, 
            // TryCheckForOccludedObject's Assert will fail.
            //D.Log(ShowDebugLog, "{0}.OnClick() received while in {1}.{2}.", DebugName, typeof(GameInputMode).Name, _inputMgr.InputMode.GetValueName());
            return;
        }

        GameObject objectToNotify;
        GameObject occludedObject;
        SystemItem occludedSystem;
        if (TryCheckForOccludedObject(out occludedObject, out occludedSystem)) {
            objectToNotify = occludedSystem == null ? occludedObject : occludedSystem.gameObject;
        }
        else {
            objectToNotify = System.gameObject;
        }
        _inputHelper.Notify(objectToNotify, "OnClick");
    }

    private void DoubleClickEventHandler() {
        //D.Log(ShowDebugLog, "{0}.OnDoubleClick() received.", DebugName);
        if (_inputMgr.InputMode == GameInputMode.PartialPopup) {
            D.Warn("{0}.OnDoubleClick() received while in {1}.{2}.",
                DebugName, typeof(GameInputMode).Name, _inputMgr.InputMode.GetValueName());   // OPTIMIZE I haven't seen this occur so far
        }

        GameObject objectToNotify;
        SystemItem occludedSystem;
        GameObject occludedObject;
        if (TryCheckForOccludedObject(out occludedObject, out occludedSystem)) {
            objectToNotify = occludedSystem == null ? occludedObject : occludedSystem.gameObject;
        }
        else {
            objectToNotify = System.gameObject;

        }
        _inputHelper.Notify(objectToNotify, "OnDoubleClick");
    }

    private void PressEventHandler(bool isDown) {
        //D.Log(ShowDebugLog, "{0}.OnPress({1}) received.", DebugName, isDown);
        GameObject objectToNotify;
        SystemItem occludedSystem;
        GameObject occludedObject;
        if (TryCheckForOccludedObject(out occludedObject, out occludedSystem)) {
            objectToNotify = occludedSystem == null ? occludedObject : occludedSystem.gameObject;
        }
        else {
            objectToNotify = System.gameObject;
        }
        _inputHelper.Notify(objectToNotify, "OnPress", isDown);
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
     *  system. The TryCheckForOccludedObject() method tests for and returns the non-system
     *  occluded object found, if any. If an object is found, it is notified of the event.
     *  
     *  If a non-system object (star, planet, cmd, etc.) is not found, TryCheckForOccludedObject()
     *  then checks for an orbital plane collider that might be occluded by this orbital plane.
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
     * occluded objects move on their own (e.g. - fleets and planetoids) and can move under 
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
    /// The buffer to use for Raycasting hits on a System's OrbitalPlaneCollider to avoid memory allocations.
    /// <remarks>UNCLEAR I use a size of 2 as I'm only interested in _rayHits[1]?</remarks>
    /// </summary>
    private static RaycastHit[] _occludedSystemHitBuffer = new RaycastHit[2];

    /// <summary>
    /// Checks if this orbitalPlane's collider is occluding another object behind it.
    /// </summary>
    /// <param name="occludedObject">The occluded object that was found or null if no object was found.</param>
    /// <param name="occludedSystem">The occluded system that was found or null if no system was found.</param>
    /// <returns>
    ///   <c>true</c> if an occluded object was found, else <c>false</c>.
    /// </returns>
    private bool TryCheckForOccludedObject(out GameObject occludedObject, out SystemItem occludedSystem) {
        if (_universeDiameter == Constants.ZeroF) { // OPTIMIZE set value in Awake once preset composition DebugSystemCreators no longer in use
            _universeDiameter = GameManager.Instance.GameSettings.UniverseSize.Radius() * 2F;
            D.AssertNotApproxEqual(Constants.ZeroF, _universeDiameter);
        }

        D.AssertEqual(GameInputMode.Normal, _inputMgr.InputMode, _inputMgr.InputMode.GetValueName());  // Occlusion check should only occur during Normal InputMode

        bool isObjectOccluded = false;
        occludedObject = null;
        occludedSystem = null;

        if (!_inputHelper.IsOverUI) {
            RaycastHit hit;
            Ray ray = UICamera.lastWorldRay;    // Ngui 3.11.0 introduced UICamera.lastWorldRay // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, _universeDiameter, _occludedObjectDetectionMask)) {
                occludedObject = hit.collider.gameObject;
                //D.Log(ShowDebugLog, "{0} found occluded object {1}. \nMask = {2}.", DebugName, occludedObject.name, _occludedObjectDetectionMask.MaskToString());
                isObjectOccluded = true;
            }
            else {
                // No non-system object found, so now try to find another system orbital plane
                int hitCount = Physics.RaycastNonAlloc(ray, _occludedSystemHitBuffer, _universeDiameter, _occludedSystemDetectionMask);
                D.Assert(hitCount > Constants.Zero); // must at least hit this orbital plane
                if (hitCount > Constants.One) {
                    occludedObject = _occludedSystemHitBuffer[1].collider.gameObject;
                    OrbitalPlaneInputEventRouter systemPlaneRouter = occludedObject.GetComponent<OrbitalPlaneInputEventRouter>();
                    occludedSystem = systemPlaneRouter.System;
                    //D.Log(ShowDebugLog, "{0} found occluded System {1}. \nMask = {2}.", DebugName, occludedSystem.DebugName, _occludedSystemDetectionMask.MaskToString());
                    isObjectOccluded = true;
                }
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
    /// The occluded system recorded during the last check. Can be null.
    /// </summary>
    private SystemItem _currentOccludedSystem;

    /// <summary>
    /// Coroutine Job that spawns OnHoverOccludedObjectChecks over time. Used to 
    /// detect occluded objects when the occluded object can move itself to/from
    /// under the mouse without any required mouse motion.
    /// </summary>
    private Job _spawnOccludedObjectChecksJob;

    /// <summary>
    /// Assesses the OnHover events received from the Ngui Event System by this OrbitalPlane.
    /// Returns true if the event should be executed by this class, false if not. 
    /// <remarks>If the event should be executed by this class, and there is
    /// currently an occludedObject, the occludedObject will be sent an OnHover(false) event to
    /// allow it to cleanup prior to returning true.</remarks>
    /// </summary>
    /// <param name="isOver">if set to <c>true</c> [is over].</param>
    /// <returns></returns>
    private bool AssessOnHoverEvent(bool isOver) {
        if (_isAlreadyHovering && isOver) {
            // duplicate isOver = true so ignore
            //D.Log(ShowDebugLog, "{0} received duplicate OnHover(true). Ignoring.", DebugName);
            return false;
        }
        _isAlreadyHovering = isOver;

        if (isOver) {
            if (_inputMgr.InputMode != GameInputMode.Normal) {
                D.Error("{0} received OnHover(true) during {1}.{2}.", DebugName, typeof(GameInputMode).Name, _inputMgr.InputMode.GetValueName());
            }
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
                GameObject objectToNotify = _currentOccludedSystem == null ? _currentOccludedObject : _currentOccludedSystem.gameObject;
                _inputHelper.Notify(objectToNotify, "OnHover", false);
                _currentOccludedObject = null;
                _currentOccludedSystem = null;
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
    //            // notOccluded -> occluded transition
    //            // also handles offSystem -> occluded transition with unneeded ProcessSystemViewOnHover(false)
    //            ExecuteThisOnHoverContent(false);
    //            _inputHelper.Notify(newOccludedObject, "OnHover", true);
    //            _currentOccludedObject = newOccludedObject;
    //        }
    //    }
    //}
    #endregion

    /// <summary>
    /// The OnHover version of CheckForOccludedObject() which also executes any required OnHover notifications.
    /// </summary>
    private void CheckForOccludedObjectAndProcessOnHoverNotifications() {
        GameObject currentObjectToNotify = null;
        GameObject newObjectToNotify = null;

        GameObject newOccludedObject;
        SystemItem newOccludedSystem;
        if (TryCheckForOccludedObject(out newOccludedObject, out newOccludedSystem)) {
            // new state is occluded
            D.AssertNotNull(newOccludedObject);


            if (_currentOccludedObject != null) {
                // occluded -> occluded transition
                if (newOccludedObject != _currentOccludedObject) {
                    // occluded -> different occluded transition
                    D.Log(ShowDebugLog, "Occluded => DifferentOccluded: CurrentOccludedObject = {0}, NewOccludedObject = {1}.", _currentOccludedObject.name, newOccludedObject.name);

                    currentObjectToNotify = _currentOccludedSystem == null ? _currentOccludedObject : _currentOccludedSystem.gameObject;
                    newObjectToNotify = newOccludedSystem == null ? newOccludedObject : newOccludedSystem.gameObject;

                    _inputHelper.Notify(currentObjectToNotify, "OnHover", false);
                    _inputHelper.Notify(newObjectToNotify, "OnHover", true);
                }
                // occluded -> same occluded transition: do nothing
            }
            else {
                // notOccluded -> occluded transition
                // also handles offSystem -> occluded transition with unneeded ExecuteThisOnHoverContent(false)
                D.Log(ShowDebugLog, "NotOccluded => Occluded: NewOccludedObject = {0}.", newOccludedObject.name);

                D.AssertNull(_currentOccludedSystem);

                currentObjectToNotify = System.gameObject;
                newObjectToNotify = newOccludedSystem == null ? newOccludedObject : newOccludedSystem.gameObject;

                _inputHelper.Notify(currentObjectToNotify, "OnHover", false);
                _inputHelper.Notify(newObjectToNotify, "OnHover", true);
            }
        }
        else {
            // new state is not occluded
            D.AssertNull(newOccludedObject);
            D.AssertNull(newOccludedSystem);

            if (_currentOccludedObject != null) {
                // occluded -> notOccluded transition
                D.Log(ShowDebugLog, "Occluded => NotOccluded: CurrentOccludedObject = {0}.", _currentOccludedObject.name);

                currentObjectToNotify = _currentOccludedSystem == null ? _currentOccludedObject : _currentOccludedSystem.gameObject;
                newObjectToNotify = System.gameObject;

                _inputHelper.Notify(currentObjectToNotify, "OnHover", false);
                _inputHelper.Notify(newObjectToNotify, "OnHover", true);
            }
            // notOccluded -> notOccluded transition: do nothing as System already knows hovered = true
        }
        _currentOccludedObject = newOccludedObject; // can be object=null, object=object, null=object or null=null
        _currentOccludedSystem = newOccludedSystem; // can be system=null, system=system, null=system or null=null
    }

    /// <summary>
    ///  Enables continuous checking for occluded objects over time (currently every second)
    ///  and each time the mouse moves. Used to work around the fact that UICamera only sends 
    ///  OnHover events when the object under the mouse changes. This way, the check for an occluded 
    ///  object continues to occur even when the underlying object (this orbitalPlane collider) remains
    ///  under the mouse.
    /// </summary>
    /// <param name="toEnable">if set to <c>true</c> enable checking. If false, disables it.</param>
    private void EnableOnHoverCheckingForOccludedObjects(bool toEnable) {
        //D.Log(ShowDebugLog, "{0}.EnableOnHoverCheckingForOccludedObjects({1}) called.", DebugName, toEnable);
        if (toEnable) {
            CheckForOccludedObjectAndProcessOnHoverNotifications();

            if (_spawnOccludedObjectChecksJob == null) {
                string jobName = "OrbitalPlaneCheckOcclusionsJob";
                // Spawn job allows pausing as its sole purpose is to show occluded objects that move on their own.
                // Note that can receive MULTIPLE D.Log messages from JobManager indicating spawn job was paused immediately 
                // after starting it while looking through a system while paused
                _spawnOccludedObjectChecksJob = _jobMgr.StartGameplayJob(SpawnOnHoverOccludedObjectChecks(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                    D.Assert(jobWasKilled);
                });
            }

            D.Assert(!__isSubscribedToMouseMove);
            // C# GOTCHA! You can subscribe to a delegate using a Lambda expression...
            //UICamera.onMouseMove += (moveDelta) => MouseMoveWhileOnHoverCheckingForOccludedObjectsEventHandler();
            UICamera.onMouseMove += MouseMoveWhileOnHoverCheckingForOccludedObjectsEventHandler;
        }
        else {
            KillSpawnOccludedObjectChecksJob();

            // ... but you CAN'T unsubscribe from a delegate using a Lambda expression! There is no error, it just doesn't work!
            //UICamera.onMouseMove -= (moveDelta) => MouseMoveWhileOnHoverCheckingForOccludedObjectsEventHandler();
            UICamera.onMouseMove -= MouseMoveWhileOnHoverCheckingForOccludedObjectsEventHandler;

            //if(UICamera.onMouseMove != null) {
            //    D.Log(ShowDebugLog, "{0}: UICamera.onMouseMove invocation list length = {1}.", DebugName, UICamera.onMouseMove.GetInvocationList().Length);
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
            yield return Yielders.GetWaitForSeconds(1F);
        }
    }

    private void KillSpawnOccludedObjectChecksJob() {
        if (_spawnOccludedObjectChecksJob != null) {
            _spawnOccludedObjectChecksJob.Kill();
            _spawnOccludedObjectChecksJob = null;
        }
    }

    #endregion

    #endregion

    // 8.12.16 Job pausing moved to JobManager to consolidate handling

    protected override void Cleanup() {
        EnableOnHoverCheckingForOccludedObjects(false);
        _universeDiameter = Constants.ZeroF;
        Unsubscribe();
    }

    private void Unsubscribe() {
        if (_subscriptions != null) {
            _subscriptions.ForAll(d => d.Dispose());
            _subscriptions.Clear();
        }
    }

    public override string ToString() {
        return DebugName;
    }

    #region Debug

    /// <summary>
    /// Debug flag indicating whether this instance has subscribed to the mouse move delegate.
    /// Used as a double check to Assert that multiple subscriptions aren't occurring from
    /// this instance as there isn't another way to tell. Note: this delegate is static and
    /// therefore can be subscribed to by each instance so checking for number of subscriptions in
    /// the InvocationList won't work.
    /// </summary>
    private bool __isSubscribedToMouseMove = false;

    #endregion
}

