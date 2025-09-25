using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        internal sealed class Instruction {
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

                public Instruction(OpCode operation, string? parameter) {
                        Operation = operation;
                        Parameter = parameter;
                }

                public Instruction(OpCode operation) {
                        Operation = operation;
                }

                public Instruction(OpCode operation, int target) {
                        Operation = operation;
                        Target = target;
                }

                public OpCode Operation { get; }

                public string? Parameter { get; }

                public int Target { get; set; } = -1;
        }

        internal sealed class WSProgram {
                public List<Instruction> Instructions { get; } = new();

                public Dictionary<string, int> Targets { get; } = new();
        }

        sealed class Tokenizer {
                public enum Token {
                        SPACE = 0x20,
                        TAB = 0x09,
                        LF = 0x0A,
                        EOF = 0
                }
                private readonly BinaryReader input;

                public int Line { get; private set; }

                public Tokenizer(BinaryReader reader) {
                        input = reader;
                        Line = 1;
                }

                public Token Next() {
                        while (input.BaseStream.Position < input.BaseStream.Length) {
                                var value = input.Read();
                                switch ((Token)value) {
                                        case Token.SPACE:
                                        case Token.TAB:
                                                return (Token)value;
                                        case Token.LF:
                                                Line++;
                                                return Token.LF;
                                }
                        }

                        return Token.EOF;
                }
        }

        sealed class Parser {
                private readonly Tokenizer tokenizer;

                public Parser(Tokenizer tokenizer) {
                        this.tokenizer = tokenizer;
                }

                private InvalidDataException UnexpectedEof() => new($"Unexpected EOF at line: {tokenizer.Line}");

                private InvalidDataException ParserError(string message) => new($"Whitespace.NET ({tokenizer.Line}): {message}");

                private string ParseLiteral() {
                        var sb = new StringBuilder();
                        Tokenizer.Token token;
                        while ((token = tokenizer.Next()) != Tokenizer.Token.LF) {
                                if (token == Tokenizer.Token.EOF) {
                                        throw UnexpectedEof();
                                }

                                sb.Append(token == Tokenizer.Token.SPACE ? ' ' : '\t');
                        }

                        return sb.ToString();
                }

                private void ParseStack(WSProgram program) {
                        switch (tokenizer.Next()) {
                                case Tokenizer.Token.SPACE:
                                        program.Instructions.Add(new Instruction(Instruction.OpCode.push, ParseLiteral()));
                                        break;
                                case Tokenizer.Token.LF:
                                        switch (tokenizer.Next()) {
                                                case Tokenizer.Token.SPACE:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.dup));
                                                        break;
                                                case Tokenizer.Token.TAB:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.swap));
                                                        break;
                                                case Tokenizer.Token.LF:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.pop));
                                                        break;
                                                default:
                                                        throw ParserError("Unexpected end of file!");
                                        }

                                        break;
                                default:
                                        throw ParserError("Wrong stack operation!");
                        }
                }

                private void ParseControlFlow(WSProgram program) {
                        switch (tokenizer.Next()) {
                                case Tokenizer.Token.SPACE:
                                        switch (tokenizer.Next()) {
                                                case Tokenizer.Token.SPACE: {
                                                        var label = ParseLiteral();
                                                        program.Targets[label] = program.Instructions.Count;
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.mrk, label));
                                                        break;
                                                }
                                                case Tokenizer.Token.TAB:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.call, ParseLiteral()));
                                                        break;
                                                case Tokenizer.Token.LF:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.jmp, ParseLiteral()));
                                                        break;
                                                default:
                                                        throw ParserError("Unexpected end of file!");
                                        }

                                        break;
                                case Tokenizer.Token.TAB:
                                        switch (tokenizer.Next()) {
                                                case Tokenizer.Token.SPACE:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.jz, ParseLiteral()));
                                                        break;
                                                case Tokenizer.Token.TAB:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.jlz, ParseLiteral()));
                                                        break;
                                                case Tokenizer.Token.LF:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.ret));
                                                        break;
                                                default:
                                                        throw ParserError("Unexpected end of file!");
                                        }

                                        break;
                                case Tokenizer.Token.LF:
                                        if (tokenizer.Next() == Tokenizer.Token.LF) {
                                                program.Instructions.Add(new Instruction(Instruction.OpCode.end));
                                        }
                                        else {
                                                throw ParserError("Wrong control flow operation!");
                                        }

                                        break;
                                default:
                                        throw ParserError("Unexpected end of file!");
                        }
                }

                private void ParseArithmetic(WSProgram program) {
                        switch (tokenizer.Next()) {
                                case Tokenizer.Token.SPACE:
                                        switch (tokenizer.Next()) {
                                                case Tokenizer.Token.SPACE:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.add));
                                                        break;
                                                case Tokenizer.Token.TAB:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.sub));
                                                        break;
                                                case Tokenizer.Token.LF:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.mul));
                                                        break;
                                                default:
                                                        throw ParserError("Unexpected end of file!");
                                        }

                                        break;
                                case Tokenizer.Token.TAB:
                                        switch (tokenizer.Next()) {
                                                case Tokenizer.Token.SPACE:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.div));
                                                        break;
                                                case Tokenizer.Token.TAB:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.mod));
                                                        break;
                                                default:
                                                        throw ParserError("Unknown arithmetic operation!");
                                        }

                                        break;
                                default:
                                        throw ParserError("Wrong arithmetic operation!");
                        }
                }

                private void ParseHeap(WSProgram program) {
                        switch (tokenizer.Next()) {
                                case Tokenizer.Token.SPACE:
                                        program.Instructions.Add(new Instruction(Instruction.OpCode.sth));
                                        break;
                                case Tokenizer.Token.TAB:
                                        program.Instructions.Add(new Instruction(Instruction.OpCode.ldh));
                                        break;
                                default:
                                        throw ParserError("Wrong heap operation!");
                        }
                }

                private void ParseIo(WSProgram program) {
                        switch (tokenizer.Next()) {
                                case Tokenizer.Token.SPACE:
                                        switch (tokenizer.Next()) {
                                                case Tokenizer.Token.SPACE:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.wrc));
                                                        break;
                                                case Tokenizer.Token.TAB:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.wri));
                                                        break;
                                                default:
                                                        throw ParserError("Wrong I/O operation!");
                                        }

                                        break;
                                case Tokenizer.Token.TAB:
                                        switch (tokenizer.Next()) {
                                                case Tokenizer.Token.SPACE:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.rdc));
                                                        break;
                                                case Tokenizer.Token.TAB:
                                                        program.Instructions.Add(new Instruction(Instruction.OpCode.rdi));
                                                        break;
                                                default:
                                                        throw ParserError("Wrong I/O operation!");
                                        }

                                        break;
                                default:
                                        throw ParserError("Wrong I/O operation!");
                        }
                }

                public void Parse(WSProgram program) {
                        while (true) {
                                switch (tokenizer.Next()) {
                                        case Tokenizer.Token.SPACE:
                                                ParseStack(program);
                                                break;
                                        case Tokenizer.Token.LF:
                                                ParseControlFlow(program);
                                                break;
                                        case Tokenizer.Token.TAB:
                                                switch (tokenizer.Next()) {
                                                        case Tokenizer.Token.SPACE:
                                                                ParseArithmetic(program);
                                                                break;
                                                        case Tokenizer.Token.TAB:
                                                                ParseHeap(program);
                                                                break;
                                                        case Tokenizer.Token.LF:
                                                                ParseIo(program);
                                                                break;
                                                        default:
                                                                throw UnexpectedEof();
                                                }

                                                break;
                                        default:
                                                return;
                                }
                        }
                }
        }

}
