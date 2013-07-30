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
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;


    public class XYield {

        public class XResourceValuePair {

            public XResource Resource { get; private set; }
            public float Value { get; private set; }

            public XResourceValuePair(XResource xResource, float value) {
                Resource = xResource;
                Value = value;
            }
        }

        private IDictionary<XResource, XResourceValuePair> resources = new Dictionary<XResource, XResourceValuePair>();

        public XYield() { }

        public XYield(XResource xResource, float value)
            : this(new XResourceValuePair(xResource, value)) { }

        public XYield(params XResourceValuePair[] xResourcePairs) {
            AddResource(xResourcePairs);
        }

        public float GetYield(XResource xResource) {
            XResourceValuePair valuePair;
            if (resources.TryGetValue(xResource, out valuePair)) {
                return valuePair.Value;
            }
            D.Warn("{0} {1} is not present. Value of 0 returned.", typeof(XResource), xResource);
            return Constants.ZeroF;
        }

        public IList<XResourceValuePair> GetAllResources() {
            return resources.Values.ToList<XResourceValuePair>();
        }

        public void AddResource(params XResourceValuePair[] xResourcePairs) {
            foreach (var xPair in xResourcePairs) {
                if (xPair.Value != Constants.ZeroF) {
                    resources.Add(xPair.Resource, xPair);
                }
                else {
                    D.Warn("Attempting to add {0} {1} with a value of 0. Resource has not been added.", typeof(XResource), xPair.Resource);
                }
            }
        }

        public void ChangeYieldValue(XResource xResource, float value) {
            if (!resources.Remove(xResource)) {
                D.Warn("{0} {1} should be present but is not. New Yield value was added.", typeof(XResource), xResource);
            }
            resources.Add(xResource, new XResourceValuePair(xResource, value));
            // TODO raise XResourceValueChanged event
        }

        public void RemoveResource(params XResource[] xResources) {
            foreach (var x in xResources) {
                if (!resources.Remove(x)) {
                    D.Warn("Attempting to remove {0} {1} that is not present.", typeof(XResource), x);
                }
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

