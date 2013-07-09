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
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Simple script that relays changes in its Renderer's visibility state to one or more Target gameobjects
/// that implement the IOnVisible interface.
///<remarks>Used when I wish to separate a mesh and its renderer from a parent GameObject that does most of the work.</remarks>
/// </summary>
public class VisibilityRelay : AMonoBehaviourBase {

    public Transform[] relayTargets;

    void Awake() {
        UnityUtility.ValidateComponentPresence<Renderer>(gameObject);
        if (relayTargets.Length == 0) {
            Transform parent = transform.parent;
            relayTargets = new Transform[1];
            relayTargets[0] = parent;
            Debug.Log("Parent {0} auto assigned as {1}.".Inject(parent.name, typeof(IOnVisibleRelayTarget)));
        }

        foreach (var t in relayTargets) {
            if (t.GetInterface<IOnVisibleRelayTarget>() == null) {
                Debug.LogWarning("Assigned GameObject {0} is not of Type {1}.".Inject(t.gameObject.name, typeof(IOnVisibleRelayTarget).Name));
                return;
            }
        }
    }

    void OnBecameVisible() {
        foreach (var t in relayTargets) {
            IOnVisibleRelayTarget i = t.GetInterface<IOnVisibleRelayTarget>();
            if (i != null) {
                i.OnBecameVisible();
            }
        }
    }

    void OnBecameInvisible() {
        foreach (var t in relayTargets) {
            if (t && t.gameObject.activeInHierarchy) {  // avoids NullReferenceException during Inspector shutdown
                IOnVisibleRelayTarget i = t.GetInterface<IOnVisibleRelayTarget>();
                if (i != null) {
                    i.OnBecameInvisible();
                }
            }
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

