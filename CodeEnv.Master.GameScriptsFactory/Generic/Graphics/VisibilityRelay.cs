// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VisibilityRelay.cs
// Simple script that relays changes in its Renderer's visibility state to one or more Target gameobjects
// that implement the IOnVisible interface.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Simple script that relays changes in its Renderer's visibility state to one or more Target gameobjects
/// that implement the IOnVisible interface.
///<remarks>Used when I wish to separate a mesh and its renderer from a parent GameObject that does most of the work.</remarks>
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class VisibilityRelay : AMonoBehaviourBase {

    public Transform[] visibilityRelayTargets;
    //private IOnVisible[] iRelayTargets;

    void Awake() {
        if (visibilityRelayTargets.Length == 0) {
            Transform parent = transform.parent;
            IOnVisible iOnVisibleParent = parent.gameObject.GetInterface<IOnVisible>();
            if (iOnVisibleParent == null) {
                Debug.LogError("No {0} targets assigned to {1}.".Inject(typeof(IOnVisible).Name, gameObject.name));
                return;
            }
            visibilityRelayTargets = new Transform[1];
            visibilityRelayTargets[0] = parent;
        }

        //int arrayLength = visibilityRelayTargets.Length;
        //iRelayTargets = new IOnVisible[arrayLength];
        //for (int i = 0; i < arrayLength; i++) {
        //    IOnVisible iRelayTarget = visibilityRelayTargets[i].gameObject.GetInterface<IOnVisible>();
        //    if (iRelayTarget == null) {
        //        Debug.LogError("Assigned GameObject {0} is not of Type {1}.".Inject(visibilityRelayTargets[i].gameObject.name, typeof(IOnVisible).Name));
        //        return;
        //    }
        //    iRelayTargets[i] = iRelayTarget;
        //}

        foreach (var t in visibilityRelayTargets) {
            if (t.GetInterface<IOnVisible>() == null) {
                Debug.LogError("Assigned GameObject {0} is not of Type {1}.".Inject(t.gameObject.name, typeof(IOnVisible).Name));
                return;
            }
        }
    }

    void OnBecameVisible() {
        foreach (var t in visibilityRelayTargets) {
            (t.GetInterface<IOnVisible>()).OnBecameVisible();
        }
    }

    void OnBecameInvisible() {
        foreach (var t in visibilityRelayTargets) {
            if (t && t.gameObject.activeInHierarchy) {  // avoids NullReferenceException during Inspector shutdown
                (t.GetInterface<IOnVisible>()).OnBecameInvisible();
            }
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

