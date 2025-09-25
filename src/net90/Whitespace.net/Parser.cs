using System;
using System.Collections;
using System.IO;

namespace Whitespace.net {
	/// <summary>
	/// Stack Manipulation (IMP: [Space])
	/// Command					Parameters	Meaning 
	/// [Space]					Number			Push the number onto the stack 
	/// [LF][Space]			-						Duplicate the top item on the stack 
	/// [LF][Tab]				-						Swap the top two items on the stack 
	/// [LF][LF]				-						Discard the top item on the stack 
	/// 
	/// Arithmetic (IMP: [Tab][Space])
	/// Command					Parameters	Meaning 
	/// [Space][Space]	-						Addition 
	/// [Space][Tab]		-						Subtraction 
	/// [Space][LF]			-						Multiplication 
	/// [Tab][Space]		-						Integer Division 
	/// [Tab][Tab]			-						Modulo
	/// 
	/// Heap Access (IMP: [Tab][Tab])
	/// Command					Parameters	Meaning 
	/// [Space]					-						Store 
	/// [Tab]						-						Retrieve
	/// 
	/// Flow Control (IMP: [LF])
	/// Command					Parameters	Meaning 
	/// [Space][Space]	Label				Mark a location in the program 
	/// [Space][Tab]		Label				Call a subroutine 
	/// [Space][LF]			Label				Jump unconditionally to a label 
	/// [Tab][Space]		Label				Jump to a label if the top of the stack is zero 
	/// [Tab][Tab]			Label				Jump to a label if the top of the stack is negative 
	/// [Tab][LF]				-						End a subroutine and transfer control back to the caller 
	/// [LF][LF]				-						End the program
	/// 
	/// I/O (IMP: [Tab][LF])
	/// Command					Parameters	Meaning 
	/// [Space][Space]	-						Output the character at the top of the stack 
	/// [Space][Tab]		-						Output the number at the top of the stack 
	/// [Tab][Space]		-						Read a character and place it in the location given by the top of the stack 
	/// [Tab][Tab]			-						Read a number and place it in the location given by the top of the stack 
	/// </summary>

	internal class Instruction {
		public enum OpCode {
			push,
			dup,
			swap,
			pop,
			add,
			sub,
			mul,
			div,
			mod,
			sth,
			ldh,
			mrk,
			call,
			jmp,
			jz,
			jlz,
			ret,
			end,
			wrc,
			wri,
			rdc,
			rdi
		}
		public int tgt;
		public string param;
		public OpCode op;

		public Instruction(OpCode o, string p) {
			op = o;
			param = p;
			tgt = -1;
		}
		public Instruction(OpCode o) {
			op = o;
			param = null;
			tgt = -1;
		}
		public Instruction(OpCode o, int t) {
			op = o;
			param = null;
			tgt = t;
		}
	}

	internal class WSProgram {
		public ArrayList Instructions;
		public Hashtable Targets;

		public WSProgram() {
			Instructions = new ArrayList();
			Targets = new Hashtable();
		}
	}

	class Tokenizer {
		public enum Token {
			SPACE = 0x20,
			TAB = 0x09,
			LF = 0x0A,
			EOF = 0
		}
		BinaryReader inp;
		public int Line;

		public Tokenizer(BinaryReader s) {
			inp = s;
			Line = 1;
		}

		public Token Next() {
			while (inp.BaseStream.Position < inp.BaseStream.Length) {
				int c = inp.Read();
				switch ((Token)c) {
					case Token.SPACE:
					case Token.TAB:
						return (Token)c;
					case Token.LF:
						Line++;
						return Token.LF;
					default:
						continue;
				}
			}
			return Token.EOF;
		}
	}

	class Parser {
		private Tokenizer tok;
		
		public Parser(Tokenizer t) {
			tok = t;
		}

		private string ParseLiteral() {
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			Tokenizer.Token t;
			while ((t = tok.Next()) != Tokenizer.Token.LF) {
				if (t == Tokenizer.Token.EOF)
					throw new Exception(string.Format("Unexpected EOF at line: {0}", tok.Line));
				sb.Append(t == Tokenizer.Token.SPACE ? ' ' : '\t');
			}
			return sb.ToString();
		}

		private void ParseStack(WSProgram p) {
			switch (tok.Next()) {
				case Tokenizer.Token.SPACE:
					p.Instructions.Add(new Instruction(Instruction.OpCode.push, ParseLiteral()));
					break;
				case Tokenizer.Token.LF:
				switch (tok.Next()) {
					case Tokenizer.Token.SPACE:
						p.Instructions.Add(new Instruction(Instruction.OpCode.dup));
						break;
					case Tokenizer.Token.TAB:
						p.Instructions.Add(new Instruction(Instruction.OpCode.swap));
						break;
					case Tokenizer.Token.LF:
						p.Instructions.Add(new Instruction(Instruction.OpCode.pop));
						break;
					default:
						throw new Exception(string.Format("Whitespace.NET ({0}): Unexpected end of file!", tok.Line));
				}
					break;
				default:
					throw new Exception(string.Format("Whitespace.NET ({0}): Wrong stack operation!", tok.Line));
			}
		}

