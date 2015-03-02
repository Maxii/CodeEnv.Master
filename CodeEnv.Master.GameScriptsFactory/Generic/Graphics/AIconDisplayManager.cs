// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIconDisplayManager.cs
// Abstract base class for DisplayManager's that also manage Icons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract base class for DisplayManager's that also manage Icons.
/// </summary>
public abstract class AIconDisplayManager : ADisplayManager {

    private ResponsiveTrackingSprite _icon;
    public ResponsiveTrackingSprite Icon {
        get { return _icon; }
        set { SetProperty<ResponsiveTrackingSprite>(ref _icon, value, "Icon", OnIconChanged, OnIconChanging); }
    }

    protected bool _isIconInCameraLOS = true;

    public AIconDisplayManager(GameObject itemGO) : base(itemGO) { }

    private void OnIconChanging(ResponsiveTrackingSprite newIcon) {
        if (Icon != null) {
            DestroyIcon();
        }
    }

    private void OnIconChanged() {
        if (Icon != null) {
            var iconCameraLosChgdListener = Icon.CameraLosChangedListener;
            iconCameraLosChgdListener.onCameraLosChanged += (iconGo, isIconInCameraLOS) => OnIconInCameraLOSChanged(isIconInCameraLOS);
            iconCameraLosChgdListener.enabled = true;
        }
    }

    private void ShowIcon(bool toShow) {
        if (Icon != null) {
            //D.Log("{0}.ShowIcon({1}) called.", GetType().Name, toShow);
            if (Icon.IsShowing == toShow) {
                //D.Log("{0} recording duplicate call to ShowIcon({1}).", GetType().Name, toShow);
                return;
            }
            Icon.Show(toShow);
        }
    }

    private void OnIconInCameraLOSChanged(bool isIconInCameraLOS) {
        //D.Log("{0}.OnIconInCameraLOSChanged({1}) called.", GetType().Name, isIconInCameraLOS);
        _isIconInCameraLOS = isIconInCameraLOS;
        AssessInCameraLOSState();
        AssessComponentsToShow();
    }

    protected override void AssessInCameraLOSState() {
        InCameraLOS = Icon == null ? _isPrimaryMeshInCameraLOS : _isPrimaryMeshInCameraLOS || _isIconInCameraLOS;
    }

    protected override void AssessComponentsToShow() {
        base.AssessComponentsToShow();
        bool toShowIcon = ShouldIconShow();
        ShowIcon(toShowIcon);
    }

    /// <summary>
    /// Determines the conditions under which the Icon should show. The default version
    /// shows the icon when the icon is within the camera's LOS, and the primary mesh is
    /// no longer showing due to clipping planes. Derived classes that wish the icon to show 
    /// even when the primary mesh is showing should override this method.
    /// </summary>
    /// <returns></returns>
    protected virtual bool ShouldIconShow() {
        return IsDisplayEnabled && _isIconInCameraLOS && !_isPrimaryMeshShowing;
    }

    private void DestroyIcon() {
        D.Assert(Icon != null);
        ShowIcon(false); // accessing destroy gameObject error if we are showing it while destroying it
        var iconCameraLosChgdListener = Icon.CameraLosChangedListener;
        iconCameraLosChgdListener.onCameraLosChanged -= (iconGo, isIconInCameraLOS) => OnIconInCameraLOSChanged(isIconInCameraLOS);
        GameObject.Destroy(Icon.gameObject);
    }

}

