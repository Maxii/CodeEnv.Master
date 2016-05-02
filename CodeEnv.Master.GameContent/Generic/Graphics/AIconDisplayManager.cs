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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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

        protected string Name {
            get {
                if (_trackedItem == null) { return GetType().Name; }
                return "{0}.{1}".Inject(_trackedItem.DisplayName, GetType().Name);
            }
        }

        private ITrackingSprite _icon;
        protected ITrackingSprite Icon {
            get { return _icon; }
            private set { SetProperty<ITrackingSprite>(ref _icon, value, "Icon"); }
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
                D.Log("{0}.ShowIcon({1}) called.", GetType().Name, toShow);
                if (Icon.IsShowing == toShow) {
                    D.Log("{0} recording duplicate call to ShowIcon({1}).", GetType().Name, toShow);
                    return;
                }
                Icon.Show(toShow);
            }
            else {
                D.Assert(!toShow, "{0}.ShowIcon(true) called when there is no Icon.", GetType().Name);
            }
        }

        #region Event and Property Change Handlers

        private void IconInfoPropChangedHandler() {
            D.Assert(_primaryMeshRenderer != null, "Always Initialize before setting IconInfo.");
            if (Icon != null) {
                // icon already present
                if (IconInfo != null) {
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

        private ITrackingSprite MakeIcon() {
            var icon = MakeIconInstance();
            icon.DrawDepth = IconDepth;
            var iconCameraLosChgdListener = icon.CameraLosChangedListener;
            iconCameraLosChgdListener.inCameraLosChanged += IconInCameraLosChangedEventHandler;
            iconCameraLosChgdListener.enabled = true;
            return icon;
        }

        protected virtual ITrackingSprite MakeIconInstance() {
            return References.TrackingWidgetFactory.MakeResponsiveTrackingSprite(_trackedItem, IconInfo);
        }

        protected override void AssessInMainCameraLOS() {
            IsInMainCameraLOS = Icon == null ? IsPrimaryMeshInMainCameraLOS : IsPrimaryMeshInMainCameraLOS || _isIconInMainCameraLOS;
            D.Log("{0}.AssessInMainCameraLOS() called. IsInMainCameraLOS = {1}.", GetType().Name, IsInMainCameraLOS);
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
            return IsDisplayEnabled && Icon != null && _isIconInMainCameraLOS && !IsPrimaryMeshInMainCameraLOS;
        }

        /// <summary>
        /// Destroys the icon.
        /// WARNING: Destroying an Icon that is used for other purposes by the Item can result in difficult to diagnose errors.
        /// eg. CmdIcon transforms are used by the Highlighter to position highlights.
        /// </summary>
        protected virtual void DestroyIcon() {
            D.Log("{0}.Icon about to be destroyed.", _trackedItem.DisplayName);
            D.Assert(Icon != null);
            ShowIcon(false); // accessing destroy gameObject error if we are showing it while destroying it
            var iconCameraLosChgdListener = Icon.CameraLosChangedListener;
            iconCameraLosChgdListener.inCameraLosChanged -= IconInCameraLosChangedEventHandler;
            // event subscriptions already removed by Item before Icon changed
            GameUtility.DestroyIfNotNullOrAlreadyDestroyed(Icon);
        }
    }

}

