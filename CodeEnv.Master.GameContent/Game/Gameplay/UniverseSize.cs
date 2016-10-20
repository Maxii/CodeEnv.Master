// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseSize.cs
// The Universe sizes available to play. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 


namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// The Universe sizes available to play. 
    /// </summary>
    public enum UniverseSize {

        None = 0,

        /// <summary>
        /// The smallest.
        /// </summary>
        [EnumAttribute("The smallest.")]
        Tiny,

        /// <summary>
        /// Smaller than Normal.
        /// </summary>
        [EnumAttribute("Smaller than Normal.")]
        Small,

        /// <summary>
        /// The most common.
        /// </summary>
        [EnumAttribute("The most common.")]
        Normal,

        /// <summary>
        ///Larger than Normal.
        /// </summary>
        [EnumAttribute("Larger than Normal.")]
        Large,

        /// <summary>
        /// Larger than Large.
        /// </summary>
        [EnumAttribute("Larger than Large.")]
        Enormous,

        /// <summary>
        /// The largest.
        /// </summary>
        [EnumAttribute("The largest.")]
        Gigantic


    }
}

