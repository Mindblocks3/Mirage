using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEditor.Compilation;
using UnityEngine;

namespace Mirage.Weaver
{
    public class CompiledAssembly : ICompiledAssembly
    {
        private readonly string assemblyPath;
        private InMemoryAssembly inMemoryAssembly;

        public CompiledAssembly(string assemblyPath)
        {
            this.assemblyPath = assemblyPath;
        }

        public InMemoryAssembly InMemoryAssembly
        {
            get
            {

                if (inMemoryAssembly == null)
                {
                    byte[] peData = File.ReadAllBytes(assemblyPath);

                    string pdbFileName = Path.GetFileNameWithoutExtension(assemblyPath) + ".pdb";

                    byte[] pdbData = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assemblyPath), pdbFileName));

                    inMemoryAssembly = new InMemoryAssembly(peData, pdbData);
                }
                return inMemoryAssembly;
            }
        }

        public string Name => Path.GetFileNameWithoutExtension(assemblyPath);

        public string[] References { get; set; }

        public string[] Defines { get; set; }
    }

    public class Assembler
    {
        public string OutputFile { get; set; }
        public string ProjectPathFile => Path.Combine(WeaverTestLocator.OutputDirectory, OutputFile);
        public List<CompilerMessage> CompilerMessages { get; private set; }
        public bool CompilerErrors { get; private set; }

        readonly HashSet<string> sourceFiles = new HashSet<string>();

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
        public AssemblyDefinition Build(IWeaverLogger logger, IEnumerable<string> sourceFiles)
        {
            AssemblyDefinition assembly = null;

            // This will compile scripts with the same references as files in the asset folder.
            // This means that the dll will get references to all asmdef just as if it was the default "Assembly-CSharp.dll"
            var assemblyBuilder = new AssemblyBuilder(ProjectPathFile, AddSourceFiles(sourceFiles).ToArray())
            {
                referencesOptions = ReferencesOptions.UseEngineModules
            };

            assemblyBuilder.buildFinished += delegate (string assemblyPath, CompilerMessage[] compilerMessages)
            {

                // assembly builder does not call ILPostProcessor (WTF Unity?),  so we must invoke it ourselves.
                var compiledAssembly = new CompiledAssembly(assemblyPath)
                {
                    Defines = assemblyBuilder.defaultDefines,
                    References = assemblyBuilder.defaultReferences
                };

                var weaver = new Weaver(logger);

                assembly = weaver.Weave(compiledAssembly);
            };

            // Start build of assembly
            if (!assemblyBuilder.Build())
            {
                Debug.LogErrorFormat("Failed to start build of assembly {0}", assemblyBuilder.assemblyPath);
                return assembly;
            }

            while (assemblyBuilder.status != AssemblyBuilderStatus.Finished)
            {
                System.Threading.Thread.Sleep(10);
            }

            return assembly;
        }
    }
}
