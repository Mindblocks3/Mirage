using System;
using Mirage;

namespace GeneratedReaderWriter.CreatesForReadOnlyMemory
{
    public class CreatesForReadOnlyMemory : NetworkBehaviour
    {
        [ClientRpc]
        public void RpcDoSomething(ReadOnlyMemory<int> data)
        {
            // empty
        }
    }
}
