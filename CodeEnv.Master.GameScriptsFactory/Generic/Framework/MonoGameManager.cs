// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MonoGameManager.cs
// MonoBehaviour version of the GameManager which has access to the Unity event system. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using System.Globalization;
using System.Threading;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;


/// <summary>
///MonoBehaviour version of the GameManager which has access to the Unity event system. All the work
///should be done by GameManager. The purpose of this class is to call GameManager.
/// </summary>
//[SerializeAll] This is redundant as this Object already has a StoreInformation script on it. It causes duplication of referenced SIngletons when saving
public class MonoGameManager : AMonoBehaviourBaseSingleton<MonoGameManager>, IDisposable, IInstanceIdentity {

    private GameManager gameMgr;
    private GameEventManager eventMgr;

    void Awake() {
        //Debug.Log("MonoGameManager Awake() called. IsEnabled = " + enabled);
        IncrementInstanceCounter();
        if (TryDestroyExtraCopies()) {
            return;
        }

        // TODO add choose language GUI
        //string language = "fr-FR";
        // ChangeLanguage(language);

        eventMgr = GameEventManager.Instance;
        AddListeners();
        gameMgr = GameManager.Instance;
        AwakeBasedOnStartScene();
    }

    private void ChangeLanguage(string language) {
        CultureInfo newCulture = new CultureInfo(language);
        Thread.CurrentThread.CurrentCulture = newCulture;
        Thread.CurrentThread.CurrentUICulture = newCulture;
        Debug.Log("Current culture of thread is {0}.".Inject(Thread.CurrentThread.CurrentUICulture.DisplayName));
        Debug.Log("Current OS Language of Unity is {0}.".Inject(Application.systemLanguage.GetName()));
    }


    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each sscene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (instance != null && instance != this) {
            Debug.Log("Extra {0} found. Now destroying.".Inject(this.name));
            Destroy(gameObject);
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            instance = this;
            return false;
        }
    }

    private void AddListeners() {

    }

    void Start() {
        StartBasedOnStartScene();
    }

    #region Startup Simulation
    private void AwakeBasedOnStartScene() {
        SceneLevel startScene = (SceneLevel)Application.loadedLevel;
        switch (startScene) {
            case SceneLevel.IntroScene:
                break;
            case SceneLevel.GameScene:
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(startScene));
        }
        gameMgr.AwakeBasedOnStartScene(startScene);
    }

    private void StartBasedOnStartScene() {
        SceneLevel startScene = (SceneLevel)Application.loadedLevel;
        switch (startScene) {
            case SceneLevel.IntroScene:
                break;
            case SceneLevel.GameScene:
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(startScene));
        }
        gameMgr.StartBasedOnStartScene(startScene);
    }
    #endregion

    void OnEnable() {
        // TODO - Fixed as of Unity 4.0. Now to test... Reqd due to bug in script execution order. Scripts with an OnEnable() method will always be first
        // in execution order, effectively ignoring execution order project settings. As _CameraControl uses OnEnable(), it 
        // always was called first. Placing this empty method here makes script execution order settings effective.
    }

    // This simply substitutes my own Event for OnLevelWasLoaded so I don't have to use OnLevelWasLoaded anywhere else
    // Wiki: OnLevelWasLoaded is NOT guaranteed to run before all of the Awake calls. In most cases it will, but in some 
    // might produce some unexpected bugs. If you need some code to be executed before Awake calls, use OnDisable instead.
    void OnLevelWasLoaded(int level) {
        Debug.Log("Loader.OnLevelWasLoaded(level = {0}) called.".Inject(level));
        if (eventMgr != null) { // event can be called even when gameobject is being destroyed
            eventMgr.Raise<SceneLevelChangedEvent>(new SceneLevelChangedEvent(this, (SceneLevel)level));
        }
    }

    void OnDeserialized() {
        gameMgr.OnDeserialized();
    }

    // IMPROVE when to add/remove GameManager EventListeners? This removes them.
    void OnDestroy() {
        Debug.Log("A {0} instance is being destroyed.".Inject(this.name));
        Dispose();
    }

    protected override void OnApplicationQuit() {
        //System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        //Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));
        instance = null;
    }

    private void RemoveListeners() {
        if (eventMgr != null) {

        }
    }

    #region IDisposable
    [NonSerialized]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <arg name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</arg>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            RemoveListeners();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

