// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OptionalEquipMountCategory.cs
// Enum indicating the type of mount for optional equipment present on a Unit Member.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Enum indicating the type of mount for optional equipment present on a Unit Member.
    /// </summary>
    public enum OptionalEquipMountCategory {

        None,

        /// <summary>
        /// A rotating external mount for weapons that need to point at a target to engage it.
        /// </summary>
        [EnumAttribute("TurretMnt")]
        Turret,

        /// <summary>
        /// A fixed external mount for weapons capable of guiding themselves to a target to engage it.
        /// </summary>
        [EnumAttribute("SiloMnt")]
        Silo,

        /// <summary>
        /// A fixed internal mount for equipment capable of detecting Elements and other Celestial Objects.
        /// <remarks>Capable of mounting Sensor equipment that detect objects within the sphere defined by its range.</remarks>
        /// <remarks>Currently implemented on Elements and CmdModules.</remarks>
        /// </summary>
        [EnumAttribute("SensMnt")]
        Sensor,

        /// <summary>
        /// A fixed internal mount for equipment capable of mitigating damage from weapon ordnance impacts.
        /// <remarks>Capable of mounting PassiveCountermeasure equipment, aka armor, etc.</remarks>
        /// <remarks>Currently implemented as an internal mount that 'covers' all exterior surfaces of an Element
        /// and CmdModule.</remarks>
        /// </summary>
        [EnumAttribute("SkinMnt")]
        Skin,

        /// <summary>
        /// A fixed internal mount for equipment capable of detecting and intercepting weapon ordnance before impact.
        /// <remarks>Capable of mounting ActiveCountermeasure and ShieldGenerator equipment that can intercept ordnance 
        /// within the sphere defined by its range.</remarks>
        /// <remarks>Currently implemented on Elements.</remarks>
        /// </summary>
        [EnumAttribute("ScrnMnt")]
        Screen,

        /// <summary>
        /// A fixed internal mount for an FTL propulsion engine.
        /// <remarks>Currently implemented on Ships.</remarks>
        /// </summary>
        [EnumAttribute("FtlEngMnt")]
        FtlEngine,

        /// <summary>
        /// A fixed internal mount for equipment capable of being mounted on Sensor, Skin and Screen mounts.
        /// <remarks>Capable of mounting ActiveCountermeasure and ShieldGenerator equipment that can intercept ordnance 
        /// within the sphere defined by its range.</remarks>
        /// <remarks>Currently implemented on Elements.</remarks>
        /// </summary>
        [EnumAttribute("FlexMnt")]
        Flex

    }
}

