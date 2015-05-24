// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarGraphics.cs
//  Graphics manager for Stars derived from ItemGraphics.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Graphics manager for Stars derived from ItemGraphics. 
/// Assumes location on the same game object as the Star.
/// </summary>
[System.Obsolete]
public class StarGraphics : ItemGraphics {

    protected override void RegisterComponentsToDisable() {
        base.RegisterComponentsToDisable();
        IEnumerable<GameObject> glowGameObjects = gameObject.GetSafeMonoBehavioursInChildren<StarGlowAnimator>().Select(sg => sg.gameObject);
        if (disableGameObjectOnCameraDistance.IsNullOrEmpty()) {
            disableGameObjectOnCameraDistance = new GameObject[0];
        }
        disableGameObjectOnCameraDistance = disableGameObjectOnCameraDistance.Union(glowGameObjects).ToArray();

        Component[] starAnimatingBehaviours = new Component[2] { gameObject.GetSafeMonoBehaviour<StarAnimator>(), gameObject.GetSafeMonoBehaviourInChildren<Billboard>() };
        if (disableComponentOnCameraDistance.IsNullOrEmpty()) {
            disableComponentOnCameraDistance = new Component[0];
        }
        disableComponentOnCameraDistance = disableComponentOnCameraDistance.Union(starAnimatingBehaviours).ToArray();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

