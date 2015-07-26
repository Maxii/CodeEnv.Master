// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameColor.cs
// My GameColor enum. Use gameColorConstant.Value() to get Unity's Color.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// My GameColor enum. Use gameColorValue.ToUnityColor() to get Unity's Color.
    /// </summary>
    public enum GameColor {

        None,

        Clear,
        Black,
        Gray,
        White,

        Blue,
        Cyan,
        Green,
        Magenta,
        Red,
        Yellow,
        Brown,
        Purple,
        DarkGreen,
        Teal
    }
}

