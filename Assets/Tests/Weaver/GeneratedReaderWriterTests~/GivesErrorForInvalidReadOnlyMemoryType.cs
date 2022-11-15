using System;
using Mirage;
using UnityEngine;

namespace GeneratedReaderWriter.GivesErrorForInvalidReadOnlyMemoryType
{
    public class GivesErrorForInvalidReadOnlyMemoryType : NetworkBehaviour
    {
        [ClientRpc]
        public void RpcDoSomething(ReadOnlyMemory<MonoBehaviour> data)
        {
            // empty
        }
    }
}
