using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Mirage.Weaver
{
    /// <summary>
    /// convenience methods for type definitions
    /// </summary>
    public static class TypeExtensions
    {

        public static MethodDefinition GetMethod(this TypeDefinition td, string methodName)
        {
            // Linq allocations don't matter in weaver
            return td.Methods.FirstOrDefault(method => method.Name == methodName);
        }

        public static List<MethodDefinition> GetMethods(this TypeDefinition td, string methodName)
        {
            // Linq allocations don't matter in weaver
            return td.Methods.Where(method => method.Name == methodName).ToList();
        }

        public static MethodReference GetMethodInBaseType(this TypeReference td, string methodName)
        {
            TypeDefinition typedef = td.Resolve();
            TypeReference typeRef = td;
            while (typedef != null)
            {
                foreach (MethodDefinition md in typedef.Methods)
                {
                    if (md.Name == methodName)
                    {
                        MethodReference method = md;
                        if (typeRef.IsGenericInstance)
                        {
                            var baseTypeInstance = (GenericInstanceType)td;
                            method = method.MakeHostInstanceGeneric(baseTypeInstance);
                        }

                        return method;
                    }
                }

                try
                {
                    TypeReference parent = typedef.BaseType;
                    typeRef = parent;
                    typedef = parent?.Resolve();
                }
                catch (AssemblyResolutionException)
                {
                    // this can happen for plugins.
                    break;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds public fields in type and base type
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public static IEnumerable<FieldDefinition> FindAllPublicFields(this TypeReference variable)
        {
            return FindAllPublicFields(variable.Resolve());
        }

        /// <summary>
        /// Finds public fields in type and base type
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public static IEnumerable<FieldDefinition> FindAllPublicFields(this TypeDefinition typeDefinition)
        {
            while (typeDefinition != null)
            {
                foreach (FieldDefinition field in typeDefinition.Fields)
                {
                    if (field.IsStatic || field.IsPrivate)
                        continue;

                    if (field.IsNotSerialized)
                        continue;

                    yield return field;
                }

                try
                {
                    typeDefinition = typeDefinition.BaseType?.Resolve();
                }
                catch
                {
                    break;
                }
            }
        }

        public static MethodDefinition AddMethod(this TypeDefinition typeDefinition, string name, MethodAttributes attributes, TypeReference typeReference)
        {
            var method = new MethodDefinition(name, attributes, typeReference);
            typeDefinition.Methods.Add(method);
            return method;
        }

        public static MethodDefinition AddMethod(this TypeDefinition typeDefinition, string name, MethodAttributes attributes) =>
            AddMethod(typeDefinition, name, attributes, typeDefinition.Module.ImportReference(typeof(void)));

        /// <summary>
        /// Creates a generic type out of another type, if needed.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static TypeReference ConvertToGenericIfNeeded(this TypeDefinition type)
        {
            if (type.HasGenericParameters)
            {
                // get all the generic parameters and make a generic instance out of it
                var genericTypes = new TypeReference[type.GenericParameters.Count];
                for (int i = 0; i < type.GenericParameters.Count; i++)
                {
                    genericTypes[i] = type.GenericParameters[i].GetElementType();
                }

                return type.MakeGenericInstanceType(genericTypes);
            }
            else
            {
                return type;
            }
        }

        public static FieldReference GetField(this TypeDefinition type, string fieldName)
        {
            if (type.HasFields)
            {
                for (int i = 0; i < type.Fields.Count; i++)
                {
                    if (type.Fields[i].Name == fieldName)
                    {
                        return type.Fields[i];
                    }
                }
            }

            return null;
        }


        public static SequencePoint GetSequencePoint(this MethodDefinition methodDefinition)
        {
            return methodDefinition.DebugInformation.HasSequencePoints ?
                methodDefinition.DebugInformation.SequencePoints[0] :
                methodDefinition.DeclaringType.GetSequencePoint();
        }

        public static SequencePoint GetSequencePoint(this TypeDefinition type)
        {
            // find sequence point of first method
            foreach (MethodDefinition md in type.Methods)
            {
                if (md.DebugInformation.HasSequencePoints)
                {
                    return md.DebugInformation.SequencePoints[0];
                }
            }

            string url = type.FullName.Replace('.', '/') + ".cs";

            // if that fails, make up a fake Sequence point
            return new SequencePoint(
                Instruction.Create(OpCodes.Nop),
                new Document(url));
        }

        public static SequencePoint GetSequencePoint(this TypeReference type)
        {
            TypeDefinition td = type.Resolve();

            if (td is not null)
            {
                return td.GetSequencePoint();
            }

            string url = type.FullName.Replace('.', '/') + ".cs";

            // if that fails, make up a fake Sequence point
            return new SequencePoint(
                Instruction.Create(OpCodes.Nop),
                new Document(url));
        }
    }
}
