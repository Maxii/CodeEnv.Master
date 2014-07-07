// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetModel.cs
// The data-holding class for all planets in the game. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// The data-holding class for all planets in the game. 
/// </summary>
public class PlanetModel : APlanetoidModel, IPlanetModel {

    public new PlanetData Data {
        get { return base.Data as PlanetData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        base.Initialize();
        float orbitalRadius = _transform.localPosition.magnitude;
        Data.OrbitalSpeed = gameObject.GetSafeMonoBehaviourComponentInParents<Orbiter>().GetSpeedOfBodyInOrbit(orbitalRadius);
    }

    protected override void OnDeath() {
        base.OnDeath();
        var moons = _transform.GetComponentsInChildren<MoonModel>();
        if (moons.Any()) {
            // since the planet is on its way to destruction, the moons need to show their destruction too
            moons.ForAll(moon => moon.OnPlanetDying());
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

