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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using Vectrosity;

    /// <summary>
    /// Generates a line that shows the course provided. 
    /// The course is composed of INavigableTarget waypoints whose reported
    /// position can change depending on their implementation.
    /// DONOT use Points.
    /// </summary>
    public class CoursePlotLine : A3DVectrosityBase {

        private List<INavigableTarget> _course;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoursePlotLine" /> class
        /// with a line width of 1 and Color.Gray.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="course">The course.</param>
        public CoursePlotLine(string name, IList<INavigableTarget> course)
            : this(name, course, 1F) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoursePlotLine" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="course">The course.</param>
        /// <param name="lineWidth">Width of the line.</param>
        /// <param name="color">The color of the line. Default is Gray.</param>
        public CoursePlotLine(string name, IList<INavigableTarget> course, float lineWidth, GameColor color = GameColor.Gray)
            : base(name, course.Select(wayPt => wayPt.Position).ToList(), null, LineType.Continuous, lineWidth, color) {
            // Copied. Otherwise changes to the original immediately show up in _course without using UpdateCourse()
            _course = new List<INavigableTarget>(course);
        }

        /// <summary>
        /// Updates the course. 
        /// Use this when you have added, replaced or removed one or more waypoints in your course. DONOT use Points.
        /// </summary>
        /// <param name="course">The course.</param>
        public void UpdateCourse(IList<INavigableTarget> course) {
            // Copied. Otherwise changes to the original immediately show up in _course without using UpdateCourse()
            _course = new List<INavigableTarget>(course);
            Points = course.Select(wayPt => wayPt.Position).ToList();   // updating Points will update _line.points3 list
        }

        protected override void Draw3D() {  // OPTIMIZE not clear why _course or this override is needed
            for (int i = 0; i < _course.Count; i++) {
                _line.points3[i] = _course[i].Position;
            }
            base.Draw3D();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

