// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: XYield.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    public class XYield {

        public class XResourceValuePair {

            public XResource Resource { get; set; }
            public float Value { get; set; }

            public XResourceValuePair(XResource x, float value) {
                Resource = x;
                Value = value;
            }
        }

        private IDictionary<XResource, XResourceValuePair> resources = new Dictionary<XResource, XResourceValuePair>();

        public XYield(XResource xResource, float xValue)
            : this(new XResourceValuePair(xResource, xValue)) { }

        public XYield(params XResourceValuePair[] xResourceArgs) {
            foreach (var xArg in xResourceArgs) {
                resources.Add(xArg.Resource, xArg);
            }
        }

        public float GetYield(XResource opeResource) {
            XResourceValuePair valuePair;
            if (resources.TryGetValue(opeResource, out valuePair)) {
                return valuePair.Value;
            }
            D.Warn("{0} {1} is not present. Value of 0 returned.", typeof(XResource), opeResource);
            return Constants.Zero;
        }

        public IList<XResourceValuePair> GetAllResources() {
            return resources.Values.ToList<XResourceValuePair>();
        }

        public void SetYield(XResource opeResource, float value) {
            XResourceValuePair valuePair;
            if (resources.TryGetValue(opeResource, out valuePair)) {
                valuePair.Value = value;
                return;
            }
            resources.Add(opeResource, new XResourceValuePair(opeResource, value));
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

