// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectionEvent.cs
// Event indicating a new Selection has occured.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Event indicating a new Selection has occured.
    /// </summary>
    public class SelectionEvent : AGameEvent {

        public new ISelectable Source {
            get { return base.Source as ISelectable; }
        }

        public GameObject GameObject { get; private set; }

        public SelectionEvent(ISelectable source, GameObject gameObject)
            : base(source) {
            GameObject = gameObject;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

