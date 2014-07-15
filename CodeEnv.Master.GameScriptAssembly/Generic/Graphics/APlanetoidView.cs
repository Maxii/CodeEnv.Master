// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APlanetoidView.cs
// An MVPresenter abstract base class for Planet and Moon Views.
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
/// An MVPresenter abstract base class for Planet and Moon Views.
/// </summary>
public abstract class APlanetoidView : AMortalItemView, ICameraFollowable {

    /// <summary>
    /// The Collider encompassing the bounds of this planetoid that intercepts input events for this view. 
    /// This collider also detects collisions with other operating objects in the universe and therefore
    /// should NOT be disabled when it is undiscernible.
    /// </summary>
    protected new SphereCollider Collider { get { return base.Collider as SphereCollider; } }

    protected override IIntel InitializePlayerIntel() {
        return new ImprovingIntel();
    }

    protected override void SubscribeToPlayerIntelCoverageChanged() {
        _subscribers.Add((PlayerIntel as ImprovingIntel).SubscribeToPropertyChanged<ImprovingIntel, IntelCoverage>(pi => pi.CurrentCoverage, OnPlayerIntelCoverageChanged));
    }

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 3.0F;
    public virtual float CameraFollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public virtual float CameraFollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion

}

