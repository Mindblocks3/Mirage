using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Mirage.Tests
{

    public class MockTransport : Transport
    {
        public override IEnumerable<string> Scheme => new[] { "kcp" };

        public override bool Supported => true;

        public override UniTask<IConnection> ConnectAsync(Uri uri)
        {
            return UniTask.FromResult<IConnection>(default);
        }


        public override void Disconnect()
        {
            Stopped.Invoke();
        }

        public override void Listen()
        {
            Started.Invoke();
        }

        public override IEnumerable<Uri> ServerUri()
        {
            return new[] { new Uri("kcp://localhost") };
        }
    }
}
