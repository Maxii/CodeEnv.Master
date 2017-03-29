﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GridWireframe.cs
//  Generates an entire Grid of Sectors as a Wireframe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Generates an entire Grid of Sectors as a Wireframe.
    /// </summary>
    public class GridWireframe : A3DVectrosityBase {

        /// <summary>
        /// Initializes a new instance of the <see cref="GridWireframe"/> class
        /// using the DynamicObjectsFolder as the parent.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="gridPoints">All the points in the grid.</param>
        /// <param name="width">The width.</param>
        /// <param name="color">The color.</param>
        public GridWireframe(string name, List<Vector3> gridPoints, float width = 1F, GameColor color = GameColor.Gray)
            : this(name, gridPoints, GameReferences.DynamicObjectsFolder.Folder, width, color) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridWireframe"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="gridPoints">All the points in the grid.</param>
        /// <param name="lineParent">The line parent.</param>
        /// <param name="width">The width.</param>
        /// <param name="color">The color.</param>
        public GridWireframe(string name, List<Vector3> gridPoints, Transform lineParent, float width = 1F, GameColor color = GameColor.Gray)
            : base(name, gridPoints, null, lineParent, LineType.Discrete, width, color) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

