// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEquipmentTriggerMonitor.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
[Obsolete]
public abstract class AEquipmentTriggerMonitor<EquipmentType> : AEquipmentMonitor<EquipmentType> where EquipmentType : ARangedEquipment {

    protected static HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();

    protected sealed override void OnTriggerEnter(Collider other) {
        base.OnTriggerEnter(other);
        D.Log("{0}.OnTriggerEnter() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.OnTriggerEnter() ignored TriggerCollider {1}.", Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        OnColliderEntering(other);
    }

    protected abstract void OnColliderEntering(Collider other);

    protected sealed override void OnTriggerExit(Collider other) {
        base.OnTriggerExit(other);
        D.Log("{0}.OnTriggerExit() tripped by {1}.", Name, other.name);
        if (other.isTrigger) {
            D.Log("{0}.OnTriggerExit() ignored TriggerCollider {1}.", Name, other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        OnColliderExiting(other);
    }

    protected abstract void OnColliderExiting(Collider other);
}

