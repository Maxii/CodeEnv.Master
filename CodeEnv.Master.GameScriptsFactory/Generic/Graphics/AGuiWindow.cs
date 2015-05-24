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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Gui Windows with fading ability.
/// </summary>
/// <remarks>A replacement for SpaceD UIWindow without all the automatic panel depth changes.</remarks>
public abstract class AGuiWindow : AMonoBase {

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

    public bool IsShowing { get; private set; }

    [Tooltip("The initial content to be shown.")]
    public Transform currentContentHolder;
    public bool startHidden = true;
    public bool useFading = true;
    public float fadeDuration = 0.2f;

    private UIPanel _panel;
    private FadeMode _currentFadeMode = FadeMode.None;
    private Job _fadeJob;

    /// <summary>
    /// Note: InitializeOnAwake() must be called from Awake() from a derived class
    /// </summary>
    protected virtual void InitializeOnAwake() {
        D.Assert(currentContentHolder != null, "{0}.ContentHolder has not been set.".Inject(GetType().Name), gameObject);
        AcquireReferences();
    }

    protected virtual void AcquireReferences() {
        _panel = UnityUtility.ValidateMonoBehaviourPresence<UIPanel>(gameObject);
    }

    protected override void Start() {
        base.Start();
        ExecuteStartingState();
    }

    private void ExecuteStartingState() {
        if (startHidden) {
            _panel.alpha = Constants.ZeroF;
            currentContentHolder.gameObject.SetActive(false);
            IsShowing = false;
        }
        else {
            _panel.alpha = Constants.OneF;
            currentContentHolder.gameObject.SetActive(true);
            IsShowing = true;
        }
    }

    /// <summary>
    /// Show the window.
    /// </summary>
    protected void ShowWindow() {
        if (!enabled || !gameObject.activeSelf || IsShowing) {
            return;
        }
        IsShowing = true;

        if (!useFading) {
            _panel.alpha = 1F;
            currentContentHolder.gameObject.SetActive(true);
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
            return;
        }
        IsShowing = false;

        if (!useFading) {
            _panel.alpha = 0F;
            currentContentHolder.gameObject.SetActive(false);
            EventDelegate.Execute(onHideBegin);
            EventDelegate.Execute(onHideComplete);
        }
        else {
            FadeOut(fadeDuration);
            EventDelegate.Execute(onHideBegin);
        }
    }

    /// <summary>
    /// Fades the window in.
    /// </summary>
    /// <param name="duration">Fade In duration.</param>
    private void FadeIn(float duration) {
        if (!currentContentHolder.gameObject.activeSelf) {
            currentContentHolder.gameObject.SetActive(true);
        }

        if (_currentFadeMode == FadeMode.In) {
            // ignore command if we are already actively fading in
            D.Assert(_fadeJob != null && _fadeJob.IsRunning);
            return;
        }

        if (_fadeJob != null) {
            _fadeJob.Kill();
            _currentFadeMode = FadeMode.None;
        }
        _fadeJob = new Job(FadeAnimation(FadeMode.In, duration), toStart: true);
    }

    /// <summary>
    /// Fades the window out.
    /// </summary>
    /// <param name="duration">Fade Out duration.</param>
    private void FadeOut(float duration) {
        if (!currentContentHolder.gameObject.activeSelf) {
            return;
        }

        if (_currentFadeMode == FadeMode.Out) {
            // ignore command if we are already actively fading out
            D.Assert(_fadeJob != null && _fadeJob.IsRunning);
            return;
        }

        if (_fadeJob != null) {
            _fadeJob.Kill();
            _currentFadeMode = FadeMode.None;
        }
        _fadeJob = new Job(FadeAnimation(FadeMode.Out, duration), toStart: true);
    }

    private IEnumerator FadeAnimation(FadeMode mode, float duration) {
        _currentFadeMode = mode;
        var gameTimeRef = GameTime.Instance;
        float startTime = gameTimeRef.RealTime_Game;

        if (mode == FadeMode.In) {
            // Calculate the time we need to fade in from the current alpha as may have started from a partial alpha
            float internalDuration = duration - (duration * _panel.alpha);
            float endTime = startTime + internalDuration;

            while (gameTimeRef.RealTime_Game < endTime) {
                float remainingTime = endTime - gameTimeRef.RealTime_Game;
                float elapsedTime = internalDuration - remainingTime;
                _panel.alpha = elapsedTime / internalDuration;
                yield return null;
            }

            // Make sure it's 1
            _panel.alpha = Constants.OneF;
            EventDelegate.Execute(onShowComplete);
        }
        else if (mode == FadeMode.Out) {
            // Calculate the time we need to fade out from the current alpha as may have started from a partial alpha
            float internalDuration = duration * _panel.alpha;
            float endTime = startTime + internalDuration;

            while (gameTimeRef.RealTime_Game < endTime) {
                float remainingTime = endTime - gameTimeRef.RealTime_Game;
                _panel.alpha = remainingTime / internalDuration;
                yield return null;
            }

            // Make sure it's 0
            _panel.alpha = Constants.ZeroF;
            EventDelegate.Execute(onHideComplete);
            currentContentHolder.gameObject.SetActive(false);
        }
        _currentFadeMode = FadeMode.None;
    }

    protected override void Cleanup() {
        if (_fadeJob != null && _fadeJob.IsRunning) {
            _fadeJob.Kill();
        }
    }

    #region Nested Classes

    private enum FadeMode {
        None,
        In,
        Out
    }

    #endregion

}

