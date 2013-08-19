// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFollowableItem.cs
// Abstract base class for any item in the universe that is followable.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract base class for any item in the universe that is followable. Also provides
/// CursorHud support if there is Data for the item.
/// </summary>
public abstract class AFollowableItem : AFocusableItem, ICameraFollowable {

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 2.0F;
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

