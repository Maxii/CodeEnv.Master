// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ColliderRelay.cs
// Simple script that relays Collider event's received by this gameobject to one or more Target gameobjects
// that implement the ICollider interface.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Simple script that relays Collider event's received by this gameobject to one or more Target gameobjects
/// that implement the ICollider interface.
///<remarks>Typically used when the collider for an object heirarchy is implemented in a child.</remarks>
///<remarks>Obsolete as it can't do anything with Raycasts from the camera.</remarks>
/// </summary>
[Obsolete, RequireComponent(typeof(Collider))]
public class ColliderRelay : AMonoBehaviourBase {

    public Transform[] relayTargets;

    void Awake() {
        if (relayTargets.Length == 0) {
            Transform parent = transform.parent;
            relayTargets = new Transform[1];
            relayTargets[0] = parent;
            Debug.Log("Parent {0} auto assigned as {1}.".Inject(parent.name, typeof(IColliderRelayTarget)));
        }

        foreach (var t in relayTargets) {
            if (t.GetInterface<IColliderRelayTarget>() == null) {
                Debug.LogWarning("Assigned GameObject {0} is not of Type {1}.".Inject(t.gameObject.name, typeof(IColliderRelayTarget).Name));
                return;
            }
        }
    }

    void OnHover(bool isOver) {
        foreach (var t in relayTargets) {
            IColliderRelayTarget i = t.GetInterface<IColliderRelayTarget>();
            if (i != null) {
                i.OnHover(isOver);
            }
        }
    }

    void OnClick() {
        foreach (var t in relayTargets) {
            IColliderRelayTarget i = t.GetInterface<IColliderRelayTarget>();
            if (i != null) {
                i.OnClick();
            }
        }
    }

    void OnDoubleClick() {
        foreach (var t in relayTargets) {
            IColliderRelayTarget i = t.GetInterface<IColliderRelayTarget>();
            if (i != null) {
                i.OnDoubleClick();
            }
        }
    }


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

