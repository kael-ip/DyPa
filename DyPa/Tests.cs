#if DEBUGTEST
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Collections;
using HexTex.Data.Common;

namespace HexTex.Dypa.PEG {
    [TestFixture]
    public class RDParserTests {


        //public object Dumper(Rule r, object context, IList args) { ((StringBuilder)context).AppendFormat("{0} ", r.Product); return null; }

        [SetUp]
        public void SetUp() {
            ParserHelper.Instance = new ParserHelper();
        }

        [Test]
        public void TestMatchPrimitives() {
            {
                var q = new LiteralChar("0123456789");
                {
                    var r = q.Match(TextCursor.Create("7"));
                    Assert.IsNotNull(r);
                    Assert.AreEqual(1, r.Cursor.Position);
                    Assert.AreEqual('7', r.Value);
                }
                {
                    var r = q.Match(TextCursor.Create("78"));
                    Assert.IsNotNull(r);
                    Assert.AreEqual(1, r.Cursor.Position);
                    Assert.AreEqual('7', r.Value);
                }
            }
            {
                var q = new Literal(TextCursor.EOI);
                {
                    var r = q.Match(TextCursor.Create(""));
                    Assert.IsNotNull(r);
                    Assert.IsNull(ParserHelper.Instance.FailCursor);
                    Assert.AreEqual(0, r.Cursor.Position);
                    Assert.AreEqual(TextCursor.EOI, r.Value);
                }
                {
                    var r = q.Match(TextCursor.Create("0123"));
                    Assert.IsNull(r);
                    Assert.IsNotNull(ParserHelper.Instance.FailCursor);
                    Assert.AreEqual(0, ParserHelper.Instance.FailCursor.Position);
                }
            }
        }

        [Test]
        public void TestSequence() {
            {
                var q = new Sequence(new LiteralChar("abc"), new LiteralChar("+-"), new LiteralChar("def"), new Literal(TextCursor.EOI));
                {
                    var r = q.Match(TextCursor.Create(""));
                    Assert.IsNull(r);
                    Assert.IsNotNull(ParserHelper.Instance.FailCursor);
                    Assert.AreEqual(0, ParserHelper.Instance.FailCursor.Position);
                }
                {
                    var r = q.Match(TextCursor.Create("abf"));
                    Assert.IsNull(r);
                    Assert.IsNotNull(ParserHelper.Instance.FailCursor);
                    Assert.AreEqual(1, ParserHelper.Instance.FailCursor.Position);
                }
                {
                    var r = q.Match(TextCursor.Create("a+a"));
                    Assert.IsNull(r);
                    Assert.IsNotNull(ParserHelper.Instance.FailCursor);
                    Assert.AreEqual(2, ParserHelper.Instance.FailCursor.Position);
                }
                {
                    var r = q.Match(TextCursor.Create("b-d"));
                    Assert.IsNotNull(r);
                    Assert.AreEqual(3, r.Cursor.Position);
                    var v = r.Value as IVector;
                    Assert.IsTrue(r.Value is IVector);
                    Assert.AreEqual(4, v.Length);
                    Assert.AreEqual('d', v[2]);
                    Assert.AreEqual(TextCursor.EOI, v[3]);
                }
            }
        }

        [Test]
        public void TestFirstOf() {
            {
                var q = new First(new LiteralChar("a"), new LiteralChar("+-"), new LiteralChar("z"));
                {
                    var r = q.Match(TextCursor.Create(""));
                    Assert.IsNull(r);
                    Assert.IsNotNull(ParserHelper.Instance.FailCursor);
                    Assert.AreEqual(0, ParserHelper.Instance.FailCursor.Position);
                }
                {
                    var r = q.Match(TextCursor.Create("+"));
                    Assert.IsNotNull(r);
                    Assert.AreEqual(1, r.Cursor.Position);
                    Assert.AreEqual('+', r.Value);
                }
                {
                    var r = q.Match(TextCursor.Create("z"));
                    Assert.IsNotNull(r);
                    Assert.AreEqual(1, r.Cursor.Position);
                    Assert.AreEqual('z', r.Value);
                }
            }
        }

