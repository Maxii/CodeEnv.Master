﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VelocityRay.cs
// Creates and continuously updates a Ray eminating from Target that 
// indicates the Target's forward direction and speed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System.Collections;
using CodeEnv.Master.Common;
using UnityEngine;
using Vectrosity;

/// <summary>
/// Creates and continuously updates a Ray eminating from Target that 
/// indicates the Target's forward direction and speed.
/// </summary>
public class VelocityRay : AVectrosityBase {

    /// <summary>
    /// The transform that this VelocityRay is eminating from.
    /// </summary>
    public Transform Target { get; set; }
    public Reference<float> Speed { get; set; }

    private float _width = 1F;
    public float Width {
        get { return _width; }
        set { SetProperty<float>(ref _width, value, "Width", OnWidthChanged); }
    }

    private GameColor _color = GameColor.White;
    public GameColor Color {
        get { return _color; }
        set { SetProperty<GameColor>(ref _color, value, "Color", OnColorChanged); }
    }

    protected override void Awake() {
        base.Awake();
        _line = new VectorLine(LineName, new Vector3[2], Color.ToUnityColor(), null, Width);
        //_line.vectorObject.transform.parent = _transform;
        UnityUtility.AttachChildToParent(_line.vectorObject, _transform.gameObject);
        _line.active = false;
    }

    /// <summary>
    /// Shows a directional line indicating the forward direction and speed of Target.
    /// </summary>
    public void ShowRay(bool toShow) {
        if (_line != null) {
            if (toShow && !_line.active) {
                StartCoroutine(KeepLineCurrent);
            }
            else if (!toShow && _line.active) {
                // the speed and heading is being shown by the coroutine
                _line.active = false;
            }
        }
    }

    private IEnumerator KeepLineCurrent() {
        _line.active = true;
        while (_line.active) {
            _line.points3[1] = Vector3.forward * Speed.Value;
            _line.Draw3D(Target); // using transform adjusts for position, rotation and scale
            // D.Log("FrameCount: {0}.", Time.frameCount);
            yield return null;
        }
    }

    private void OnColorChanged() {
        if (_line != null) {
            _line.SetColor(Color.ToUnityColor());
        }
    }

    private void OnWidthChanged() {
        if (_line != null) {
            _line.lineWidth = Width;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

