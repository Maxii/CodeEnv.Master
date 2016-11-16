// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AWorldTrackingWidget_VariableSize.cs
// Abstract base class world-space tracking widget that becomes parented to and tracks a world target.
// The user perceives the widget changing size as the camera and/or tracked gameObject moves.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract base class world-space tracking widget that becomes parented to and tracks a world target. 
/// The user perceives the widget changing size as the camera and/or tracked gameObject moves.
/// </summary>
public abstract class AWorldTrackingWidget_VariableSize : AWorldTrackingWidget {

    /// <summary>
    /// The Units per Pixel multiplier used to calculate the maximum distance that world tracking widgets are legible.
    /// </summary>
    protected static float _maxShowDistanceMultiplier = 50F;

    protected override float CalcMaxShowDistance(float max) {
        float result = GetSmallestWidgetDimension() * _maxShowDistanceMultiplier;
        if (max < Mathf.Infinity) {
            if (max < result) {
                result = max;
            }
            else {
                D.WarnContext(this, "{0} requested maxShowDistance {1} is too large to be legible. \nSetting reduced to {2}.", Name, max, result);
            }
        }
        return result;
    }

    protected abstract int GetSmallestWidgetDimension();

}

