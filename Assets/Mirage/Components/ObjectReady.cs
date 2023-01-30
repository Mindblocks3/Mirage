namespace Mirage
{
    public class ObjectReady : NetworkBehaviour
    {
        [SyncVar]
        public bool IsReady { get; set; }

        [Server]
        public void SetClientReady()
        {
            IsReady = true;
        }

        [Server]
        public void SetClientNotReady()
        {
            IsReady = false;
        }

        [Client]
        public void Ready()
        {
            ReadyRpc();
        }

        [ServerRpc]
        void ReadyRpc()
        {
            IsReady = true;
        }
    }
}
