// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CircleHighlightManager.cs
// Item Manager for Circle Highlights.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Item Manager for Circle Highlights.
    /// </summary>
    public class CircleHighlightManager : AHighlightManager {

        public override bool IsHighlightShowing { get { return _circles != null && _circles.IsShowing; } }

        public string CircleTitle { get; set; }

        private CircleHighlightID[] _circleHighlightsToShow;
        private float _circleRadius;
        private HighlightCircle _circles;
        /// <summary>
        /// Performance optimization flag indicating whether the size of the 
        /// highlight on the screen should dynamically adjust as the camera distance changes or stay the same.
        /// </summary>
        private bool _isCircleSizeDynamic;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircleHighlightManager"/> class.
        /// </summary>
        /// <param name="trackedClientTransform">The tracked client transform.</param>
        /// <param name="circleRadius">The circle radius.</param>
        /// <param name="isCircleSizeDynamic">Performance optimization flag indicating whether the size of the 
        /// highlight on the screen should dynamically adjust as the camera distance changes or stay the same.</param>
        public CircleHighlightManager(Transform trackedClientTransform, float circleRadius, bool isCircleSizeDynamic = true) : base(trackedClientTransform) {
            _circleRadius = circleRadius;
            _isCircleSizeDynamic = isCircleSizeDynamic;
            CircleTitle = "{0} Circle Highlight".Inject(trackedClientTransform.name);
        }

        public void SetCirclesToShow(params CircleHighlightID[] circleIDs) {
            D.Assert(!circleIDs.Contains(CircleHighlightID.None));
            _circleHighlightsToShow = circleIDs;
        }

        public override void Show(bool toShow) {
            IEnumerable<CircleHighlightID> circlesToHide;
            if (toShow) {
                D.AssertNotNull(_circleHighlightsToShow);   // won't catch most but will catch when don't set circles following Show(false)
                _circleHighlightsToShow.ForAll(circleID => ShowCircle(circleID));
                circlesToHide = Enums<CircleHighlightID>.GetValues(excludeDefault: true).Except(_circleHighlightsToShow);
            }
            else {
                circlesToHide = Enums<CircleHighlightID>.GetValues(excludeDefault: true);
                _circleHighlightsToShow = null;
            }
            circlesToHide.ForAll(circleID => HideCircle(circleID));
        }

        private void ShowCircle(CircleHighlightID id) {
            if (_circles == null) {
                D.AssertNotNull(_trackedClientTransform, Name);
                _circles = new HighlightCircle(CircleTitle, _trackedClientTransform, _circleRadius, _isCircleSizeDynamic, maxCircles: 3);
                _circles.Colors = new List<GameColor>() { TempGameValues.FocusedColor, TempGameValues.SelectedColor, TempGameValues.GeneralHighlightColor };
                _circles.Widths = new List<float>() { 2F, 2F, 1F };
            }
            _circles.Show(true, GetCircleIndex(id));
        }

        private void HideCircle(CircleHighlightID id) {
            if (_circles == null) {
                return;
            }
            _circles.Show(false, GetCircleIndex(id));
        }

        private int GetCircleIndex(CircleHighlightID id) {
            switch (id) {
                case CircleHighlightID.Focused:
                    return 0;
                case CircleHighlightID.Selected:
                    return 1;
                case CircleHighlightID.UnitElement:
                    return 2;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        public override void HandleClientDeath() {
            base.HandleClientDeath();
            if (_circles != null) {
                _circles.Dispose();
                _circles = null;
            }
        }

        protected override void Cleanup() {
            if (_circles != null) {
                _circles.Dispose();
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

