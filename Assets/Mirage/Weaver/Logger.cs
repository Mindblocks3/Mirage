using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace Mirage.Weaver
{
    public class Logger : IWeaverLogger
    {
        public List<DiagnosticMessage> Diagnostics = new List<DiagnosticMessage>();

        public void Error(string message, MethodDefinition md)
        {
            AddMessage($"{message} (at {md})", md.GetSequencePoint(), DiagnosticType.Error);
        }

        public void Error(string message, MemberReference mr, SequencePoint sequencePoint)
        {
            AddMessage($"{message} (at {mr})", sequencePoint, DiagnosticType.Error);
        }

        public void Warning(string message, MemberReference mr, SequencePoint sequencePoint)
        {
            AddMessage($"{message} (at {mr})", sequencePoint, DiagnosticType.Warning);
        }

        private void AddMessage(string message, SequencePoint sequencePoint, DiagnosticType diagnosticType)
        {
            if (sequencePoint == null)
            {
                Console.WriteLine("I should always have a sequence point");
            }
            Diagnostics.Add(new DiagnosticMessage
            {
                DiagnosticType = diagnosticType,
                File = sequencePoint?.Document.Url.Replace($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}", ""),
                Line = sequencePoint?.StartLine ?? 0,
                Column = sequencePoint?.StartColumn ?? 0,
                MessageData = message
            });
        }
    }
}
