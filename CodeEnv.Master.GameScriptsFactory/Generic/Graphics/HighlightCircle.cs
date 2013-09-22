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

// default namespace

using System.Collections;
using System.Linq;
using CodeEnv.Master.Common;
using UnityEngine;
using Vectrosity;

/// <summary>
/// Draws circle[s] around a Target. 
/// </summary>
public class HighlightCircle : AVectrosityBase {

    public bool IsShowing {
        get { return _line.active; }
    }

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
    /// Controls showing of circle[s] around a Target.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
    /// <param name="index">The index of the circle to apply <c>toShow</c></param>
    public void ShowCircle(bool toShow, int index = 0) {
        Arguments.ValidateForRange(index, Constants.Zero, MaxCircles - 1);
        if (_line == null) { Initialize(); }

        if (toShow) {
            _circlesToShow[index] = true;
            if (!_line.active) {
                StartCoroutine(KeepCirclesCurrent);
            }
        }
        else if (!toShow && _circlesToShow[index]) {
            _circlesToShow[index] = false;
            // Note: selectively zeroing only points for this circle draws a line to (0,0)
            _line.ZeroPoints();
            if (_circlesToShow.Where(cShowing => cShowing == true).IsNullOrEmpty()) {
                D.Log("Line {0} no longer active.", LineName);
                _line.active = false;
            }
        }
    }

    private void Initialize() {
        int points = MaxCircles * _segmentsPerCircle * 2;   // 2 points per segment for a discrete line
        _line = new VectorLine(LineName, new Vector2[points], null, 1F, LineType.Discrete);
        _line.vectorObject.transform.parent = _transform;
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

    private IEnumerator KeepCirclesCurrent() {
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

    public void Clear() {
        _line.ZeroPoints();
        _circlesToShow.ForAll(c => c = false);
        _line.active = false;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

