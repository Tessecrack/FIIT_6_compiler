using System.Collections.Generic;
using SimpleLang;
using SimpleLang.Visitors;
using SimpleParser;
using SimpleScanner;

namespace SimpleLanguage.Tests.AST
{
    public class ASTTestsBase
    {
        protected Parser GenAST(string sourceCode)
        {
            ThreeAddressCodeTmp.ResetTmpName();
            ThreeAddressCodeTmp.ResetTmpLabel();
            SymbolTable.vars.Clear();   // oh yeah, all variables are stored in a static dict :D
            var scanner = new Scanner();
            scanner.SetSource(sourceCode, 0);
            var parser = new Parser(scanner);
            parser.Parse();
            var fillParents = new FillParentsVisitor();
            parser.root.Visit(fillParents);
            return parser;
        }
    }
}
