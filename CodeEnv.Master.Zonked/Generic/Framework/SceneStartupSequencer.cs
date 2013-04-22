// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SceneStartupSequencer.cs
// Raises events that help control startup sequencing in scene transitions.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR


// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Raises events that help control startup sequencing when making scene transitions. A new
/// instance of this class will be created in each new scene instance. This class
/// needs to be immediately after MonoGameManager and Loader in Unity's Script Execution Order.
/// </summary>
[Obsolete]
public class SceneStartupSequencer : MonoBehaviourBase, IInstanceIdentity {

    private GameEventManager eventMgr;
    private bool isFirstUpdate = true;
    private bool isFirstLateUpdate = true;

    void Awake() {
        IncrementInstanceCounter();
        eventMgr = GameEventManager.Instance;
        eventMgr.Raise<SceneStartupSequenceEvent>(new SceneStartupSequenceEvent(this, SceneStartupEventName.Awake));
    }

    void Start() {
        eventMgr.Raise<SceneStartupSequenceEvent>(new SceneStartupSequenceEvent(this, SceneStartupEventName.Start));
    }

    void Update() {
        if (isFirstUpdate) {
            eventMgr.Raise<SceneStartupSequenceEvent>(new SceneStartupSequenceEvent(this, SceneStartupEventName.Update));
            isFirstUpdate = false;
        }
    }

    void LateUpdate() {
        if (isFirstLateUpdate) {
            eventMgr.Raise<SceneStartupSequenceEvent>(new SceneStartupSequenceEvent(this, SceneStartupEventName.LateUpdate));
            isFirstLateUpdate = false;
            Destroy(gameObject);
        }
    }

    void OnEnable() {
        // Reqd due to bug in script execution order. Scripts with an OnEnable() method will always be first
        // in execution order, effectively ignoring execution order project settings. As _CameraControl uses OnEnable(), it 
        // always was called first. Placing this empty method here makes script execution order settings effective.
    }

    void OnDestroy() {
        Debug.Log("{0} is being destroyed.".Inject(this.name));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

