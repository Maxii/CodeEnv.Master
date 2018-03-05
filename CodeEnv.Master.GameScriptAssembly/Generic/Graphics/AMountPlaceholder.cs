// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMountPlaceholder.cs
// Abstract base class for a mount placeholder.
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

    private const string DebugNameFormat = "{0}[{1}]";

    [SerializeField]
    private int _slotNumber = 0;

    public EquipmentSlotID SlotID { get; private set; }

    public override string DebugName { get { return DebugNameFormat.Inject(base.DebugName, SlotID.DebugName); } }

    protected abstract EquipmentMountCategory SupportedMount { get; }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        SlotID = new EquipmentSlotID(_slotNumber, SupportedMount);
    }

    protected override void Validate() {
        base.Validate();
        D.AssertNotDefault(_slotNumber);
    }

}

