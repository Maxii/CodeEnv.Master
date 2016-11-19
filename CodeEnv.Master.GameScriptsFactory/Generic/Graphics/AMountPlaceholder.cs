// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMountPlaceholder.cs
//  Abstract base class for a mount placeholder.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Abstract base class for a mount placeholder.
/// </summary>
public abstract class AMountPlaceholder : AMount {

    //[FormerlySerializedAs("slotID")]
    [SerializeField]
    private MountSlotID _slotID = MountSlotID.None;

    public MountSlotID SlotID { get { return _slotID; } }

    protected override void Validate() {
        base.Validate();
        D.AssertNotDefault((int)_slotID);
    }

}

