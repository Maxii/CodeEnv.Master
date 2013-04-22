// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameSpecificExtensions.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using CodeEnv.Master.Common.LocalResources;

    public static class GameSpecificExtensions {

        public static float GetSpeedMultiplier(this GameClockSpeed gameSpeed) {
            switch (gameSpeed) {
                case GameClockSpeed.Slowest:
                    return 0.25F;
                case GameClockSpeed.Slow:
                    return 0.50F;
                case GameClockSpeed.Normal:
                    return 1.0F;
                case GameClockSpeed.Fast:
                    return 2.0F;
                case GameClockSpeed.Fastest:
                    return 4.0F;
                case GameClockSpeed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(gameSpeed));
            }
        }

        public static float GetUniverseRadius(this UniverseSize universeSize) {
            switch (universeSize) {
                case UniverseSize.Tiny:
                    return 250F;
                case UniverseSize.Small:
                    return 500F;
                case UniverseSize.Normal:
                    return 1000F;
                case UniverseSize.Large:
                    return 2000F;
                case UniverseSize.Enormous:
                    return 5000F;
                case UniverseSize.Gigantic:
                    return 10000F;
                case UniverseSize.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
            }
        }
    }
}

