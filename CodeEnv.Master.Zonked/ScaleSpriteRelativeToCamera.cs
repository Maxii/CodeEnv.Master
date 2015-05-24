// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ScaleSpriteRelativeToCamera.cs
// Scales Ngui3 WIdget height and width relative to camera distance rather than the gameobject transform's scale.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Scales Ngui3 WIdget height and width relative to camera distance rather than the gameobject transform's scale.
/// </summary>
[Obsolete]
public class ScaleSpriteRelativeToCamera : AMonoBase {

    public FrameUpdateFrequency updateRate = FrameUpdateFrequency.Continuous;
    public float spriteScale = 1.0F;

    private Vector2 _initialScale;
    private UISprite _sprite;

    protected override void Awake() {
        base.Awake();
        // record initial scale of the GO and use it as a basis
        _sprite = gameObject.GetSafeMonoBehaviour<UISprite>();
        _initialScale = _sprite.localSize;
        UpdateRate = updateRate;
    }

    // scale object relative to distance from camera plane
    void Update() {
        if (ToUpdate()) {
            _sprite.height = Mathf.CeilToInt(_initialScale.y * _transform.DistanceToCameraInt() * spriteScale);
            _sprite.width = Mathf.CeilToInt(_initialScale.x * _transform.DistanceToCameraInt() * spriteScale);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

