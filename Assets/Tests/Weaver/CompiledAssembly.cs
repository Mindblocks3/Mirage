using System.IO;
using Unity.CompilationPipeline.Common.ILPostProcessing;

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
}
