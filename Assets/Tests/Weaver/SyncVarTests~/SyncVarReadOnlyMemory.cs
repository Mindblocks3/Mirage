using Mirage;
using System;

namespace SyncVarTests.SyncVarReadOnlyMemory
{
    class SyncVarReadOnlyMemory : NetworkBehaviour
    {
       [SyncVar]
       public ReadOnlyMemory<int> data { get; set; }
    }
}