        [Test]
        public void TestComposition() {
            {
                var q = new First(
                    new Sequence(new LiteralChar("a"), new LiteralChar("+-"), new LiteralChar("z")),
                    new Sequence(new LiteralChar("a"), new LiteralChar("z")),
                    new LiteralChar("?")
                    );
                {
                    var r = q.Match(TextCursor.Create(""));
                    Assert.IsNull(r);
                    Assert.IsNotNull(ParserHelper.Instance.FailCursor);
                    Assert.AreEqual(0, ParserHelper.Instance.FailCursor.Position);
                }
                {
                    var r = q.Match(TextCursor.Create("a+z"));
                    Assert.IsNotNull(r);
                    Assert.AreEqual(3, r.Cursor.Position);
                    var v = r.Value as IVector;
                    Assert.IsTrue(r.Value is IVector);
                    Assert.AreEqual(3, v.Length);
                    Assert.AreEqual('a', v[0]);
                    Assert.AreEqual('+', v[1]);
                    Assert.AreEqual('z', v[2]);
                }
                {
                    var r = q.Match(TextCursor.Create("az"));
                    Assert.IsNotNull(r);
                    Assert.AreEqual(2, r.Cursor.Position);
                    var v = r.Value as IVector;
                    Assert.IsTrue(r.Value is IVector);
                    Assert.AreEqual(2, v.Length);
                    Assert.AreEqual('a', v[0]);
                    Assert.AreEqual('z', v[1]);
                }
                {
                    var r = q.Match(TextCursor.Create("?"));
                    Assert.IsNotNull(r);
                    Assert.AreEqual(1, r.Cursor.Position);
                    Assert.AreEqual('?', r.Value);
                }
                {
                    var r = q.Match(TextCursor.Create("a?"));
                    Assert.IsNull(r);
                    Assert.IsNotNull(ParserHelper.Instance.FailCursor);
                    Assert.AreEqual(1, ParserHelper.Instance.FailCursor.Position);
                }
            }
        }

        [Test]
        public void TestRecursion() {
            {
                Placeholder expr = new Placeholder();
                expr.Expression = new First(
                    new Sequence(new Literal('1'), expr),
                    new Literal('0')
                    );
                {
                    Result r = expr.Match(TextCursor.Create("11110"));
                    Assert.IsNotNull(r);
                    Assert.AreEqual(TextCursor.EOI, r.Cursor.Peek());
                    Assert.IsTrue(r.Value is IVector);
                    IVector v = (IVector)r.Value;
                    Assert.AreEqual(2, v.Length);
                    Assert.AreEqual('1', v[0]);
                    Assert.IsTrue(v[1] is IVector);
                    while (v[1] is IVector) {
                        v = (IVector)v[1];
                    }
                    Assert.AreEqual('0', v[1]);
                }
                {
                    Result r = expr.Match(TextCursor.Create("0"));
                    Assert.IsNotNull(r);
                    Assert.AreEqual(TextCursor.EOI, r.Cursor.Peek());
                    Assert.AreEqual('0', r.Value);
                }
            }
        }

        [Test]
        public void TestSomeMatcher() {
            {
                var digit = new LiteralChar("0123456789");
                var q = new Sequence(
                    Some.Optional(new LiteralChar("-")),
                    Some.ZeroOrMore(digit),
                    new LiteralChar("."),
                    Some.OneOrMore(digit)
                    );
                {
                    var r = q.Match(TextCursor.Create("-.52"));
                    Assert.IsNotNull(r);
                    Assert.AreEqual(4, r.Cursor.Position);
                    Assert.IsTrue(r.Value is IVector);
                    IVector v = (IVector)r.Value;
                    Assert.AreEqual(4, v.Length);
                    Assert.IsTrue(v[0] is IVector);
                    IVector v0 = (IVector)v[0];
                    Assert.AreEqual(1, v0.Length);
                    Assert.AreEqual('-', v0[0]);
                    Assert.IsTrue(v[1] is IVector);
                    Assert.AreEqual(0, ((IVector)v[1]).Length);
                    Assert.AreEqual('.', v[2]);
                    Assert.IsTrue(v[3] is IVector);
                    IVector v3 = (IVector)v[3];
                    Assert.AreEqual(2, v3.Length);
                    Assert.AreEqual('5', v3[0]);
                    Assert.AreEqual('2', v3[1]);                   
                }
            }
        }

