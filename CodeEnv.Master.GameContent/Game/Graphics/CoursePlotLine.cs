// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CoursePlotLine.cs
// Generates a line that shows the course provided. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Generates a line that shows the course provided. 
    /// The course is composed of INavigableTarget waypoints whose reported
    /// position can change depending on their implementation.
    /// </summary>
    public class CoursePlotLine : A3DVectrosityBase {

        /// <summary>
        /// Initializes a new instance of the <see cref="CoursePlotLine" /> class
        /// with lineParent as the DynamicObjectsFolder, a line width of 1 and Color.Gray.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="course">The course.</param>
        public CoursePlotLine(string name, IList<INavigable> course)
            : this(name, course, References.DynamicObjectsFolder.Folder, 1F) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoursePlotLine" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="course">The course.</param>
        /// <param name="lineParent">The line parent.</param>
        /// <param name="lineWidth">Width of the line.</param>
        /// <param name="color">The color of the line. Default is Gray.</param>
        public CoursePlotLine(string name, IList<INavigable> course, Transform lineParent, float lineWidth, GameColor color = GameColor.Gray)
            : base(name, course.Select(wayPt => wayPt.Position).ToList(), null, lineParent, LineType.Continuous, lineWidth, color) {
        }

        /// <summary>
        /// Updates the course. 
        /// Use this when you have added, replaced or removed one or more waypoints in your course.
        /// </summary>
        /// <param name="course">The course.</param>
        public void UpdateCourse(IList<INavigable> course) {
            Utility.ValidateNotNull(course);
            List<Vector3> waypointLocations = new List<Vector3>(course.Count);
            for (int i = 0; i < course.Count; i++) {
                INavigable waypoint = course[i];
                D.AssertNotNull(waypoint, course.Select(wpt => wpt.FullName).Concatenate());
                waypointLocations.Add(waypoint.Position);
            }
            Points = waypointLocations;
            //Points = course.Select(wayPt => wayPt.Position).ToList();   // updating Points will update _line.points3 list
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

