using System;
using Mono.Cecil.Cil;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace TrixxInjection.Fody
{
    internal partial class Processor
    {
        /// <summary>
        /// Helper functions for injecting a Try-Catch-Finally(?) block.
        /// </summary>
        /// <param name="tryStart">The first instruction inside the try block (must already be in the method body and before <paramref name="tryEnd"/>)</param>
        /// <param name="tryEnd">The last instruction inside the try block (must already be in the method body and after <paramref name="tryStart"/>)</param>
        /// <param name="finallyBlock">An action that creates the instructions that would be inside the finally block, optional.</param>
        /// <param name="catches">An array of Types to catch, and actions of instructions to create inside their respective catches.</param>
        public Processor AddTryCatchFinally(
            Instruction tryStart,
            Instruction tryEnd,
            Action<Processor> finallyBlock = null,
            params (TypeReference CatchType, Action<Processor> Handler)[] catches)
        {
            var body = _processor.Body;
            var origTarget = _target;
            var origAfter = AfterTarget;

            foreach (var (catchType, handler) in catches)
            {
                var hStart = Instruction.Create(OpCodes.Nop);
                var hEnd = Instruction.Create(OpCodes.Nop);
                body.Instructions.Add(hStart);
                _target = hStart;
                AfterTarget = true;
                handler(this);
                body.Instructions.Add(hEnd);
                body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
                {
                    TryStart = tryStart,
                    TryEnd = tryEnd,
                    HandlerStart = hStart,
                    HandlerEnd = hEnd,
                    CatchType = catchType
                });
            }

            if (finallyBlock != null)
            {
                var fStart = Instruction.Create(OpCodes.Nop);
                var fEnd = Instruction.Create(OpCodes.Nop);
                body.Instructions.Add(fStart);
                _target = fStart;
                AfterTarget = true;
                finallyBlock(this);
                body.Instructions.Add(fEnd);
                body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
                {
                    TryStart = tryStart,
                    TryEnd = tryEnd,
                    HandlerStart = fStart,
                    HandlerEnd = fEnd
                });
            }

            _target = origTarget;
            AfterTarget = origAfter;
            return this;
        }

        /// <summary>
        /// Defines and adds a new local variable of the given <paramref name="type"/>
        /// to the current method body, and returns its <see cref="VariableDefinition"/>.
        /// </summary>
        public VariableDefinition DefineLocal(TypeReference type)
        {
            var body = _processor.Body;
            var variable = new VariableDefinition(type);
            body.Variables.Add(variable);
            body.InitLocals = true;
            return variable;
        }

        /// <summary>
        /// Inserts a sequence of IL instructions at the very start of the method, so, before the first instruction
        /// </summary>
        public Processor InsertOnEntry(Action<Processor> emit)
        {
            var first = _processor.Body.Instructions[0];
            MoveTo(first, after: false);
            emit(this);
            return this;
        }

        /// <summary>
        /// Finds all <c>ret</c> (return) instructions in the method and, for each one,
        /// runs <paramref name="emit"/> immediately before it. Good for clean ups???
        /// </summary>
        public Processor InsertOnExit(Action<Processor> emit)
        {
            var returns = new List<Instruction>(_processor.Body.Instructions);
            foreach (var instr in returns)
            {
                if (instr.OpCode == OpCodes.Ret)
                {
                    MoveTo(instr, after: false);
                    emit(this);
                }
            }
            return this;
        }

        /// <summary>
        /// Replaces a single <paramref name="oldInstruction"/> with the supplied
        /// <paramref name="newInstructions"/> sequence. Existing exception handlers
        /// targeting the old instruction will be updated to point at the first new one.
        /// </summary>
        public Processor ReplaceInstruction(
            Instruction oldInstruction,
            IList<Instruction> newInstructions)
        {
            foreach (var ins in newInstructions)
                _processor.InsertBefore(oldInstruction, ins);

            foreach (var eh in _processor.Body.ExceptionHandlers)
            {
                if (eh.TryStart == oldInstruction) eh.TryStart = newInstructions.First();
                if (eh.TryEnd == oldInstruction) eh.TryEnd = newInstructions.First();
                if (eh.HandlerStart == oldInstruction) eh.HandlerStart = newInstructions.First();
                if (eh.HandlerEnd == oldInstruction) eh.HandlerEnd = newInstructions.First();
            }

            _processor.Remove(oldInstruction);
            return this;
        }

        /// <summary>
        /// Removes every instruction from <paramref name="start"/> up to
        /// and including <paramref name="end"/>. Useful for cutting out whole blocks.
        /// </summary>
        public Processor RemoveRange(Instruction start, Instruction end)
        {
            var body = _processor.Body;
            var toRemove = new List<Instruction>();
            bool inRange = false;

            foreach (var instr in body.Instructions)
            {
                if (instr == start) inRange = true;
                if (inRange) toRemove.Add(instr);
                if (instr == end) break;
            }

            foreach (var instr in toRemove)
                body.Instructions.Remove(instr);

            return this;
        }

        /// <summary>
        /// Imports (adds to the module) a <see cref="MethodReference"/> for the given
        /// <paramref name="methodInfo"/>, so you can call BCL methods by reflection.
        /// </summary>
        public MethodReference ImportReference(System.Reflection.MethodInfo methodInfo)
            => _processor.Body.Method.Module.ImportReference(methodInfo);

        /// <summary>
        /// Marks a debug sequence point (start/end of a source-code range)
        /// so that stepping in a debugger will map back to your original file.
        /// </summary>
        public Processor MarkSequencePoint(
            Document doc,
            int startLine, int startColumn,
            int endLine, int endColumn)
        {
            var sp = new SequencePoint(_target, doc)
            {
                StartLine = startLine,
                StartColumn = startColumn,
                EndLine = endLine,
                EndColumn = endColumn
            };
            _processor.Body.Method.DebugInformation.SequencePoints.Add(sp);
            return this;
        }
    }
}
