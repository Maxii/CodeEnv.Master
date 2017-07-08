// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiWindow.cs
// Abstract base class for Gui Windows with fading ability.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Gui Windows with fading ability. 
/// GuiWIndows have the ability to appear and disappear, aka 'pop up'.
/// </summary>
/// <remarks>A replacement for SpaceD UIWindow without all the automatic _panel depth changes.</remarks>
public abstract class AGuiWindow : AMonoBase {

    private const string DebugNameFormat = "{0}.{1}";

    /// <summary>
    /// A delegate invoked when the window begins to show.
    /// </summary>
    public List<EventDelegate> onShowBegin = new List<EventDelegate>();

    /// <summary>
    /// A delegate invoked when the window is completely shown.
    /// </summary>
    public List<EventDelegate> onShowComplete = new List<EventDelegate>();

    /// <summary>
    /// A delegate invoked when the window begins to hide.
    /// </summary>
    public List<EventDelegate> onHideBegin = new List<EventDelegate>();

    /// <summary>
    /// A delegate invoked when the window is completely hidden.
    /// </summary>
    public List<EventDelegate> onHideComplete = new List<EventDelegate>();

    // Has Editor
    public bool startHidden = true;
    public bool useFading = true;
    public float fadeDuration = 0.2f;

    private bool _isShowing;
    public bool IsShowing {
        get { return _isShowing; }
        private set { SetProperty<bool>(ref _isShowing, value, "IsShowing"); }
    }


    private string _debugName;
    public string DebugName {
        get {
            if (_debugName == null) {
                _debugName = DebugNameFormat.Inject(gameObject.name, GetType().Name);
            }
            return _debugName;
        }
    }

    private bool _autoExecuteStartingState = true;
    /// <summary>
    /// Controls whether Start automatically initializes the window to the starting
    /// state indicated by the startHidden field. Default is true.
    /// <remarks>Typically used to keep Start from reversing a Show call when it immediately follows Awake().</remarks>
    /// </summary>
    public bool AutoExecuteStartingState {
        get { return _autoExecuteStartingState; }
        set { _autoExecuteStartingState = value; }
    }

    protected abstract Transform ContentHolder { get; }

    protected GameManager _gameMgr;
    protected UIPanel _panel;

    private FadeMode _currentFadeMode = FadeMode.None;
    private Job _fadeInJob;
    private Job _fadeOutJob;
    private GameTime _gameTime;
    private JobManager _jobMgr;
    /// <summary>
    /// Indicates whether to optimize the widgets are static value when showing/hiding a window.
    /// <remarks>If true, the editor value of widgetsAreStatic is false meaning there are widgets that can move under the panel.</remarks>
    /// </summary>
    private bool _optimizeWidgetsAreStatic;

    /// <summary>
    /// Note: InitializeOnAwake() must be called from Awake() from a derived class.
    /// </summary>
    /// <remarks> This is because derived class AHudWindow is a singleton, using InitializeOnAwake(), not Awake().</remarks>
    protected virtual void InitializeOnAwake() {
        InitializeValuesAndReferences();
    }

    protected virtual void InitializeValuesAndReferences() {
        _gameTime = GameTime.Instance;
        _gameMgr = GameManager.Instance;
        _jobMgr = JobManager.Instance;
        _panel = UnityUtility.ValidateComponentPresence<UIPanel>(gameObject);
        _optimizeWidgetsAreStatic = !_panel.widgetsAreStatic;
        if (_optimizeWidgetsAreStatic) {
            _panel.widgetsAreStatic = true; // OPTIMIZE see http://www.tasharen.com/forum/index.php?board=1.0
        }
    }

    protected sealed override void Start() {
        base.Start();
        if (AutoExecuteStartingState) {
            ExecuteStartingState();
        }
    }

    /// <summary>
    /// Executes the starting state as determined by the startHidden field.
    /// </summary>
    private void ExecuteStartingState() {
        if (startHidden) {
            _panel.alpha = Constants.ZeroF;
            ContentHolder.gameObject.SetActive(false);
            IsShowing = false;
        }
        else {
            _panel.alpha = Constants.OneF;
            ContentHolder.gameObject.SetActive(true);
            IsShowing = true;
        }
    }

