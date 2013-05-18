// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TempGameValues.cs
// Static class of common constants specific to the Unity Engine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using Microsoft.Win32;
    using UnityEngine;

    public static class TempGameValues {

        public static readonly Vector3 UniverseOrigin = Vector3.zero;

        public const string DynamicObjectsFolderName = "DynamicObjects";

        public const int StartingGameYear = 2700;
        public const int DaysPerGameYear = 100;
        public const float GameDaysPerSecond = 1.0F;



    }
}