		private void ParseControlFlow(WSProgram p) {
			string l;
			switch (tok.Next()) {
				case Tokenizer.Token.SPACE:
				switch (tok.Next()) {
					case Tokenizer.Token.SPACE:
						l = ParseLiteral();
						p.Targets[l] = p.Instructions.Count;
						p.Instructions.Add(new Instruction(Instruction.OpCode.mrk, l));
						break;
					case Tokenizer.Token.TAB:
						p.Instructions.Add(new Instruction(Instruction.OpCode.call, ParseLiteral()));
						break;
					case Tokenizer.Token.LF:
						p.Instructions.Add(new Instruction(Instruction.OpCode.jmp, ParseLiteral()));
						break;
					default:
						throw new Exception(string.Format("Whitespace.NET ({0}): Unexpected end of file!", tok.Line));
				}
					break;
				case Tokenizer.Token.TAB:
				switch (tok.Next()) {
					case Tokenizer.Token.SPACE:
						p.Instructions.Add(new Instruction(Instruction.OpCode.jz, ParseLiteral()));
						break;
					case Tokenizer.Token.TAB:
						p.Instructions.Add(new Instruction(Instruction.OpCode.jlz, ParseLiteral()));
						break;
					case Tokenizer.Token.LF:
						p.Instructions.Add(new Instruction(Instruction.OpCode.ret));
						break;
					default:
						throw new Exception(string.Format("Whitespace.NET ({0}): Unexpected end of file!", tok.Line));
				}
					break;
				case Tokenizer.Token.LF:
					if (tok.Next() == Tokenizer.Token.LF) 
						p.Instructions.Add(new Instruction(Instruction.OpCode.end));
					else
						throw new Exception(string.Format("Whitespace.NET ({0}): Wrong control flow operation!", tok.Line));
					break;
				default:
					throw new Exception(string.Format("Whitespace.NET ({0}): Unexpected end of file!", tok.Line));
			}
		}

		private void ParseArithmetic(WSProgram p) {
			switch (tok.Next()) {
				case Tokenizer.Token.SPACE:
				switch (tok.Next()) {
					case Tokenizer.Token.SPACE:
						p.Instructions.Add(new Instruction(Instruction.OpCode.add));
						break;
					case Tokenizer.Token.TAB:
						p.Instructions.Add(new Instruction(Instruction.OpCode.sub));
						break;
					case Tokenizer.Token.LF:
						p.Instructions.Add(new Instruction(Instruction.OpCode.mul));
						break;
					default:
						throw new Exception(string.Format("Whitespace.NET ({0}): Unexpected end of file!", tok.Line));
				}
					break;
				case Tokenizer.Token.TAB:
				switch (tok.Next()) {
					case Tokenizer.Token.SPACE:
						p.Instructions.Add(new Instruction(Instruction.OpCode.div));
						break;
					case Tokenizer.Token.TAB:
						p.Instructions.Add(new Instruction(Instruction.OpCode.mod));
						break;
					default:
						throw new Exception(string.Format("Whitespace.NET ({0}): Unknown arithmetic operation!", tok.Line));
				}
					break;
				default:
					throw new Exception(string.Format("Whitespace.NET ({0}): Wrong arithmetic operation!", tok.Line));
			}
		}
	
		private void ParseHeap(WSProgram p) {
			switch (tok.Next()) {
				case Tokenizer.Token.SPACE:
					p.Instructions.Add(new Instruction(Instruction.OpCode.sth));
					break;
				case Tokenizer.Token.TAB:
					p.Instructions.Add(new Instruction(Instruction.OpCode.ldh));
					break;
				default:
					throw new Exception(string.Format("Whitespace.NET ({0}): Wrong heap operation!", tok.Line));
			}
		}

		private void ParseIO(WSProgram p) {
			switch (tok.Next()) {
				case Tokenizer.Token.SPACE:
				switch (tok.Next()) {
					case Tokenizer.Token.SPACE:
						p.Instructions.Add(new Instruction(Instruction.OpCode.wrc));
						break;
					case Tokenizer.Token.TAB:
						p.Instructions.Add(new Instruction(Instruction.OpCode.wri));
						break;
					default:
						throw new Exception(string.Format("Whitespace.NET ({0}): Wrong I/O operation!", tok.Line));
				}
					break;
				case Tokenizer.Token.TAB:
				switch (tok.Next()) {
					case Tokenizer.Token.SPACE:
						p.Instructions.Add(new Instruction(Instruction.OpCode.rdc));
						break;
					case Tokenizer.Token.TAB:
						p.Instructions.Add(new Instruction(Instruction.OpCode.rdi));
						break;
					default:
						throw new Exception(string.Format("Whitespace.NET ({0}): Wrong I/O operation!", tok.Line));
				}
					break;
				default:
					throw new Exception(string.Format("Whitespace.NET ({0}): Wrong I/O operation!", tok.Line));
			}
		}

		public void Parse(WSProgram p) {
			for(;;) {
				switch (tok.Next()) {
					case Tokenizer.Token.SPACE:
						ParseStack(p);
						break;
					case Tokenizer.Token.LF:
						ParseControlFlow(p);
						break;
					case Tokenizer.Token.TAB:
					switch (tok.Next()) {
						case Tokenizer.Token.SPACE:
							ParseArithmetic(p);
							break;
						case Tokenizer.Token.TAB:
							ParseHeap(p);
							break;
						case Tokenizer.Token.LF:
							ParseIO(p);
							break;
						default:
							throw new Exception(string.Format("Unexpected EOF at line: {0}", tok.Line));
					}
						break;
					default:
						return;
				}
			}
		}
	}

}
