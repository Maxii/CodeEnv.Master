// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormationStationSlotID.cs
// Identifier for the station slot an element can occupy in a formation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Identifier for the station slot an element can occupy in a formation.
    /// First digit is layer from bottom (0) to top (2), second digit
    /// is row from back (0) to front (4), third digit is slot from left (0)
    /// to right (4).
    /// OPTIMIZE is this useful?
    /// </summary>
    public enum FormationStationSlotID {

        None,

        Slot_0_0_0,
        Slot_0_0_1,
        Slot_0_0_2,
        Slot_0_0_3,
        Slot_0_0_4,

        Slot_0_1_0,
        Slot_0_1_1,
        Slot_0_1_2,
        Slot_0_1_3,
        Slot_0_1_4,

        Slot_0_2_0,
        Slot_0_2_1,
        Slot_0_2_2,
        Slot_0_2_3,
        Slot_0_2_4,

        Slot_0_3_0,
        Slot_0_3_1,
        Slot_0_3_2,
        Slot_0_3_3,
        Slot_0_3_4,

        Slot_0_4_0,
        Slot_0_4_1,
        Slot_0_4_2,
        Slot_0_4_3,
        Slot_0_4_4,

        Slot_1_0_0,
        Slot_1_0_1,
        Slot_1_0_2,
        Slot_1_0_3,
        Slot_1_0_4,

        Slot_1_1_0,
        Slot_1_1_1,
        Slot_1_1_2,
        Slot_1_1_3,
        Slot_1_1_4,

        Slot_1_2_0,
        Slot_1_2_1,
        Slot_1_2_2,
        Slot_1_2_3,
        Slot_1_2_4,

        Slot_1_3_0,
        Slot_1_3_1,
        Slot_1_3_2,
        Slot_1_3_3,
        Slot_1_3_4,

        Slot_1_4_0,
        Slot_1_4_1,
        Slot_1_4_2,
        Slot_1_4_3,
        Slot_1_4_4,

        Slot_2_0_0,
        Slot_2_0_1,
        Slot_2_0_2,
        Slot_2_0_3,
        Slot_2_0_4,

        Slot_2_1_0,
        Slot_2_1_1,
        Slot_2_1_2,
        Slot_2_1_3,
        Slot_2_1_4,

        Slot_2_2_0,
        Slot_2_2_1,
        Slot_2_2_2,
        Slot_2_2_3,
        Slot_2_2_4,

        Slot_2_3_0,
        Slot_2_3_1,
        Slot_2_3_2,
        Slot_2_3_3,
        Slot_2_3_4,

        Slot_2_4_0,
        Slot_2_4_1,
        Slot_2_4_2,
        Slot_2_4_3,
        Slot_2_4_4

    }
}

