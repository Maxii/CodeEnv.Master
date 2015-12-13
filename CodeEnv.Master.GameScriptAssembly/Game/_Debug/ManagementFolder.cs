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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton for easy access to this Management folder in all scenes. Persists from scene to scene
/// attaching any Management folder child objects in the new scene to this persistent folder, then destroys
/// the Management folder that was already present in the new scene.
/// </summary>
public class ManagementFolder : AFolderAccess<ManagementFolder> {

    protected override bool IsPersistentAcrossScenes { get { return true; } }

    private IEnumerable<Transform> _formerChildren;
    private IGameManager _gameMgr;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameMgr = References.GameManager;
        Subscribe();
    }

    protected override void ExecutePriorToDestroy() {
        base.ExecutePriorToDestroy();
        TransferChildren();
    }

    /// <summary>
    /// Transfers the children of this extra copy (that is about to be destroyed) to the instance that persists.
    /// The children transfered may subsequently persist themselves or they may be destroyed.
    /// </summary>
    private void TransferChildren() {
        D.Log("{0}_{1} has {2} children being reparented to {3}_{4}.",
            this.name, this.InstanceCount, this.transform.childCount, Instance.name, Instance.InstanceCount);
        var children = this.gameObject.GetComponentsInImmediateChildren<Transform>();
        children.ForAll(child => {
            UnityUtility.AttachChildToParent(child.gameObject, Instance.gameObject);
            D.Log("Before being destroyed, {0}_{1} is attaching its child {2} \nto new parent {3}_{4}. The child may not survive.",
                this.name, InstanceCount, child.name, Instance.name, Instance.InstanceCount);
        });
    }

    private void Subscribe() {
        _gameMgr.sceneLoading += SceneLoadingEventHandler;
        _gameMgr.sceneLoaded += SceneLoadedEventHandler;
    }

    #region Event and Property Change Handlers

    private void SceneLoadingEventHandler(object sender, EventArgs e) {
        RecordAndDetachChildren();
    }

    private void SceneLoadedEventHandler(object sender, EventArgs e) {
        ReattachPersistentChildren();
    }

    #endregion

    /// <summary>
    /// The instance that is persisting has this called before the new scene starts loading. 
    /// It records and then detaches its children so they can be destroyed (if they are 
    /// not themselves persistent). The recorded children are then used by ReattachPersistentChildren()
    /// to find any that persist and reparent them to this persistent instance.
    /// </summary>
    private void RecordAndDetachChildren() {
        _formerChildren = gameObject.GetComponentsInImmediateChildren<Transform>();
        Instance.transform.DetachChildren();
    }

    private void ReattachPersistentChildren() {
        var persistentChildren = _formerChildren.Where(t => t != null);
        persistentChildren.ForAll(persistentChild => {
            UnityUtility.AttachChildToParent(persistentChild.gameObject, Instance.gameObject);
            D.Log("{0}_{1} is reattaching its persistent child {2}.", Instance.name, InstanceCount, persistentChild.name);
        });
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _gameMgr.sceneLoading -= SceneLoadingEventHandler;
        _gameMgr.sceneLoaded -= SceneLoadedEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


