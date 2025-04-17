using Mono.Cecil.Cil;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil.Rocks;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace TrixxInjection.Fody
{
    internal class ModuleProcessor
    {
        public ModuleDefinition ModuleDefinition { get; }
        public ModuleProcessor(ModuleDefinition module)
        {
            ModuleDefinition = module;
        }

        public void AttachMethodLogging(MethodDefinition method)
        {
            if (!HasTimedAttribute(method))
                return;
            if (IsAsync(method) || IsIterator(method))
            {
                var stateMachine = GetStateMachineType(method);
                if (stateMachine == null) return;
                var moveNext = stateMachine.Methods.First(m => m.Name == "MoveNext");
                ProcessStateMachineMethod(moveNext, method);
            }
            else
            {
                AttachTimer(method);
            }
        }

        private bool HasTimedAttribute(MethodDefinition method)
            => method.CustomAttributes.Any(a => a.AttributeType.FullName == "YourNamespace.TimedAttribute");

        private bool IsAsync(MethodDefinition method)
            => method.CustomAttributes.Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute");

        private bool IsIterator(MethodDefinition method)
            => method.CustomAttributes.Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.IteratorStateMachineAttribute");

        private TypeDefinition GetStateMachineType(MethodDefinition method)
        {
            var attr = method.CustomAttributes.FirstOrDefault(a =>
                a.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute" ||
                a.AttributeType.FullName == "System.Runtime.CompilerServices.IteratorStateMachineAttribute");
            var stateMachineTypeRef = (TypeReference)attr?.ConstructorArguments[0].Value;
            return stateMachineTypeRef?.Resolve();
        }

        private void AttachTimer(MethodDefinition methodSig)
        {
            var stopwatchType = ModuleDefinition.ImportReference(typeof(Stopwatch));
            var startNewMethod = ModuleDefinition.ImportReference(typeof(Stopwatch).GetMethod("StartNew"));
            var stopMethod = ModuleDefinition.ImportReference(typeof(Stopwatch).GetMethod("Stop"));
            var getElapsedMethod = ModuleDefinition.ImportReference(typeof(Stopwatch).GetProperty("Elapsed").GetGetMethod());
            //var logMethod = ModuleDefinition.ImportReference(typeof(AttributeCallFunctions).GetMethod("LogString"));
            var method = methodSig.Body;
            method.SimplifyMacros();
            var il = method.GetILProcessor();
            var firstInstruction = method.Instructions.First();
            var stopwatchVar = new VariableDefinition(stopwatchType);
            method.Variables.Add(stopwatchVar);
            method.InitLocals = true;
            il.InsertBefore(firstInstruction, il.Create(OpCodes.Call, startNewMethod));
            il.InsertBefore(firstInstruction, il.Create(OpCodes.Stloc, stopwatchVar));
            var retInstructions = method.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToList();
            foreach (var ret in retInstructions)
            {
                il.Replace(ret, il.Create(OpCodes.Leave, ret));
                il.InsertBefore(ret, il.Create(OpCodes.Ldloc, stopwatchVar));
                il.InsertBefore(ret, il.Create(OpCodes.Call, stopMethod));
                il.InsertBefore(ret, il.Create(OpCodes.Ldstr, methodSig.FullName));
                il.InsertBefore(ret, il.Create(OpCodes.Ldloc, stopwatchVar));
                il.InsertBefore(ret, il.Create(OpCodes.Call, getElapsedMethod));
                //il.InsertBefore(ret, il.Create(OpCodes.Call, logMethod));
            }
            method.OptimizeMacros();
        }

        private void ProcessStateMachineMethod(MethodDefinition moveNext, MethodDefinition originalMethod)
        {
            var stopwatchType = ModuleDefinition.ImportReference(typeof(Stopwatch));
            var startNewMethod = ModuleDefinition.ImportReference(typeof(Stopwatch).GetMethod("StartNew"));
            var stopMethod = ModuleDefinition.ImportReference(typeof(Stopwatch).GetMethod("Stop"));
            var getElapsedMethod = ModuleDefinition.ImportReference(typeof(Stopwatch).GetProperty("Elapsed").GetGetMethod());
            //var logMethod = ModuleDefinition.ImportReference(typeof(AttributeCallFunctions).GetMethod("LogString"));
            var stateMachineType = moveNext.DeclaringType;
            var stopwatchField = stateMachineType.Fields.FirstOrDefault(f => f.Name == "__methodStopwatch");
            if (stopwatchField == null)
            {
                stopwatchField = new FieldDefinition("__methodStopwatch", FieldAttributes.Private, stopwatchType);
                stateMachineType.Fields.Add(stopwatchField);
            }
            moveNext.Body.SimplifyMacros();
            var il = moveNext.Body.GetILProcessor();
            var firstInstruction = moveNext.Body.Instructions.First();
            var initInstructions = new List<Instruction>
        {
            il.Create(OpCodes.Ldarg_0),
            il.Create(OpCodes.Ldfld, stopwatchField),
            il.Create(OpCodes.Brtrue_S, firstInstruction),
            il.Create(OpCodes.Ldarg_0),
            il.Create(OpCodes.Call, startNewMethod),
            il.Create(OpCodes.Stfld, stopwatchField)
        };
            foreach (var instr in initInstructions)
                il.InsertBefore(firstInstruction, instr);
            var retInstructions = moveNext.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToList();
            foreach (var ret in retInstructions)
            {
                var beforeRet = new List<Instruction>
            {
                il.Create(OpCodes.Ldarg_0),
                il.Create(OpCodes.Ldfld, stopwatchField),
                il.Create(OpCodes.Call, stopMethod),
                il.Create(OpCodes.Ldstr, originalMethod.FullName),
                il.Create(OpCodes.Ldarg_0),
                il.Create(OpCodes.Ldfld, stopwatchField),
                il.Create(OpCodes.Call, getElapsedMethod),
                //il.Create(OpCodes.Call, logMethod)
            };
                foreach (var instr in beforeRet)
                    il.InsertBefore(ret, instr);
            }
            moveNext.Body.OptimizeMacros();
        }
    }
}
