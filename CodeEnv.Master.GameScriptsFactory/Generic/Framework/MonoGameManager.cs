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

// default namespace

using System;
using System.Collections;
using System.Globalization;
using System.Threading;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///MonoBehaviour version of the GameManager which has access to the Unity event system. All the work
///should be done by GameManager. The purpose of this class is to call GameManager.
/// </summary>
//[SerializeAll] This is redundant as this Object already has a StoreInformation script on it. It causes duplication of referenced SIngletons when saving
public class MonoGameManager : AMonoBehaviourBaseSingletonInstanceIdentity<MonoGameManager> {

    private GameManager _gameMgr;
    private GameEventManager _eventMgr;

    protected override void Awake() {
        base.Awake();
        if (TryDestroyExtraCopies()) {
            return;
        }

        // TODO add choose language GUI
        //string language = "fr-FR";
        // ChangeLanguage(language);
        _gameMgr = GameManager.Instance;
        _gameMgr.CompleteInitialization();
        _eventMgr = GameEventManager.Instance;
        Subscribe();
        __AwakeBasedOnStartScene();
    }

    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each scene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (_instance && _instance != this) {
            D.Log("{0}_{1} found as extra. Initiating destruction sequence.".Inject(this.name, InstanceID));
            Destroy(gameObject);
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            _instance = this;
            return false;
        }
    }

    private void ChangeLanguage(string language) {
        CultureInfo newCulture = new CultureInfo(language);
        Thread.CurrentThread.CurrentCulture = newCulture;
        Thread.CurrentThread.CurrentUICulture = newCulture;
        D.Log("Current culture of thread is {0}.".Inject(Thread.CurrentThread.CurrentUICulture.DisplayName));
        D.Log("Current OS Language of Unity is {0}.".Inject(Application.systemLanguage.GetName()));
    }

    protected override void Start() {
        base.Start();
        __StartBasedOnStartScene();
    }

    #region Startup Simulation
    private void __AwakeBasedOnStartScene() {
        SceneLevel startScene = (SceneLevel)Application.loadedLevel;
        switch (startScene) {
            case SceneLevel.IntroScene:
                break;
            case SceneLevel.GameScene:
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(startScene));
        }
        _gameMgr.__AwakeBasedOnStartScene(startScene);
    }

    private void __StartBasedOnStartScene() {
        SceneLevel startScene = (SceneLevel)Application.loadedLevel;
        switch (startScene) {
            case SceneLevel.IntroScene:
                break;
            case SceneLevel.GameScene:
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(startScene));
        }
        _gameMgr.StartBasedOnStartScene(startScene);
    }
    #endregion

    private void Subscribe() {
        _eventMgr.AddListener<BuildNewGameEvent>(Instance, OnBuildNewGame);
        _eventMgr.AddListener<LoadSavedGameEvent>(Instance, OnLoadSavedGame);
    }

    private void OnBuildNewGame(BuildNewGameEvent e) {
        D.Log("BuildNewGameEvent received.");
        StartCoroutine<GameSettings>(WaitUntilReadyThenBuildNewGame, e.Settings);
    }

    private IEnumerator WaitUntilReadyThenBuildNewGame(GameSettings settings) {
        while (!GuiManager.Instance.ReadyForSceneChange) {
            yield return null;
        }
        _gameMgr.BuildAndLoadNewGame(settings);
    }

    private void OnLoadSavedGame(LoadSavedGameEvent e) {
        D.Log("LoadSavedGameEvent received.");
        StartCoroutine<string>(WaitUntilReadyThenLoadAndRestoreSavedGame, e.GameID);
    }

    private IEnumerator WaitUntilReadyThenLoadAndRestoreSavedGame(string gameID) {
        while (!GuiManager.Instance.ReadyForSceneChange) {
            yield return null;
        }
        _gameMgr.LoadAndRestoreSavedGame(gameID);
    }

    // This simply substitutes my own Event for OnLevelWasLoaded so I don't have to use OnLevelWasLoaded anywhere else
    // Wiki: OnLevelWasLoaded is NOT guaranteed to run before all of the Awake calls. In most cases it will, but in some 
    // might produce some unexpected bugs. If you need some code to be executed before Awake calls, use OnDisable instead.
    void OnLevelWasLoaded(int level) {
        if (enabled) {
            // OnLevelWasLoaded is called on all active components and at any time. The earliest thing that happens after Destroy(gameObject)
            // is component disablement. GameObject deactivation happens later, but before OnDestroy()
            D.Log("{0}_{1}.OnLevelWasLoaded(level = {2}) called.".Inject(this.name, InstanceID, ((SceneLevel)level).GetName()));
            _gameMgr.OnLevelHasCompletedLoading((SceneLevel)level);
        }
    }

    protected override void OnDeserialized() {
        _gameMgr.OnDeserialized();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        if (enabled) { // The earliest thing that happens after Destroy(gameObject) is component disablement. GameObject deactivation happens later, but before OnDestroy()
            Unsubscribe();
            _gameMgr.Dispose();
            // other cleanup here including any tracking Gui2D elements
        }
    }

    private void Unsubscribe() {
        _eventMgr.RemoveListener<BuildNewGameEvent>(Instance, OnBuildNewGame);
        _eventMgr.RemoveListener<LoadSavedGameEvent>(Instance, OnLoadSavedGame);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
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
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
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

}