        [Test]
        public void LookAheadTest() {
            /*
                 expr:
                   term '+' expr
                 | term
                 ;
                 
                 term:
                   '(' expr ')'
                 | term '!'
                 | NUMBER
                 ;
             * '1+2'
             */
            Placeholder expr = new Placeholder();
            Placeholder term = new Placeholder();
            expr.Expression = new First(
                new Sequence(term, new Literal('+'), expr),
                term);
            //--left recursive
            //term.Expression = new First(
            //    new Sequence(new Literal('('), expr, new Literal(')')),
            //    new Sequence(term, new Literal('!')),
            //    new First(new Literal('1'), new Literal('2')));
            //--replaced:
            Placeholder term_r = new Placeholder();
            term.Expression = new Sequence(new First(
                new Sequence(new Literal('('), expr, new Literal(')')),
                new First(new Literal('1'), new Literal('2'))),
                term_r);
            term_r.Expression = new First(
                new Sequence(new Literal('!'), term_r),
                new EmptyExpression(null));

            {
                Result r = expr.Match(TextCursor.Create("1+2"));
                Assert.IsNotNull(r);
                Assert.AreEqual(TextCursor.EOI, r.Cursor.Peek());
            }
            {
                Result r = expr.Match(TextCursor.Create("1+2!"));
                Assert.IsNotNull(r);
                Assert.AreEqual(TextCursor.EOI, r.Cursor.Peek());
            }
        }

        //[Ignore("Right recursion")]
        //[Test]
        //public void ShiftReduceConflictTest() {
        //    /*
        //     stmt:
        //       expr
        //     | if_stmt
        //     ;
             
        //     if_stmt:
        //       IF expr THEN stmt
        //     | IF expr THEN stmt ELSE stmt
        //     ;
             
        //     expr:
        //       variable
        //     ;             
        //     *
        //     * Since the parser prefers to shift the ELSE, the result is to attach the else-clause to the innermost if-statement, making these two inputs equivalent:
        //     *      if x then if y then win (); else lose;
        //     *      if x then do; if y then win (); else lose; end;
        //     * But if the parser chose to reduce when possible rather than shift, the result would be to attach the else-clause to the outermost if-statement, making these two inputs equivalent:
        //     *      if x then if y then win (); else lose;
        //     *      if x then do; if y then win (); end; else lose;
        //     */
        //    Rule[] grammar = new Rule[]{
        //        new Rule("expr", new object[] { "x" }, new RuleAction(Dumper)),
        //        new Rule("expr", new object[] { "y" }, new RuleAction(Dumper)),
        //        new Rule("expr", new object[] { "z" }, new RuleAction(Dumper)),
        //        new Rule("expr", new object[] { "w" }, new RuleAction(Dumper)),
        //        new Rule("stmt", new object[] { "expr" }, new RuleAction(Dumper)),
        //        new Rule("stmt", new object[] { "if_stmt" }, new RuleAction(Dumper)),
        //        new Rule("if_stmt", new object[] { "IF", "expr", "THEN", "stmt" }, new RuleAction(Dumper)),
        //        new Rule("if_stmt", new object[] { "IF", "expr", "THEN", "stmt", "ELSE", "stmt" }, new RuleAction(Dumper))
        //    };
        //    StringBuilder sb = new StringBuilder();
        //    ParserGenerator gen = new ParserGenerator(grammar, "stmt");
        //    gen.Generate();

