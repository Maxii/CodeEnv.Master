// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetItem.cs
// Item class for Planet Planetoids.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Item class for Planet Planetoids. 
/// </summary>
public class PlanetItem : APlanetoidItem {

    //public new PlanetData Data {
    //    get { return base.Data as PlanetData; }
    //    set { base.Data = value; }
    //}

    #region Initialization

    //protected override void InitializeModelMembers() {
    //    base.InitializeModelMembers();
    //    float orbitalRadius = _transform.localPosition.magnitude;
    //    Data.OrbitalSpeed = gameObject.GetSafeMonoBehaviourComponentInParents<Orbiter>().GetRelativeOrbitSpeed(orbitalRadius);
    //}

    #endregion

    #region Model Methods

    protected override void OnDeath() {
        base.OnDeath();
        var moons = _transform.GetComponentsInChildren<MoonItem>();
        if (moons.Any()) {
            // since the planet is on its way to destruction, the moons need to show their destruction too
            moons.ForAll(moon => moon.OnPlanetDying());
        }
        // TODO consider destroying the orbiter object and separating it from the OrbitSlot
    }

    #endregion

    #region View Methods
    #endregion

    #region Mouse Events
    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

