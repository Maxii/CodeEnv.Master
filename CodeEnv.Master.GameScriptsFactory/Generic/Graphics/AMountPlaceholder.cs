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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for a mount placeholder.
/// </summary>
public abstract class AMountPlaceholder : AMount {

    public MountSlotID slotID;

    protected override void Validate() {
        base.Validate();
        D.Assert(slotID != MountSlotID.None);
    }

}

