// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ObjectAnalyzerClassTests.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using CodeEnv.Master.Resources;
    using System.Globalization;

    /// <summary>
    /// Unit Tests for the ObjectAnalyzer Class.
    /// </summary>
    [TestClass]
    public class ObjectAnalyzerClassTests {
        // Naming convention = [NameOfClassUT]ClassTests

        // Bug workaround used to call the [ClassInitialize] annotated method
        private bool isClassInitCalled = false;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        private static ObjectAnalyzer objAnalyzer;

        [ClassInitialize]
        public static void InitializeBeforeFirstTest(TestContext testContext) {
            objAnalyzer = new ObjectAnalyzer();
        }


        private ToStringMethod.ToStringTestClass testTarget;

        [TestInitialize]
        public void InitializeBeforeEachTest() {
            if (!isClassInitCalled) { InitializeBeforeFirstTest(null); }

            testTarget = new ToStringMethod.ToStringTestClass();
        }

        #region Additional test attributes
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize]
        // public static void MyClassInitialize(TestContext TestContext) { }
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

        // Other testing attributes can be found at: http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.testtools.unittesting.aspx

        // 
        //  If the ClassUnderTest isn't a static class, create instances once as below, then inherit the ParentTestClass from the NestedTestClasses to use the instances.
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
        public class ToStringMethod : ObjectAnalyzerClassTests {
            // Nested Class naming convention = [nameOfMethodUnderTest]Method

            // Setup conditions prior to each ToString() test 
            private static string TestString = "This is a test!";
            private static int TestNumber = 5;
            private static int testObjectQty = new Random().Next(3);

            /// <summary>
            /// Class that implements ToString() for testing
            /// </summary>
            internal class ToStringTestClass {
                private string fieldString = TestString;
                // Class to test how ToString() handles non-primitive objects
                private class TestObject { int number = TestNumber; }
                // Array to test how ToString() handles arrays of objects
                private TestObject[] testArray = new TestObject[testObjectQty];

                internal ToStringTestClass() {
                    for (int i = 0; i < testObjectQty; i++) {
                        testArray[i] = new TestObject();
                    }
                }

                public override string ToString() {
                    return objAnalyzer.ToString(this);
                }
            }


            [TestMethod]
            public void Null() {
                // Naming convention = [conditionTested]
                // Tests a specific condition of the above method

                string correctMsg = GeneralMessages.NullObject;
                string testMsg = objAnalyzer.ToString(null);

                Assert.AreEqual(testMsg, correctMsg);

                // use TestContext.WriteLine(string); to write to the test output stream
                // see Pro Visual Studio 2010 Chapter 11 (iPad) for Database-based Testing
            }

            [TestMethod]
            public void String() {
                StringAssert.Contains(testTarget.ToString(), TestString);
            }

            [TestMethod]
            public void Array() {
                IList<string> toStringList = Utility.ConstructListFromString(testTarget.ToString(), Constants.CommaDelimiter);
                int testStringCount = 0, testNumberCount = 0;
                foreach (string s in toStringList) {
                    if (s.Contains(TestString)) {
                        testStringCount++;
                    }
                    else if (s.Contains(TestNumber.ToString(CultureInfo.InvariantCulture))) {
                        testNumberCount++;
                    }
                }
                Assert.IsTrue(testStringCount == 1 && testNumberCount == testObjectQty);
            }
        }
    }
}
