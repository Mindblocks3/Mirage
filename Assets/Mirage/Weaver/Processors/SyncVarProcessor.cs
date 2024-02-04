using System;
using System.Collections.Generic;
using System.Linq;
using Mirage.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;

namespace Mirage.Weaver
{
    /// <summary>
    /// Processes [SyncVar] in NetworkBehaviour
    /// </summary>
    public class SyncVarProcessor : RpcProcessor
    {
        private readonly List<PropertyDefinition> syncVars = new List<PropertyDefinition>();

        // store the unwrapped types for every field
        private readonly Dictionary<PropertyDefinition, FieldDefinition> wrappedBackingFields = new Dictionary<PropertyDefinition, FieldDefinition>();

        public SyncVarProcessor(ModuleDefinition module, Readers readers, Writers writers, IWeaverLogger logger) : base(module, readers, writers, logger)
        {
        }

        // ulong = 64 bytes
        const int SyncVarLimit = 64;
        private const string SyncVarCount = "SYNC_VAR_COUNT";

        static string HookParameterMessage(string hookName, TypeReference ValueType)
            => string.Format("void {0}({1} oldValue, {1} newValue)", hookName, ValueType);

        static bool MatchesParameters(MethodDefinition method, TypeReference originalType)
        {
            // matches void onValueChange(T oldValue, T newValue)
            return method.Parameters[0].ParameterType.FullName == originalType.FullName &&
                   method.Parameters[1].ParameterType.FullName == originalType.FullName;
        }

        void ProcessGetter(PropertyDefinition pd, FieldDefinition backingField)
        {
            MethodDefinition oldGetter = SubstituteMethod(pd.GetMethod);

            // normal getter is fine most types
            // but wrapped types need to unwrap the value
            MethodDefinition getter = pd.GetMethod;

            ILProcessor worker = getter.Body.GetILProcessor();
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Ldflda, backingField.MakeHostGenericIfNeeded()));

            MethodReference wrappedGetter = module.ImportReference(backingField.FieldType.Resolve().GetMethod("get_Value"));
            worker.Append(worker.Create(OpCodes.Call, wrappedGetter));

            // When we use NetworkBehaviors, we normally use a derived class,
            // but the NetworkBehaviorSyncVar returns just NetworkBehavior
            // thus we need to cast it to the user specicfied type
            // otherwise IL2PP fails to build.  see #629
            if (getter.ReturnType.FullName != pd.PropertyType.FullName)
            {
                worker.Append(worker.Create(OpCodes.Castclass, pd.PropertyType));
            }

