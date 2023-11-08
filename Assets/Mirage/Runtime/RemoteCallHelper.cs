using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Mirage.Logging;
using Mirage.Serialization;
using UnityEngine;

namespace Mirage.RemoteCalls
{
    /// <summary>
    /// Delegate for ServerRpc functions.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="reader"></param>
    public delegate void CmdDelegate(NetworkBehaviour obj, NetworkReader reader, INetworkPlayer senderPlayer, int replyId);

    // invoke type for Rpc
    public enum RpcInvokeType
    {
        ServerRpc,
        ClientRpc
    }

    /// <summary>
    /// Stub Skeleton for RPC
    /// </summary>
    class Skeleton
    {
        public Type invokeClass;
        public RpcInvokeType invokeType;
        public CmdDelegate invokeFunction;
        public bool cmdRequireAuthority;

        public bool AreEqual(Type invokeClass, RpcInvokeType invokeType, CmdDelegate invokeFunction)
        {
            return this.invokeClass == invokeClass &&
                    this.invokeType == invokeType &&
                    this.invokeFunction == invokeFunction;
        }

        internal void Invoke(NetworkReader reader, NetworkBehaviour invokingType, INetworkPlayer senderPlayer = null, int replyId = 0)
        {
            if (invokeClass.IsInstanceOfType(invokingType))
            {
                invokeFunction(invokingType, reader, senderPlayer, replyId);
                return;
            }
            throw new MethodInvocationException($"Invalid Rpc call {invokeFunction} for component {invokingType}");
        }
    }

    /// <summary>
    /// Used to help manage remote calls for NetworkBehaviours
    /// </summary>
    public static class RemoteCallHelper
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(RemoteCallHelper));

        /*
            Note: this should not be reinitialized without domain reload.

            The delegates are added as part of class constructors
            so if we add a [InitializeOnLoad] constructor to this class
            it will be called after all the other constructors
            and we will lose all the delegates.

            Note the skeletons don't have references to gameobjects
            thus it is safe to persist them between reload. If
            the classes are reloaded, the skeletons will be overriden
            with new ones.
        */
        static Dictionary<int, Skeleton> cmdHandlerDelegates = new Dictionary<int, Skeleton>();

        /// <summary>
        /// Creates hash from Type and method name
        /// </summary>
        /// <param name="invokeClass"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        internal static int GetMethodHash(Type invokeClass, string methodName)
        {
            // (invokeClass + ":" + cmdName).GetStableHashCode() would cause allocations.
            // so hash1 + hash2 is better.
            unchecked
            {
                int hash = invokeClass.FullName.GetStableHashCode();
                return hash * 503 + methodName.GetStableHashCode();
            }
        }

        /// <summary>
        /// helper function register a ServerRpc/Rpc delegate
        /// </summary>
        /// <param name="invokeClass"></param>
        /// <param name="cmdName"></param>
        /// <param name="invokerType"></param>
        /// <param name="func"></param>
        /// <param name="cmdRequireAuthority"></param>
        /// <returns>remote function hash</returns>
        public static int RegisterDelegate(Type invokeClass, string cmdName, RpcInvokeType invokerType, CmdDelegate func, bool cmdRequireAuthority = true)
        {
            // type+func so Inventory.RpcUse != Equipment.RpcUse
            int cmdHash = GetMethodHash(invokeClass, cmdName);

            if (CheckIfDelegateExists(invokeClass, invokerType, func, cmdHash))
                return cmdHash;

            var invoker = new Skeleton
            {
                invokeType = invokerType,
                invokeClass = invokeClass,
                invokeFunction = func,
                cmdRequireAuthority = cmdRequireAuthority,
            };

            cmdHandlerDelegates[cmdHash] = invoker;

            if (logger.LogEnabled())
            {
                string requireAuthorityMessage = invokerType == RpcInvokeType.ServerRpc ? $" RequireAuthority:{cmdRequireAuthority}" : "";
                logger.Log($"RegisterDelegate hash: {cmdHash} invokerType: {invokerType} method: {func.Method.Name}{requireAuthorityMessage}");
            }

            return cmdHash;
        }

        static bool CheckIfDelegateExists(Type invokeClass, RpcInvokeType invokerType, CmdDelegate func, int cmdHash)
        {
            if (cmdHandlerDelegates.ContainsKey(cmdHash))
            {
                // something already registered this hash
                Skeleton oldInvoker = cmdHandlerDelegates[cmdHash];
                if (oldInvoker.AreEqual(invokeClass, invokerType, func))
                {
                    // it's all right,  it was the same function
                    return true;
                }

                logger.LogError($"Function {oldInvoker.invokeClass}.{oldInvoker.invokeFunction.Method.Name} and {invokeClass}.{func.Method.Name} have the same hash.  Please rename one of them");
            }

            return false;
        }

        public static void RegisterServerRpcDelegate(Type invokeClass, string cmdName, CmdDelegate func, bool requireAuthority)
        {
            RegisterDelegate(invokeClass, cmdName, RpcInvokeType.ServerRpc, func, requireAuthority);
        }

        public static void RegisterRpcDelegate(Type invokeClass, string rpcName, CmdDelegate func)
        {
            RegisterDelegate(invokeClass, rpcName, RpcInvokeType.ClientRpc, func);
        }

        /// <summary>
        /// We need this in order to clean up tests
        /// </summary>
        internal static void RemoveDelegate(int hash)
        {
            cmdHandlerDelegates.Remove(hash);
        }

        internal static Skeleton GetSkeleton(int cmdHash)
        {

            if (cmdHandlerDelegates.TryGetValue(cmdHash, out Skeleton invoker))
            {
                return invoker;
            }

            throw new MethodInvocationException($"No RPC method found for hash {cmdHash}");
        }

        /// <summary>
        /// Gets the handler function for a given hash
        /// Can be used by profilers and debuggers
        /// </summary>
        /// <param name="cmdHash">rpc function hash</param>
        /// <returns>The function delegate that will handle the ServerRpc</returns>
        public static CmdDelegate GetDelegate(int cmdHash)
        {
            if (cmdHandlerDelegates.TryGetValue(cmdHash, out Skeleton invoker))
            {
                return invoker.invokeFunction;
            }
            return null;
        }
    }
}

