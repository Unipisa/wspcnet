using System;
using System.IO;
using Whitespace.net;

namespace dews {
	class DeWSMain {
		public static int ParseNumber(string s) {
			bool pos = s[0] == ' ';
			int num = 0;
			if (s.Length == 32)
				throw new Exception("Whitespace.NET: Overflow (>31 bits used)!");

			for (int i = 1; i < s.Length; i++)
				if (s[i] == ' ')
					num = num << 1;
				else
					num = (num << 1) + 1;
			return pos ? num : -num;
		}
		static void Main(string[] args) {
			Console.WriteLine("Whitespace.NET DeWhiteSpace v.0.1\nAntonio Cisternino (C)2003\n\n");
			if (args.Length != 1) {
				Console.WriteLine("Usage: dews FileIn");
				return;
			}
			FileStream src = new FileStream(args[0], FileMode.Open, FileAccess.Read);
			Tokenizer tok = new Tokenizer(new BinaryReader(src));
			Parser p = new Parser(tok);

			WSProgram prg = new WSProgram();
			p.Parse(prg);

			for (int i = 0; i < prg.Instructions.Count; i++) {
				Instruction instr = prg.Instructions[i] as Instruction;
				string par = "";

				if (instr.param != null) {
					if ((int)instr.op >= (int)Instruction.OpCode.mrk && (int)instr.op <= (int)instr.op)
						par = instr.param.Replace(' ', 's').Replace('\x09', 't');
					else
						par = ParseNumber(instr.param).ToString();
				}
				Console.WriteLine("{0} {1}", instr.op.ToString(), par);
			}
		}
	}
}