        //    object[] in1 = new object[] { "IF", "x", "THEN", "IF", "y", "THEN", "z", "ELSE", "w" };
        //    bool result = new LRParser(gen.RootState, new SampleChain(in1), sb).Parse();
        //    Assert.IsTrue(result);
        //    Assert.AreEqual("term term expr ", sb.ToString());
        //}

        //[Test]
        //public void TestLR0() {
        //    Rule[] grammar = new Rule[]{
        //        new Rule("expr", new object[] { "expr", "*", "rest" }, new RuleAction(Dumper)),
        //        new Rule("expr", new object[] { "expr", "+", "rest" }, new RuleAction(Dumper)),
        //        new Rule("expr", new object[] { "rest" }, new RuleAction(Dumper)),
        //        new Rule("rest", new object[] { "X" }, new RuleAction(Dumper)),
        //        new Rule("rest", new object[] { "Y" }, new RuleAction(Dumper)),
        //    };
        //    ParserGenerator gen = new ParserGenerator(grammar, "expr");
        //    gen.Generate();
        //    Console.WriteLine("TestLR0");
        //    Console.WriteLine(gen.DumpStates());

        //    StringBuilder sb = new StringBuilder();
        //    object[] in1 = new object[] { "X", "+", "Y", "*", "X" };
        //    Assert.IsTrue(new LRParser(gen.RootState, new SampleChain(in1), sb).Parse());
        //    Assert.AreEqual("rest expr rest expr rest expr ", sb.ToString());
        //}

