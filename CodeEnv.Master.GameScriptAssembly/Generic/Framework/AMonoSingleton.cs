﻿// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract base Singleton Pattern for MonoBehaviours that initializes when Instance is first called. 
/// Also has responsibility for destroying extra copies of instances if IsPersistentAcrossScenes is <c>true</c>.
/// </summary>
/// <typeparam name="T">The Type of the derived class.</typeparam>
public abstract class AMonoSingleton<T> : AMonoBaseSingleton, IInstanceCount where T : AMonoSingleton<T> {

    /// <summary>
    /// Flag indicating whether this instance is an extra copy. If true, then it has not been initialized
    /// and is slated for destruction. Used to determine cleanup behaviour during OnDestroy().
    /// </summary>
    protected bool IsExtraCopy { get; private set; }

    /// <summary>
    /// Indicates whether this instance is a root GameObject, aka it has no parent. Default is false.
    /// <remarks>If it is a root GameObject AND it is also persistent across scenes, then DontDestroyOnLoad 
    /// is used on this instance to keep it AND its children from being destroyed on a scene transition.
    /// </remarks>
    /// </summary>
    protected virtual bool IsRootGameObject { get { return false; } }

    #region MonoBehaviour Singleton Pattern

    protected static T _instance;
    public static T Instance {
        get {
            if (_instance == null) {
                if (IsApplicationQuiting) {
                    //D.Log("Application is quiting while trying to access {0}.Instance.".Inject(typeof(T).Name));
                    return null;
                }
                // Instance is required for the first time, so look for it                        
                Type thisType = typeof(T);
                _instance = GameObject.FindObjectOfType(thisType) as T;
                // value is required for the first time, so look for it                        
                if (_instance == null) { //if (_instance == null && !Application.isLoadingLevel) {
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
            //var tempInstanceCount = _instanceCounter + 1;  // HACK as InitializeOnInstance doesn't get called for extra copies so can't increment there
            //D.Log("{0}_{1} is initializing Instance from Awake().", GetType().Name, tempInstanceCount);
            _instance = this as T;
            InitializeOnInstance();
        }

        IncrementInstanceCounter();

        if (IsPersistentAcrossScenes) {
            IsExtraCopy = _instance != null && _instance != this;

            if (IsExtraCopy) {
                DestroyExtraCopy();
                // this extra copy is being destroyed so don't initialize
                return;
            }
            else {
                // persistent but not extra
                if (IsRootGameObject) {  // DontDestroyOnLoad only works for Root GameObjects (and their children)
                    DontDestroyOnLoad(gameObject);
                }
                else {
                    // MgmtFolder is the only class setup to keep its persistent children from being destroyed
                    ValidateMgmtFolderIsAParent();
                }
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

    protected override void Start() {
        base.Start();
        // deferred to allow some objects that aren't root to be parented as Awake runs before any dynamic parenting can take place
        ValidateRootGameObjectState();
    }

    /// <summary>
    /// Hook method for any work to be done before the extra copy is destroyed.
    /// Default does nothing.
    /// </summary>
    protected virtual void ExecutePriorToDestroy() { }

    private void DestroyExtraCopy() {
        D.Assert(IsExtraCopy);
        //D.Log("{0}_{1} is extra. Initiating destruction sequence.".Inject(gameObject.name, InstanceCount));
        ExecutePriorToDestroy();
        //D.Log("Destroying {0}_{1}.", gameObject.name, InstanceCount);
        Destroy(gameObject);
    }

    private void ValidateRootGameObjectState() {
        if (IsRootGameObject) {
            D.AssertNull(transform.parent, transform.name);
        }
        else {
            D.AssertNotNull(transform.parent, transform.name);
        }
    }

    private void ValidateMgmtFolderIsAParent() {
        gameObject.GetSingleComponentInParents<ManagementFolder>(excludeSelf: true);
    }

    #region Cleanup

    protected sealed override void OnDestroy() {
        if (IsExtraCopy) {
            // no reason to cleanup if never initialized
            return;
        }
        base.OnDestroy();   // calls Cleanup()
        if (!IsPersistentAcrossScenes) {
            // Warning: nulling the static _instance of Singletons that persist across scenes will affect both copies of the singleton
            _instance = null;
        }
    }

    protected override void __CleanupOnApplicationQuit() {
        base.__CleanupOnApplicationQuit();
        _instance = null;
    }

    #endregion

    #region Debug

    private const string AMonoSingletonDebugLogEventMethodNameFormat = "{0}(from transform).{1}_{2}.{3}()";

    private static int _instanceCounter = 0;

    private void IncrementInstanceCounter() {
        InstanceCount = System.Threading.Interlocked.Increment(ref _instanceCounter);
        //D.Log("{0}.InstanceID now set to {1}, static counter now {2}.", typeof(T).Name, InstanceID, _instanceCounter);
    }

    /// <summary>
    /// Logs a warning statement that the method that calls this has been called. Includes the instance counter to ID the caller.
    /// <remarks>Typically used to ID method calls that I don't expect to occur.</remarks>
    /// </summary>
    public override void LogEventWarning() {
        string methodName = GetCallingMethodName();
        string fullMethodName = AMonoSingletonDebugLogEventMethodNameFormat.Inject(transform.name, GetType().Name, InstanceCount, methodName);
        Debug.LogWarning("Unclear why {0} was called.".Inject(fullMethodName));
    }

    /// <summary>
    /// Logs a statement that the method that calls this has been called. Includes the instance counter to ID the caller.
    /// Logging only occurs if DebugSettings.EnableEventLogging is true.
    /// </summary>
    public override void LogEvent() {
        if (_debugSettings.EnableEventLogging) {
            string methodName = GetCallingMethodName();
            string fullMethodName = AMonoSingletonDebugLogEventMethodNameFormat.Inject(transform.name, GetType().Name, InstanceCount, methodName);
            Debug.Log("{0} beginning execution. Frame {1}, UnityTime {2:0.0}.".Inject(fullMethodName, Time.frameCount, Time.time));
        }
    }

    #endregion

    #region ExtraCopy Destruction Archive

    //protected bool _isExtraCopy;

    //protected sealed override void Awake() {
    //    base.Awake();
    //    // If no other MonoBehaviour has requested Instance in an Awake() call executing
    //    // before this one, then we are it. There is no reason to search for an object
    //    if (_instance == null) {
    //        var tempInstanceCount = _instanceCounter + 1;  // HACK as InitializeOnInstance doesn't get called for extra copies so can't increment there
    //        D.Log("{0}_{1} is initializing Instance from Awake().", GetType().Name, tempInstanceCount);
    //        _instance = this as T;
    //        InitializeOnInstance();
    //    }

    //    IncrementInstanceCounter();

    //    if (IsPersistentAcrossScenes) {
    //        if (TryDestroyExtraCopies()) {
    //            // this extra copy is being destroyed so don't initialize
    //            return;
    //        }
    //    }
    //    InitializeOnAwake();
    //}

    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each scene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    //private bool TryDestroyExtraCopies() {
    //    if (_instance && _instance != this) {
    //        D.Log("{0}_{1} is extra. Initiating destruction sequence.".Inject(gameObject.name, InstanceCount));
    //        _isExtraCopy = true;
    //        ExecutePriorToDestroy();
    //        D.Log("Destroying {0}_{1}.", gameObject.name, InstanceCount);
    //        Destroy(gameObject);
    //    }
    //    else {
    //        D.Warn("{0}.DontDestroyOnLoad() about to be called.", GetType().Name);
    //        DontDestroyOnLoad(gameObject);
    //    }
    //    return _isExtraCopy;
    //}

    #endregion

    #region IInstanceCount Members

    public int InstanceCount { get; private set; }

    #endregion


}


