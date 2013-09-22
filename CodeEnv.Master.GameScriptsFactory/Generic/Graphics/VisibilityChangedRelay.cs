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
using CodeEnv.Master.GameContent;
using UnityEngine;
using System;

/// <summary>
/// Simple script that relays changes in its Renderer's visibility state to one or more Target gameobjects
/// that implement the IOnVisible interface.
///<remarks>Used when I wish to separate a mesh and its renderer from a parent GameObject that does most of the work.</remarks>
/// </summary>
public class VisibilityChangedRelay : AMonoBehaviourBase {

    public Transform[] relayTargets;

    private INotifyVisibilityChanged[] _iRelayTargets;

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<Renderer>(gameObject);
        if (relayTargets.Length == 0) {
            Transform relayTarget = _transform.GetSafeTransformWithInterfaceInParents<INotifyVisibilityChanged>();
            if (relayTarget != null) {
                D.Warn("{0} {1} target field is not assigned. Automatically assigning {1} as target.", _transform.name, this.GetType().Name, relayTarget);
            }
            else {
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

    private bool _isVisible;
    void OnBecameVisible() {
        if (ValidateVisibilityChange(isVisible: true)) {
            for (int i = 0; i < relayTargets.Length; i++) {
                INotifyVisibilityChanged iNotify = _iRelayTargets[i];
                if (iNotify != null) {
                    ReportVisibilityChange(_transform.name, relayTargets[i].name, isVisible: true);
                    iNotify.NotifyVisibilityChanged(_transform, isVisible: true);
                }
            }
            // more efficient and easier but can't provide the client target name for debug
            //foreach (var iNotify in _iRelayTargets) {    
            //    if (iNotify != null) {
            //        iNotify.NotifyVisibilityChanged(_transform, isVisible: true);
            //        D.Log("{0} has notified a client of becoming Visible.", _transform.name);
            //    }
            //}
            _isVisible = true;
        }
    }

    void OnBecameInvisible() {
        if (ValidateVisibilityChange(isVisible: false)) {
            for (int i = 0; i < relayTargets.Length; i++) {
                Transform t = relayTargets[i];
                if (t && t.gameObject.activeInHierarchy) {  // avoids NullReferenceException during Inspector shutdown
                    INotifyVisibilityChanged iNotify = _iRelayTargets[i];
                    if (iNotify != null) {
                        ReportVisibilityChange(_transform.name, relayTargets[i].name, isVisible: false);
                        iNotify.NotifyVisibilityChanged(_transform, isVisible: false);
                    }
                }
            }
            _isVisible = false;
        }
    }

    [System.Diagnostics.Conditional("DEBUG_LOG")]
    private void ReportVisibilityChange(string notifier, string client, bool isVisible) {
        if (DebugSettings.Instance.EnableVerboseDebugLog) {
            string iNotifyParentName = _transform.GetSafeTransformWithInterfaceInParents<INotifyVisibilityChanged>().name;
            string visibility = isVisible ? "Visible" : "Invisible";
            D.Log("{0} of parent {1} is notifying client {2} of becoming {3}.", notifier, iNotifyParentName, client, visibility);
        }
    }

    private bool ValidateVisibilityChange(bool isVisible) {
        bool isValid = true;
        string visibility = isVisible ? "Visible" : "Invisible";
        if (isVisible == _isVisible) {
            D.Warn("Duplicate {0}.OnBecame{1}() received and filtered out.", gameObject.name, visibility);
            isValid = false;
        }
        if (gameObject.activeInHierarchy) {
            if (isVisible != renderer.IsVisibleFrom(Camera.main)) {
                D.Warn("{0}.OnBecame{1}() received from a camera that is not Camera.main.", gameObject.name, visibility);
                isValid = false;
            }
        }
        return isValid;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

