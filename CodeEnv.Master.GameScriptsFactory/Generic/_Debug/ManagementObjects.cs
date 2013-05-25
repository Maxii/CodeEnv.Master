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

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

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
public class ManagementObjects : AMonoBehaviourBaseSingleton<ManagementObjects>, IDisposable {

    /// <summary>
    /// Gets the ManagementObjects folder.
    /// </summary>
    /// <tPrefsValue>
    /// The folder's Transform.
    /// </tPrefsValue>
    public static Transform Folder {
        get { return Instance.transform; }
    }

    private Transform[] children;

    private GameEventManager eventMgr;

    void Awake() {
        if (TryDestroyExtraCopies()) {
            return;
        }
        eventMgr = GameEventManager.Instance;
        AddListeners();
    }

    /// <summary>
    /// Ensures that no matter how many scenes this ManagementObjects is
    /// in (having one dedicated to each startScene is useful for testing) there's only ever one copy
    /// in memory if you make a startScene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (instance != null && instance != this) {
            Debug.Log("Extra {0} found. Now destroying.".Inject(this.name));
            TransferChildrenThenDestroy();
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            instance = this;
            return false;
        }
    }

    private void TransferChildrenThenDestroy() {
        Debug.Log("{0} has {1} children.".Inject(this.name, transform.childCount));
        Transform[] transforms = gameObject.GetComponentsInChildren<Transform>(includeInactive: true);   // includes the parent t
        foreach (Transform t in transforms) {
            if (t != transform) {
                t.parent = Instance.transform;
                Debug.Log("Child [{0}].parent changed to {1}.".Inject(t.name, Instance.name));
            }
        }
        Destroy(gameObject);
    }

    private void AddListeners() {
        eventMgr.AddListener<SceneLevelChangingEvent>(this, OnSceneLevelChanging);
        eventMgr.AddListener<SceneLevelChangedEvent>(this, OnSceneLevelChanged);
    }

    /// <summary>
    /// The instance that is not going to be destroyed has this called when [startScene level change].
    /// This implementation records and then detaches its children so they can die if they are 
    /// supposed to. The record of who the children were will be used by OnLevelWasLoaded 
    /// to find any former children still alive after the startScene transition and reparent them.
    /// </summary>
    /// <arg name="e">The e.</arg>
    /// <exception cref="System.NotImplementedException"></exception>
    private void OnSceneLevelChanging(SceneLevelChangingEvent e) {
        Transform[] transforms = gameObject.GetComponentsInChildren<Transform>(includeInactive: true);
        children = (from t in transforms where t != transform select t).ToArray<Transform>();
        transform.DetachChildren();
    }

    private void OnSceneLevelChanged(SceneLevelChangedEvent e) {
        if (children != null) { // this method is still called even if the object is going to be destroyed
            var childrenToReattach = from t in children where t != null select t;
            childrenToReattach.ForAll<Transform>(t => t.parent = transform);
        }
    }

    // Make sure values isn't referenced anymore
    protected override void OnApplicationQuit() {
        instance = null;
    }

    void OnDestroy() {
        Debug.Log("A {0} instance is being destroyed.".Inject(this.name));
        Dispose();
    }

    private void RemoveListeners() {
        if (eventMgr != null) {
            eventMgr.RemoveListener<SceneLevelChangingEvent>(this, OnSceneLevelChanging);
            eventMgr.RemoveListener<SceneLevelChangedEvent>(this, OnSceneLevelChanged);
        }
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
            RemoveListeners();
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


