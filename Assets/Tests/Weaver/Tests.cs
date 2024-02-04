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
using UnityEditor.Compilation;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Mirage.Weaver
{
    public class AssertionMethodAttribute : Attribute { }

    public abstract class TestsBuildFromTestName : Tests
    {

        public static IEnumerable<ITest> GetDescendants(ITest test)
            => test.Tests.Concat(test.Tests.SelectMany(GetDescendants));

        [OneTimeSetUp]
        public virtual void TestsSetup() {

            // compile the code once
            Debug.Log("TestsSetup");
            TestContext.TestAdapter testAdapter = TestContext.CurrentContext.Test;
            
            Type testAdapterType = testAdapter.GetType();
            FieldInfo testFieldInfo = testAdapterType.GetField("_test", BindingFlags.NonPublic | BindingFlags.Instance);
            var test = testFieldInfo.GetValue(testAdapter) as ITest;
            
            // get the name of all the tests in this class
            var allTests = GetDescendants(test).ToList();

            string className = TestContext.CurrentContext.Test.ClassName.Split('.').Last();

            BuildAndWeaveTestAssembly(className, allTests.Select(t => t.Name).ToArray());

            Debug.Log($"One time build of {className} compiler messages {assembler.CompilerMessages.Count}");
            TestSetup();

        }

        public virtual void TestSetup()
        {
            ICompiledAssembly compiledAssembly = LoadAssembly();

            var weaver = new Weaver(weaverLog);
            assembly = weaver.Weave(compiledAssembly);
            Assert.That(assembler.CompilerErrors, Is.False);
            foreach (DiagnosticMessage error in weaverLog.Diagnostics)
            {
                // ensure all errors have a location
                Assert.That(error.MessageData, Does.Match(@"\(at .*\)$"));
                Assert.That(error.File, Is.Not.Null);
            }
        }

        [AssertionMethod]
        protected void IsSuccess()
        {
            // figure out the class
            string testName = TestContext.CurrentContext.Test.Name;

            Assert.That(weaverLog.Diagnostics.Where(msg => msg.File.Split(new char[] { '/', '.' }).Contains(testName)), Is.Empty);
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

        [OneTimeTearDown]
        public void TestCleanup()
        {
            assembler.DeleteOutput();
        }

    }

    [TestFixture]
    public abstract class Tests
    {
        public static readonly ILogger logger = LogFactory.GetLogger<Tests>(LogType.Exception);

        protected Logger weaverLog = new Logger();

        protected AssemblyDefinition assembly;

        protected Assembler assembler;

        protected AssemblyBuilder assemblyBuilder;

        protected AssemblyDefinition Asemble(string className, params string [] testName)
        {
            weaverLog.Diagnostics.Clear();
            assembler = new Assembler();

            string testSourceDirectory = className + "~";
            string outputFile = Path.Combine(testSourceDirectory, className + ".dll");
            IEnumerable<string> sourceFiles = from test in testName
                              select Path.Combine(testSourceDirectory, test + ".cs");

            return assembler.Assemble(outputFile, sourceFiles);

        }

        protected void BuildAndWeaveTestAssembly(string className, params string [] testName)
        {
            weaverLog.Diagnostics.Clear();
            assembler = new Assembler();

            string testSourceDirectory = className + "~";
            assembler.OutputFile = Path.Combine(testSourceDirectory, testName[0] + ".dll");
            IEnumerable<string> sourceFiles = from test in testName
                              select Path.Combine(testSourceDirectory, test + ".cs");

            assemblyBuilder = assembler.Build(sourceFiles);

        }


        protected ICompiledAssembly LoadAssembly()
        {

            return new CompiledAssembly(assemblyBuilder.assemblyPath)
            {
                Defines = assemblyBuilder.defaultDefines,
                References = assemblyBuilder.defaultReferences
            };
        }
    }
}
