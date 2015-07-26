﻿// --------------------------------------------------------------------------------------------------------------------
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

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for DisplayManager's that also manage Icons.
    /// </summary>
    public abstract class AIconDisplayManager : ADisplayManager {

        private IconInfo _iconInfo;
        public IconInfo IconInfo {
            get { return _iconInfo; }
            set { SetProperty<IconInfo>(ref _iconInfo, value, "IconInfo", OnIconInfoChanged); }
        }

        public IResponsiveTrackingSprite Icon { get; private set; }

        protected abstract Vector2 IconSize { get; }

        protected abstract WidgetPlacement IconPlacement { get; }

        protected bool _isIconInMainCameraLOS = true;

        private IWidgetTrackable _trackedItem;

        public AIconDisplayManager(IWidgetTrackable itemTracked)
            : base(itemTracked.Transform.gameObject) {
            _trackedItem = itemTracked;
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

        private void OnIconInfoChanged() {
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

        private IResponsiveTrackingSprite MakeIcon() {
            var icon = References.TrackingWidgetFactory.MakeResponsiveTrackingSprite(_trackedItem, IconInfo, IconSize, IconPlacement);
            var iconCameraLosChgdListener = icon.CameraLosChangedListener;
            iconCameraLosChgdListener.onCameraLosChanged += (iconGo, isIconInCameraLOS) => OnIconInMainCameraLosChanged(isIconInCameraLOS);
            iconCameraLosChgdListener.enabled = true;
            return icon;
        }

        private void OnIconInMainCameraLosChanged(bool isIconInMainCameraLOS) {
            //D.Log("{0}.OnIconInMainCameraLosChanged({1}) called.", GetType().Name, isIconInMainCameraLOS);
            _isIconInMainCameraLOS = isIconInMainCameraLOS;
            AssessInMainCameraLOS();
            AssessComponentsToShowOrOperate();
        }

        protected override void AssessInMainCameraLOS() {
            IsInMainCameraLOS = Icon == null ? IsPrimaryMeshInMainCameraLOS : IsPrimaryMeshInMainCameraLOS || _isIconInMainCameraLOS;
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
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
            return IsDisplayEnabled && _isIconInMainCameraLOS && !IsPrimaryMeshInMainCameraLOS;
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
            iconCameraLosChgdListener.onCameraLosChanged -= (iconGo, isIconInCameraLOS) => OnIconInMainCameraLosChanged(isIconInCameraLOS);
            // event subscriptions already removed by Item before Icon changed
            UnityUtility.DestroyIfNotNullOrAlreadyDestroyed<IResponsiveTrackingSprite>(Icon);
        }
    }

}

