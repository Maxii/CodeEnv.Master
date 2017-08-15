// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIconDisplayManager.cs
// Abstract base class for DisplayManager's that also manage Icons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for DisplayManager's that also manage Icons.
    /// </summary>
    public abstract class AIconDisplayManager : ADisplayManager {

        private TrackingIconInfo _iconInfo;
        public TrackingIconInfo IconInfo {
            get { return _iconInfo; }
            set { SetProperty<TrackingIconInfo>(ref _iconInfo, value, "IconInfo", IconInfoPropChangedHandler); }
        }

        protected override string DebugName { get { return DebugNameFormat.Inject(_trackedItem.DebugName, GetType().Name); } }

        private IWorldTrackingSprite _trackingIcon;
        protected IWorldTrackingSprite TrackingIcon {
            get { return _trackingIcon; }
            private set { SetProperty<IWorldTrackingSprite>(ref _trackingIcon, value, "TrackingIcon"); }
        }

        /// <summary>
        /// The UIPanel depth of the icon. Higher values are drawn over lower values.
        /// </summary>
        protected abstract int IconDepth { get; }

        protected bool _isIconInMainCameraLOS = true;
        protected IWidgetTrackable _trackedItem;

        public AIconDisplayManager(IWidgetTrackable trackedItem, Layers meshLayer)
            : base(trackedItem.transform.gameObject, meshLayer) {
            _trackedItem = trackedItem;
        }

        private void ShowIcon(bool toShow) {
            if (TrackingIcon != null) {
                //D.Log("{0}.ShowIcon({1}) called.", DebugName, toShow);
                if (TrackingIcon.IsShowing == toShow) {
                    //D.Warn("{0} recording duplicate call to ShowIcon({1}).", DebugName, toShow);
                    return;
                }
                TrackingIcon.Show(toShow);
            }
            else {
                D.Assert(!toShow, DebugName);
            }
        }

        #region Event and Property Change Handlers

        private void IconInfoPropChangedHandler() {
            D.AssertNotNull(_primaryMeshRenderer, "Always Initialize before setting IconInfo.");
            if (TrackingIcon != null) {
                // icon already present
                if (IconInfo != null) {
                    // something about the existing icon needs to change
                    TrackingIcon.IconInfo = IconInfo;
                }
                else {
                    // Element Icons have an option that allows them to be turned off, aka IconInfo changed to default(IconInfo)
                    DestroyIcon();
                }
            }
            else {
                // initial or subsequent generation of the Icon
                TrackingIcon = MakeIcon();
            }
        }

        private void IconInCameraLosChangedEventHandler(object sender, EventArgs e) {
            _isIconInMainCameraLOS = (sender as ICameraLosChangedListener).InCameraLOS;
            AssessInMainCameraLOS();
            AssessComponentsToShowOrOperate();
        }

        #endregion

        private IWorldTrackingSprite MakeIcon() {
            var icon = MakeIconInstance();
            if (icon is IWorldTrackingSprite_Independent) {
                (icon as IWorldTrackingSprite_Independent).DrawDepth = IconDepth;
            }
            var iconCameraLosChgdListener = icon.CameraLosChangedListener;
            iconCameraLosChgdListener.inCameraLosChanged += IconInCameraLosChangedEventHandler;
            iconCameraLosChgdListener.enabled = true;
            return icon;
        }

        protected virtual IWorldTrackingSprite MakeIconInstance() {
            return GameReferences.TrackingWidgetFactory.MakeInteractiveWorldTrackingSprite(_trackedItem, IconInfo);
        }

        protected override void AssessInMainCameraLOS() {
            IsInMainCameraLOS = TrackingIcon == null ? IsPrimaryMeshInMainCameraLOS : IsPrimaryMeshInMainCameraLOS || _isIconInMainCameraLOS;
            //D.Log("{0}.AssessInMainCameraLOS() called. IsInMainCameraLOS = {1}.", DebugName, IsInMainCameraLOS);
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            bool toShowIcon = ShouldIconShow();
            ShowIcon(toShowIcon);
        }

        /// <summary>
        /// Determines the conditions under which the Icon should show. The default version
        /// shows the icon when 1) the display is enabled, 2) the icon exists and is within the camera's LOS, 
        /// and 3) the primary mesh is no longer showing due to clipping planes. 
        /// Derived classes that wish the icon to show under other circumstances should override this method.
        /// </summary>
        /// <returns></returns>
        protected virtual bool ShouldIconShow() {
            bool result = IsDisplayEnabled && TrackingIcon != null && _isIconInMainCameraLOS && !IsPrimaryMeshInMainCameraLOS;
            //D.Log("{0}.ShouldIconShow() result = {1}, IsDisplayEnabled = {2}, _isIconInMainCameraLOS = {3}, IsPrimaryMeshInMainCameraLOS = {4}.", 
            //DebugName, result, IsDisplayEnabled, _isIconInMainCameraLOS, IsPrimaryMeshInMainCameraLOS);
            return result;
        }

        /// <summary>
        /// Destroys the icon if present, include any subscription un-wiring required.
        /// WARNING: Destroying an Icon that is used for other purposes by the Item can result in difficult to diagnose errors.
        /// e.g. Cmds use the Icon as the transform upon which to center highlighting.
        /// </summary>
        protected void DestroyIcon() {
            if (TrackingIcon != null) {  // Use of Element Icons is an option
                //D.Log("{0}.Icon about to be destroyed.", DebugName);
                ShowIcon(false); // accessing destroy gameObject error if we are showing it while destroying it
                var iconCameraLosChgdListener = TrackingIcon.CameraLosChangedListener;
                iconCameraLosChgdListener.inCameraLosChanged -= IconInCameraLosChangedEventHandler;
                // event subscriptions already removed by Item before Icon changed
                iconCameraLosChgdListener.enabled = false;  // avoids no subscribers warning when OnBecameInvisible is called when destroyed
                GameUtility.DestroyIfNotNullOrAlreadyDestroyed(TrackingIcon);
                TrackingIcon = null;    // Destroying the Icon doesn't null the reference
            }
        }

        #region Cleanup

        protected override void Cleanup() {
            base.Cleanup();
            DestroyIcon();
        }

        #endregion

        #region Debug

        protected override List<MeshRenderer> __GetMeshRenderers() {
            List<MeshRenderer> result = base.__GetMeshRenderers();
            if (TrackingIcon != null) {
                result.AddRange((TrackingIcon as Component).GetComponentsInChildren<MeshRenderer>());
            }
            return result;
        }

        #endregion

    }

}

