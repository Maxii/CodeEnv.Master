// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FtlEngine.cs
// A FasterThanLight engine that produces power to generate thrust for a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// A FasterThanLight engine that produces power to generate thrust for a ship.
    /// </summary>
    public class FtlEngine : Engine {

        private bool _isDampedByField;
        /// <summary>
        /// Indicates whether the FTL engine is damped by an FTL Damping Field. 
        /// </summary>
        public bool IsDampedByField {
            get { return _isDampedByField; }
            set { SetProperty<bool>(ref _isDampedByField, value, "IsDampedByField", IsDampedByFieldPropChangedHandler); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Engine"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public FtlEngine(EngineStat stat, string name = null) : base(stat, name) { }

        protected override void AssessIsOperational() {
            IsOperational = IsActivated && !IsDamaged && !IsDampedByField;
        }

        #region Event and Property Change Handlers

        private void IsDampedByFieldPropChangedHandler() {
            AssessIsOperational();
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

