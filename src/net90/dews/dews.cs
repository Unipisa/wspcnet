using System;
using System.IO;
using Whitespace.net;

namespace dews {
        class DeWSMain {
                public static int ParseNumber(string literal) {
                        var positive = literal[0] == ' ';
                        var number = 0;
                        if (literal.Length == 32)
                                throw new InvalidOperationException("Whitespace.NET: Overflow (>31 bits used)!");

                        for (var i = 1; i < literal.Length; i++)
                                number = (number << 1) + (literal[i] == ' ' ? 0 : 1);

                        return positive ? number : -number;
                }
                static void Main(string[] args) {
                        Console.WriteLine("Whitespace.NET DeWhiteSpace v.0.1\nAntonio Cisternino (C)2003\n\n");
                        if (args.Length != 1) {
                                Console.WriteLine("Usage: dews FileIn");
                                return;
                        }
                        using var src = new FileStream(args[0], FileMode.Open, FileAccess.Read);
                        var tok = new Tokenizer(new BinaryReader(src));
                        var p = new Parser(tok);

                        var prg = new WSProgram();
                        p.Parse(prg);

                        for (var i = 0; i < prg.Instructions.Count; i++) {
                                var instr = prg.Instructions[i];
                                var par = string.Empty;

                                if (instr.Parameter != null) {
                                        if (instr.Operation is >= Instruction.OpCode.mrk and <= Instruction.OpCode.jlz)
                                                par = instr.Parameter.Replace(' ', 's').Replace('\x09', 't');
                                        else
                                                par = ParseNumber(instr.Parameter).ToString();
                                }
                                Console.WriteLine("{0} {1}", instr.Operation, par);
                        }
                }
        }
}
