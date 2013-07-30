// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ManagementObjects.cs
// Singleton for easy access to this Management folder in all scenes. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Singleton for easy access to this Management folder in all scenes. Transitions from startScene to startScene
/// attaching any Management folder child objects in the new startScene to this incoming folder, then destroys
/// the Management folder that was already present in the new startScene.
/// </summary>
public class ManagementObjects : AMonoBehaviourBaseSingleton<ManagementObjects>, IDisposable, IInstanceIdentity {

    /// <summary>
    /// Gets the ManagementObjects folder.
    /// </summary>
    /// <tPrefsValue>
    /// The folder's Transform.
    /// </tPrefsValue>
    public static Transform Folder { get { return Instance.transform; } }

    private Transform[] _children;

    private GameEventManager _eventMgr;
    private Transform _transform;
    private bool _isInitialized;

    void Awake() {
        if (TryDestroyExtraCopies()) {
            return;
        }
        _transform = transform;
        _eventMgr = GameEventManager.Instance;
        Subscribe();
        _isInitialized = true;
    }

    /// <summary>
    /// Ensures that no matter how many scenes this ManagementObjects is
    /// in (having one dedicated to each startScene is useful for testing) there's only ever one copy
    /// in memory if you make a startScene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (_instance != null && _instance != this) {
            Logger.Log("{0}_{1} found as extra. Initiating destruction sequence.".Inject(this.name, InstanceID));
            TransferChildrenThenDestroy();
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            _instance = this;
            return false;
        }
    }

    private void TransferChildrenThenDestroy() {
        Logger.Log("{0} has {1} children.".Inject(this.name, transform.childCount));
        Transform[] transforms = gameObject.GetComponentsInChildren<Transform>(includeInactive: true);   // includes the parent t
        foreach (Transform t in transforms) {
            if (t != transform) {
                t.parent = Instance.transform;
                Logger.Log("Child [{0}].parent changed to {1}.".Inject(t.name, Instance.name));
            }
        }
        Destroy(gameObject);
    }

    private void Subscribe() {
        _eventMgr.AddListener<SceneChangingEvent>(this, OnSceneChanging);
        _eventMgr.AddListener<SceneChangedEvent>(this, OnSceneChanged);
    }

    /// <summary>
    /// The instance that is not going to be destroyed has this called when the scene is going to change
    /// but before it changes. This implementation records and then detaches its children so they can die if they are 
    /// supposed to. The record of who the children were will be used by OnSceneChanged()
    /// to find any former children still alive after the scene transition and reparent them.
    /// </summary>
    private void OnSceneChanging(SceneChangingEvent e) {
        // what the scene is changing to is irrelevant
        Transform[] transforms = gameObject.GetComponentsInChildren<Transform>(includeInactive: true);
        _children = (from t in transforms where t != _transform select t).ToArray<Transform>();
        _transform.DetachChildren();
    }

    private void OnSceneChanged(SceneChangedEvent e) {
        var childrenToReattach = from t in _children where t != null select t;
        childrenToReattach.ForAll<Transform>(t => t.parent = _transform);
    }

    void OnDestroy() {
        if (_isInitialized) {
            // no reason to cleanup if this object was destroyed before it was initialized.
            Debug.Log("{0}_{1} instance is disposing.".Inject(this.name, InstanceID));
            Dispose();
        }
    }

    protected override void OnApplicationQuit() {
        _instance = null;
    }

    private void Unsubscribe() {
        _eventMgr.RemoveListener<SceneChangingEvent>(this, OnSceneChanging);
        _eventMgr.RemoveListener<SceneChangedEvent>(this, OnSceneChanged);
    }

    #region IDisposable
    [NonSerialized]
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
    /// <arg name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</arg>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Unsubscribe();
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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


