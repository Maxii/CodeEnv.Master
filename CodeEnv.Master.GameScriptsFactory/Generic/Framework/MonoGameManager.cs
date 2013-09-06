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

#define DEBUG_WARN
#define DEBUG_ERROR

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
public class MonoGameManager : AMonoBehaviourBaseSingleton<MonoGameManager> {

    private GameManager _gameMgr;
    private bool _isInitialized;

    protected override void Awake() {
        base.Awake();
        //Logger.Log("MonoGameManager Awake() called. IsEnabled = " + enabled);
        IncrementInstanceCounter();
        if (TryDestroyExtraCopies()) {
            return;
        }

        // TODO add choose language GUI
        //string language = "fr-FR";
        // ChangeLanguage(language);
        _gameMgr = GameManager.Instance;
        _gameMgr.DebugHud = DebugHud.Instance;
        _gameMgr.CompleteInitialization();

        _isInitialized = true;
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
            Logger.Log("{0}_{1} found as extra. Initiating destruction sequence.".Inject(this.name, InstanceID));
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
        Logger.Log("Current culture of thread is {0}.".Inject(Thread.CurrentThread.CurrentUICulture.DisplayName));
        Logger.Log("Current OS Language of Unity is {0}.".Inject(Application.systemLanguage.GetName()));
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

    // This simply substitutes my own Event for OnLevelWasLoaded so I don't have to use OnLevelWasLoaded anywhere else
    // Wiki: OnLevelWasLoaded is NOT guaranteed to run before all of the Awake calls. In most cases it will, but in some 
    // might produce some unexpected bugs. If you need some code to be executed before Awake calls, use OnDisable instead.
    void OnLevelWasLoaded(int level) {
        if (_isInitialized) { // OnLevelWasLoaded will be called even when this gameobject is immediately destroyed
            Logger.Log("{0}_{1}.OnLevelWasLoaded(level = {1}) called.".Inject(this.name, InstanceID, level));
            _gameMgr.OnSceneChanged((SceneLevel)level);
        }
    }

    protected override void OnDeserialized() {
        _gameMgr.OnDeserialized();
    }

    protected override void OnApplicationQuit() {
        base.OnApplicationQuit();
        _gameMgr.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

