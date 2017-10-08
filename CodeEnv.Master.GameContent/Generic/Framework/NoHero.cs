// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NoHero.cs
// A Hero with no abilities for use with UnitCmds that have no hero.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

using System;
using CodeEnv.Master.Common;

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// A Hero with no abilities for use with UnitCmds that have no hero.
    /// </summary>
    public class NoHero : Hero {

        public override int Level { get { return Constants.Zero; } }

        public NoHero() : base(new HeroStat("NoHero", AtlasID.MyGui, TempGameValues.EmptyImageFilename, Species.None, HeroCategory.None,
            "NoDescription", 0F, 0F)) { }

        public override void IncrementExperienceBy(float increasedExperience) {
            throw new NotImplementedException();
        }
    }
}