        [Test]
        public void TestCalc1A() {
            LispPrinter.PrintVectorsAsLists = true;//required to compare the result
            ParserHelper.UseArray(true);//Use ArrayVectorFactory
            TestCalc1();
        }
        [Test]
        public void TestCalc1B() {
            ParserHelper.UseArray(false);//Use BNodeVectorFactory
            TestCalc1();
        }
        //[Test]//used by TestCalc1A and TestCalc1B
        public void TestCalc1() {
            /*
                Expression ← Term ((‘+’ / ‘-’) Term)*
                Term ← Factor (('*' / '/') Factor)*
                Factor ← Number / '(' Expression ')'
                Number ← [0-9]+
             */
            Expression eDigit = new LiteralChar("0123456789");
            Placeholder eNumber = new Placeholder();
            //eNumber.Expression = new First(new CallbackHandler( new Sequence(eDigit, eNumber), delegate(object v){
            //    object[] a = (object[])v;
            //    return string.Concat(a[0], a[1]);
            //}), eDigit);
            eNumber.Expression = new First(new CollapseToString(new Sequence(eDigit, eNumber)), new CallbackHandler(eDigit, Convert.ToString));
            //eNumber.Expression = new CallbackHandler(
            //    new First(new CollapseToString(new Sequence(eDigit, eNumber)), new CallbackHandler(eDigit, Convert.ToString)),
            //    delegate(object v) { return Int64.Parse((string)v); });
            //eNumber.Expression = new CollapseToArray(new First(new Sequence(eDigit, eNumber), new Sequence(eDigit, new TailStub())));
            Placeholder eAdditive = new Placeholder();
            Placeholder eMultiplicative = new Placeholder();
            Expression eSingular = new First(eNumber,
                new ExtractOne(1, new Sequence(new Literal('('), eAdditive, new Literal(')'))));
            //
            Placeholder eAdditiveSuffix = new Placeholder();
            eAdditiveSuffix.Expression = new First(
                new CallbackHandler(new Sequence(new LiteralChar("+-"), eMultiplicative, eAdditiveSuffix), delegate(object v) {
                    IVector a = (IVector)v;
                    IVector tail = (IVector)a[2];
                    return ParserHelper.VectorFactory.InsertBefore(a[0], ParserHelper.VectorFactory.InsertBefore(a[1], tail));
                //return v;
                //return ((object[])v)[1];
            }),
                //new EmptyExpression(null));
                new EmptyExpression(ParserHelper.VectorFactory.Empty));
            //eAdditive.Expression = new CallbackHandler( new Sequence(eMultiplicative, eAdditiveSuffix), DoAdd);
            eAdditive.Expression = new CallbackHandler(new Sequence(eMultiplicative, eAdditiveSuffix), delegate(object v) {
                IVector a = (IVector)v;
                IVector tail = (IVector)a[1];
                return ParserHelper.VectorFactory.InsertBefore(a[0], tail);
            });
            Placeholder eMultiplicativeSuffix = new Placeholder();
            eMultiplicativeSuffix.Expression = new First(
                new CallbackHandler(new Sequence(new LiteralChar("*/"), eSingular, eMultiplicativeSuffix), delegate(object v) {
                    IVector a = (IVector)v;
                    IVector tail = (IVector)a[2];
                    return ParserHelper.VectorFactory.InsertBefore(a[0], ParserHelper.VectorFactory.InsertBefore(a[1], tail));
                //return v;
                //return ((object[])v)[1];
            }),
                //new EmptyExpression(null));
                new EmptyExpression(ParserHelper.VectorFactory.Empty));
            //eMultiplicative.Expression = new CallbackHandler(new Sequence(eSingular, eMultiplicativeSuffix), DoMultiply);
            eMultiplicative.Expression = new CallbackHandler(new Sequence(eSingular, eMultiplicativeSuffix), delegate(object v) {
                IVector a = (IVector)v;
                IVector tail = (IVector)a[1];
                if (tail == ParserHelper.VectorFactory.Empty) return a[0];
                return ParserHelper.VectorFactory.InsertBefore(a[0], tail);
            });
            Expression expr = eAdditive;

            {                
                //Result r = expr.Match(TextCursor.Create("12+345+6+7890"));
                Result r = expr.Match(TextCursor.Create("12+3*45+6+7*(6+2)*9+0"));
                //string expected = "(+ 12 (* 3 45) 6 (* 7 8 9) 0)";
                string expected = "(12 + (3 * 45) + 6 + (7 * (6 + 2) * 9) + 0)";
                Assert.IsNotNull(r);
                Assert.AreEqual(TextCursor.EOI, r.Cursor.Peek());
                Assert.AreEqual(expected, Convert.ToString(r.Value).Replace("\"", "").Replace("'", ""));
            }
            {
                Result r = expr.Match(TextCursor.Create("120+34*((5*6+7)*8+9)-1"));
                Assert.IsNotNull(r);
                Assert.AreEqual(TextCursor.EOI, r.Cursor.Peek());
                //Assert.AreEqual(120 + 34 * ((5 * 6 + 7) * 8 + 9) - 1, r.Value);
            }
        }
        private object DoMultiply(object v) {
            object[] a = (object[])v;
            if (a[1] == null) return a[0];
            return Convert.ToString(Int64.Parse(Convert.ToString(a[0])) * Int64.Parse(Convert.ToString(a[1])));
        }
        private object DoAdd(object v) {
            object[] a = (object[])v;
            if (a[1] == null) return a[0];
            return Convert.ToString(Int64.Parse(Convert.ToString(a[0])) + Int64.Parse(Convert.ToString(a[1])));
        }


