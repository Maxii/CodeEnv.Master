// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VectorExtensions.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    public static class VectorExtensions {

        public static void ValidateNormalized(this Vector3 v) {
            if (!Mathfx.Approx(v, v.normalized, 0.01F)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.NotNormalized.Inject(v, callingMethodName));
            }
        }

        public static Color Value(this GameColor color) {
            switch (color) {
                case GameColor.Black:
                    return Color.black;
                case GameColor.Blue:
                    return Color.blue;
                case GameColor.Cyan:
                    return Color.cyan;
                case GameColor.Green:
                    return Color.green;
                case GameColor.Gray:
                    return Color.gray;
                case GameColor.Magenta:
                    return Color.magenta;
                case GameColor.Red:
                    return Color.red;
                case GameColor.White:
                    return Color.white;
                case GameColor.Yellow:
                    return Color.yellow;
                case GameColor.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(color));
            }
        }
    }
}

