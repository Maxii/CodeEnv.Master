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
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for DisplayManager's that also manage Icons.
    /// </summary>
    public abstract class AIconDisplayManager : ADisplayManager {

        private IconInfo _iconInfo;
        public IconInfo IconInfo {
            get { return _iconInfo; }
            set { SetProperty<IconInfo>(ref _iconInfo, value, "IconInfo", IconInfoPropChangedHandler); }
        }

        private string _debugName;
        protected override string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(_trackedItem.DebugName, GetType().Name);
                }
                return _debugName;
            }
        }

        private IWorldTrackingSprite _icon;
        protected IWorldTrackingSprite Icon {
            get { return _icon; }
            private set { SetProperty<IWorldTrackingSprite>(ref _icon, value, "Icon"); }
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
            if (Icon != null) {
                //D.Log("{0}.ShowIcon({1}) called.", DebugName, toShow);
                if (Icon.IsShowing == toShow) {
                    //D.Warn("{0} recording duplicate call to ShowIcon({1}).", DebugName, toShow);
                    return;
                }
                Icon.Show(toShow);
            }
            else {
                D.Assert(!toShow, DebugName);
            }
        }

        #region Event and Property Change Handlers

        private void IconInfoPropChangedHandler() {
            D.AssertNotNull(_primaryMeshRenderer, "Always Initialize before setting IconInfo.");
            if (Icon != null) {
                // icon already present
                if (IconInfo != default(IconInfo)) {
                    // something about the existing icon needs to change
                    Icon.IconInfo = IconInfo;
                }
                else {
                    // Element Icons have an option that allows them to be turned off, aka IconInfo changed to default(IconInfo)
                    DestroyIcon();
                }
            }
            else {
                // initial or subsequent generation of the Icon
                Icon = MakeIcon();
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
            return References.TrackingWidgetFactory.MakeInteractiveWorldTrackingSprite(_trackedItem, IconInfo);
        }

        protected override void AssessInMainCameraLOS() {
            IsInMainCameraLOS = Icon == null ? IsPrimaryMeshInMainCameraLOS : IsPrimaryMeshInMainCameraLOS || _isIconInMainCameraLOS;
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
            bool result = IsDisplayEnabled && Icon != null && _isIconInMainCameraLOS && !IsPrimaryMeshInMainCameraLOS;
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
            if (Icon != null) {  // Use of Element Icons is an option
                //D.Log("{0}.Icon about to be destroyed.", DebugName);
                ShowIcon(false); // accessing destroy gameObject error if we are showing it while destroying it
                var iconCameraLosChgdListener = Icon.CameraLosChangedListener;
                iconCameraLosChgdListener.inCameraLosChanged -= IconInCameraLosChangedEventHandler;
                // event subscriptions already removed by Item before Icon changed
                iconCameraLosChgdListener.enabled = false;  // avoids no subscribers warning when OnBecameInvisible is called when destroyed
                GameUtility.DestroyIfNotNullOrAlreadyDestroyed(Icon);
                Icon = null;    // Destroying the Icon doesn't null the reference
            }
        }

        #region Cleanup

        protected override void Cleanup() {
            base.Cleanup();
            DestroyIcon();
        }

        #endregion
    }

}

