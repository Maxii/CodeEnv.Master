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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class world-space tracking widget that becomes parented to and tracks a world target.
/// </summary>
public abstract class AWorldTrackingWidget : ATrackingWidget, IWorldTrackingWidget {

    public override IWidgetTrackable Target {
        get { return base.Target; }
        set {
            if (Target != null) {
                // cannot change a target of a WorldTrackingWidget once set as it is parented to the target
                throw new NotSupportedException("Attempted invalid change of Target {0} to {1}.".Inject(Target.transform.name, value.transform.name));
            }
            base.Target = value;
        }
    }

    protected Billboard _billboard;

    protected override void Awake() {
        base.Awake();
        _billboard = gameObject.GetSingleComponentInChildren<Billboard>();
        _billboard.WarnIfUIPanelPresentInParents = false;
        _billboard.enabled = false;
    }

    protected override void Show() {
        base.Show();
        _billboard.enabled = true;
    }

    protected override void Hide() {
        base.Hide();
        _billboard.enabled = false;
    }

    #region Debug

    protected override void __RenameGameObjects() {
        base.__RenameGameObjects();
        if (Target != null) {   // Target can be null if OptionalRootName is set before Target
            var rootName = OptionalRootName.IsNullOrEmpty() ? Target.DebugName : OptionalRootName;
            _billboard.name = rootName + Constants.Space + typeof(Billboard).Name;
        }
    }

    #endregion

}

