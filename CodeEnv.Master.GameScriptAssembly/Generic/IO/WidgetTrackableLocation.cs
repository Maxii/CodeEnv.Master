// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WidgetTrackableLocation.cs
// An IWidgetTrackable GameObject that can be moved around as needed.
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
/// An IWidgetTrackable GameObject that can be moved around as needed.
/// Useful for hosting an Invisible CameraLosChangedListener.
/// </summary>
public class WidgetTrackableLocation : AMonoBase, IWidgetTrackable {

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IWidgetTrackable Members

    public string DisplayName { get { return typeof(WidgetTrackableLocation).Name; } }

    public Vector3 Position { get { return transform.position; } }

    public Vector3 GetOffset(WidgetPlacement placement) {
        // 5.4.16 Currently only used as an IWidgetTrackable for invisible CameraLosChangedListeners
        D.AssertEqual(WidgetPlacement.Over, placement);
        return Vector3.zero;
    }

    #endregion
}

