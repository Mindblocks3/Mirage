using UnityEngine;
using UnityEngine.AI;

namespace Mirage.Examples.Tanks
{
    public class Tank : NetworkBehaviour
    {
        [Header("Components")]
        public NavMeshAgent agent;
        public Animator animator;

        [Header("Movement")]
        public float rotationSpeed = 100;

        [Header("Firing")]
        public KeyCode shootKey = KeyCode.Space;
        public GameObject projectilePrefab;
        public Transform projectileMount;

        [SyncVar]
        public int health { get; set; }
        [SyncVar]
        public int score { get; set; }
        [SyncVar]
        public string playerName { get; set; }
        [SyncVar]
        public bool allowMovement { get; set; }
        [SyncVar]
        public bool isReady { get; set; }

        public bool IsDead => health <= 0;

        [Header("Game Stats")]
        public TextMesh nameText;


        void Update()
        {
            if (Camera.main)
            {
                nameText.text = playerName;
                nameText.transform.rotation = Camera.main.transform.rotation;
            }

            // movement for local player
            if (!IsLocalPlayer)
                return;

            //Set local players name color to green
            nameText.color = Color.green;

            if (!allowMovement)
                return;

            if (IsDead)
                return;

            // rotate
            float horizontal = Input.GetAxis("Horizontal");
            transform.Rotate(0, horizontal * rotationSpeed * Time.deltaTime, 0);

            // move
            float vertical = Input.GetAxis("Vertical");
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            agent.velocity = forward * Mathf.Max(vertical, 0) * agent.speed;
            animator.SetBool("Moving", agent.velocity != Vector3.zero);

            // shoot
            if (Input.GetKeyDown(shootKey))
            {
                CmdFire();
            }
        }

        // this is called on the server
        [ServerRpc]
        void CmdFire()
        {
            GameObject projectile = Instantiate(projectilePrefab, projectileMount.position, transform.rotation);
            projectile.GetComponent<Projectile>().source = gameObject;
            ServerObjectManager.Spawn(projectile);
            RpcOnFire();
        }

        // this is called on the tank that fired for all observers
        [ClientRpc]
        void RpcOnFire()
        {
            animator.SetTrigger("Shoot");
        }

        public void SendReadyToServer(string playername)
        {
            if (!IsLocalPlayer)
                return;

            CmdReady(playername);
        }

        [ServerRpc]
        void CmdReady(string playername)
        {
            if (string.IsNullOrEmpty(playername))
            {
                playerName = "PLAYER" + Random.Range(1, 99);
            }
            else
            {
                playerName = playername;
            }

            isReady = true;
        }
    }
}
