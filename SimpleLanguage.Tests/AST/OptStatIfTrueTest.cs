using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimpleLang;
using SimpleLang.Visitors;

namespace SimpleLanguage.Tests.AST
{
    [TestFixture]
    internal class OptStatIfTrueTest : ASTTestsBase
    {
        OptStatIfTrue opt = new SimpleLang.Visitors.OptStatIfTrue();

    }
}