        [Test]
        public void TestCalc2A() {
            LispPrinter.PrintVectorsAsLists = true;//required to compare the result
            ParserHelper.UseArray(true);//Use ArrayVectorFactory
            TestCalc2();
        }
        [Test]
        public void TestCalc2B() {
            ParserHelper.UseArray(false);//Use BNodeVectorFactory
            TestCalc2();
        }
        [Test]
        public void TestCalc2() {
            /*
                E ← T ((‘+’ / ‘-’) T)*
                T ← F (('*' / '/') F)*
                F ← N / '(' E ')'
                N ← [0-9]+
             */
            Expression eDigit = new LiteralChar("0123456789");
            Placeholder eNumber = new Placeholder();
            eNumber.Expression = new First(new CollapseToString(new Sequence(eDigit, eNumber)), new CallbackHandler(eDigit, Convert.ToString));
            Placeholder eAdditive = new Placeholder();
            Placeholder eMultiplicative = new Placeholder();
            Expression eSingular = new First(eNumber, new ExtractOne(1, new Sequence(new Literal('('), eAdditive, new Literal(')'))));
            //
            Placeholder eAdditiveSuffix = new Placeholder();
            eAdditiveSuffix.Expression = new First(
                new Sequence(new LiteralChar("+-"), eMultiplicative, eAdditiveSuffix),
                new EmptyExpression(ParserHelper.VectorFactory.Empty));
            eAdditive.Expression = new CallbackHandler(new Sequence(eMultiplicative, eAdditiveSuffix), BinaryInfixToPrefix);
            Placeholder eMultiplicativeSuffix = new Placeholder();
            eMultiplicativeSuffix.Expression = new First(
                new Sequence(new LiteralChar("*/"), eSingular, eMultiplicativeSuffix),
                new EmptyExpression(ParserHelper.VectorFactory.Empty));
            eMultiplicative.Expression = new CallbackHandler(new Sequence(eSingular, eMultiplicativeSuffix), BinaryInfixToPrefix);
            Expression expr = eAdditive;

            {
                Result r = expr.Match(TextCursor.Create("12+3*45+6+7*(6+2)*9+0"));
                string expected = "(+ (+ (+ (+ 12 (* 3 45)) 6) (* (* 7 (+ 6 2)) 9)) 0)";
                Assert.IsNotNull(r);
                Assert.AreEqual(TextCursor.EOI, r.Cursor.Peek());
                Assert.AreEqual(expected, Convert.ToString(r.Value).Replace("\"", "").Replace("'", ""));
                object num = new CalculatorVisitor().Process(r.Value);
                Assert.AreEqual(Convert.ToDouble(12 + 3 * 45 + 6 + 7 * (6 + 2) * 9 + 0), num);
            }
            {
                ParserHelper.Instance = new ParserHelper();
                Result r = expr.Match(TextCursor.Create("12+3*4a5+6+7*(6+2)*9+0"));
                //Assert.IsNull(r);
                Assert.AreNotEqual(TextCursor.EOI, r.Cursor.Peek());
                //Console.WriteLine(ParserHelper.Instance.GetError());
                Assert.AreEqual(6, ParserHelper.Instance.FailCursor.Position);
            }
            {
                ParserHelper.Instance = new ParserHelper();
                Result r = expr.Match(TextCursor.Create("12+3*45+(6+7*(6+2)*9+0"));
                //Assert.IsNull(r);
                Assert.AreNotEqual(TextCursor.EOI, r.Cursor.Peek());
                //Console.WriteLine(ParserHelper.Instance.GetError());
                Assert.AreEqual(22, ParserHelper.Instance.FailCursor.Position);
            }
            {
                ParserHelper.Instance = new ParserHelper();
                Result r = expr.Match(TextCursor.Create("(12+3*45+(6+7*(6+2)*9+0)"));
                Assert.IsNull(r);
                //Assert.AreNotEqual(TextCursor.EOI, r.Cursor.Peek());
                //Console.WriteLine(ParserHelper.Instance.GetError());
                Assert.AreEqual(24, ParserHelper.Instance.FailCursor.Position);
            }
        }

