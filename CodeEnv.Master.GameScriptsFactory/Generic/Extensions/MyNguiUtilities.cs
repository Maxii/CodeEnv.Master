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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

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

