using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEditor.Compilation;
using UnityEngine;
using System;

namespace Mirage.Weaver
{
    public class Assembler
    {
        public string OutputFile { get; set; }
        public string ProjectPathFile => Path.Combine(WeaverTestLocator.OutputDirectory, OutputFile);
        public List<CompilerMessage> CompilerMessages { get; private set; }
        public bool CompilerErrors { get; private set; }

        public Assembler()
        {
            CompilerMessages = new List<CompilerMessage>();
        }

        // Add a range of source files to compile
        private IEnumerable<string> AddSourceFiles(IEnumerable<string> sourceFiles)
        {
            return from sourceFile in sourceFiles
                select Path.Combine(WeaverTestLocator.OutputDirectory, sourceFile);
        }

        // Delete output dll / pdb / mdb
        public void DeleteOutput()
        {
            // "x.dll" shortest possible dll name
            if (OutputFile.Length < 5)
            {
                return;
            }

            try
            {
                File.Delete(ProjectPathFile);
            }
            catch { /* Do Nothing */ }

            try
            {
                File.Delete(Path.ChangeExtension(ProjectPathFile, ".pdb"));
            }
            catch { /* Do Nothing */ }

            try
            {
                File.Delete(Path.ChangeExtension(ProjectPathFile, ".dll.mdb"));
            }
            catch { /* Do Nothing */ }
        }

        /// <summary>
        /// Calls the compiler to build the provided scripts
        /// </summary>
        /// <param name="outputFile">The path of the output assembly</param>
        /// <param name="sourceFiles">The source files to be compiled</param>
        /// <returns>The assembly definition that was generated</returns>
        /// <exception cref="AssemblyDefinitionException"></exception>
        public AssemblyDefinition Assemble(string outputFile, IEnumerable<string> sourceFiles)
        {
            var assemblyPath = Path.Combine(WeaverTestLocator.OutputDirectory, outputFile);

            // This will compile scripts with the same references as files in the asset folder.
            // This means that the dll will get references to all asmdef just as if it was the default "Assembly-CSharp.dll"
            var assemblyBuilder = new AssemblyBuilder(assemblyPath, AddSourceFiles(sourceFiles).ToArray())
            {
                referencesOptions = ReferencesOptions.UseEngineModules
            };

            if (!assemblyBuilder.Build())
            {
                throw new AssemblyDefinitionException("Failed to start build of assembly {0}", assemblyBuilder.assemblyPath);
            }

            while (assemblyBuilder.status != AssemblyBuilderStatus.Finished)
            {
                System.Threading.Thread.Sleep(10);
            }

            var path = assemblyBuilder.assemblyPath;

            var compiledAssembly = new CompiledAssembly(assemblyPath)
                {
                    Defines = assemblyBuilder.defaultDefines,
                    References = assemblyBuilder.defaultReferences
                };

            return Weaver.AssemblyDefinitionFor(compiledAssembly);
        }

        /// <summary>
        /// Builds and Weaves an Assembly with references to unity engine and other asmdefs.
        /// <para>
        ///     NOTE: Does not write the weaved assemble to disk
        /// </para>
        /// </summary>
        public ICompiledAssembly Build(IEnumerable<string> sourceFiles)
        {
            AssemblyDefinition assembly = null;

            // This will compile scripts with the same references as files in the asset folder.
            // This means that the dll will get references to all asmdef just as if it was the default "Assembly-CSharp.dll"
            var assemblyBuilder = new AssemblyBuilder(ProjectPathFile, AddSourceFiles(sourceFiles).ToArray())
            {
                referencesOptions = ReferencesOptions.UseEngineModules
            };

            // Start build of assembly
            if (!assemblyBuilder.Build())
            {
                Debug.LogErrorFormat($"Failed to start build of assembly {assemblyBuilder.assemblyPath}");
                throw new Exception($"Failed to start build of assembly {assemblyBuilder.assemblyPath}");
            }

            // wait for assemblyBuilder to finish
            while (assemblyBuilder.status != AssemblyBuilderStatus.Finished)
            {
                System.Threading.Thread.Sleep(10);
            }

            return new CompiledAssembly(assemblyBuilder.assemblyPath)
                {
                    Defines = assemblyBuilder.defaultDefines,
                    References = assemblyBuilder.defaultReferences
                };
        }
    }
}