        [Test]
        public void TestCalc2Repeaters() {
            ParserHelper.UseArray(false);
            /*
                E ← T ((‘+’ / ‘-’) T)*
                T ← F (('*' / '/') F)*
                F ← N / '(' E ')'
                N ← [0-9]+
             */
            var eNumber = new CollapseToString(Some.OneOrMore(new LiteralChar("0123456789")));
            var factor = new Placeholder();
            var term = new Placeholder();
            var expr = new Placeholder();
            factor.Expression = new First(eNumber, new ExtractOne(1, new Sequence(new Literal('('), expr, new Literal(')'))));
            Function toPrefix = z => {
                    IVector v = (IVector)z;
                    object v0 = v[0];
                    IVector v1 = (IVector)v[1];
                    if (v1 == ParserHelper.VectorFactory.Empty) return v0;
                    for (int i = 0; i < v1.Length; i++) {
                        IVector vv = (IVector)v1[i];
                        v0 = ParserHelper.VectorFactory.Create(vv[0], v0, vv[1]);
                    }
                    return v0;
                };
            term.Expression = new CallbackHandler(new Sequence(factor, Some.ZeroOrMore(new Sequence(new LiteralChar("*/"), factor))), toPrefix);
            expr.Expression = new CallbackHandler(new Sequence(term, Some.ZeroOrMore(new Sequence(new LiteralChar("+-"), term))), toPrefix);

            {
                Result r = expr.Match(TextCursor.Create("12+3*45+6+7*(6+2)*9+0"));
                string expected = "(+ (+ (+ (+ 12 (* 3 45)) 6) (* (* 7 (+ 6 2)) 9)) 0)";
                Assert.IsNotNull(r);
                Assert.AreEqual(TextCursor.EOI, r.Cursor.Peek());
                Assert.AreEqual(expected, Convert.ToString(r.Value).Replace("\"", "").Replace("'", ""));
                object num = new CalculatorVisitor().Process(r.Value);
                Assert.AreEqual(Convert.ToDouble(12 + 3 * 45 + 6 + 7 * (6 + 2) * 9 + 0), num);
            }
            {
                ParserHelper.Instance = new ParserHelper();
                Result r = expr.Match(TextCursor.Create("12+3*4a5+6+7*(6+2)*9+0"));
                Assert.IsNotNull(r);
                Assert.AreNotEqual(TextCursor.EOI, r.Cursor.Peek());
                //Console.WriteLine(ParserHelper.Instance.GetError());
                Assert.AreEqual(6, ParserHelper.Instance.FailCursor.Position);
            }
            {
                ParserHelper.Instance = new ParserHelper();
                Result r = expr.Match(TextCursor.Create("12+3*45+(6+7*(6+2)*9+0"));
                Assert.IsNotNull(r);
                Assert.AreNotEqual(TextCursor.EOI, r.Cursor.Peek());
                //Console.WriteLine(ParserHelper.Instance.GetError());
                Assert.AreEqual(22, ParserHelper.Instance.FailCursor.Position);
            }
            {
                ParserHelper.Instance = new ParserHelper();
                Result r = expr.Match(TextCursor.Create("(12+3*45+(6+7*(6+2)*9+0)"));
                Assert.IsNull(r);
                //Console.WriteLine(ParserHelper.Instance.GetError());
                Assert.AreEqual(24, ParserHelper.Instance.FailCursor.Position);
            }
        }

        private static object BinaryInfixToPrefix(object v) {
            IVector a = (IVector)v;
            IVector tail = (IVector)a[1];
            object x = a[0];
            while (tail != ParserHelper.VectorFactory.Empty) {
                x = ParserHelper.VectorFactory.Create(tail[0], x, tail[1]);
                tail = (IVector)tail[2];
            }
            return x;
        }

    }

    public class CalculatorVisitor {
        public object Process(object x) {
            if (x is string) {
                return Convert.ToDouble((string)x);
            }
            if (x is IVector) {
                IVector v = (IVector)x;
                if (!typeof(char).IsInstanceOfType(v[0])) throw new NotSupportedException(Convert.ToString(v[0]));
                switch ((char)v[0]) {
                    case '+': return ((double)Process(v[1])) + ((double)Process(v[2]));
                    case '-': return ((double)Process(v[1])) - ((double)Process(v[2]));
                    case '*': return ((double)Process(v[1])) * ((double)Process(v[2]));
                    case '/': return ((double)Process(v[1])) / ((double)Process(v[2]));
                    default: throw new NotSupportedException(Convert.ToString(v[0]));
                }
            }
            if (ReferenceEquals(x, null)) throw new ArgumentNullException();
            throw new NotSupportedException(x.GetType().FullName);
        }
    }

    public class TestContainer {
        private object value;
        public TestContainer(object value) {
            this.value = value;
        }
        public override string ToString() {
            return value == null ? "<NULL>" : Convert.ToString(value);
        }
    }
}
#endif