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

    using UnityEngine;

    public static class TempGameValues {

        public static readonly Vector3 UniverseOrigin = Vector3.zero;

        public const int StartingGameYear = 2700;
        public const int DaysPerGameYear = 100;
        public const float GameDaysPerSecond = 1.0F;

        public const int ShipLabelDisplayThreshold = 300;
        public const int SystemLabelDisplayThreshold = 2500;


        public const int ShipAnimateDisplayThreshold = 100;
        public const int SystemAnimateDisplayThreshold = 1000;

        public const float InjuredHealthThreshold = 0.80F;
        public const float CriticalHealthThreshold = 0.50F;

    }
}


