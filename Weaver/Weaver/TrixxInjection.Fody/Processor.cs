using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TrixxInjection.Fody
{
    internal partial class Processor
    {
        private readonly Mono.Cecil.Cil.ILProcessor _processor;
        private Instruction _target;
        internal SimulatedBody _simulation = null;

        /// <summary>
        /// Determines whether new instructions will be inserted immediately after
        /// (<c>true</c>) or immediately before (<c>false</c>) the current target.
        /// </summary>
        public bool AfterTarget { get; private set; } = true;

        public bool AutoShift { get; private set; } = true;

        /// <summary>
        /// Initializes a new <see cref="Processor"/> around the given Cecil
        /// <paramref name="processor"/> and insertion <paramref name="target"/>.
        /// </summary>
        /// <param name="processor">
        ///   The Cecil <see cref="ILProcessor"/> that actually emits instructions
        ///   into the method body.
        /// </param>
        /// <param name="target">
        ///   The instruction before/after which new instructions will be inserted.
        /// </param>
        public Processor(Mono.Cecil.Cil.ILProcessor processor, Instruction target)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            MoveTo(target, after: true);
        }

        public Processor InsertSequence(IEnumerable<Instruction> instructions)
        {
            if (_target == null)
                throw new WeavingException("Processor target cannot be null");
            if (AfterTarget)
            {
                var current = _target;
                foreach (var instr in instructions)
                {
                    _processor.InsertAfter(current, instr);
                    current = instr;
                }
            }
            else
            {
                foreach (var instr in instructions.Reverse())
                    _processor.InsertBefore(_target, instr);
            }
            return this;
        }

        private Processor(Mono.Cecil.Cil.ILProcessor processor)
            => _processor = processor ?? throw new ArgumentNullException(nameof(processor));

        public static explicit operator Processor(Mono.Cecil.Cil.ILProcessor processor)
            => new Processor(processor);

        /// <summary>
        /// Changes the insertion point to <paramref name="newTarget"/>, and
        /// specifies whether subsequent inserts go before or after it.
        /// </summary>
        /// <param name="newTarget">The instruction to insert around.</param>
        /// <param name="after">
        ///   If <c>true</c>, new instructions go after <paramref name="newTarget"/>;
        ///   if <c>false</c>, they go before.
        /// </param>
        /// <returns>The same <see cref="Processor"/> for fluent chaining.</returns>
        public Processor MoveTo(Instruction newTarget, bool after = true)
        {
            if (newTarget == null)
                throw new WeavingException("Processor target cannot be null");

            _target = newTarget;
            AfterTarget = after;
            return this;
        }

        /// <summary>
        /// Inserts the given raw IL <paramref name="instruction"/> at the current
        /// insertion point (before or after <see cref="_target"/>).
        /// </summary>
        /// <param name="instruction">The IL instruction to insert.</param>
        private void Insert(Instruction instruction)
        {
            if (_target == null)
                throw new WeavingException("Processor target cannot be null");

            Action<Instruction, Instruction> after = delegate (Instruction target, Instruction _instruction)
            {
                if (_simulation == null)
                    _processor.InsertAfter(target, _instruction);
                else
                    _simulation.InsertAfter(target, _instruction);
            };

            Action<Instruction, Instruction> before = delegate (Instruction target, Instruction _instruction)
            {
                if (_simulation == null)
                    _processor.InsertBefore(target, _instruction);
                else
                    _simulation.InsertBefore(target, _instruction);
            };


            if (AfterTarget)
                after(_target, instruction);
            else
                before(_target, instruction);
        }

        /// <summary>
        /// Emits a <c>call</c> to a static method. Use this when the target
        /// <paramref name="method"/> has no <c>this</c> parameter.
        /// </summary>
        /// <param name="method">The static <see cref="MethodInfo"/> to import and then call.</param>
        public Processor CallStatic(MethodInfo mi)
        {
            var method = ImportReference(mi);
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (method.HasThis)
                throw new WeavingException("Cannot static call an instance method");

            Insert(Instruction.Create(OpCodes.Call, method));
            return this;
        }

        /// <summary>
        /// Emits a <c>call</c> to a static method. Use this when the target
        /// <paramref name="method"/> has no <c>this</c> parameter.
        /// </summary>
        /// <param name="method">The static <see cref="MethodReference"/> to call.</param>
        public Processor CallStatic(MethodReference method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (method.HasThis)
                throw new WeavingException("Cannot static call an instance method");

            Insert(Instruction.Create(OpCodes.Call, method));
            return this;
        }

        /// <summary>
        /// Pushes the given constants onto the stack and then emits a static call.
        /// </summary>
        /// <param name="method">The static <see cref="MethodReference"/> to call.</param>
        /// <param name="args">
        ///   A sequence of constant values (int, long, float, double, string, bool)
        ///   to push before the call.
        /// </param>
        public Processor CallStatic(MethodReference method, params object[] args)
        {
            using (AutoSimulation)
            {
                foreach (var arg in args)
                    Push(arg);
                CallStatic(method);
            }
            return this;
        }

        /// <summary>
        /// Emits an instance call (<c>call</c> or <c>callvirt</c>) on an object.
        /// You must first load the instance, then the arguments (if any).
        /// </summary>
        /// <param name="method">The instance <see cref="MethodReference"/> to call.</param>
        /// <param name="instance">
        ///   A <see cref="ParameterDefinition"/> or <see cref="VariableDefinition"/>
        ///   representing the object whose method you’re invoking.
        /// </param>
        public Processor Call(MethodReference method, object instance)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (!method.HasThis || method.ExplicitThis)
                throw new WeavingException("Cannot instance call a static or explicit instance method");
            using (AutoSimulation)
            {
                if (instance is ParameterDefinition p)
                    LoadArg(p);
                else if (instance is VariableDefinition v)
                    LoadLocalVariable(v);
                else
                    throw new WeavingException($"Unsupported instance loader for type {instance.GetType()}");

                var opcode = method.Resolve()?.IsVirtual == true
                    ? OpCodes.Callvirt
                    : OpCodes.Call;
                Insert(Instruction.Create(opcode, method));
            }
            return this;
        }

        /// <summary>
        /// Loads an instance, pushes constants, then emits an instance call.
        /// </summary>
        /// <param name="method">The instance <see cref="MethodReference"/> to call.</param>
        /// <param name="instance">
        ///   A <see cref="ParameterDefinition"/> or <see cref="VariableDefinition"/>
        ///   for the object instance.
        /// </param>
        /// <param name="args">
        ///   Additional method arguments.
        /// </param>
        public Processor Call(MethodReference method, object instance, params object[] args)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (!method.HasThis || method.ExplicitThis)
                throw new WeavingException("Cannot instance call a static or explicit instance method");
            using (AutoSimulation)
            {
                if (instance is ParameterDefinition p)
                    LoadArg(p);
                else if (instance is VariableDefinition v)
                    LoadLocalVariable(v);
                else
                    throw new WeavingException($"Unsupported instance loader for type {instance.GetType()}");

                foreach (var arg in args)
                    Push(arg);

                var opcode = method.Resolve()?.IsVirtual == true
                    ? OpCodes.Callvirt
                    : OpCodes.Call;
                Insert(Instruction.Create(opcode, method));
            }
            return this;
        }

        /// <summary>
        /// Inserts a <c>pop</c> instruction, which removes the top item from the stack.
        /// </summary>
        public Processor Pop()
        {
            Insert(Instruction.Create(OpCodes.Pop));
            return this;
        }

        /// <summary>
        /// Pushes a constant of primitive type (int, long, float, double, string, bool)
        /// onto the evaluation stack.
        /// </summary>
        /// <typeparam name="T">The constant’s compile-time type.</typeparam>
        /// <param name="value">The value to push.</param>
        public Processor Push<T>(T value)
        {
            switch (value)
            {
                case int i: return PushInt(i);
                case long l: return PushLong(l);
                case float f: return PushFloat(f);
                case double d: return PushDouble(d);
                case string s: return PushString(s);
                case bool b: return PushInt(b ? 1 : 0);
                default:
                    throw new WeavingException(
                        $"Unsupported constant type {typeof(T)} in IL push");
            }
        }

        /// <summary>
        /// Pushes a 32-bit integer onto the stack, using the smallest encoding
        /// form available (<c>ldc.i4.0</c>…<c>ldc.i4.8</c>, <c>ldc.i4.s</c>, etc.).
        /// </summary>
        public Processor PushInt(int value)
        {
            switch (value)
            {
                case -1: Insert(Instruction.Create(OpCodes.Ldc_I4_M1)); break;
                case 0: Insert(Instruction.Create(OpCodes.Ldc_I4_0)); break;
                case 1: Insert(Instruction.Create(OpCodes.Ldc_I4_1)); break;
                case 2: Insert(Instruction.Create(OpCodes.Ldc_I4_2)); break;
                case 3: Insert(Instruction.Create(OpCodes.Ldc_I4_3)); break;
                case 4: Insert(Instruction.Create(OpCodes.Ldc_I4_4)); break;
                case 5: Insert(Instruction.Create(OpCodes.Ldc_I4_5)); break;
                case 6: Insert(Instruction.Create(OpCodes.Ldc_I4_6)); break;
                case 7: Insert(Instruction.Create(OpCodes.Ldc_I4_7)); break;
                case 8: Insert(Instruction.Create(OpCodes.Ldc_I4_8)); break;
                default:
                    if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                        Insert(Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)value));
                    else
                        Insert(Instruction.Create(OpCodes.Ldc_I4, value));
                    break;
            }

            return this;
        }

        /// <summary>
        /// Pushes a 64-bit integer constant (<c>ldc.i8</c>).
        /// </summary>
        public Processor PushLong(long value)
        {
            Insert(Instruction.Create(OpCodes.Ldc_I8, value));
            return this;
        }

        /// <summary>
        /// Pushes a single-precision float constant (<c>ldc.r4</c>).
        /// </summary>
        public Processor PushFloat(float value)
        {
            Insert(Instruction.Create(OpCodes.Ldc_R4, value));
            return this;
        }

        /// <summary>
        /// Pushes a double-precision float constant (<c>ldc.r8</c>).
        /// </summary>
        public Processor PushDouble(double value)
        {
            Insert(Instruction.Create(OpCodes.Ldc_R8, value));
            return this;
        }

        /// <summary>
        /// Pushes a string literal reference (<c>ldstr</c>).
        /// </summary>
        public Processor PushString(string value)
        {
            Insert(Instruction.Create(OpCodes.Ldstr, value));
            return this;
        }

        /// <summary>
        /// Pushes a null reference (<c>ldnull</c>).
        /// </summary>
        public Processor PushNull()
        {
            Insert(Instruction.Create(OpCodes.Ldnull));
            return this;
        }

        /// <summary>
        /// Loads an argument (by its <paramref name="param"/>) onto the stack
        /// (<c>ldarg</c>).
        /// </summary>
        public Processor LoadArg(ParameterDefinition param)
        {
            Insert(Instruction.Create(OpCodes.Ldarg, param));
            return this;
        }

        /// <summary>
        /// Loads argument #0 (<c>this</c> for instance methods) onto the stack.
        /// </summary>
        public Processor LoadArg0()
        {
            Insert(Instruction.Create(OpCodes.Ldarg_0));
            return this;
        }

        /// <summary>Loads argument #1 onto the stack (<c>ldarg.1</c>).</summary>
        public Processor LoadArg1()
        {
            Insert(Instruction.Create(OpCodes.Ldarg_1));
            return this;
        }

        /// <summary>Loads argument #2 onto the stack (<c>ldarg.2</c>).</summary>
        public Processor LoadArg2()
        {
            Insert(Instruction.Create(OpCodes.Ldarg_2));
            return this;
        }

        /// <summary>Loads argument #3 onto the stack (<c>ldarg.3</c>).</summary>
        public Processor LoadArg3()
        {
            Insert(Instruction.Create(OpCodes.Ldarg_3));
            return this;
        }

        /// <summary>
        /// Loads the address of an argument (for ref/out parameters) (<c>ldarga</c>).
        /// </summary>
        public Processor LoadArgAddress(ParameterDefinition param)
        {
            Insert(Instruction.Create(OpCodes.Ldarga, param));
            return this;
        }

        /// <summary>
        /// Stores the top-of-stack value into an argument slot (<c>starg</c>).
        /// </summary>
        public Processor StoreArg(ParameterDefinition param)
        {
            Insert(Instruction.Create(OpCodes.Starg, param));
            return this;
        }

        /// <summary>
        /// Loads a local variable onto the stack (<c>ldloc</c>).
        /// </summary>
        public Processor LoadLocalVariable(VariableDefinition variable)
        {
            Insert(Instruction.Create(OpCodes.Ldloc, variable));
            return this;
        }

        /// <summary>Loads local variable #0 (<c>ldloc.0</c>).</summary>
        public Processor LoadLoc0()
        {
            Insert(Instruction.Create(OpCodes.Ldloc_0));
            return this;
        }

        /// <summary>Loads local variable #1 (<c>ldloc.1</c>).</summary>
        public Processor LoadLoc1()
        {
            Insert(Instruction.Create(OpCodes.Ldloc_1));
            return this;
        }

        /// <summary>Loads local variable #2 (<c>ldloc.2</c>).</summary>
        public Processor LoadLoc2()
        {
            Insert(Instruction.Create(OpCodes.Ldloc_2));
            return this;
        }

        /// <summary>Loads local variable #3 (<c>ldloc.3</c>).</summary>
        public Processor LoadLoc3()
        {
            Insert(Instruction.Create(OpCodes.Ldloc_3));
            return this;
        }

        /// <summary>
        /// Pops a value then stores it into a local variable (<c>stloc</c>).
        /// </summary>
        public Processor PopValueAndStore(VariableDefinition variable)
        {
            Insert(Instruction.Create(OpCodes.Stloc, variable));
            return this;
        }

        /// <summary>Stores into local variable #0 (<c>stloc.0</c>).</summary>
        public Processor StoreLoc0()
        {
            Insert(Instruction.Create(OpCodes.Stloc_0));
            return this;
        }

        /// <summary>Stores into local variable #1 (<c>stloc.1</c>).</summary>
        public Processor StoreLoc1()
        {
            Insert(Instruction.Create(OpCodes.Stloc_1));
            return this;
        }

        /// <summary>Stores into local variable #2 (<c>stloc.2</c>).</summary>
        public Processor StoreLoc2()
        {
            Insert(Instruction.Create(OpCodes.Stloc_2));
            return this;
        }

        /// <summary>Stores into local variable #3 (<c>stloc.3</c>).</summary>
        public Processor StoreLoc3()
        {
            Insert(Instruction.Create(OpCodes.Stloc_3));
            return this;
        }

        /// <summary>
        /// Creates a new object by calling its constructor (<c>newobj</c>).
        /// </summary>
        /// <param name="constructor">
        ///   The <see cref="MethodReference"/> for the type’s constructor.
        /// </param>
        public Processor NewInstance(MethodReference constructor)
        {
            Insert(Instruction.Create(OpCodes.Newobj, constructor));
            return this;
        }

        /// <summary>
        /// Boxes a value type so it can be used as an object
        /// (<c>box</c>).
        /// </summary>
        public Processor Box(TypeReference type)
        {
            Insert(Instruction.Create(OpCodes.Box, type));
            return this;
        }

        /// <summary>
        /// Unboxes a reference to a value type (<c>unbox.any</c>).
        /// </summary>
        public Processor Unbox(TypeReference type)
        {
            Insert(Instruction.Create(OpCodes.Unbox_Any, type));
            return this;
        }

        /// <summary>
        /// Casts an object reference to a more specific class (<c>castclass</c>).
        /// </summary>
        public Processor CastClass(TypeReference type)
        {
            Insert(Instruction.Create(OpCodes.Castclass, type));
            return this;
        }

        /// <summary>
        /// Throws whatever exception object is on the stack (<c>throw</c>).
        /// </summary>
        public Processor ThrowException()
        {
            Insert(Instruction.Create(OpCodes.Throw));
            return this;
        }

        /// <summary>
        /// Returns from the current method/function (<c>ret</c>).
        /// </summary>
        public Processor Return()
        {
            Insert(Instruction.Create(OpCodes.Ret));
            return this;
        }

        /// <summary>
        /// Duplicates the top value on the stack (<c>dup</c>).
        /// </summary>
        public Processor Duplicate()
        {
            Insert(Instruction.Create(OpCodes.Dup));
            return this;
        }

        /// <summary>
        /// Inserts a no-operation instruction (<c>nop</c>) that does nothing.
        /// </summary>
        public Processor Nop()
        {
            Insert(Instruction.Create(OpCodes.Nop));
            return this;
        }

        /// <summary>
        /// Unconditional branch to <paramref name="target"/> (<c>br</c>).
        /// </summary>
        public Processor JumpTo(Instruction target)
        {
            Insert(Instruction.Create(OpCodes.Br, target));
            return this;
        }

        /// <summary>
        /// Short-form unconditional branch (<c>br.s</c>) when the jump is close.
        /// </summary>
        public Processor JumpShortTo(Instruction target)
        {
            Insert(Instruction.Create(OpCodes.Br_S, target));
            return this;
        }

        /// <summary>
        /// Branches if the top of stack is non-null (reference) or non-zero (numeric)
        /// (<c>brtrue</c>).
        /// </summary>
        public Processor IfNotNull(Instruction target)
        {
            Insert(Instruction.Create(OpCodes.Brtrue, target));
            return this;
        }

        /// <summary>
        /// Short-form <c>brtrue.s</c> when the jump is close.
        /// </summary>
        public Processor IfNotNullShort(Instruction target)
        {
            Insert(Instruction.Create(OpCodes.Brtrue_S, target));
            return this;
        }

        /// <summary>
        /// Branches if the top of stack is null (reference) or zero (numeric)
        /// (<c>brfalse</c>).
        /// </summary>
        public Processor IfNull(Instruction target)
        {
            Insert(Instruction.Create(OpCodes.Brfalse, target));
            return this;
        }

        /// <summary>
        /// Short-form <c>brfalse.s</c> when the jump is close.
        /// </summary>
        public Processor IfNullShort(Instruction target)
        {
            Insert(Instruction.Create(OpCodes.Brfalse_S, target));
            return this;
        }

        /// <summary>Adds the top two values on the stack (<c>add</c>).</summary>
        public Processor Add()
        {
            Insert(Instruction.Create(OpCodes.Add));
            return this;
        }

        /// <summary>Subtracts the top value from the second (<c>sub</c>).</summary>
        public Processor Sub()
        {
            Insert(Instruction.Create(OpCodes.Sub));
            return this;
        }

        /// <summary>Multiplies the top two values (<c>mul</c>).</summary>
        public Processor Mul()
        {
            Insert(Instruction.Create(OpCodes.Mul));
            return this;
        }

        /// <summary>Divides the second value by the top (<c>div</c>).</summary>
        public Processor Div()
        {
            Insert(Instruction.Create(OpCodes.Div));
            return this;
        }

        /// <summary>Computes the remainder of the division (<c>rem</c>).</summary>
        public Processor Rem()
        {
            Insert(Instruction.Create(OpCodes.Rem));
            return this;
        }

        /// <summary>Bitwise AND of top two values (<c>and</c>).</summary>
        public Processor And()
        {
            Insert(Instruction.Create(OpCodes.And));
            return this;
        }

        /// <summary>Bitwise OR of top two values (<c>or</c>).</summary>
        public Processor Or()
        {
            Insert(Instruction.Create(OpCodes.Or));
            return this;
        }

        /// <summary>Bitwise XOR of top two values (<c>xor</c>).</summary>
        public Processor Xor()
        {
            Insert(Instruction.Create(OpCodes.Xor));
            return this;
        }

        /// <summary>Shifts the second value left by the top (<c>shl</c>).</summary>
        public Processor ShiftLeft()
        {
            Insert(Instruction.Create(OpCodes.Shl));
            return this;
        }

        /// <summary>Shifts the second value right by the top (<c>shr</c>).</summary>
        public Processor ShiftRight()
        {
            Insert(Instruction.Create(OpCodes.Shr));
            return this;
        }

        /// <summary>Bitwise NOT of the top value (<c>not</c>).</summary>
        public Processor Not()
        {
            Insert(Instruction.Create(OpCodes.Not));
            return this;
        }

        /// <summary>Compares equality of the top two values (<c>ceq</c>).</summary>
        public Processor CompareEqual()
        {
            Insert(Instruction.Create(OpCodes.Ceq));
            return this;
        }

        /// <summary>Compares if second &gt; top (<c>cgt</c>).</summary>
        public Processor CompareGreater()
        {
            Insert(Instruction.Create(OpCodes.Cgt));
            return this;
        }

        /// <summary>Compares if second &lt; top (<c>clt</c>).</summary>
        public Processor CompareLess()
        {
            Insert(Instruction.Create(OpCodes.Clt));
            return this;
        }

        /// <summary>Loads an instance field (<c>ldfld</c>).</summary>
        public Processor LoadField(FieldReference field)
        {
            Insert(Instruction.Create(OpCodes.Ldfld, field));
            return this;
        }

        /// <summary>Stores into an instance field (<c>stfld</c>).</summary>
        public Processor StoreField(FieldReference field)
        {
            Insert(Instruction.Create(OpCodes.Stfld, field));
            return this;
        }

        /// <summary>Loads a static field (<c>ldsfld</c>).</summary>
        public Processor LoadStaticField(FieldReference field)
        {
            Insert(Instruction.Create(OpCodes.Ldsfld, field));
            return this;
        }

        /// <summary>Stores into a static field (<c>stsfld</c>).</summary>
        public Processor StoreStaticField(FieldReference field)
        {
            Insert(Instruction.Create(OpCodes.Stsfld, field));
            return this;
        }

        /// <summary>Creates a new single-dimensional array (<c>newarr</c>).</summary>
        public Processor NewArray(TypeReference type)
        {
            Insert(Instruction.Create(OpCodes.Newarr, type));
            return this;
        }

        /// <summary>Loads an element of known type (<c>ldelem.any</c>).</summary>
        public Processor LoadElement(TypeReference type)
        {
            Insert(Instruction.Create(OpCodes.Ldelem_Any, type));
            return this;
        }

        /// <summary>Stores into an array element (<c>stelem.any</c>).</summary>
        public Processor StoreElement(TypeReference type)
        {
            Insert(Instruction.Create(OpCodes.Stelem_Any, type));
            return this;
        }

        /// <summary>Loads the address of an array element (<c>ldelema</c>).</summary>
        public Processor LoadElementAddress(TypeReference type)
        {
            Insert(Instruction.Create(OpCodes.Ldelema, type));
            return this;
        }

        /// <summary>
        /// Pushes a metadata token for a type, method, or field (<c>ldtoken</c>).
        /// </summary>
        public Processor LoadToken(TypeReference type)
        {
            Insert(Instruction.Create(OpCodes.Ldtoken, type));
            return this;
        }

        /// <summary>Pushes a metadata token for a method (<c>ldtoken</c>).</summary>
        public Processor LoadToken(MethodReference method)
        {
            Insert(Instruction.Create(OpCodes.Ldtoken, method));
            return this;
        }

        /// <summary>Pushes a metadata token for a field (<c>ldtoken</c>).</summary>
        public Processor LoadToken(FieldReference field)
        {
            Insert(Instruction.Create(OpCodes.Ldtoken, field));
            return this;
        }

        /// <summary>Loads an indirect 8-bit integer (<c>ldind.i1</c>).</summary>
        public Processor LoadIndirectI1()
        {
            Insert(Instruction.Create(OpCodes.Ldind_I1));
            return this;
        }

        /// <summary>Loads an indirect 16-bit integer (<c>ldind.i2</c>).</summary>
        public Processor LoadIndirectI2()
        {
            Insert(Instruction.Create(OpCodes.Ldind_I2));
            return this;
        }

        /// <summary>Loads an indirect 32-bit integer (<c>ldind.i4</c>).</summary>
        public Processor LoadIndirectI4()
        {
            Insert(Instruction.Create(OpCodes.Ldind_I4));
            return this;
        }

        /// <summary>Loads an indirect 64-bit integer (<c>ldind.i8</c>).</summary>
        public Processor LoadIndirectI8()
        {
            Insert(Instruction.Create(OpCodes.Ldind_I8));
            return this;
        }

        /// <summary>Loads an indirect single-precision float (<c>ldind.r4</c>).</summary>
        public Processor LoadIndirectR4()
        {
            Insert(Instruction.Create(OpCodes.Ldind_R4));
            return this;
        }

        /// <summary>Loads an indirect double-precision float (<c>ldind.r8</c>).</summary>
        public Processor LoadIndirectR8()
        {
            Insert(Instruction.Create(OpCodes.Ldind_R8));
            return this;
        }

        /// <summary>Stores indirectly an 8-bit integer (<c>stind.i1</c>).</summary>
        public Processor StoreIndirectI1()
        {
            Insert(Instruction.Create(OpCodes.Stind_I1));
            return this;
        }

        /// <summary>Stores indirectly a 16-bit integer (<c>stind.i2</c>).</summary>
        public Processor StoreIndirectI2()
        {
            Insert(Instruction.Create(OpCodes.Stind_I2));
            return this;
        }

        /// <summary>Stores indirectly a 32-bit integer (<c>stind.i4</c>).</summary>
        public Processor StoreIndirectI4()
        {
            Insert(Instruction.Create(OpCodes.Stind_I4));
            return this;
        }

        /// <summary>Stores indirectly a 64-bit integer (<c>stind.i8</c>).</summary>
        public Processor StoreIndirectI8()
        {
            Insert(Instruction.Create(OpCodes.Stind_I8));
            return this;
        }

        /// <summary>Stores indirectly a single-precision float (<c>stind.r4</c>).</summary>
        public Processor StoreIndirectR4()
        {
            Insert(Instruction.Create(OpCodes.Stind_R4));
            return this;
        }

        /// <summary>Stores indirectly a double-precision float (<c>stind.r8</c>).</summary>
        public Processor StoreIndirectR8()
        {
            Insert(Instruction.Create(OpCodes.Stind_R8));
            return this;
        }

        /// <summary>Converts the top value to 64-bit integer (<c>conv.i8</c>).</summary>
        public Processor ConvertToI8()
        {
            Insert(Instruction.Create(OpCodes.Conv_I8));
            return this;
        }

        /// <summary>Converts the top value to double (<c>conv.r8</c>).</summary>
        public Processor ConvertToR8()
        {
            Insert(Instruction.Create(OpCodes.Conv_R8));
            return this;
        }

        /// <summary>Unsigned convert to 32-bit integer (<c>conv.u4</c>).</summary>
        public Processor ConvertUnsignedToI4()
        {
            Insert(Instruction.Create(OpCodes.Conv_U4));
            return this;
        }

        /// <summary>Unsigned convert to 64-bit integer (<c>conv.u8</c>).</summary>
        public Processor ConvertUnsignedToI8()
        {
            Insert(Instruction.Create(OpCodes.Conv_U8));
            return this;
        }

        /// <summary>
        /// Exits a try/finally or try/catch block, jumping out of the protected region
        /// (<c>leave</c>).
        /// </summary>
        public Processor Leave(Instruction target)
        {
            Insert(Instruction.Create(OpCodes.Leave, target));
            return this;
        }

        /// <summary>Short-form leave (<c>leave.s</c>) when the jump is close.</summary>
        public Processor LeaveShort(Instruction target)
        {
            Insert(Instruction.Create(OpCodes.Leave_S, target));
            return this;
        }

        /// <summary>Rethrows the current exception (<c>rethrow</c>).</summary>
        public Processor Rethrow()
        {
            Insert(Instruction.Create(OpCodes.Rethrow));
            return this;
        }
    }
}
