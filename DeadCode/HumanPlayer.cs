// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HumanPlayer.cs
// The human Player with the optionally ability to set a custom Leadername and Color.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// The human Player with the optionally ability to set a 
    /// custom Leadername and Color.
    /// </summary>
    [System.Obsolete]
    public class HumanPlayer : Player {

        public HumanPlayer(Race race, GameColor color = GameColor.None, string leaderName = Constants.Empty)
            : base(race, IQ.Normal) {
            _leaderName = leaderName;
            _color = color;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IPlayer Members

        //public override bool IsPlayer { get { return true; } }

        private string _leaderName;
        //public override string LeaderName {
        //    get { return !_leaderName.Equals(string.Empty) ? _leaderName : base.LeaderName; }
        //}

        private GameColor _color;
        //public override GameColor Color {
        //    get { return _color != GameColor.None ? _color : base.Color; }
        //}

        #endregion

    }
}

