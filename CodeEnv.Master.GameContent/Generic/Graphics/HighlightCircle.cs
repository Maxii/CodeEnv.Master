// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HighlightCircle.cs
// Draws circle[s] around a Target. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Draws circle[s] around a Target. 
    /// </summary>
    public class HighlightCircle : AVectrosityBase {

        /// <summary>
        /// The transform the circle will encompass.
        /// </summary>
        public Transform Target { get; private set; }

        /// <summary>
        /// The desired radius of the circle in pixels when the Target is
        /// 1 unity unit away from the plane of the camera.
        /// </summary>
        public float NormalizedRadius { get; set; }

        /// <summary>
        /// Indicates whether this circle should vary its radius with Target's
        /// distance to the camera.
        /// </summary>
        public bool IsRadiusDynamic { get; set; }

        public int MaxCircles { get; set; }

        public List<GameColor> Colors { get; set; }

        public List<float> Widths { get; set; }

        private bool[] _circlesToShow;
        private int _segmentsPerCircle = 30;
        private int _circleSeparation = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="HighlightCircle"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The target.</param>
        /// <param name="normalizedRadius">The normalized radius.</param>
        /// <param name="isRadiusDynamic">if set to <c>true</c> [is radius dynamic].</param>
        /// <param name="maxCircles">The maximum circles.</param>
        /// <param name="width">The width.</param>
        /// <param name="color">The color.</param>
        public HighlightCircle(string name, Transform target, float normalizedRadius, bool isRadiusDynamic = true, int maxCircles = 1, float width = 1F, GameColor color = GameColor.White)
            : base(name) {
            Target = target;
            NormalizedRadius = normalizedRadius;
            IsRadiusDynamic = isRadiusDynamic;
            MaxCircles = maxCircles;
            Widths = new List<float>(maxCircles);
            Widths.Populate<float>(width);
            Colors = new List<GameColor>(maxCircles);
            Colors.Populate<GameColor>(color);
            //InitializeCamera();
        }

        /// <summary>
        /// Initializes the camera so highlight circles appear behind UI elements.
        /// <see cref="http://www.tasharen.com/forum/index.php?topic=8476.0"/>
        /// FIXME Currently doesn't work as Vectrosity 5 only supports RenderMode.ScreenSpace-Overlay.
        /// </summary>
        private void InitializeCamera() {
            if (VectorLine.canvas == null) {
                D.Log("Initializing HighlightCircle Camera and RenderMode.");
                VectorLine.SetCanvasCamera(References.GuiCameraControl.GuiCamera);  // sets up the canvas
                //VectorLine.canvas.renderMode = RenderMode.ScreenSpaceCamera;    // SetCanvasCamera() sets mode to ScreenSpaceCamera
                VectorLine.canvas.planeDistance = 1;
                VectorLine.canvas.sortingOrder = -1;
            }
        }

        /// <summary>
        /// Shows or hides a circle around a target.
        /// </summary>
        /// <param name="toShow">if set to <c>true</c> the circle with this index will show.</param>
        /// <param name="index">The index of the circle to show or hide.</param>
        public void Show(bool toShow, int index) {
            Arguments.ValidateForRange(index, Constants.Zero, MaxCircles - 1);
            if (_line == null) {
                InitializeLine();
                InitializeColors();
                InitializeWidths();
            }

            if (toShow) {
                _drawJob = _drawJob ?? new Job(DrawCircles(), toStart: true, jobCompleted: delegate {
                    D.Log("{0}.Job(DrawCircles()) completed.", LineName);
                    //TODO
                });
                AddCircle(index);
                _line.active = true;
            }
            else {
                if (_drawJob != null && _drawJob.IsRunning) {
                    RemoveCircle(index);
                }
            }
        }

        /// <summary>
        /// Coroutine method that draws one or more circles around Target.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DrawCircles() {
            //D.Log("{0} totalLinePoints = {1}.", GetType().Name, _line.points2.Count);
            while (true) {
                Vector2 screenPoint = Camera.main.WorldToScreenPoint(Target.position);
                float distanceToCamera = IsRadiusDynamic ? Target.DistanceToCamera() : 1F;
                //float distanceToCamera = Camera.main.transform.InverseTransformPoint(Target.position).z;
                for (int circleIndex = 0; circleIndex < MaxCircles; circleIndex++) {
                    if (_circlesToShow[circleIndex]) {
                        float radius = (NormalizedRadius / distanceToCamera) + (circleIndex * _circleSeparation);

                        int startpointIndex = _segmentsPerCircle * circleIndex * 2;
                        _line.MakeCircle(screenPoint, radius, _segmentsPerCircle, startpointIndex);
                    }
                    // Note: Can't use _line.drawStart and .drawEnd to visually clear a not showing circle as second circle 
                    // might be not showing requiring a start and end point each for first and third circle
                }
                _line.Draw();
                yield return null;
            }
        }

        /// <summary>
        /// Adds the circle with this index to the coroutine that is showing the circles.
        /// </summary>
        /// <param name="index">The index.</param>
        private void AddCircle(int index) {
            _circlesToShow[index] = true;
        }

        /// <summary>
        /// Removes the circle with this index from the coroutine that is showing the circles.
        /// If this is the last circle showing, the coroutine will be terminated.
        /// </summary>
        /// <param name="index">The index.</param>
        private void RemoveCircle(int index) {
            if (_circlesToShow[index]) {
                _circlesToShow[index] = false;
                D.Log("Circle {0} removed from {1}.", index, LineName);

                //_line.ZeroPoints();   // ZeroPoints() removed in Vectrosity 4.0
                // HACK my replacement for ZeroPoints() as whole class needs to be re-designed for Vectrosity 4.0
                _line.points2.Clear();
                _line.Draw();   // clears the screen of all circles. Coroutine will refill with showing circles
                int pointsCount = MaxCircles * _segmentsPerCircle * 2;  // 2 points per segment for a discrete line
                _line.Resize(pointsCount);  //_line.points2.AddRange(new Vector2[pointsCount]);
                InitializeColors();
                InitializeWidths();

                if (_circlesToShow.Where(cShowing => cShowing == true).IsNullOrEmpty()) {
                    D.Log("Line {0} no longer active.", LineName);
                    _drawJob.Kill();
                    _drawJob = null;
                    _line.active = false;
                }
            }
        }

        private void InitializeLine() {
            int pointsCount = MaxCircles * _segmentsPerCircle * 2;   // 2 points per segment for a discrete line
            _line = new VectorLine(LineName, new List<Vector2>(pointsCount), texture, 1F, LineType.Discrete);
            _line.active = false;

            _circlesToShow = new bool[MaxCircles];
        }

        private void InitializeColors() {
            int colorCount = Colors.Count;
            if (colorCount == 1) {
                _line.SetColor(Colors[0].ToUnityColor());
            }
            else if (colorCount == MaxCircles) {
                for (int circleIndex = 0; circleIndex < MaxCircles; circleIndex++) {
                    int segmentStartIndex = _segmentsPerCircle * circleIndex;
                    int segmentEndIndex = _segmentsPerCircle * (circleIndex + 1) - 1;
                    _line.SetColor(Colors[circleIndex].ToUnityColor(), segmentStartIndex, segmentEndIndex);
                }
            }
            else {
                D.Warn("{0} color count {1} does not match Circle count {2}. Defaulting to {3}.", LineName, colorCount, MaxCircles, Colors[0].GetValueName());
                _line.SetColor(GameColor.White.ToUnityColor());
            }
        }

        private void InitializeWidths() {
            int widthCount = Widths.Count;
            if (widthCount == 1) {
                _line.SetWidth(Widths[0]);
            }
            else if (widthCount == MaxCircles) {
                //D.Log("WidthCount, MaxCircles = {0}, SegmentsPerCircle = {1}.", widthCount, _segmentsPerCircle);
                List<float> segmentWidths = new List<float>(MaxCircles * _segmentsPerCircle);
                for (int circleIndex = 0; circleIndex < MaxCircles; circleIndex++) {
                    int segmentStartIndex = _segmentsPerCircle * circleIndex;
                    int segmentEndIndex = _segmentsPerCircle * (circleIndex + 1) - 1;
                    _line.SetWidth(Widths[circleIndex], segmentStartIndex, segmentEndIndex);
                }
            }
            else {
                D.Warn("{0} width count {1} does not match Circle count {2}. Defaulting to {3}.", LineName, widthCount, MaxCircles, Widths[0]);
                _line.SetWidth(Widths[0]);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}
