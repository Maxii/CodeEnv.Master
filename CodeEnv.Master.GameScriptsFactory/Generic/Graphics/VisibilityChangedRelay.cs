// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VisibilityChangedRelay.cs
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
public class VisibilityChangedRelay : AMonoBehaviourBase {

    public Transform[] relayTargets;

    private INotifyVisibilityChanged[] _iRelayTargets;
    private Transform _transform;

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<Renderer>(gameObject);
        _transform = transform;
        if (relayTargets.Length == 0) {
            Transform relayTarget = _transform.GetSafeTransformWithInterfaceInParents<INotifyVisibilityChanged>();
            if (relayTarget == null) {
                D.Warn("No {0} assigned or found for {1}.", typeof(INotifyVisibilityChanged), _transform.name);
                return;
            }
            relayTargets = new Transform[1] { relayTarget };
        }

        int length = relayTargets.Length;
        _iRelayTargets = new INotifyVisibilityChanged[length];

        for (int i = 0; i < length; i++) {
            INotifyVisibilityChanged iTarget = relayTargets[i].GetInterface<INotifyVisibilityChanged>();
            if (iTarget == null) {
                D.Warn("{0} is not an {1}.", relayTargets[i].name, typeof(INotifyVisibilityChanged));
                continue;
            }
            _iRelayTargets[i] = iTarget;
        }
    }

    void OnBecameVisible() {
        for (int i = 0; i < relayTargets.Length; i++) {
            INotifyVisibilityChanged iNotify = _iRelayTargets[i];
            if (iNotify != null) {
                //Logger.Log("{0} is notifying client {1} of becoming Visible.", _transform.name, relayTargets[i].name);
                iNotify.NotifyVisibilityChanged(_transform, isVisible: true);
            }
        }
        // more efficient and easier but can't provide the client target name for debug
        //foreach (var iNotify in _iRelayTargets) {    
        //    if (iNotify != null) {
        //        iNotify.NotifyVisibilityChanged(_transform, isVisible: true);
        //        Logger.Log("{0} has notified a client of becoming Visible.", _transform.name);
        //    }
        //}
    }

    void OnBecameInvisible() {
        for (int i = 0; i < relayTargets.Length; i++) {
            Transform t = relayTargets[i];
            if (t && t.gameObject.activeInHierarchy) {  // avoids NullReferenceException during Inspector shutdown
                INotifyVisibilityChanged iNotify = _iRelayTargets[i];
                if (iNotify != null) {
                    //Logger.Log("{0} is notifying client {1} of becoming Invisible.", _transform.name, t.name);
                    iNotify.NotifyVisibilityChanged(_transform, isVisible: false);
                }
            }
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

