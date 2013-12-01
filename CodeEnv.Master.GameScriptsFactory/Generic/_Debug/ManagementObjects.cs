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
/// Singleton for easy access to this Management folder in all scenes. Transitions from scene to scene
/// attaching any Management folder child objects in the new scene to this incoming folder, then destroys
/// the Management folder that was already present in the new scene.
/// </summary>
public class ManagementObjects : AMonoBaseSingleton<ManagementObjects>, IDisposable {

    /// <summary>
    /// Gets the ManagementObjects folder transform.
    /// </summary>
    public static Transform Folder { get { return Instance.transform; } }

    private Transform[] _children;

    private GameEventManager _eventMgr;
    private bool _isInitialized;

    protected override void Awake() {
        base.Awake();
        if (TryDestroyExtraCopies()) {
            return;
        }
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
        if (_instance && _instance != this) {
            D.Log("{0}_{1} found as extra. Initiating destruction sequence.", this.name, InstanceID);
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
        D.Log("{0}_{1} has {2} children.".Inject(Instance.name, InstanceID, Folder.childCount));
        Transform[] transforms = gameObject.GetComponentsInChildren<Transform>(includeInactive: true);   // includes the parent t
        foreach (Transform t in transforms) {
            if (t != Folder) {
                //t.parent = Instance.transform;
                UnityUtility.AttachChildToParent(t.gameObject, Instance.gameObject);
                D.Log("Child [{0}].parent changed to {1}_{2}.".Inject(t.name, Instance.name, InstanceID));
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
        _children = (from t in transforms where t != Folder select t).ToArray<Transform>();
        Folder.DetachChildren();
    }

    private void OnSceneChanged(SceneChangedEvent e) {
        var childrenToReattach = from t in _children where t != null select t;
        //childrenToReattach.ForAll<Transform>(t => t.parent = Folder);
        childrenToReattach.ForAll<Transform>(t => UnityUtility.AttachChildToParent(t.gameObject, Folder.gameObject));
        __FixGameObjectName();
    }

    /// <summary>
    /// Temporary. Changes IntroManagement to GameManagement when transitioning
    /// from IntroScene to GameScene. Had to change the name of the gameobject to make
    /// a separate prefab for IntroManagement.
    /// </summary>
    private void __FixGameObjectName() {
        gameObject.name = "GameManagement";
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (_isInitialized) {
            // no reason to cleanup if this object was destroyed before it was initialized.
            Dispose();
        }
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _eventMgr.RemoveListener<SceneChangingEvent>(this, OnSceneChanging);
        _eventMgr.RemoveListener<SceneChangedEvent>(this, OnSceneChanged);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
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
            Cleanup();
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


}


