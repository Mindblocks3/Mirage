using System;
using System.IO;
using System.Linq;
using Mirage.Logging;
using Mono.Cecil;
using NUnit;
using NUnit.Framework;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityEngine;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using System.Reflection;

namespace Mirage.Weaver
{
    public class AssertionMethodAttribute : Attribute { }

    public abstract class TestsBuildFromTestName : Tests
    {

        public static IEnumerable<ITest> GetDescendants(ITest test)
            => test.Tests.Concat(test.Tests.SelectMany(GetDescendants));

        [OneTimeSetUp]
        public virtual void TestsSetup() {

            Debug.Log("TestsSetup");
            var testAdapter = TestContext.CurrentContext.Test;
            
            Type testAdapterType = testAdapter.GetType();
            FieldInfo testFieldInfo = testAdapterType.GetField("_test", BindingFlags.NonPublic | BindingFlags.Instance);
            var test = testFieldInfo.GetValue(testAdapter) as ITest;
            
            // get the name of all the tests in this class
            var allTests = GetDescendants(test).ToList();

            string className = TestContext.CurrentContext.Test.ClassName.Split('.').Last();

            BuildAndWeaveTestAssembly(className, allTests.Select(t => t.Name).ToArray());

            Debug.Log(assembler.CompilerMessages.Count);

        }

        [SetUp]
        public virtual void TestSetup()
        {
            string className = TestContext.CurrentContext.Test.ClassName.Split('.').Last();

            BuildAndWeaveTestAssembly(className, TestContext.CurrentContext.Test.Name);
        }

        [AssertionMethod]
        protected void IsSuccess()
        {
            Assert.That(weaverLog.Diagnostics, Is.Empty);
        }

        [AssertionMethod]
        protected void HasError(string messsage, string atType)
        {
            Assert.That(weaverLog.Diagnostics
                .Where(d => d.DiagnosticType == DiagnosticType.Error)
                .Select(d => d.MessageData), Contains.Item($"{messsage} (at {atType})"));
        }

        [AssertionMethod]
        protected void HasWarning(string messsage, string atType)
        {
            Assert.That(weaverLog.Diagnostics
                .Where(d => d.DiagnosticType == DiagnosticType.Warning)
                .Select(d => d.MessageData), Contains.Item($"{messsage} (at {atType})"));
        }
    }

    [TestFixture]
    public abstract class Tests
    {
        public static readonly ILogger logger = LogFactory.GetLogger<Tests>(LogType.Exception);

        protected Logger weaverLog = new Logger();

        protected AssemblyDefinition assembly;

        protected Assembler assembler;

        protected AssemblyDefinition Asemble(string className, params string [] testName)
        {
            weaverLog.Diagnostics.Clear();
            assembler = new Assembler();

            string testSourceDirectory = className + "~";
            var outputFile = Path.Combine(testSourceDirectory, className + ".dll");
            var sourceFiles = from test in testName
                              select Path.Combine(testSourceDirectory, test + ".cs");

            return assembler.Assemble(outputFile, sourceFiles);

        }

        protected void BuildAndWeaveTestAssembly(string className, params string [] testName)
        {
            weaverLog.Diagnostics.Clear();
            assembler = new Assembler();

            string testSourceDirectory = className + "~";
            assembler.OutputFile = Path.Combine(testSourceDirectory, testName[0] + ".dll");
            var sourceFiles = from test in testName
                              select Path.Combine(testSourceDirectory, test + ".cs");

            assembly = assembler.Build(weaverLog, sourceFiles);

            Assert.That(assembler.CompilerErrors, Is.False);
            foreach (DiagnosticMessage error in weaverLog.Diagnostics)
            {
                // ensure all errors have a location
                Assert.That(error.MessageData, Does.Match(@"\(at .*\)$"));
            }
        }

        [TearDown]
        public void TestCleanup()
        {
            assembler.DeleteOutput();
        }
    }
}
