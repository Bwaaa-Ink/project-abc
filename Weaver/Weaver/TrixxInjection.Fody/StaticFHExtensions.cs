using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TrixxInjection.Fody
{
    public static class StaticFileHandler
    {
        public static void AddLog(MethodBody body, string message, Instruction location = null, bool after = false)
        {
            var il = body.GetILProcessor();
            var writeRef =
                ModuleWeaver.That.TrixxInjection_Framework_ExpressionTree[
                    "TrixxInjection.FileHandling.StaticFileHandler", "Write"].M;
            location = location ?? body.Instructions.First();
            if (after)
            {
                il.InsertAfter(location, il.Create(OpCodes.Call, writeRef));
                il.InsertAfter(location, il.Create(OpCodes.Ldstr, message));
            }
            else
            {
                il.InsertBefore(location, il.Create(OpCodes.Ldstr, message));
                il.InsertBefore(location, il.Create(OpCodes.Call, writeRef));
            }
        }
    }
}
