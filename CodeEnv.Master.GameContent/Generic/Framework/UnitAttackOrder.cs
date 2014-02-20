// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitAttackOrder.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public class UnitAttackOrder<T> : UnitOrder<T> where T : struct {

        public new ITarget Target {
            get { return base.Target as ITarget; }
            set { base.Target = value; }
        }

        public UnitAttackOrder(T order, ITarget target = null, float speed = Constants.ZeroF)
            : base(order, target, speed) {
        }

        //public UnitAttackOrder(T order, ITarget target, Speed speed)
        //    : base(order, target, speed) {
        //}


        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

