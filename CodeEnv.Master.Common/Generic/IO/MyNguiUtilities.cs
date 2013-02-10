// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyNguiUtilities.cs
// Utilities in support of using NGUI User Interface components.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    public static class MyNguiUtilities {

        /// <summary>
        /// Generates slider step values in ascending order based on the number of steps 
        /// selected for the slider.
        /// </summary>
        /// <param name="numberOfSteps">The number of steps.</param>
        /// <returns>An array of step values in ascending order.</returns>
        public static float[] GenerateOrderedSliderStepValues(int numberOfSteps) {
            float[] orderedSliderStepValues = new float[numberOfSteps];
            for (int i = 0; i < numberOfSteps; i++) {
                orderedSliderStepValues[i] = (float)i / (float)(numberOfSteps - 1);
            }
            return orderedSliderStepValues;
        }
    }
}

