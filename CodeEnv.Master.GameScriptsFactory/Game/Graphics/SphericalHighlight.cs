// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemHighlight.cs
// Singleton spherical highlight control whose radius and position can be set as needed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton spherical highlight control whose radius and position can be set as needed.
/// </summary>
public class SphericalHighlight : AMonoBaseSingleton<SphericalHighlight>, ISphericalHighlight {

    public float showAlphaValue = 1.0F;

    private Renderer _renderer;
    private float _baseSphereRadius;

    protected override void Awake() {
        base.Awake();
        _renderer = UnityUtility.ValidateComponentPresence<Renderer>(gameObject);
        _baseSphereRadius = _renderer.bounds.size.x / 2F;
        //D.Log("{0} base sphere radius = {1}.", GetType().Name, _baseSphereRadius);
        Show(false);
    }

    public Vector3 Position { set { _transform.position = value; } }

    public float Radius {
        set {
            float scaleFactor = value / _baseSphereRadius;
            _transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        }
    }

    public void Show(bool toShow) {
        float alpha = toShow ? showAlphaValue : Constants.ZeroF;
        _renderer.material.SetAlpha(alpha);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