    /// <summary>
    /// Show the window.
    /// </summary>
    protected void ShowWindow() {
        if (!enabled || !gameObject.activeSelf || IsShowing) {
            //D.Log("{0}.ShowWindow() erroneously called. enabled = {1}, IsShowing = {2}.", DebugName, enabled, IsShowing);
            return;
        }

        if (_optimizeWidgetsAreStatic) {
            _panel.widgetsAreStatic = false;
        }

        IsShowing = true;

        if (!useFading) {
            _panel.alpha = 1F;
            ContentHolder.gameObject.SetActive(true);
            EventDelegate.Execute(onShowBegin);
            EventDelegate.Execute(onShowComplete);
        }
        else {
            FadeIn(fadeDuration);
            EventDelegate.Execute(onShowBegin);
        }
    }

    /// <summary>
    /// Hide the window.
    /// </summary>
    protected void HideWindow() {
        if (!enabled || !gameObject.activeSelf || !IsShowing) {
            //D.Log("{0}.HideWindow() erroneously called. enabled = {1}, IsShowing = {2}.", DebugName, enabled, IsShowing);
            return;
        }
        IsShowing = false;

        if (!useFading) {
            _panel.alpha = 0F;
            ContentHolder.gameObject.SetActive(false);
            EventDelegate.Execute(onHideBegin);
            EventDelegate.Execute(onHideComplete);
        }
        else {
            FadeOut(fadeDuration);
            EventDelegate.Execute(onHideBegin);
        }

        if (_optimizeWidgetsAreStatic) {
            _panel.widgetsAreStatic = true;
        }
    }

    /// <summary>
    /// Fades the window in.
    /// </summary>
    /// <param name="duration">Fade In duration.</param>
    private void FadeIn(float duration) {
        if (!ContentHolder.gameObject.activeSelf) {
            ContentHolder.gameObject.SetActive(true);
        }

        if (_currentFadeMode == FadeMode.In) {
            // ignore command if we are already actively fading in
            D.AssertNotNull(_fadeInJob);
            //D.Log("{0}.FadeIn() called when already fading in.", DebugName);
            return;
        }

        if (_currentFadeMode == FadeMode.Out) {
            D.AssertNotNull(_fadeOutJob);
            KillFadeOutJob();
        }

        _currentFadeMode = FadeMode.In;
        //D.Log("{0}.FadeInJob is beginning in Frame {1}.", DebugName, Time.frameCount);
        string jobName = "{0}.FadeInJob".Inject(DebugName);    // no pause controls on fadeJob as I want window access while paused
        _fadeInJob = _jobMgr.StartNonGameplayJob(FadeAnimation(FadeMode.In, duration), jobName, jobCompleted: (jobWasKilled) => {
            if (jobWasKilled) {
                // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                ////__ValidateKilledFadeJobReference(_fadeInJob);
            }
            else {
                //D.Log("{0}.FadeInJob jobCompleted called in Frame {1}.", DebugName, Time.frameCount);
                _fadeInJob = null;
            }
        });
    }

    /// <summary>
    /// Fades the window out.
    /// </summary>
    /// <param name="duration">Fade Out duration.</param>
    private void FadeOut(float duration) {
        if (!ContentHolder.gameObject.activeSelf) {
            return;
        }

        if (_currentFadeMode == FadeMode.Out) {
            // ignore command if we are already actively fading out
            D.AssertNotNull(_fadeOutJob);
            //D.Log("{0}.FadeOut() called when already fading out.", DebugName);
            return;
        }

        if (_currentFadeMode == FadeMode.In) {
            D.AssertNotNull(_fadeInJob);
            KillFadeInJob();
        }

        _currentFadeMode = FadeMode.Out;
        //D.Log("{0}.FadeOutJob is beginning in Frame {1}.", DebugName, Time.frameCount);
        string jobName = "{0}.FadeOutJob".Inject(DebugName);    // no pause controls on fadeJob as I want window access while paused
        _fadeOutJob = _jobMgr.StartNonGameplayJob(FadeAnimation(FadeMode.Out, duration), jobName, jobCompleted: (jobWasKilled) => {
            if (jobWasKilled) {
                // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                ////__ValidateKilledFadeJobReference(_fadeOutJob);
            }
            else {
                //D.Log("{0}.FadeOutJob jobCompleted called in Frame {1}.", DebugName, Time.frameCount);
                _fadeOutJob = null;
            }
        });
    }

