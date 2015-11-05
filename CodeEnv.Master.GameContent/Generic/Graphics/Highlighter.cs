// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Highlighter.cs
// Highlighter that places circles and spheres around IHighlightable objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
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
    /// Highlighter that places circles and spheres around IHighlightable objects.
    /// </summary>
    public class Highlighter : IHighlighter, IDisposable {

        private IHighlightable _highlightableObject;
        private Transform _trackedTransform;

        /// <summary>
        /// Performance optimization flag indicating whether the size of the 
        /// highlight on the screen should change or stay the same.
        /// </summary>
        private bool _shouldHighlightSizeOnScreenChange;
        private HighlightCircle _circles;

        /// <summary>
        /// Initializes a new instance of the <see cref="Highlighter"/> class. This version highlights
        /// and tracks the provided object, dynamically adjusting the size of the highlight to reflect the size
        /// of the object on the screen.
        /// </summary>
        /// <param name="highlightableObject">The highlightable object.</param>
        public Highlighter(IHighlightable highlightableObject)
            : this(highlightableObject, highlightableObject.transform, true) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Highlighter"/> class. This version highlights
        /// and tracks the provided transform using the information provided by the highlightableObject.
        /// Performance can be optimized if the size of the highlight on the screen does not need to dynamically
        /// change, aka the transform being tracked has a constant size on the screen.
        /// </summary>
        /// <param name="highlightableObject">The highlighted object.</param>
        /// <param name="trackedTransform">The alternative transform for the highlight to track instead of the highlightedObject.</param>
        /// <param name="shouldHighlightSizeOnScreenChange">Performance optimization flag indicating whether the size of the 
        /// highlight on the screen should change or stay the same.</param>
        public Highlighter(IHighlightable highlightableObject, Transform trackedTransform, bool shouldHighlightSizeOnScreenChange) {
            _highlightableObject = highlightableObject;
            _trackedTransform = trackedTransform;
            D.Log("{0} _trackedTransform assigned is named {1}.", highlightableObject.DisplayName + GetType().Name, trackedTransform.name);
            _shouldHighlightSizeOnScreenChange = shouldHighlightSizeOnScreenChange;
        }

        private void ShowCircle(HighlightID id) {
            if (_circles == null) {
                string circlesTitle = "{0} Circle Highlight".Inject(_highlightableObject.DisplayName);
                float innerCircleRadius = _highlightableObject.HighlightRadius;
                D.Assert(_trackedTransform != null, "{0} _trackedTransform is null.".Inject(circlesTitle));
                _circles = new HighlightCircle(circlesTitle, _trackedTransform, innerCircleRadius, _shouldHighlightSizeOnScreenChange, maxCircles: 3);
                _circles.Colors = new List<GameColor>() { TempGameValues.FocusedColor, TempGameValues.SelectedColor, TempGameValues.GeneralHighlightColor };
                _circles.Widths = new List<float>() { 2F, 2F, 1F };
            }
            _circles.Show(true, GetCircleIndex(id));
        }

        private void HideCircle(HighlightID id) {
            if (_circles == null) { return; }
            _circles.Show(false, GetCircleIndex(id));
        }

        private int GetCircleIndex(HighlightID id) {
            switch (id) {
                case HighlightID.Focused:
                    return 0;
                case HighlightID.Selected:
                    return 1;
                case HighlightID.UnitElement:
                    return 2;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        /// <summary>
        /// Hides any remaining <c>HighlightID</c>s that aren't present in <c>IDsToShow</c>.
        /// </summary>
        /// <param name="IDsToShow">The ids to show.</param>
        private void HideRemaining(IEnumerable<HighlightID> IDsToShow) {
            var remainingIDs = Enums<HighlightID>.GetValues(excludeDefault: true).Except(IDsToShow);
            remainingIDs.ForAll(id => HideCircle(id));
        }

        private void Cleanup() {
            if (_circles != null) { _circles.Dispose(); }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IHighlighter Members

        public void Show(params HighlightID[] IDs) {
            IEnumerable<HighlightID> idsToShow = IDs;
            if (idsToShow.Contains(HighlightID.None)) {
                D.Assert(IDs.Length == 1);
                idsToShow = Enumerable.Empty<HighlightID>();
            }
            else {
                idsToShow.ForAll(id => ShowCircle(id));
            }
            HideRemaining(idsToShow);
        }

        public void ShowHovered(bool toShow) {
            var hoveredHighlight = References.SphericalHighlight;
            if (hoveredHighlight != null) {  // allows deactivation of the SphericalHighlight gameObject
                if (toShow) {
                    hoveredHighlight.SetTarget(_highlightableObject, _highlightableObject.HoverHighlightRadius);
                }
                hoveredHighlight.Show(toShow);
            }
        }

        #endregion

        #region IDisposable

        private bool _alreadyDisposed = false;
        protected bool _isDisposing = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (_alreadyDisposed) {
                return;
            }

            _isDisposing = true;
            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            _alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion

    }
}

