// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CoursePlot3DLine.cs
// Generates a line with 3D depth perception that shows the course provided. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Generates a line with 3D depth perception that shows the course provided. 
    /// The course is composed of INavigableTarget waypoints whose reported position can change depending on their implementation.
    /// <remarks>7.21.18 3D depth perception is internally created using varying line segment widths and opacity values.</remarks>
    /// </summary>
    public class CoursePlot3DLine : A3DVectrosityBase {

        private const float MaxSegmentWidth = 3F;
        private const float MinSegmentWidth = 1F;
        private const float MaxAlpha = 1F;
        private const float MinAlpha = 0.1F;

        private const float SegmentFillWidth = -1F;

        private static readonly Color32 _segmentFillColor = GameColor.Black.ToUnityColor();
        // Collections used to minimize allocations
        private static readonly IList<PointIndexCameraDistancePair> _pointIndexCameraDistancePairs = new List<PointIndexCameraDistancePair>();
        private static readonly List<float> _segmentWidths = new List<float>();
        private static readonly List<Color32> _segmentColors = new List<Color32>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CoursePlot3DLine" /> class
        /// with lineParent as the DynamicObjectsFolder.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="course">The course.</param>
        public CoursePlot3DLine(string name, IList<INavigableDestination> course, GameColor color)
            : this(name, course, GameReferences.DynamicObjectsFolder.Folder, color) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoursePlot3DLine" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="course">The course.</param>
        /// <param name="lineParent">The line parent.</param>
        /// <param name="color">The color of the line.</param>
        public CoursePlot3DLine(string name, IList<INavigableDestination> course, Transform lineParent, GameColor color)
            : base(name, course.Select(wayPt => wayPt.Position).ToList(), null, lineParent, LineType.Continuous, 1F, color) {
        }

        protected override void Initialize() {
            base.Initialize();
            _line.smoothWidth = true;
            RefreshWidthsAndColors();
            Subscribe();
        }

        private void Subscribe() {
            GameReferences.MainCameraControl.sectorIDChanged += CameraSectorChangedHandler;
        }

        /// <summary>
        /// Updates the course. 
        /// Use this when you have added, replaced or removed one or more waypoints in your course.
        /// </summary>
        /// <param name="course">The course.</param>
        public void RefreshCourse(IList<INavigableDestination> course) {
            D.AssertNotNull(course);
            List<Vector3> waypointLocations = new List<Vector3>(course.Count);
            for (int i = 0; i < course.Count; i++) {
                INavigableDestination waypoint = course[i];
                D.AssertNotNull(waypoint, course.Select(wpt => wpt.DebugName).Concatenate());
                waypointLocations.Add(waypoint.Position);
            }
            Points = waypointLocations;   // updating Points will update _line.points3 list
        }

        #region Event and Property Change Handlers

        private void CameraSectorChangedHandler(object sender, EventArgs e) {
            HandleCameraChangedSector();
        }

        #endregion

        private void HandleCameraChangedSector() {
            if (IsLineActive) {
                RefreshWidthsAndColors();
            }
        }

        protected override void HandleColorPropChanged() {
            base.HandleColorPropChanged();
            if (IsLineActive) {
                RefreshWidthsAndColors();
            }
        }

        protected override void HandleLineWidthPropChanged() {
            throw new InvalidOperationException();
        }

        protected override void HandlePointsPropChanged() {
            base.HandlePointsPropChanged();
            if (IsLineActive) {
                RefreshWidthsAndColors();
            }
        }

        protected override void HandleLineActivated() {
            base.HandleLineActivated();
            RefreshWidthsAndColors();
        }

        /// <summary>
        /// Refreshes the width and color values of all Points-defined line segments.
        /// <remarks>Public to allow client to call. 7.21.18 Currently called internally when 
        /// 1) Line is initially shown, 2) Color property is changed, 3) Course is refreshed, 4) Line is
        /// re-shown (Reactivated) and 5) MainCamera moves enough to change sectors.</remarks>
        /// <remarks>7.31.18 Segments are thickest and brightest on the course segments that are
        /// closest to the camera providing some depth perception between the segments of a particular course plot.
        /// Depth perception BETWEEN multiple course plots is currently not supported.</remarks>
        /// </summary>
        public void RefreshWidthsAndColors() {
            int pointsCount = Points.Count;
            if (pointsCount == Constants.Zero) {
                return; // 7.21.18 No apparent issues with widths and colors when points eliminated 
            }
            //D.Log("{0} is refreshing widths and colors.", DebugName);
            D.Assert(pointsCount >= 2);
            int segmentCount = pointsCount - 1;
            // Calc ascending order of Points-defined line segments by distance from camera

            _pointIndexCameraDistancePairs.Clear(); // Can't use Dictionary as Points can contain duplicate values
            for (int pointIndex = 0; pointIndex < segmentCount; pointIndex++) {
                Vector3 segmentStartPt = Points[pointIndex];
                Vector3 segmentEndPt = Points[pointIndex + 1];
                Vector3 segmentMidPt = segmentStartPt + (segmentEndPt - segmentStartPt) / 2F;

                float segmentMidPtCameraDistance = segmentMidPt.DistanceToCamera();
                var pointIndexCameraDistancePair = new PointIndexCameraDistancePair(pointIndex, segmentMidPtCameraDistance);
                _pointIndexCameraDistancePairs.Add(pointIndexCameraDistancePair);
            }

            // segment indices are in ascending order of distance from camera, aka those that are closest to camera come first
            var distanceOrderedPointIndices = _pointIndexCameraDistancePairs.OrderBy(pair => pair.Distance).Select(pair => pair.Index).ToArray();

            _segmentWidths.Clear();
            _segmentWidths.Capacity = segmentCount;
            _segmentWidths.Fill(SegmentFillWidth);

            _segmentColors.Clear();
            _segmentColors.Capacity = segmentCount;
            _segmentColors.Fill(_segmentFillColor);

            float startingWidth = MaxSegmentWidth;
            float endingWidth = MinSegmentWidth;
            float widthDecrement = (startingWidth - endingWidth) / segmentCount;
            float segmentWidth = startingWidth;
            if (segmentCount == 1) { // no depth perception with only 1 segment so use MinSegmentWidth
                segmentWidth = endingWidth;
            }

            float startingAlpha = MaxAlpha;
            float endingAlpha = MinAlpha;
            float alphaDecrement = (startingAlpha - endingAlpha) / segmentCount;
            float segmentAlpha = startingAlpha;

            for (int i = 0; i < distanceOrderedPointIndices.Length; i++) {
                int pointIndex = distanceOrderedPointIndices[i];
                _segmentWidths[pointIndex] = segmentWidth;
                _segmentColors[pointIndex] = Color.ToUnityColor(segmentAlpha);
                segmentWidth -= widthDecrement;
                segmentAlpha -= alphaDecrement;
            }

            __ValidateSegmentWidths(_segmentWidths);
            __ValidateSegmentColors(_segmentColors);

            _line.SetWidths(_segmentWidths);
            _line.SetColors(_segmentColors);
        }

        #region Cleanup

        private void Unsubscribe() {
            GameReferences.MainCameraControl.sectorIDChanged -= CameraSectorChangedHandler;
        }

        protected override void Cleanup() {
            base.Cleanup();
            Unsubscribe();
        }

        #endregion

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __ValidateSegmentWidths(List<float> widths) {
            foreach (var width in widths) {
                D.AssertNotEqual(SegmentFillWidth, width);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void __ValidateSegmentColors(List<Color32> colors) {
            foreach (var color in colors) {
                D.AssertNotEqual(_segmentFillColor, color);
            }
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Custom alternative to a Dictionary's KeyValuePair holding a Points Index value and a Camera Distance
        /// to the line segment that begins at the Vector3 point held in Points[Index].
        /// <remarks>Required as Points can have duplicates so a Dictionary can't be used.</remarks>
        /// </summary>
        private struct PointIndexCameraDistancePair : IEquatable<PointIndexCameraDistancePair> {

            public int Index { get; private set; }

            public float Distance { get; private set; }

            public PointIndexCameraDistancePair(int index, float distance) {
                Index = index;
                Distance = distance;
            }

            #region Object.Equals and GetHashCode Override

            public override bool Equals(object obj) {
                if (!(obj is PointIndexCameraDistancePair)) { return false; }
                return Equals((PointIndexCameraDistancePair)obj);
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// See "Page 254, C# 4.0 in a Nutshell."
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode() {
                unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                    int hash = 17;  // 17 = some prime number
                    hash = hash * 31 + Index.GetHashCode(); // 31 = another prime number
                    hash = hash * 31 + Distance.GetHashCode();
                    return hash;
                }
            }

            #endregion

            #region IEquatable<PointIndexCameraDistancePair> Members

            public bool Equals(PointIndexCameraDistancePair other) {
                return Index == other.Index && Distance == other.Distance;
            }

            #endregion

        }

        #endregion

    }
}

