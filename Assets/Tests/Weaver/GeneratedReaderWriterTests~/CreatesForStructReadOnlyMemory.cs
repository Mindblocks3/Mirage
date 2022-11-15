using System;
using Mirage;
using UnityEngine;

namespace GeneratedReaderWriter.CreatesForStructReadOnlyMemory
{
    public class CreatesForStructReadOnlyMemory : NetworkBehaviour
    {
        [ClientRpc]
        public void RpcDoSomething(ReadOnlyMemory<MyStruct> data)
        {
            // empty
        }
    }

    public struct MyStruct
    {
        public int someValue;
        public Vector3 anotherValue;
    }
}
