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

    using System.Collections;
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
        public Transform Target { get; set; }

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

        public GameColor[] Colors { get; set; }

        public float[] Widths { get; set; }

        private bool[] _circlesToShow;
        private int _segmentsPerCircle = 30;
        private int _circleSeparation = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="HighlightCircle"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The target.</param>
        /// <param name="normalizedRadius">The normalized radius.</param>
        /// <param name="parent">The parent to attach the VectorObject too.</param>
        /// <param name="isRadiusDynamic">if set to <c>true</c> [is radius dynamic].</param>
        /// <param name="maxCircles">The maximum circles.</param>
        /// <param name="width">The width.</param>
        /// <param name="color">The color.</param>
        public HighlightCircle(string name, Transform target, float normalizedRadius, Transform parent = null, bool isRadiusDynamic = true, int maxCircles = 1, float width = 1F, GameColor color = GameColor.White)
            : base(name, parent) {
            Target = target;
            NormalizedRadius = normalizedRadius;
            IsRadiusDynamic = isRadiusDynamic;
            MaxCircles = maxCircles;
            Widths = new float[maxCircles];
            Widths.Populate<float>(width);
            Colors = new GameColor[maxCircles];
            Colors.Populate<GameColor>(color);
        }

        /// <summary>
        /// Coroutine method that shows a circle around Target.
        /// </summary>
        /// <param name="index">The index of the circle.</param>
        /// <returns></returns>
        public IEnumerator ShowCircles(int index) {
            Arguments.ValidateForRange(index, Constants.Zero, MaxCircles - 1);
            if (_line == null) { Initialize(); }

            _circlesToShow[index] = true;
            // D.Log("Circle {0} added to {1}.", index, LineName);

            if (!_line.active) {
                _line.active = true;
                D.Log("{0} coroutine started.", LineName);
                while (_line.active) {
                    Vector2 screenPoint = Camera.main.WorldToScreenPoint(Target.position);
                    float distanceToCamera = IsRadiusDynamic ? Target.DistanceToCamera() : 1F;
                    //float distanceToCamera = Camera.main.transform.InverseTransformPoint(Target.position).z;
                    for (int circleIndex = 0; circleIndex < MaxCircles; circleIndex++) {
                        if (_circlesToShow[circleIndex]) {
                            //float radius = NormalizedRadius + (circleIndex * _circleSeparation) / distanceToCamera;
                            float radius = (NormalizedRadius / distanceToCamera) + (circleIndex * _circleSeparation);

                            int startpointIndex = _segmentsPerCircle * circleIndex * 2;
                            _line.MakeCircle(screenPoint, radius, _segmentsPerCircle, startpointIndex);
                        }
                    }
                    _line.Draw();
                    yield return null;
                }
                D.Log("{0} coroutine finished.", LineName);
            }
            else {
                D.Warn("{0} coroutine is already running. To add this circle, use AddCircle({1}).", LineName, index);
            }
        }

        /// <summary>
        /// Adds the circle with this index to the coroutine that is showing the circles.
        /// </summary>
        /// <param name="index">The index.</param>
        public void AddCircle(int index) {
            _circlesToShow[index] = true;
        }

        /// <summary>
        /// Removes the circle with this index from the coroutine that is showing the circles.
        /// If this is the last circle showing, the coroutine will be terminated.
        /// </summary>
        /// <param name="index">The index.</param>
        public void RemoveCircle(int index) {
            if (_circlesToShow[index]) {
                _circlesToShow[index] = false;
                //D.Log("Circle {0} removed from {1}.", index, LineName);
                // Note: selectively zeroing only points for this circle draws a line to (0,0)
                _line.ZeroPoints();
                if (_circlesToShow.Where(cShowing => cShowing == true).IsNullOrEmpty()) {
                    //D.Log("Line {0} no longer active.", LineName);
                    _line.active = false;
                }
            }
        }

        protected override void Initialize() {
            int points = MaxCircles * _segmentsPerCircle * 2;   // 2 points per segment for a discrete line
            _line = new VectorLine(LineName, new Vector2[points], null, 1F, LineType.Discrete);
            if (Parent != null) {
                OnParentChanged();
            }

            _line.vectorObject.layer = (int)Layers.Vectrosity2D;
            _line.active = false;

            _circlesToShow = new bool[MaxCircles];

            InitializeColors();
            InitializeWidths();
        }

        private void InitializeColors() {
            int length = Colors.Length;
            if (length == 1) {
                _line.SetColor(Colors[0].ToUnityColor());
            }
            else if (length == MaxCircles) {
                for (int i = 0; i < MaxCircles; i++) {
                    int segmentStartIndex = _segmentsPerCircle * i;
                    int segmentEndIndex = _segmentsPerCircle * (i + 1) - 1;
                    _line.SetColor(Colors[i].ToUnityColor(), segmentStartIndex, segmentEndIndex);
                }
            }
            else {
                D.Warn("{0} color count {1} does not match Circle count {2}. Defaulting to {3}.", LineName, length, MaxCircles, Colors[0].GetName());
                _line.SetColor(GameColor.White.ToUnityColor());
            }
        }

        private void InitializeWidths() {
            int length = Widths.Length;
            if (length == 1) {
                _line.lineWidth = Widths[0];
            }
            else if (length == MaxCircles) {
                float[] segmentWidths = new float[MaxCircles * _segmentsPerCircle];
                for (int circleIndex = 0; circleIndex < MaxCircles; circleIndex++) {
                    int segmentStartIndex = _segmentsPerCircle * circleIndex;
                    int segmentEndIndex = _segmentsPerCircle * (circleIndex + 1) - 1;
                    for (int segmentIndex = segmentStartIndex; segmentIndex <= segmentEndIndex; segmentIndex++) {
                        segmentWidths[segmentIndex] = Widths[circleIndex];
                    }
                }
                _line.SetWidths(segmentWidths);
            }
            else {
                D.Warn("{0} width count {1} does not match Circle count {2}. Defaulting to {3}.", LineName, length, MaxCircles, Widths[0]);
                _line.SetColor(GameColor.White.ToUnityColor());
            }
        }

        public void Clear() {
            _line.ZeroPoints();
            _circlesToShow.ForAll(c => c = false);
            _line.active = false;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}
