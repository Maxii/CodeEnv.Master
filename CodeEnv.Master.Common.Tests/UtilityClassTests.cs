// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UtilityClassTests.cs
// TODO - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;


    /// <summary>
    /// Summary description for UtilityClassTests
    /// </summary>
    [TestClass]
    public class UtilityClassTests {

        public UtilityClassTests() {

        }

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup]
        // public void MyTestCleanup() { }
        //


        // 
        //  If the ClassUnderTest isn'fieldType a static class, create instances once as below, then inherit the ParentTestClass from the NestedTestClasses to use the instances.
        //
        //  protected ClassUnderTest target;
        //
        //  [ClassInitialize]
        //  Initialize() {
        //      target = new ClassUnderTest();
        //  }
        //
        // OR
        //
        //  [TestInitialize]
        //  Initialize() {
        //      target = new ClassUnderTest();
        //  }
        //
        #endregion


        [TestClass]
        public class IsInRangeMethod {

            private int low = 3;
            private int value = 0;
            private int high = 12;

            [TestMethod]
            public void ValueAboveRange() {
                value = 20;
                Assert.IsFalse(Utility.IsInRange(value, low, high));
            }

            [TestMethod]
            public void ValueBelowRange() {
                value = 2;
                Assert.IsFalse(Utility.IsInRange(value, low, high));
            }

            [TestMethod]
            public void ValueInRange() {
                int[] values = { low, 5, high };
                foreach (int v in values) {
                    Assert.IsTrue(Utility.IsInRange(v, low, high));
                }
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void LowGreaterThanHighException() {
                low = high + 5;
                Utility.IsInRange(value, low, high);
            }

            [TestMethod]
            public void NegativeValueInRange() {
                low = -7;
                value = -2;
                high = -1;
                Assert.IsTrue(Utility.IsInRange(value, low, high));
            }
        }

        [TestClass]
        public class ParseBooleanMethod {

            string trueLowerCaseText = "true";
            string falseLowerCaseText = "false";

            [TestMethod]
            public void ParseTrueEitherCase() {
                Assert.IsTrue(Utility.ParseBoolean(trueLowerCaseText) && Utility.ParseBoolean(trueLowerCaseText.ToUpper()));
            }

            [TestMethod]
            public void ParseFalseEitherCase() {
                Assert.IsFalse(Utility.ParseBoolean(falseLowerCaseText) || Utility.ParseBoolean(falseLowerCaseText.ToUpper()));
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void ParseNeitherException() {
                string notBooleanText = "thisShouldNotParse";
                Utility.ParseBoolean(notBooleanText);
            }
        }

        // UNDONE More methods to test

    }
}
