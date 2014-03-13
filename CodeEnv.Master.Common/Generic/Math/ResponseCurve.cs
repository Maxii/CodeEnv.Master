// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResponseCurve.cs
//  Simple linear interpolationresponse system that generates an output from an input along a predefined curve.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Simple linear interpolationresponse system that generates an output from an input along a predefined curve.
    /// </summary>
    public class ResponseCurve {

        /// <summary>
        /// The minimum value allowed as an input.  All inputs less than this value produce
        /// the output value associated with this minimum input value.  ie. inputs are clamped to this
        /// minimum value.
        /// </summary>
        public int MinimumInput { get; private set; }

        /// <summary>
        /// The maximum value allowed as an input.  All inputs greater than this value produce
        /// the output value associated with this maximum input value.  ie. inputs are clamped to this
        /// maximum value.
        /// </summary>
        public int MaximumInput { get; private set; }

        public int BucketSize { get; private set; }

        public IList<float> BucketEdges { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseCurve"/> class.
        /// </summary>
        /// <param name="minInput">The minimum input.</param>
        /// <param name="bucketSize">Size of the bucket.</param>
        /// <param name="bucketEdges">The bucket edges.</param>
        public ResponseCurve(int minInput, int bucketSize, IList<float> bucketEdges) {
            MinimumInput = minInput;
            BucketSize = bucketSize;
            BucketEdges = bucketEdges;
            MaximumInput = minInput + (bucketSize * (BucketEdges.Count - 1));
        }

        public float GetResponse(int input) {
            float response = Constants.ZeroF;
            int bucketIndex = Mathf.FloorToInt((input - MinimumInput) / BucketSize);
            if (bucketIndex < 0) {
                return response = BucketEdges[0];
            }
            int lastBucketEdgeIndex = BucketEdges.Count - 1;
            int lastBucketIndex = lastBucketEdgeIndex - 1;
            if (bucketIndex > lastBucketIndex) {
                return response = BucketEdges[lastBucketEdgeIndex];
            }

            // the point inside the bucket from which to interpolate the response, range = 0 - 1
            float bInterpolate = (float)((input - MinimumInput) - (bucketIndex * BucketSize)) / BucketSize;
            response = (BucketEdges[bucketIndex] * (1 - bInterpolate)) + (BucketEdges[bucketIndex + 1] * bInterpolate);
            D.Log("{0}: Input = {1}, Output Response = {2}.", typeof(ResponseCurve).Name, input, response);
            return response;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

