// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementItemDisplayManager.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public abstract class AUnitElementItemDisplayManager : AMortalItemDisplayManager {

    public InteractableTrackingSprite Icon { get; private set; }

    protected override float ItemTypeCircleScale { get { return 1.0F; } }

    private Color _originalMeshColor_Main;
    private Color _originalMeshColor_Specular;
    private Color _hiddenMeshColor = GameColor.Clear.ToUnityColor();

    private bool _isIconInCameraLOS = true;

    private IList<IDisposable> _subscribers;

    public AUnitElementItemDisplayManager(AUnitElementItem item)
        : base(item) {
        _originalMeshColor_Main = _primaryMeshRenderer.material.GetColor(UnityConstants.MaterialColor_Main);
        _originalMeshColor_Specular = _primaryMeshRenderer.material.GetColor(UnityConstants.MaterialColor_Specular);
        Subscribe();
    }

    protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
        var primaryMeshRenderer = itemGo.GetComponentInImmediateChildren<MeshRenderer>();
        primaryMeshRenderer.castShadows = true;
        primaryMeshRenderer.receiveShadows = true;
        D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == GetCullingLayer());    // layer automatically handles showing
        return primaryMeshRenderer;
    }

    protected override void InitializeOther(GameObject itemGo) {
        base.InitializeOther(itemGo);

        if (PlayerPrefsManager.Instance.IsElementIconsEnabled) {
            InitializeIcon();
        }
    }


    private void InitializeIcon() {
        D.Assert(PlayerPrefsManager.Instance.IsElementIconsEnabled);
        Icon = TrackingWidgetFactory.Instance.CreateInteractableTrackingSprite(Item, TrackingWidgetFactory.IconAtlasID.Fleet,
            new Vector2(12, 12), WidgetPlacement.Over);
        Icon.Set("FleetIcon_Unknown");
        // icon enabled state controlled by _icon.Show()
        var iconCameraLosChgdListener = Icon.CameraLosChangedListener;
        iconCameraLosChgdListener.onCameraLosChanged += (iconGo, isIconInCameraLOS) => OnIconInCameraLOSChanged(isIconInCameraLOS);
        iconCameraLosChgdListener.enabled = true;
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(PlayerPrefsManager.Instance.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(ppm => ppm.IsElementIconsEnabled, OnElementIconsEnabledChanged));
    }

    protected abstract Layers GetCullingLayer();


    private void ShowIcon(bool toShow) {
        if (Icon != null) { // this Icon can be null as it is a GraphicsOption
            //D.Log("{0}.ShowIcon({1}) called.", GetType().Name, toShow);
            if (Icon.IsShowing == toShow) {
                D.Warn("{0} recording duplicate call to ShowIcon({1}).", GetType().Name, toShow);
                return;
            }
            Icon.Show(toShow);
        }
    }

    //protected override void OnIsDisplayEnabledChanged() {
    //    base.OnIsDisplayEnabledChanged();
    //    if (!IsDisplayEnabled) {
    //        if (Icon != null && Icon.IsShowing) {
    //            ShowIcon(false);
    //        }
    //    }
    //    else {
    //        // Nothing to do when enabled. Icon will show when determined by AssessInCameraLosChanged()
    //    }
    //}


    protected override void ShowPrimaryMesh() {
        base.ShowPrimaryMesh();
        _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _originalMeshColor_Main);
        _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Specular, _originalMeshColor_Specular);
    }

    protected override void HidePrimaryMesh() {
        base.HidePrimaryMesh();
        _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _hiddenMeshColor);
        _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Specular, _hiddenMeshColor);
    }


    //protected override void OnMeshInCameraLOSChanged(bool isMeshInCameraLOS) {
    //    base.OnMeshInCameraLOSChanged(isMeshInCameraLOS);

    //    if (Icon != null) {
    //        if (IsDisplayEnabled) {
    //            if (!isMeshInCameraLOS) {
    //                // mesh moved beyond culling distance while on the screen or off the screen while within culling distance
    //                D.Assert(!Icon.IsShowing);
    //                if (UnityUtility.IsWithinCameraViewport(Item.Position)) {
    //                    // mesh moved beyond culling distance while on the screen
    //                    // icon only shows when in front of the camera and beyond the star mesh's culling distance
    //                    ShowIcon(true);
    //                }
    //                else {
    //                    // mesh moved off the screen while within culling distance
    //                }
    //            }
    //            else {
    //                // mesh moved within culling distance while on the screen or onto the screen while within culling distance
    //                ShowIcon(false);
    //            }
    //        }
    //    }
    //}

    private void OnIconInCameraLOSChanged(bool isIconInCameraLOS) {
        D.Log("{0}.OnIconInCameraLOSChanged({1}) called.", GetType().Name, isIconInCameraLOS);
        _isIconInCameraLOS = isIconInCameraLOS;
        AssessInCameraLOS();
        AssessShowing();

        //if (IsDisplayEnabled) {
        //    if (isIconInCameraLOS) {
        //        // icon moved onto the screen
        //        if (!_isMeshShowing) {
        //            if (!Icon.IsShowing) {
        //                ShowIcon(true);
        //            }
        //        }
        //    }
        //    else {
        //        // icon moved off the screen
        //        if (Icon.IsShowing) {
        //            ShowIcon(false);
        //        }
        //    }
        //}
    }

    protected override void AssessInCameraLOS() {
        // one or the other inCameraLOS needs to be true for this to be set to true, both inCameraLOS needs to be false for this to trigger false
        //if (Icon == null) {
        //    InCameraLOS = _isMeshInCameraLOS;
        //    return;
        //}
        //InCameraLOS = _isMeshInCameraLOS || _isIconInCameraLOS;
        InCameraLOS = Icon == null ? _isMeshInCameraLOS : _isMeshInCameraLOS || _isIconInCameraLOS;
    }

    protected override void AssessShowing() {
        base.AssessShowing();
        if (Icon != null) {
            bool toShowIcon = IsDisplayEnabled && _isIconInCameraLOS && !_isMeshShowing;
            ShowIcon(toShowIcon);
        }
    }

    private void OnElementIconsEnabledChanged() {
        if (PlayerPrefsManager.Instance.IsElementIconsEnabled) {
            // toggled from disabled to enabled
            InitializeIcon();
            //ShowIcon(IsDiscernible);
        }
        else {
            // toggled from enabled to disabled
            DestroyIcon();
            //ShowIcon(false); // accessing destroy gameObject error if we are showing it while destroying it
            //var iconCameraLosChgdListener = Icon.CameraLosChangedListener;
            //iconCameraLosChgdListener.onCameraLosChanged -= (iconGo, isIconInCameraLOS) => OnIconInCameraLOSChanged(isIconInCameraLOS);

            //GameObject.Destroy(Icon.gameObject);
            //Icon = null;
        }
    }

    private void DestroyIcon() {
        ShowIcon(false); // accessing destroy gameObject error if we are showing it while destroying it
        var iconCameraLosChgdListener = Icon.CameraLosChangedListener;
        iconCameraLosChgdListener.onCameraLosChanged -= (iconGo, isIconInCameraLOS) => OnIconInCameraLOSChanged(isIconInCameraLOS);
        GameObject.Destroy(Icon.gameObject);
        Icon = null;
    }

    #region Animations

    // these run until finished with no requirement to call OnShowCompletion
    protected override void ShowCmdHit() {
        base.ShowCmdHit();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingCmdHit(), toStart: true);
    }

    protected override void ShowAttacking() {
        base.ShowAttacking();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingAttacking(), toStart: true);
    }

    // these run continuously until they are stopped via StopAnimation() 
    protected override void ShowRepairing() {
        base.ShowRepairing();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingRepairing(), toStart: true);
    }

    protected override void ShowRefitting() {
        base.ShowRefitting();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingRefitting(), toStart: true);
    }

    protected override void ShowDisbanding() {
        base.ShowDisbanding();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingDisbanding(), toStart: true);
    }

    private IEnumerator ShowingCmdHit() {
        AudioClip cmdHit = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.CmdHit);
        if (cmdHit != null) {
            _audioSource.PlayOneShot(cmdHit);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    private IEnumerator ShowingAttacking() {
        AudioClip attacking = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Attacking);
        if (attacking != null) {
            _audioSource.PlayOneShot(attacking);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    private IEnumerator ShowingRefitting() {
        AudioClip refitting = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Refitting);
        if (refitting != null) {
            _audioSource.PlayOneShot(refitting);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not useOnShowCompletion
    }

    private IEnumerator ShowingDisbanding() {
        AudioClip disbanding = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Disbanding);
        if (disbanding != null) {
            _audioSource.PlayOneShot(disbanding);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    private IEnumerator ShowingRepairing() {
        AudioClip repairing = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Repairing);
        if (repairing != null) {
            _audioSource.PlayOneShot(repairing);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    #endregion

    protected override void Cleanup() {
        base.Cleanup();
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
    }

}

