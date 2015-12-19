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
/// Singleton for easy access to this Management folder in all scenes. 
/// Persists from scene to scene detaching any non-persistent children from the old scene, 
/// then re-attaching any non-persistent children present in the new scene 
/// that are attached to the extra instance that is about to be destroyed.
/// </summary>
public class ManagementFolder : AFolderAccess<ManagementFolder> {

    public override bool IsPersistentAcrossScenes { get { return true; } }

    protected override bool IsRootGameObject { get { return true; } }

    private IGameManager _gameMgr;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameMgr = References.GameManager;
        Subscribe();
    }

    private void Subscribe() {
        _gameMgr.sceneLoading += SceneLoadingEventHandler;
    }

    protected override void ExecutePriorToDestroy() {
        base.ExecutePriorToDestroy();
        TransferNonpersistentChildren();
    }

    /// <summary>
    /// Transfers the non-persistent children of this extra copy of the ManagementFolder (about to be destroyed) 
    /// to the instance that persists. The non-persistent children (if any) of the ManagementFolder that persists have already 
    /// been detached and destroyed by DetachNonPersistentChildren() so they need to be replaced with those (if any) present in the new scene.
    /// </summary>
    private void TransferNonpersistentChildren() {
        var allImmediateChildren = gameObject.GetComponentsInImmediateChildren<AMonoBase>();
        allImmediateChildren.ForAll(child => D.Assert(child is AMonoBaseSingleton));   // all children of ManagementFolder are singletons, so far...
        var allImmediateSingletonChildren = allImmediateChildren.Cast<AMonoBaseSingleton>();
        var nonPersistentChildren = allImmediateSingletonChildren.Where(child => !child.IsPersistentAcrossScenes);
        nonPersistentChildren.ForAll(npChild => UnityUtility.AttachChildToParent(child: npChild.gameObject, parent: Instance.gameObject));
    }

    #region Event and Property Change Handlers

    private void SceneLoadingEventHandler(object sender, EventArgs e) {
        DetachNonPersistentChildren();
    }

    #endregion

    /// <summary>
    /// The instance that is persisting has this called before the new scene starts loading. It detaches 
    /// (and therefore allows their automatic destruction) any non-persistent children from this persisting instance
    /// so they can be automatically destroyed during the scene transition. Their counterparts (if any) present in the new scene
    /// will be [re]attached to this instance by TransferNonpersistentChildren().
    /// </summary>
    private void DetachNonPersistentChildren() {
        var allImmediateChildren = gameObject.GetComponentsInImmediateChildren<AMonoBase>();
        allImmediateChildren.ForAll(child => D.Assert(child is AMonoBaseSingleton));   // all children of ManagementFolder are singletons, so far...
        var allImmediateSingletonChildren = allImmediateChildren.Cast<AMonoBaseSingleton>();
        var nonPersistentImmediateChildren = allImmediateSingletonChildren.Where(child => !child.IsPersistentAcrossScenes);
        nonPersistentImmediateChildren.ForAll(npChild => UnityUtility.AttachChildToParent(child: npChild.gameObject, parent: null));
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _gameMgr.sceneLoading -= SceneLoadingEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Record and Attach Children Archive

    //private IEnumerable<Transform> _formerChildren;

    //private void SceneLoadingEventHandler(object sender, EventArgs e) {
    //    RecordAndDetachChildren();
    //}

    //private void SceneLoadedEventHandler(object sender, EventArgs e) {
    //    ReattachPersistentChildren();
    //}

    /// <summary>
    /// Transfers the children of this extra copy (that is about to be destroyed) to the instance that persists.
    /// The children transfered may subsequently persist themselves or they may be destroyed.
    /// </summary>
    //private void TransferChildren() {
    //    D.Log("{0}_{1} has {2} children being reparented to {3}_{4}.",
    //        this.name, this.InstanceCount, this.transform.childCount, Instance.name, Instance.InstanceCount);
    //    var children = this.gameObject.GetComponentsInImmediateChildren<Transform>();
    //    children.ForAll(child => {
    //        UnityUtility.AttachChildToParent(child.gameObject, Instance.gameObject);
    //        D.Log("Before being destroyed, {0}_{1} is attaching its child {2} \nto new parent {3}_{4}. The child may not survive.",
    //            this.name, InstanceCount, child.name, Instance.name, Instance.InstanceCount);
    //    });
    //}

    /// <summary>
    /// The instance that is persisting has this called before the new scene starts loading. 
    /// It records and then detaches its children so they can be destroyed (if they are 
    /// not themselves persistent). The recorded children are then used by ReattachPersistentChildren()
    /// to find any that persist and reparent them to this persistent instance.
    /// </summary>
    //private void RecordAndDetachChildren() {
    //    _formerChildren = gameObject.GetComponentsInImmediateChildren<Transform>();
    //    Instance.transform.DetachChildren();
    //}

    //private void ReattachPersistentChildren() {
    //    var persistentChildren = _formerChildren.Where(t => t != null);
    //    persistentChildren.ForAll(persistentChild => {
    //        UnityUtility.AttachChildToParent(persistentChild.gameObject, Instance.gameObject);
    //        D.Log("{0}_{1} is reattaching its persistent child {2}.", Instance.name, InstanceCount, persistentChild.name);
    //    });
    //}

    #endregion

}


