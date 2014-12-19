// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonoSingleton.cs
// Abstract base Singleton Pattern for MonoBehaviours that initializes when Instance is first called. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract base Singleton Pattern for MonoBehaviours that initializes when Instance is first called. 
/// Also has responsibility for destroying extra copies of instances which implement DontDestroyOnLoad.
/// </summary>
/// <typeparam name="T">The Type of the derived class.</typeparam>
public abstract class AMonoSingleton<T> : AMonoBase, IInstanceCount where T : AMonoSingleton<T> {

    /// <summary>
    /// Determines whether this singleton is persistent across scenes. If not persistent, it
    /// is destroyed on each scene load. If it is persistent, then it is not destroyed on a scene
    /// load, and any instances already present in the new scene are destroyed.
    /// </summary>
    protected virtual bool IsPersistentAcrossScenes { get { return false; } }

    #region MonoBehaviour Singleton Pattern

    protected static T _instance;
    public static T Instance {
        get {
            if (_instance == null) {
                if (IsApplicationQuiting) {
                    //D.Warn("Application is quiting while trying to access {0}.Instance.".Inject(typeof(T).Name));
                    return null;
                }
                // Instance is required for the first time, so look for it                        
                Type thisType = typeof(T);
                _instance = GameObject.FindObjectOfType(thisType) as T;
                // value is required for the first time, so look for it                        
                if (_instance == null && !Application.isLoadingLevel) {
                    var stackFrame = new System.Diagnostics.StackTrace().GetFrame(2);
                    string callerIdMessage = "{0}.{1}().".Inject(stackFrame.GetMethod().DeclaringType, stackFrame.GetMethod().Name);
                    D.Error("No instance of {0} found. Is it destroyed/deactivated? Called by {1}.".Inject(thisType.Name, callerIdMessage));
                }
                _instance.InitializeOnInstance();
            }
            return _instance;
        }
    }

    #endregion

    protected sealed override void Awake() {
        base.Awake();
        // If no other MonoBehaviour has requested Instance in an Awake() call executing
        // before this one, then we are it. There is no reason to search for an object
        if (_instance == null) {
            var tempInstanceCount = _instanceCounter + 1;  // HACK as InitializeOnInstance doesn't get called for extra copies so can't increment there
            D.Log("{0}_{1} is initializing Instance from Awake().", GetType().Name, tempInstanceCount);
            _instance = this as T;
            InitializeOnInstance();
        }

        IncrementInstanceCounter();

        if (IsPersistentAcrossScenes) {
            if (TryDestroyExtraCopies()) {
                // this extra copy is being destroyed so don't initialize
                return;
            }
        }
        InitializeOnAwake();
    }

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// Note: This method is not called by instance copies, only by the original instance. If not persistent across scenes,
    /// then this method will be called each time the new instance in a scene is instantiated.
    /// </summary>
    protected virtual void InitializeOnInstance() {
        //var tempInstanceID = _instanceCounter + 1;  // HACK as InitializeOnInstance doesn't get called for extra copies so can't increment there
        //D.Log("{0}_{1}.InitializeOnInstance() called.", GetType().Name, tempInstanceID);
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected virtual void InitializeOnAwake() {
        //D.Log("{0}_{1}.InitializeOnAwake() called.", GetType().Name, InstanceID);
    }

    #region Cross Scene Persistence System

    /// <summary>
    /// Flag indicating whether this instance is an extra copy. If true, then it has not been initialized
    /// and is slated for destruction. Used to determine cleanup behaviour during OnDestroy().
    /// </summary>
    protected bool _isExtraCopy;

    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each scene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (_instance && _instance != this) {
            D.Log("{0}_{1} is extra. Initiating destruction sequence.".Inject(gameObject.name, InstanceCount));
            _isExtraCopy = true;
            ExecutePriorToDestroy();
            D.Log("Destroying {0}_{1}.", gameObject.name, InstanceCount);
            Destroy(gameObject);
        }
        else {
            DontDestroyOnLoad(gameObject);
            //_instance = this as T;
        }
        return _isExtraCopy;
    }

    /// <summary>
    /// Hook method for any work to be done before the extra copy is destroyed.
    /// Default does nothing.
    /// </summary>
    protected virtual void ExecutePriorToDestroy() { }

    #endregion

    #region Cleanup

    protected sealed override void OnDestroy() {
        if (_isExtraCopy) {
            // no reason to cleanup if never initialized
            return;
        }
        Cleanup();
        if (!IsPersistentAcrossScenes) {
            // Warning: nulling the static _instance of Singletons that persist across scenes will affect both copies of the singleton
            _instance = null;
        }
    }

    protected override void OnApplicationQuit() {
        base.OnApplicationQuit();
        _instance = null;
    }

    #endregion

    #region Debug

    private static int _instanceCounter = 0;

    private void IncrementInstanceCounter() {
        InstanceCount = System.Threading.Interlocked.Increment(ref _instanceCounter);
        //D.Log("{0}.InstanceID now set to {1}, static counter now {2}.", typeof(T).Name, InstanceID, _instanceCounter);
    }

    /// <summary>
    /// Logs the method name called. WARNING:  Coroutines showup as &lt;IEnumerator.MoveNext&gt; rather than the method name
    /// </summary>
    public override void LogEvent() {
        if (DebugSettings.Instance.EnableEventLogging) {
            var stackFrame = new System.Diagnostics.StackFrame(1);
            string name = _transform.name + "(from transform)";
            Debug.Log("{0}.{1}_{2}.{3}() called.".Inject(name, GetType().Name, InstanceCount, stackFrame.GetMethod().Name));
        }
    }

    #endregion

    #region IInstanceCount Members

    public int InstanceCount { get; private set; }

    #endregion

}


