// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarView.cs
// A class for managing the UI of a system's star.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a system's star.
/// </summary>
public class StarView : View {

    protected new StarPresenter Presenter {
        get { return base.Presenter as StarPresenter; }
        set { base.Presenter = value; }
    }

    private SphereCollider _keepoutCollider;

    protected override void Awake() {
        base.Awake();
        (_collider as SphereCollider).radius = TempGameValues.StarRadius;
        circleScaleFactor = 1.0F;
        _keepoutCollider = gameObject.GetComponentsInChildren<SphereCollider>().Single(c => c.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        _keepoutCollider.radius = (_collider as SphereCollider).radius * TempGameValues.KeepoutRadiusMultiplier;
    }

    protected override void InitializePresenter() {
        Presenter = new StarPresenter(this);
    }

    protected override void OnHover(bool isOver) {
        base.OnHover(isOver);
        Presenter.OnHover(isOver);
    }

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            Presenter.OnLeftClick();
        }
    }

    protected override void RegisterComponentsToDisable() {
        base.RegisterComponentsToDisable();
        IEnumerable<GameObject> glowGameObjects = gameObject.GetSafeMonoBehaviourComponentsInChildren<StarGlowAnimator>().Select(sg => sg.gameObject);
        if (disableGameObjectOnCameraDistance.IsNullOrEmpty()) {
            disableGameObjectOnCameraDistance = new GameObject[0];
        }
        disableGameObjectOnCameraDistance = disableGameObjectOnCameraDistance.Union(glowGameObjects).ToArray();

        Component[] starAnimatingBehaviours = new Component[2] { gameObject.GetSafeMonoBehaviourComponent<StarAnimator>(), gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>() };
        if (disableComponentOnCameraDistance.IsNullOrEmpty()) {
            disableComponentOnCameraDistance = new Component[0];
        }
        disableComponentOnCameraDistance = disableComponentOnCameraDistance.Union(starAnimatingBehaviours).ToArray();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

