// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarItem.cs
// The data-holding class for all Stars in the game.
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
/// The data-holding class for all Stars in the game.
/// </summary>
public class StarModel : AItemModel, IStarModel, IStarTarget, IOrbitable {

    //public static float MaxRadius { get; private set; }

    public new StarData Data {
        get { return base.Data as StarData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializeRadiiComponents() {
        var meshRenderer = gameObject.GetComponentInImmediateChildren<Renderer>();
        Radius = meshRenderer.bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        //MaxRadius = Mathf.Max(Radius, MaxRadius);

        (collider as SphereCollider).radius = Radius;

        SphereCollider keepoutZoneCollider = gameObject.GetComponentInImmediateChildren<SphereCollider>();
        D.Assert(keepoutZoneCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutZoneCollider.radius = Radius * TempGameValues.KeepoutRadiusMultiplier;
        float orbitBufferDistanceAboveKeepoutZone = 1F;
        OrbitDistance = keepoutZoneCollider.radius + orbitBufferDistanceAboveKeepoutZone;
    }

    protected override void Initialize() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IOrbitable Members

    public float OrbitDistance { get; private set; }

    #endregion

}

