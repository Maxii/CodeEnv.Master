// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AWorldTrackingWidget.cs
// Abstract base class world-space tracking widget that becomes parented to and tracks a world target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class world-space tracking widget that becomes parented to and tracks a world target.
/// </summary>
public abstract class AWorldTrackingWidget : ATrackingWidget {

    protected Billboard _billboard;

    public override IWidgetTrackable Target {
        get { return base.Target; }
        set {
            if (Target != null) {
                // cannot change a target of a WorldTrackingWidget once set as it is parented to the target
                throw new NotSupportedException("Attempted invalid change of Target {0} to {1}.".Inject(Target.Transform.name, value.Transform.name));
            }
            base.Target = value;
        }
    }

    protected override void Awake() {
        base.Awake();
        _billboard = gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>();
    }

    protected override void Show() {
        base.Show();
        _billboard.enabled = true;
    }

    protected override void Hide() {
        base.Hide();
        _billboard.enabled = false;
    }

    protected override void SetPosition() {
        //D.Log("Aligning position with target {0}. Offset is {1}.", Target.Transform.name, _offset);
        _transform.localPosition = _offset;
    }

    // No need to implement RefreshPositionOnUpdate() as this object is parented to a World gameObject that it automatically tracks
}

