using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using System.Runtime.Loader;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace Whitespace.net {
        internal class WSCompiler {
                public WSProgram Program { get; }
                public int StackHeight { get; private set; }
                private LocalBuilder swloc1, swloc2, heap, callstack, evstack;
                private readonly Dictionary<string, Label> labels;
                private readonly Dictionary<string, bool> definedLabels;
                private Label subret;
                private readonly List<Label> retTgt;

                public WSCompiler(WSProgram p) {
                        Program = p;
                        StackHeight = 0;
                        swloc1 = swloc2 = heap = callstack = evstack = null;
                        labels = new Dictionary<string, Label>();
                        definedLabels = new Dictionary<string, bool>();
                        retTgt = new List<Label>();
                }
                public int ParseNumber(string literal) {
                        var positive = literal[0] == ' ';
                        var value = 0;
                        if (literal.Length >= 32)
                                throw new Exception("Whitespace.NET: Overflow (>31 bits used)!");

                        for (var i = 1; i < literal.Length; i++)
                                value = (value << 1) + (literal[i] == ' ' ? 0 : 1);

                        return positive ? value : -value;
                }
                private Label ParseLabel(string s, ILGenerator ilg, bool defined) {
                        if (!labels.TryGetValue(s, out var label)) {
                                label = ilg.DefineLabel();
                                labels[s] = label;
                                definedLabels[s] = false;
                        }

                        if (defined)
                                definedLabels[s] = true;

                        return label;
                }
                public void Compile(ILGenerator ilg) {
                        MethodInfo PushM = typeof(Stack).GetMethod("Push", new Type[]{typeof(object)});
                        MethodInfo PopM = typeof(Stack).GetMethod("Pop", new Type[]{});
                        MethodInfo PeekM = typeof(Stack).GetMethod("Peek", new Type[]{});
                        Label l;
                        subret = ilg.DefineLabel();
                        evstack = ilg.DeclareLocal(typeof(Stack));
                        swloc1 = ilg.DeclareLocal(typeof(object));
                        swloc2 = ilg.DeclareLocal(typeof(object));
                        ilg.Emit(OpCodes.Newobj, typeof(Stack).GetConstructor(new Type[]{}));
                        ilg.Emit(OpCodes.Stloc, evstack);

                        for (int i = 0; i < Program.Instructions.Count; i++) {
                                Instruction instr = Program.Instructions[i];
                                switch (instr.Operation) {
                                        case Instruction.OpCode.push:
                                                ilg.Emit(OpCodes.Ldloc, evstack);
                                                ilg.Emit(OpCodes.Ldc_I4, ParseNumber(instr.Parameter!)); // FIXME: it handles only 32 bits!
                                                ilg.Emit(OpCodes.Box, typeof(int));
                                                ilg.EmitCall(OpCodes.Callvirt, PushM, null);
                                                StackHeight++;
                                                break;
					case Instruction.OpCode.dup:
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.Emit(OpCodes.Dup);
						ilg.EmitCall(OpCodes.Callvirt, PeekM, null);
						ilg.EmitCall(OpCodes.Callvirt, PushM, null);
                                                StackHeight++;
						break;
					case Instruction.OpCode.swap:
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Stloc, swloc1);

						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Stloc, swloc2);

						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.Emit(OpCodes.Ldloc, swloc1);
						ilg.EmitCall(OpCodes.Callvirt, PushM, null);

						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.Emit(OpCodes.Ldloc, swloc2);
						ilg.EmitCall(OpCodes.Callvirt, PushM, null);
						break;
					case Instruction.OpCode.pop:
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Pop);
                                                StackHeight--;
						break;
					case Instruction.OpCode.add:
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.Emit(OpCodes.Dup);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Add);
						ilg.Emit(OpCodes.Box, typeof(int));
						ilg.EmitCall(OpCodes.Callvirt, PushM, null);
                                                StackHeight--;
						break;
					case Instruction.OpCode.sub:
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.Emit(OpCodes.Dup);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Stloc, swloc1);
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Ldloc, swloc1);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Sub);
						ilg.Emit(OpCodes.Box, typeof(int));
						ilg.EmitCall(OpCodes.Callvirt, PushM, null);
                                                StackHeight--;
						break;
					case Instruction.OpCode.mul:
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.Emit(OpCodes.Dup);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Mul);
						ilg.Emit(OpCodes.Box, typeof(int));
						ilg.EmitCall(OpCodes.Callvirt, PushM, null);
                                                StackHeight--;
						break;
					case Instruction.OpCode.div:
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.Emit(OpCodes.Dup);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Stloc, swloc1);
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Ldloc, swloc1);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Div);
						ilg.Emit(OpCodes.Box, typeof(int));
						ilg.EmitCall(OpCodes.Callvirt, PushM, null);
                                                StackHeight--;
						break;
					case Instruction.OpCode.mod:
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.Emit(OpCodes.Dup);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Stloc, swloc1);
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Ldloc, swloc1);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Rem);
						ilg.Emit(OpCodes.Box, typeof(int));
                                                ilg.EmitCall(OpCodes.Callvirt, PushM, null);
                                                StackHeight--;
						break;
					case Instruction.OpCode.sth:
						if (heap == null) {
							heap = ilg.DeclareLocal(typeof(Hashtable));
							ilg.Emit(OpCodes.Newobj, typeof(Hashtable).GetConstructor(new Type[]{}));
							ilg.Emit(OpCodes.Stloc, heap);
						}
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Stloc, swloc2); // value
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Stloc, swloc1); // address
						ilg.Emit(OpCodes.Ldloc, heap);
						ilg.Emit(OpCodes.Ldloc, swloc1);
						ilg.Emit(OpCodes.Ldloc, swloc2);
						ilg.EmitCall(OpCodes.Callvirt, typeof(Hashtable).GetMethod("set_Item", new Type[]{ typeof(object), typeof(object) }), null);
                                                StackHeight -= 2;
						break;
					case Instruction.OpCode.ldh:
						if (heap == null) {
							heap = ilg.DeclareLocal(typeof(Hashtable));
							ilg.Emit(OpCodes.Newobj, typeof(Hashtable).GetConstructor(new Type[]{}));
							ilg.Emit(OpCodes.Stloc, heap);
						}
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Stloc, swloc1); // address
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.Emit(OpCodes.Ldloc, heap);
						ilg.Emit(OpCodes.Ldloc, swloc1);
						ilg.EmitCall(OpCodes.Callvirt, typeof(Hashtable).GetMethod("get_Item", new Type[]{ typeof(object) }), null);
						ilg.EmitCall(OpCodes.Callvirt, PushM, null);
						break;
                                        case Instruction.OpCode.mrk:
                                                l = ParseLabel(instr.Parameter!, ilg, true);
                                                ilg.MarkLabel(l);
                                                break;
                                        case Instruction.OpCode.call:
                                                if (callstack == null) {
                                                        callstack = ilg.DeclareLocal(typeof(Stack));
							ilg.Emit(OpCodes.Newobj, typeof(Stack).GetConstructor(new Type[]{}));
							ilg.Emit(OpCodes.Stloc, callstack);
						}
                                                ilg.Emit(OpCodes.Ldloc, callstack);
                                                ilg.Emit(OpCodes.Ldc_I4, retTgt.Count);
                                                ilg.Emit(OpCodes.Box, typeof(int));
                                                ilg.EmitCall(OpCodes.Callvirt, PushM, null);
                                                ilg.Emit(OpCodes.Br, ParseLabel(instr.Parameter!, ilg, false));
                                                l = ilg.DefineLabel();
                                                retTgt.Add(l);
                                                ilg.MarkLabel(l);
                                                break;
                                        case Instruction.OpCode.jmp:
                                                l = ParseLabel(instr.Parameter!, ilg, false);
                                                ilg.Emit(OpCodes.Br, l);
                                                break;
                                        case Instruction.OpCode.jz:
                                                l = ParseLabel(instr.Parameter!, ilg, false);
                                                ilg.Emit(OpCodes.Ldloc, evstack);
                                                ilg.EmitCall(OpCodes.Callvirt, PopM, null);
                                                ilg.Emit(OpCodes.Unbox, typeof(int));
                                                ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Ldc_I4_0);
						ilg.Emit(OpCodes.Beq, l);
                                                StackHeight--;
						break;
                                        case Instruction.OpCode.jlz:
                                                l = ParseLabel(instr.Parameter!, ilg, false);
                                                ilg.Emit(OpCodes.Ldloc, evstack);
                                                ilg.EmitCall(OpCodes.Callvirt, PopM, null);
                                                ilg.Emit(OpCodes.Unbox, typeof(int));
                                                ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Ldc_I4_0);
						ilg.Emit(OpCodes.Blt, l);
                                                StackHeight--;
						break;
					case Instruction.OpCode.ret:
						ilg.Emit(OpCodes.Br, subret);
						break;
					case Instruction.OpCode.end:
						ilg.Emit(OpCodes.Ret);
                                                StackHeight = 0;
						break;
					case Instruction.OpCode.wrc:
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.Emit(OpCodes.Conv_U2);
						ilg.EmitCall(OpCodes.Call, typeof(Console).GetMethod("Write", new Type[]{ typeof(char) }), null);
                                                StackHeight--;
						break;
					case Instruction.OpCode.wri:
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.Emit(OpCodes.Unbox, typeof(int));
						ilg.Emit(OpCodes.Ldind_I4);
						ilg.EmitCall(OpCodes.Call, typeof(Console).GetMethod("Write", new Type[]{ typeof(int) }), null);
                                                StackHeight--;
						break;
					case Instruction.OpCode.rdc:
						if (heap == null) {
							heap = ilg.DeclareLocal(typeof(Hashtable));
							ilg.Emit(OpCodes.Newobj, typeof(Hashtable).GetConstructor(new Type[]{}));
							ilg.Emit(OpCodes.Stloc, heap);
						}
						// FIXME: it doesn't read exacly a char... ;-)
						ilg.Emit(OpCodes.Ldloc, heap);
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.EmitCall(OpCodes.Call, typeof(Console).GetMethod("Read", new Type[]{}), null);
						ilg.Emit(OpCodes.Box, typeof(int));
						ilg.EmitCall(OpCodes.Callvirt, typeof(Hashtable).GetMethod("set_Item", new Type[]{ typeof(object), typeof(object) }), null);
                                                StackHeight--;
						break;
					case Instruction.OpCode.rdi:
						if (heap == null) {
							heap = ilg.DeclareLocal(typeof(Hashtable));
							ilg.Emit(OpCodes.Newobj, typeof(Hashtable).GetConstructor(new Type[]{}));
							ilg.Emit(OpCodes.Stloc, heap);
						}
						ilg.Emit(OpCodes.Ldloc, heap);
						ilg.Emit(OpCodes.Ldloc, evstack);
						ilg.EmitCall(OpCodes.Callvirt, PopM, null);
						ilg.EmitCall(OpCodes.Call, typeof(Console).GetMethod("ReadLine"), null);
						ilg.EmitCall(OpCodes.Call, typeof(int).GetMethod("Parse", new Type[]{typeof(string)}), null);
						ilg.Emit(OpCodes.Box, typeof(int));
						ilg.EmitCall(OpCodes.Callvirt, typeof(Hashtable).GetMethod("set_Item", new Type[]{ typeof(object), typeof(object) }), null);
                                                StackHeight--;
						break;
				}
			}
                        foreach (var kvp in definedLabels)
                                if (!kvp.Value)
                                        throw new Exception($"Whitespace.NET: undefined '{kvp.Key}' label!");

                        if (retTgt.Count > 0) { // Emit the switch instruction
                                Label[] tgts = retTgt.ToArray();
                                ilg.MarkLabel(subret);
                                ilg.Emit(OpCodes.Ldloc, callstack);
                                ilg.EmitCall(OpCodes.Callvirt, typeof(Stack).GetMethod("Pop", new Type[]{}), null);
                                ilg.Emit(OpCodes.Unbox, typeof(int));
                                ilg.Emit(OpCodes.Ldind_I4);
				ilg.Emit(OpCodes.Switch, tgts);
			}
			ilg.Emit(OpCodes.Ret);
		}
	}

	class MainCompiler {
        static void WriteRuntimeConfig(string outputPath)
        {
            var ver = Environment.Version;  // es. 9.0.0
            var tfm = $"net{ver.Major}.0";

            var config = new
            {
                runtimeOptions = new
                {
                    tfm,
                    framework = new
                    {
                        name = "Microsoft.NETCore.App",
                        version = $"{ver.Major}.{ver.Minor}.0"
                    }
                }
            };

            var text = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputPath, text);
        }

        static void Main(string[] args) {
            Console.WriteLine("Whitespace.NET compiler v.0.1\nAntonio Cisternino (C)2003\n\n");
			if (args.Length != 2 && args.Length != 4) {
				Console.WriteLine("Usage: wsc FileIn FileOut");
				return;
			}
			var fin = args.Length == 2 ? args[0] : args[args.Length - 2];
			var fout = args.Length == 2 ? args[1] : args[args.Length - 1];
            var src = new FileStream(fin, FileMode.Open, FileAccess.Read);
			var tok = new Tokenizer(new BinaryReader(src));
			var p = new Parser(tok);
			var an = new AssemblyName() { Name = fout };
            var ab = new PersistedAssemblyBuilder(an, typeof(object).Assembly);

			var mb = ab.DefineDynamicModule(fout);
			var tb = mb.DefineType("WSMain", TypeAttributes.Class | TypeAttributes.Public);
			var mtb = tb.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { typeof(string[]) });
			var ilg = mtb.GetILGenerator();

			var prg = new WSProgram();
			p.Parse(prg);
			var comp = new WSCompiler(prg);
			comp.Compile(ilg);

			tb.CreateType();
            var metadata = ab.GenerateMetadata(out BlobBuilder ilStream, out BlobBuilder mappedFieldData);
            var peBuilder = new ManagedPEBuilder(
			    header: PEHeaderBuilder.CreateExecutableHeader(), // EXE console
				metadataRootBuilder: new MetadataRootBuilder(metadata),
				ilStream: ilStream,
				mappedFieldData: mappedFieldData,
				entryPoint: MetadataTokens.MethodDefinitionHandle(mtb.MetadataToken)
				);
            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);
            using var fs = new FileStream(fout + ".dll", FileMode.Create, FileAccess.Write);
            peBlob.WriteContentTo(fs);
            WriteRuntimeConfig(fout + ".runtimeconfig.json");
        }
	}
}