    private IEnumerator FadeAnimation(FadeMode mode, float duration) {
        // 6.28.17 Do not set _currentFadeMode here as this is a whole frame after the Job is started
        float startTime = _gameTime.CurrentUnitySessionTime;

        if (mode == FadeMode.In) {
            // Calculate the time we need to fade in from the current alpha as may have started from a partial alpha
            float internalDuration = duration - (duration * _panel.alpha);
            float endTime = startTime + internalDuration;

            while (_gameTime.CurrentUnitySessionTime < endTime) {
                float remainingTime = endTime - _gameTime.CurrentUnitySessionTime;
                float elapsedTime = internalDuration - remainingTime;
                _panel.alpha = elapsedTime / internalDuration;
                yield return null;
            }
            D.AssertEqual(FadeMode.In, _currentFadeMode);
            D.AssertNotNull(_fadeInJob);
            //D.Log("{0}.FadeInJob finished execution in Frame {1}.", DebugName, Time.frameCount);

            // Make sure it's 1                
            _panel.alpha = Constants.OneF;
            EventDelegate.Execute(onShowComplete);
        }
        else {
            D.AssertEqual(FadeMode.Out, mode);
            // Calculate the time we need to fade out from the current alpha as may have started from a partial alpha
            float internalDuration = duration * _panel.alpha;
            float endTime = startTime + internalDuration;

            while (_gameTime.CurrentUnitySessionTime < endTime) {
                float remainingTime = endTime - _gameTime.CurrentUnitySessionTime;
                _panel.alpha = remainingTime / internalDuration;
                yield return null;
            }
            D.AssertEqual(FadeMode.Out, _currentFadeMode);
            D.AssertNotNull(_fadeOutJob);
            //D.Log("{0}.FadeOutJob finished execution in Frame {1}.", DebugName, Time.frameCount);

            // Make sure it's 0
            _panel.alpha = Constants.ZeroF;
            EventDelegate.Execute(onHideComplete);
            ContentHolder.gameObject.SetActive(false);
        }
        _currentFadeMode = FadeMode.None;
    }

    private void KillFadeInJob() {
        if (_fadeInJob != null) {
            //D.Log("{0}.FadeInJob is being killed in Frame {1}.", DebugName, Time.frameCount);
            _fadeInJob.Kill();
            _fadeInJob = null;
        }
    }

    private void KillFadeOutJob() {
        if (_fadeOutJob != null) {
            //D.Log("{0}.FadeOutJob is being killed in Frame {1}.", DebugName, Time.frameCount);
            _fadeOutJob.Kill();
            _fadeOutJob = null;
        }
    }

    /// <summary>
    /// Activates the GameObject holding the content of the window (ContentHolder) along with any
    /// other nested window's content.
    /// <remarks>Typically used when dynamically swapping AGuiWindow instances. Allows the new instance to
    /// acquire its references inside content which it would not be able to do if deactivated.</remarks>
    /// </summary>
    public virtual void ActivateContent() {
        ContentHolder.gameObject.SetActive(true);
    }

    protected override void Cleanup() {
        // 12.8.16 Job Disposal centralized in JobManager
        KillFadeInJob();
        KillFadeOutJob();
    }

    public sealed override string ToString() {
        return DebugName;
    }

    #region Debug

    /// <summary>
    /// Validates the provided fade Job reference that has been killed. 
    /// 
    /// <remarks>Usually a fadeJob that has been killed has its reference set to null. 
    /// However, the single instance HoveredItemHud derived class can fail this test as
    /// initiating and ending a fade is completely asynchronous (mouse hover events
    /// can change rapidly). With the 1 frame delay in the execution of 
    /// jobCompleted, the reference can be re-populated with another fade Job before this 
    /// validation test is called. This occurs when the sequence FadeA, FadeB, FadeA occurs 
    /// within the 1 frame timing gap as FadeB kills the first FadeA, then the second FadeA
    /// re-populates the reference before the first FadeA.onCompletion fires.</remarks>
    /// <remarks>FIXME Are there other derived classes with the same issue?</remarks>
    /// </summary>
    /// <param name="fadeJobRef">The fade job reference.</param>
    [System.Obsolete]
    protected virtual void __ValidateKilledFadeJobReference(Job fadeJobRef) {
        D.AssertNull(fadeJobRef, DebugName);    // if this Assert fails, another derived class also has the problem above
    }

    #endregion

    #region Nested Classes

    private enum FadeMode {
        None,
        In,
        Out
    }

    #endregion

}

