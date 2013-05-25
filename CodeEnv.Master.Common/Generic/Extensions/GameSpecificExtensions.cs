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

        public static float SpeedMultiplier(this GameClockSpeed gameSpeed) {
            GameClockSpeed_Values values = GameClockSpeed_Values.Instance;
            switch (gameSpeed) {
                case GameClockSpeed.Slowest:
                    return values.SlowestMultiplier;
                case GameClockSpeed.Slow:
                    return values.SlowMultiplier;
                case GameClockSpeed.Normal:
                    return values.NormalMultiplier;
                case GameClockSpeed.Fast:
                    return values.FastMultiplier;
                case GameClockSpeed.Fastest:
                    return values.FastestMultiplier;
                case GameClockSpeed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(gameSpeed));
            }
        }

        public static float Radius(this UniverseSize universeSize) {
            UniverseSize_Values values = UniverseSize_Values.Instance;
            switch (universeSize) {
                case UniverseSize.Tiny:
                    return values.TinyRadius;
                case UniverseSize.Small:
                    return values.SmallRadius;
                case UniverseSize.Normal:
                    return values.NormalRadius;
                case UniverseSize.Large:
                    return values.LargeRadius;
                case UniverseSize.Enormous:
                    return values.EnormousRadius;
                case UniverseSize.Gigantic:
                    return values.GiganticRadius;
                case UniverseSize.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
            }
        }

    }
}

