// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ArgumentsClassTests.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using CodeEnv.Master.Common.LocalResources;

    using System.Diagnostics;

    /// <summary>
    /// Summary description for ArgumentsClassTests
    /// </summary>
    [TestClass]
    public class ArgumentsClassTests {
        // Naming convention = [NameOfClassUT]ClassTests

        public ArgumentsClassTests() {
            // Constructor
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
        public class ValidateNotNullMethod {
            // Nested Class naming convention = [nameOfMethodUnderTest]Method

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void NullException() {
                D.AssertNotNull(null);
            }

            [TestMethod]
            public void NotNull() {
                try {
                    D.AssertNotNull(string.Empty);
                }
                catch (ArgumentNullException e) {
                    Assert.Fail(ErrorMessages.NoExceptionExpected.Inject(e.Message));
                }
            }

        }

        [TestClass]
        public class ValidateForContentMethod {
            // Nested Class naming convention = [nameOfMethodUnderTest]Method

            [TestMethod]
            public void Condition1() {
                // Naming convention = [conditionTested]
                // Tests a specific condition of the above method
            }

        }
    }
}