            worker.Append(worker.Create(OpCodes.Ret));
            return;
        }

        MethodDefinition ProcessSetter(PropertyDefinition pd, long dirtyBit)
        {

            MethodDefinition oldSetter = SubstituteMethod(pd.SetMethod);

            //Create the set method
            MethodDefinition set = pd.SetMethod;
            ParameterDefinition valueParam = set.Parameters[0];

            ILProcessor worker = set.Body.GetILProcessor();

            // if (!SyncVarEqual(value, ref playerData))
            Instruction endOfMethod = worker.Create(OpCodes.Nop);

            VariableDefinition oldValue = set.AddLocal(pd.PropertyType);
            LoadField(worker, pd);
            worker.Append(worker.Create(OpCodes.Stloc, oldValue));

            // this
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            // new value to set
            worker.Append(worker.Create(OpCodes.Ldarg, valueParam));
            // reference to field to set
            worker.Append(worker.Create(OpCodes.Ldloc, oldValue));

            MethodReference syncVarEqual = module.ImportReference<NetworkBehaviour>(nb => nb.SyncVarEqual<object>(default, default));
            var syncVarEqualGm = new GenericInstanceMethod(syncVarEqual.GetElementMethod());
            syncVarEqualGm.GenericArguments.Add(pd.PropertyType);
            worker.Append(worker.Create(OpCodes.Call, syncVarEqualGm));

            worker.Append(worker.Create(OpCodes.Brtrue, endOfMethod));

            // T oldValue = value;

            // fieldValue = value;
            StoreField(pd, oldSetter, valueParam, worker);

            // this.SetDirtyBit(dirtyBit)
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Ldc_I8, dirtyBit));
            worker.Append(worker.Create<NetworkBehaviour>(OpCodes.Call, nb => nb.SetDirtyBit(default)));

            worker.Append(endOfMethod);

            worker.Append(worker.Create(OpCodes.Ret));

            return set;
        }

        private void LoadField(ILProcessor worker, PropertyDefinition pd) {
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Call, pd.GetMethod.MakeHostGenericIfNeeded()));
        }

        private void StoreField(PropertyDefinition pd, MethodReference oldSetter, ParameterDefinition valueParam, ILProcessor worker)
        {
            if (IsWrapped(pd.PropertyType))
            {
                var backingField = wrappedBackingFields[pd];

                // there is a wrapper struct, call the setter
                MethodReference setter = module.ImportReference(backingField.FieldType.Resolve().GetMethod("set_Value"));

                worker.Append(worker.Create(OpCodes.Ldarg_0));
                worker.Append(worker.Create(OpCodes.Ldflda, backingField.MakeHostGenericIfNeeded()));
                worker.Append(worker.Create(OpCodes.Ldarg, valueParam));
                worker.Append(worker.Create(OpCodes.Call, setter.MakeHostGenericIfNeeded()));
            }
            else
            {
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                worker.Append(worker.Create(OpCodes.Ldarg, valueParam));
                worker.Append(worker.Create(OpCodes.Call, oldSetter.MakeHostGenericIfNeeded()));
            }
        }

        void ProcessSyncVar(PropertyDefinition pd, long dirtyBit)
        {
            string originalName = pd.Name;
            Weaver.DLog(pd.DeclaringType, "Sync Var " + pd.Name + " " + pd.PropertyType);

            if (IsWrapped(pd.PropertyType))
            {
                var wrappedField = GenerateWrapBackingField(pd);
                wrappedBackingFields[pd] = wrappedField;
                // wrapped struct, the getter should retrieve the value
                // from the wrapper struct
                ProcessGetter(pd, wrappedField);
            }

            ProcessSetter(pd, dirtyBit);
        }

        private FieldDefinition GenerateWrapBackingField(PropertyDefinition pd)
        {
            TypeReference wrapType = WrapType(pd.PropertyType);

            FieldDefinition wrappedBackingField = new FieldDefinition("SyncVar__" + pd.Name, FieldAttributes.Private, wrapType);
            pd.DeclaringType.Fields.Add(wrappedBackingField);
            return wrappedBackingField;
        }

        private TypeReference WrapType(TypeReference typeReference)
        {
            if (typeReference.Is<NetworkIdentity>())
            {
                // change the type of the field to a wrapper NetworkIDentitySyncvar
                return module.ImportReference<NetworkIdentitySyncvar>();
            }
            if (typeReference.Is<GameObject>())
                return module.ImportReference<GameObjectSyncvar>();

            if (typeReference.IsDerivedFrom<NetworkBehaviour>())
                return module.ImportReference<NetworkBehaviorSyncvar>();

            return typeReference;
        }

        private static bool IsWrapped(TypeReference typeReference)
        {
            return typeReference.Is<NetworkIdentity>() ||
                typeReference.Is<GameObject>() ||
                typeReference.Is<NetworkBehaviour>();
        }

        public void ProcessSyncVars(TypeDefinition td)
        {
            // the mapping of dirtybits to sync-vars is implicit in the order of the fields here. this order is recorded in m_replacementProperties.
            // start assigning syncvars at the place the base class stopped, if any

            int dirtyBitCounter = td.BaseType.Resolve().GetConst<int>(SyncVarCount);
            syncVars.Clear();
            wrappedBackingFields.Clear();

            // find syncvars
            foreach (PropertyDefinition pd in td.Properties)
            {
                if (!pd.HasCustomAttribute<SyncVarAttribute>())
                {
                    continue;
                }

                if (pd.PropertyType.IsGenericParameter)
                {
                    logger.Error($"{pd.Name} cannot be synced since it's a generic parameter", pd, td.GetSequencePoint());
                    continue;
                }

                if (!pd.HasThis)
                {
                    logger.Error($"{pd.Name} cannot be static", pd, td.GetSequencePoint());
                    continue;
                }

                if (pd.PropertyType.IsArray)
                {
                    logger.Error($"{pd.Name} has invalid type. Use SyncLists instead of arrays", pd, td.GetSequencePoint());
                    continue;
                }

                if (SyncObjectProcessor.ImplementsSyncObject(pd.PropertyType))
                {
                    logger.Warning($"{pd.Name} has [SyncVar] attribute. SyncLists should not be marked with SyncVar", pd, td.GetSequencePoint());
                    continue;
                }
                syncVars.Add(pd);

                ProcessSyncVar(pd, 1L << dirtyBitCounter);
                dirtyBitCounter += 1;
            }

            if (dirtyBitCounter >= SyncVarLimit)
            {
                logger.Error($"{td.Name} has too many SyncVars. Consider refactoring your class into multiple components", td, td.GetSequencePoint());
            }

            td.SetConst(SyncVarCount, syncVars.Count);

            GenerateSerialization(td);
            GenerateDeSerialization(td);
        }

        void CallHook(ILProcessor worker, MethodDefinition hookMethod, VariableDefinition oldValue, ParameterDefinition newValue)
        {
            // dont add this (Ldarg_0) if method is static
            if (!hookMethod.IsStatic)
            {
                // this before method call
                // eg this.onValueChanged
                worker.Append(worker.Create(OpCodes.Ldarg_0));
            }

            worker.Append(worker.Create(OpCodes.Ldloc, oldValue));
            worker.Append(worker.Create(OpCodes.Ldarg, newValue));

            WriteEndFunctionCall(worker, hookMethod);
        }

        void CallHook(ILProcessor worker, MethodDefinition hookMethod, VariableDefinition oldValue, PropertyDefinition newValue)
        {
            // dont add this (Ldarg_0) if method is static
            if (!hookMethod.IsStatic)
            {
                // this before method call
                // eg this.onValueChanged
                worker.Append(worker.Create(OpCodes.Ldarg_0));
            }

            worker.Append(worker.Create(OpCodes.Ldloc, oldValue));

            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Call, newValue.GetMethod.MakeHostGenericIfNeeded()));

            WriteEndFunctionCall(worker, hookMethod);
        }

        void WriteEndFunctionCall(ILProcessor worker, MethodDefinition hookMethod)
        {
            // only use Callvirt when not static
            OpCode opcode = hookMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt;
            MethodReference hookMethodReference = hookMethod;

            if (hookMethodReference.DeclaringType.HasGenericParameters)
            {
                // we need to get the Type<T>.HookMethod so convert it to a generic<T>.
                var genericType = (GenericInstanceType)hookMethod.DeclaringType.ConvertToGenericIfNeeded();
                hookMethodReference = hookMethod.MakeHostInstanceGeneric(genericType);
            }

            worker.Append(worker.Create(opcode, module.ImportReference(hookMethodReference)));
        }

        void GenerateSerialization(TypeDefinition netBehaviourSubclass)
        {
            Weaver.DLog(netBehaviourSubclass, "  GenerateSerialization");

            const string SerializeMethodName = nameof(NetworkBehaviour.SerializeSyncVars);
            if (netBehaviourSubclass.GetMethod(SerializeMethodName) != null)
                return;

            if (syncVars.Count == 0)
            {
                // no synvars,  no need for custom OnSerialize
                return;
            }

            MethodDefinition serialize = netBehaviourSubclass.AddMethod(SerializeMethodName,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                    module.ImportReference<bool>());

            ParameterDefinition writerParameter = serialize.AddParam<NetworkWriter>("writer");
            ParameterDefinition initializeParameter = serialize.AddParam<bool>("initialize");
            ILProcessor worker = serialize.Body.GetILProcessor();

            serialize.Body.InitLocals = true;

            // loc_0,  this local variable is to determine if any variable was dirty
            VariableDefinition dirtyLocal = serialize.AddLocal<bool>();

            MethodReference baseSerialize = netBehaviourSubclass.BaseType.GetMethodInBaseType(SerializeMethodName);
            if (baseSerialize != null)
            {
                // base
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                // writer
                worker.Append(worker.Create(OpCodes.Ldarg, writerParameter));
                // forceAll
                worker.Append(worker.Create(OpCodes.Ldarg, initializeParameter));
                worker.Append(worker.Create(OpCodes.Call, module.ImportReference(baseSerialize)));
                // set dirtyLocal to result of base.OnSerialize()
                worker.Append(worker.Create(OpCodes.Stloc, dirtyLocal));
            }

            // Generates: if (forceAll);
            Instruction initialStateLabel = worker.Create(OpCodes.Nop);
            // forceAll
            worker.Append(worker.Create(OpCodes.Ldarg, initializeParameter));
            worker.Append(worker.Create(OpCodes.Brfalse, initialStateLabel));

            foreach (PropertyDefinition syncVar in syncVars)
            {
                WriteVariable(worker, writerParameter, syncVar);
            }

            // always return true if forceAll

            // Generates: return true
            worker.Append(worker.Create(OpCodes.Ldc_I4_1));
            worker.Append(worker.Create(OpCodes.Ret));

            // Generates: end if (forceAll);
            worker.Append(initialStateLabel);

            // write dirty bits before the data fields
            // Generates: writer.WritePackedUInt64 (base.get_syncVarDirtyBits ());
            // writer
            worker.Append(worker.Create(OpCodes.Ldarg, writerParameter));
            // base
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Call, (NetworkBehaviour nb) => nb.SyncVarDirtyBits));
            MethodReference writeUint64Func = writers.GetWriteFunc<ulong>(null);
            worker.Append(worker.Create(OpCodes.Call, writeUint64Func));

            // generate a writer call for any dirty variable in this class

            // start at number of syncvars in parent
            int dirtyBit = netBehaviourSubclass.BaseType.Resolve().GetConst<int>(SyncVarCount);
            foreach (PropertyDefinition syncVar in syncVars)
            {
                Instruction varLabel = worker.Create(OpCodes.Nop);

                // Generates: if ((base.get_syncVarDirtyBits() & 1uL) != 0uL)
                // base
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                worker.Append(worker.Create(OpCodes.Call, (NetworkBehaviour nb) => nb.SyncVarDirtyBits));
                // 8 bytes = long
                worker.Append(worker.Create(OpCodes.Ldc_I8, 1L << dirtyBit));
                worker.Append(worker.Create(OpCodes.And));
                worker.Append(worker.Create(OpCodes.Brfalse, varLabel));

                // Generates a call to the writer for that field
                WriteVariable(worker, writerParameter, syncVar);

                // something was dirty
                worker.Append(worker.Create(OpCodes.Ldc_I4_1));
                // set dirtyLocal to true
                worker.Append(worker.Create(OpCodes.Stloc, dirtyLocal));

                worker.Append(varLabel);
                dirtyBit += 1;
            }

            // generate: return dirtyLocal
            worker.Append(worker.Create(OpCodes.Ldloc, dirtyLocal));
            worker.Append(worker.Create(OpCodes.Ret));
        }

        private void WriteVariable(ILProcessor worker, ParameterDefinition writerParameter, PropertyDefinition syncVar)
        {
            // Generates a writer call for each sync variable
            // writer
            worker.Append(worker.Create(OpCodes.Ldarg, writerParameter));
            // this

            var type = syncVar.PropertyType;

            if (IsWrapped(type)) {
                FieldDefinition wrappedField = wrappedBackingFields[syncVar];
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                worker.Append(worker.Create(OpCodes.Ldfld, wrappedField.MakeHostGenericIfNeeded()));
                type = wrappedField.FieldType;
            }
            else {
                LoadField(worker, syncVar);
            }

            MethodReference writeFunc = writers.GetWriteFunc(type, syncVar.DeclaringType.GetSequencePoint());
            if (writeFunc != null)
            {
                worker.Append(worker.Create(OpCodes.Call, writeFunc));
            }
            else
            {
                logger.Error($"{syncVar.Name} has unsupported type. Use a supported Mirage type instead", syncVar, syncVar.DeclaringType.GetSequencePoint());
            }
        }

        void GenerateDeSerialization(TypeDefinition netBehaviourSubclass)
        {
            Weaver.DLog(netBehaviourSubclass, "  GenerateDeSerialization");

            const string DeserializeMethodName = nameof(NetworkBehaviour.DeserializeSyncVars);
            if (netBehaviourSubclass.GetMethod(DeserializeMethodName) != null)
                return;

            if (syncVars.Count == 0)
            {
                // no synvars,  no need for custom OnDeserialize
                return;
            }

            MethodDefinition serialize = netBehaviourSubclass.AddMethod(DeserializeMethodName,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);

            ParameterDefinition readerParam = serialize.AddParam<NetworkReader>("reader");
            ParameterDefinition initializeParam = serialize.AddParam<bool>("initialState");
            ILProcessor serWorker = serialize.Body.GetILProcessor();
            // setup local for dirty bits
            serialize.Body.InitLocals = true;
            VariableDefinition dirtyBitsLocal = serialize.AddLocal<long>();

            MethodReference baseDeserialize = netBehaviourSubclass.BaseType.GetMethodInBaseType(DeserializeMethodName);
            if (baseDeserialize != null)
            {
                // base
                serWorker.Append(serWorker.Create(OpCodes.Ldarg_0));
                // reader
                serWorker.Append(serWorker.Create(OpCodes.Ldarg, readerParam));
                // initialState
                serWorker.Append(serWorker.Create(OpCodes.Ldarg, initializeParam));
                serWorker.Append(serWorker.Create(OpCodes.Call, module.ImportReference(baseDeserialize)));
            }

            // Generates: if (initialState);
            Instruction initialStateLabel = serWorker.Create(OpCodes.Nop);

            serWorker.Append(serWorker.Create(OpCodes.Ldarg, initializeParam));
            serWorker.Append(serWorker.Create(OpCodes.Brfalse, initialStateLabel));

            foreach (PropertyDefinition syncVar in syncVars)
            {
                DeserializeField(syncVar, serWorker, serialize);
            }

            serWorker.Append(serWorker.Create(OpCodes.Ret));

            // Generates: end if (initialState);
            serWorker.Append(initialStateLabel);

            // get dirty bits
            serWorker.Append(serWorker.Create(OpCodes.Ldarg, readerParam));
            serWorker.Append(serWorker.Create(OpCodes.Call, readers.GetReadFunc<ulong>(null)));
            serWorker.Append(serWorker.Create(OpCodes.Stloc, dirtyBitsLocal));

            // conditionally read each syncvar
            // start at number of syncvars in parent
            int dirtyBit = netBehaviourSubclass.BaseType.Resolve().GetConst<int>(SyncVarCount);
            foreach (PropertyDefinition syncVar in syncVars)
            {
                Instruction varLabel = serWorker.Create(OpCodes.Nop);

                // check if dirty bit is set
                serWorker.Append(serWorker.Create(OpCodes.Ldloc, dirtyBitsLocal));
                serWorker.Append(serWorker.Create(OpCodes.Ldc_I8, 1L << dirtyBit));
                serWorker.Append(serWorker.Create(OpCodes.And));
                serWorker.Append(serWorker.Create(OpCodes.Brfalse, varLabel));

                DeserializeField(syncVar, serWorker, serialize);

                serWorker.Append(varLabel);
                dirtyBit += 1;
            }

            serWorker.Append(serWorker.Create(OpCodes.Ret));
        }

        /// <summary>
        /// [SyncVar] int/float/struct/etc.?
        /// </summary>
        /// <param name="syncVar"></param>
        /// <param name="serWorker"></param>
        /// <param name="deserialize"></param>
        /// <param name="initialState"></param>
        /// <param name="hookResult"></param>
        void DeserializeField(PropertyDefinition syncVar, ILProcessor serWorker, MethodDefinition deserialize)
        {

            /*
             Generates code like:
                // for hook
                int oldValue = a;
                Networka = reader.ReadPackedInt32();
                if (!SyncVarEqual(oldValue, ref a))
                {
                    OnSetA(oldValue, Networka);
                }
             */

            if (IsWrapped(syncVar.PropertyType))
            {
                DeserializeWrappedField(syncVar, serWorker, deserialize);
            }
            else
            {
                DeserializeNormalField(syncVar, serWorker, deserialize);
            }
        }

        private void DeserializeNormalField(PropertyDefinition syncVar, ILProcessor serWorker, MethodDefinition deserialize)
        {
            MethodReference readFunc = readers.GetReadFunc(syncVar.PropertyType, deserialize.GetSequencePoint());
            if (readFunc == null)
            {
                logger.Error($"{syncVar.Name} has unsupported type. Use a supported Mirage type instead", syncVar, deserialize.GetSequencePoint());
                return;
            }

            // T oldValue = value;
            VariableDefinition oldValue = deserialize.AddLocal(syncVar.PropertyType);
            LoadField(serWorker, syncVar);

            serWorker.Append(serWorker.Create(OpCodes.Stloc, oldValue));

            // read value and store in syncvar BEFORE calling the hook
            // -> this makes way more sense. by definition, the hook is
            //    supposed to be called after it was changed. not before.
            // -> setting it BEFORE calling the hook fixes the following bug:
            //    https://github.com/vis2k/Mirror/issues/1151 in host mode
            //    where the value during the Hook call would call Cmds on
            //    the host server, and they would all happen and compare
            //    values BEFORE the hook even returned and hence BEFORE the
            //    actual value was even set.
            // put 'this.' onto stack for 'this.syncvar' below
            serWorker.Append(serWorker.Create(OpCodes.Ldarg_0));
            // reader. for 'reader.Read()' below
            serWorker.Append(serWorker.Create(OpCodes.Ldarg_1));
            // reader.Read()
            serWorker.Append(serWorker.Create(OpCodes.Call, readFunc));
            // syncvar
            serWorker.Append(serWorker.Create(OpCodes.Call, syncVar.SetMethod.MakeHostGenericIfNeeded()));
        }

        private void DeserializeWrappedField(PropertyDefinition syncVar, ILProcessor serWorker, MethodDefinition deserialize)
        {
            var backingField = wrappedBackingFields[syncVar];
            MethodReference readFunc = readers.GetReadFunc(backingField.FieldType, null);
            if (readFunc == null)
            {
                logger.Error($"{syncVar.Name} has unsupported type. Use a supported Mirage type instead", syncVar, deserialize.GetSequencePoint());
                return;
            }

            // read value and store in syncvar BEFORE calling the hook
            // -> this makes way more sense. by definition, the hook is
            //    supposed to be called after it was changed. not before.
            // -> setting it BEFORE calling the hook fixes the following bug:
            //    https://github.com/vis2k/Mirror/issues/1151 in host mode
            //    where the value during the Hook call would call Cmds on
            //    the host server, and they would all happen and compare
            //    values BEFORE the hook even returned and hence BEFORE the
            //    actual value was even set.
            // put 'this.' onto stack for 'this.syncvar' below
            serWorker.Append(serWorker.Create(OpCodes.Ldarg_0));
            // reader. for 'reader.Read()' below
            serWorker.Append(serWorker.Create(OpCodes.Ldarg_1));
            // reader.Read()
            serWorker.Append(serWorker.Create(OpCodes.Call, readFunc));
            // syncvar
            serWorker.Append(serWorker.Create(OpCodes.Stfld, backingField.MakeHostGenericIfNeeded()));
        }
    }
}
