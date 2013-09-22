// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VectorLineFactory.cs
// Singleton. Factory that makes VectorLine instances.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;

/// <summary>
/// Singleton. Factory that makes VectorLine instances.
/// </summary>
public class VectorLineFactory : AGenericSingleton<VectorLineFactory> {

    private VectorLineFactory() {
        Initialize();
    }

    protected override void Initialize() { }

    public VelocityRay MakeInstance(string name, Transform target, Reference<float> speed, GameColor color = GameColor.White) {
        GameObject velocityRayPrefab = RequiredPrefabs.Instance.VelocityRay.gameObject;
        if (velocityRayPrefab == null) {
            D.Error("Prefab of Type {0} is not present.".Inject(typeof(VelocityRay).Name));
        }
        GameObject velocityRayCloneGO = NGUITools.AddChild(DynamicObjects.Folder.gameObject, velocityRayPrefab);
        // NGUITools.AddChild handles all scale, rotation, position, parent and layer settings

        VelocityRay velocityRay = velocityRayCloneGO.GetSafeMonoBehaviourComponent<VelocityRay>();
        // assign the System as the Target of the tracking label
        velocityRay.Target = target;
        velocityRay.Speed = speed;
        velocityRay.Color = color;
        velocityRay.LineName = name;
        NGUITools.SetActive(velocityRayCloneGO, true);
        return velocityRay;
    }

    public HighlightCircle MakeInstance(string name, Transform target, float normalizedRadius, bool isRadiusDynamic = true, int maxCircles = 1, float width = 1F, GameColor color = GameColor.White) {
        GameObject circlePrefab = RequiredPrefabs.Instance.HighlightCircle.gameObject;
        if (circlePrefab == null) {
            D.Error("Prefab of Type {0} is not present.".Inject(typeof(HighlightCircle).Name));
        }
        GameObject circleCloneGO = NGUITools.AddChild(DynamicObjects.Folder.gameObject, circlePrefab);
        // NGUITools.AddChild handles all scale, rotation, position, parent and layer settings

        HighlightCircle circle = circleCloneGO.GetSafeMonoBehaviourComponent<HighlightCircle>();
        circle.LineName = name;
        circle.Target = target;
        circle.NormalizedRadius = normalizedRadius;
        circle.IsRadiusDynamic = isRadiusDynamic;
        circle.MaxCircles = maxCircles;
        circle.Widths = new float[1] { width };
        circle.Colors = new GameColor[1] { color };
        NGUITools.SetActive(circleCloneGO, true);
        return circle;
    }


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

