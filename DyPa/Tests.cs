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


        [SetUp]
        public void SetUp() {
        }

        [Test]
        public void TestVectorReverseA() {
            TestVectorReverse(true);
        }

        [Test]
        public void TestVectorReverseB() {
            TestVectorReverse(false);
        }

        public void TestVectorReverse(bool useArray) {
            IVectorFactory factory = useArray ? (IVectorFactory)new ArrayVectorFactory() : (IVectorFactory)new BNodeVectorFactory();
            {
                var v = factory.Create("a", "b", "c", "d");
                var r = factory.Reverse(v);
                Assert.AreEqual(4, r.Length);
                Assert.AreEqual("a", r[3]);
                Assert.AreEqual("b", r[2]);
                Assert.AreEqual("c", r[1]);
                Assert.AreEqual("d", r[0]);
            }
            {
                var v = factory.Create("a", "b", "c", "d", "e");
                var r = factory.Reverse(v);
                Assert.AreEqual(5, r.Length);
                Assert.AreEqual("a", r[4]);
                Assert.AreEqual("b", r[3]);
                Assert.AreEqual("c", r[2]);
                Assert.AreEqual("d", r[1]);
                Assert.AreEqual("e", r[0]);
            }
        }

        [Test]
        public void TestMatchPrimitives() {
            {
                var q = new LiteralAnyCharOf("0123456789");
                {
                    var parser = new Parser(q, TextCursor.Create("7"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(1, r.Cursor.Position);
                    Assert.AreEqual('7', r.Value);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("78"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(1, r.Cursor.Position);
                    Assert.AreEqual('7', r.Value);
                }
            }
            {
                var q = new Literal('z');
                {
                    var parser = new Parser(q, TextCursor.Create("z"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    Assert.IsNull(parser.FailCursor);
                    Assert.AreEqual(1, r.Cursor.Position);
                    Assert.AreEqual('z', r.Value);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("0123"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                    Assert.IsNotNull(parser.FailCursor);
                    Assert.AreEqual(0, parser.FailCursor.Position);
                }
            }
            {
                var q = new LiteralEOI();
                {
                    var parser = new Parser(q, TextCursor.Create(""));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    Assert.IsNull(parser.FailCursor);
                    Assert.AreEqual(0, r.Cursor.Position);
                    Assert.IsTrue(!r.Cursor.CanPop());
                    Assert.AreEqual(null, r.Value);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("0123"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                    Assert.IsNotNull(parser.FailCursor);
                    Assert.AreEqual(0, parser.FailCursor.Position);
                }
            }
        }

        [Test]
        public void TestSequence() {
            {
                var q = new Sequence(new LiteralAnyCharOf("abc"), new LiteralAnyCharOf("+-"), new LiteralAnyCharOf("def"), new LiteralEOI());
                {
                    var parser = new Parser(q, TextCursor.Create(""));
                    var r = parser.Run();
                    Assert.IsNull(r);
                    Assert.IsNotNull(parser.FailCursor);
                    Assert.AreEqual(0, parser.FailCursor.Position);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("abf"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                    Assert.IsNotNull(parser.FailCursor);
                    Assert.AreEqual(1, parser.FailCursor.Position);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("a+a"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                    Assert.IsNotNull(parser.FailCursor);
                    Assert.AreEqual(2, parser.FailCursor.Position);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("b-d"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(3, r.Cursor.Position);
                    var v = r.Value as IVector;
                    Assert.IsTrue(r.Value is IVector);
                    Assert.AreEqual(4, v.Length);
                    Assert.AreEqual('d', v[2]);
                    Assert.AreEqual(null, v[3]);
                }
            }
        }

        [Test]
        public void TestFirstOf() {
            {
                var q = new FirstOf(new LiteralAnyCharOf("a"), new LiteralAnyCharOf("+-"), new LiteralAnyCharOf("z"));
                {
                    var parser = new Parser(q, TextCursor.Create(""));
                    var r = parser.Run();
                    Assert.IsNull(r);
                    Assert.IsNotNull(parser.FailCursor);
                    Assert.AreEqual(0, parser.FailCursor.Position);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("+"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(1, r.Cursor.Position);
                    Assert.AreEqual('+', r.Value);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("z"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(1, r.Cursor.Position);
                    Assert.AreEqual('z', r.Value);
                }
            }
        }

        [Test]
        public void TestComposition() {
            {
                var q = new FirstOf(
                    new Sequence(new LiteralAnyCharOf("a"), new LiteralAnyCharOf("+-"), new LiteralAnyCharOf("z")),
                    new Sequence(new LiteralAnyCharOf("a"), new LiteralAnyCharOf("z")),
                    new LiteralAnyCharOf("?")
                    );
                {
                    var parser = new Parser(q, TextCursor.Create(""));
                    var r = parser.Run();
                    Assert.IsNull(r);
                    Assert.IsNotNull(parser.FailCursor);
                    Assert.AreEqual(0, parser.FailCursor.Position);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("a+z"));
                    var r = parser.Run();
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
                    var parser = new Parser(q, TextCursor.Create("az"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(2, r.Cursor.Position);
                    var v = r.Value as IVector;
                    Assert.IsTrue(r.Value is IVector);
                    Assert.AreEqual(2, v.Length);
                    Assert.AreEqual('a', v[0]);
                    Assert.AreEqual('z', v[1]);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("?"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(1, r.Cursor.Position);
                    Assert.AreEqual('?', r.Value);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("a?"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                    Assert.IsNotNull(parser.FailCursor);
                    Assert.AreEqual(1, parser.FailCursor.Position);
                }
            }
        }

        [Test]
        public void TestRecursion() {
            {
                Placeholder expr = new Placeholder();
                expr.Expression = new FirstOf(
                    new Sequence(new Literal('1'), expr),
                    new Literal('0')
                    );
                {
                    var parser = new Parser(expr, TextCursor.Create("11110"));
                    Result r = parser.Run();
                    Assert.IsNotNull(r);
                    Assert.IsFalse(r.Cursor.CanPop());
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
                    var parser = new Parser(expr, TextCursor.Create("0"));
                    Result r = parser.Run();
                    Assert.IsNotNull(r);
                    Assert.IsFalse(r.Cursor.CanPop());
                    Assert.AreEqual('0', r.Value);
                }
            }
        }

        [Test]
        public void TestSomeMatcher() {
            {
                var digit = new LiteralAnyCharOf("0123456789");
                var q = new Sequence(
                    Some.Optional(new LiteralAnyCharOf("-")),
                    Some.ZeroOrMore(digit),
                    new LiteralAnyCharOf("."),
                    Some.OneOrMore(digit)
                    );
                {
                    var parser = new Parser(q, TextCursor.Create("-.52"));
                    var r = parser.Run();
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
        public void TestNotPredicateMatcher() {
            {//nested comments example
                var allchars = new LiteralAnyCharOf("0123456789/* ");
                var _open = new Sequence(new LiteralAnyCharOf("/"), new LiteralAnyCharOf("*"));
                var _close = new Sequence(new LiteralAnyCharOf("*"), new LiteralAnyCharOf("/"));
                var _nested = new Placeholder();
                var _block = new Sequence(_open, Some.ZeroOrMore(_nested), _close);
                _nested.Expression = new FirstOf(_block, new Sequence(Predicate.Not(_open), Predicate.Not(_close), allchars));
                var q = new Sequence(Some.ZeroOrMore(_nested), new LiteralEOI());
                {
                    var parser = new Parser(q, TextCursor.Create("  ** /"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);                   
                }
                {
                    var parser = new Parser(q, TextCursor.Create("  **/"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                    Assert.IsNotNull(parser.FailCursor);
                    if (System.Diagnostics.Debugger.IsAttached) {
                        System.Diagnostics.Debug.WriteLine(parser.GetError());
                    }
                    Assert.AreEqual(3, parser.FailCursor.Position);
                }
                {
                    var parser = new Parser(q, TextCursor.Create(" 12 34 /* 5 6 7*8/*9 0/**/ */* * */"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                }
                {
                    var parser = new Parser(q, TextCursor.Create(" 12 34 /* 5 6 7*8/*9 0/* */* * */"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                    Assert.IsNotNull(parser.FailCursor);
                    if (System.Diagnostics.Debugger.IsAttached) {
                        System.Diagnostics.Debug.WriteLine(parser.GetError());
                    }
                    Assert.AreEqual(33, parser.FailCursor.Position);
                }
            }
        }

        [Test]
        public void TestAndPredicateMatcher() {
            {//classic {a^n b^n c^n | n>=1} example
                var _a = new Placeholder();
                _a.Expression = new Sequence(new LiteralAnyCharOf("a"), Some.Optional(_a), new LiteralAnyCharOf("b"));
                var _b = new Placeholder();
                _b.Expression = new Sequence(new LiteralAnyCharOf("b"), Some.Optional(_b), new LiteralAnyCharOf("c"));
                var q = new Sequence(Predicate.And(new Sequence(_a, Predicate.Not(new LiteralAnyCharOf("b")))), Some.OneOrMore(new LiteralAnyCharOf("a")), _b, new LiteralEOI());
                {
                    var parser = new Parser(q, TextCursor.Create("aaabbbccc"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("abc"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("aabbc"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("aabbccc"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("aabccc"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("aabcc"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("aabbbc"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("aabbbcc"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("aacbb"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("aabbac"));
                    var r = parser.Run();
                    Assert.IsNull(r);
                }
                {
                    var parser = new Parser(q, TextCursor.Create("bca"));
                    var r = parser.Run();
                    Assert.IsNull(r);
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
            expr.Expression = new FirstOf(
                new Sequence(term, new Literal('+'), expr),
                term);
            //--left recursive
            //term.Expression = new First(
            //    new Sequence(new Literal('('), expr, new Literal(')')),
            //    new Sequence(term, new Literal('!')),
            //    new First(new Literal('1'), new Literal('2')));
            //--replaced:
            Placeholder term_r = new Placeholder();
            term.Expression = new Sequence(new FirstOf(
                new Sequence(new Literal('('), expr, new Literal(')')),
                new FirstOf(new Literal('1'), new Literal('2'))),
                term_r);
            term_r.Expression = new FirstOf(
                new Sequence(new Literal('!'), term_r),
                new EmptyRule(null));

            {
                var parser = new Parser(expr, TextCursor.Create("1+2"));
                Result r = parser.Run();
                Assert.IsNotNull(r);
                Assert.IsFalse(r.Cursor.CanPop());
            }
            {
                var parser = new Parser(expr, TextCursor.Create("1+2!"));
                Result r = parser.Run();
                Assert.IsNotNull(r);
                Assert.IsFalse(r.Cursor.CanPop());
            }
        }

        [Test]
        public void TestCalc1A() {
            LispPrinter.PrintVectorsAsLists = true;//required to compare the result
            TestCalc1(true);//Use ArrayVectorFactory
        }
        [Test]
        public void TestCalc1B() {
            TestCalc1(false);//Use BNodeVectorFactory
        }
        //[Test]//used by TestCalc1A and TestCalc1B
        public void TestCalc1(bool useArray) {
            IVectorFactory factory = useArray? (IVectorFactory)new ArrayVectorFactory() : (IVectorFactory)new BNodeVectorFactory();
            /*
                Expression ← Term ((‘+’ / ‘-’) Term)*
                Term ← Factor (('*' / '/') Factor)*
                Factor ← Number / '(' Expression ')'
                Number ← [0-9]+
             */
            Rule eDigit = new LiteralAnyCharOf("0123456789");
            Placeholder eNumber = new Placeholder();
            //eNumber.Expression = new First(new CallbackHandler( new Sequence(eDigit, eNumber), delegate(object v){
            //    object[] a = (object[])v;
            //    return string.Concat(a[0], a[1]);
            //}), eDigit);
            eNumber.Expression = new FirstOf(new CollapseToString(new Sequence(eDigit, eNumber)), new CallbackHandler(eDigit, Convert.ToString));
            //eNumber.Expression = new CallbackHandler(
            //    new First(new CollapseToString(new Sequence(eDigit, eNumber)), new CallbackHandler(eDigit, Convert.ToString)),
            //    delegate(object v) { return Int64.Parse((string)v); });
            //eNumber.Expression = new CollapseToArray(new First(new Sequence(eDigit, eNumber), new Sequence(eDigit, new TailStub())));
            Placeholder eAdditive = new Placeholder();
            Placeholder eMultiplicative = new Placeholder();
            Rule eSingular = new FirstOf(eNumber,
                new ExtractOne(1, new Sequence(new Literal('('), eAdditive, new Literal(')'))));
            //
            Placeholder eAdditiveSuffix = new Placeholder();
            eAdditiveSuffix.Expression = new FirstOf(
                new CallbackHandler(new Sequence(new LiteralAnyCharOf("+-"), eMultiplicative, eAdditiveSuffix), delegate(object v) {
                    IVector a = (IVector)v;
                    IVector tail = (IVector)a[2];
                    return factory.InsertBefore(a[0], factory.InsertBefore(a[1], tail));
                //return v;
                //return ((object[])v)[1];
            }),
                //new EmptyExpression(null));
                new EmptyRule(factory.Empty));
            //eAdditive.Expression = new CallbackHandler( new Sequence(eMultiplicative, eAdditiveSuffix), DoAdd);
            eAdditive.Expression = new CallbackHandler(new Sequence(eMultiplicative, eAdditiveSuffix), delegate(object v) {
                IVector a = (IVector)v;
                IVector tail = (IVector)a[1];
                return factory.InsertBefore(a[0], tail);
            });
            Placeholder eMultiplicativeSuffix = new Placeholder();
            eMultiplicativeSuffix.Expression = new FirstOf(
                new CallbackHandler(new Sequence(new LiteralAnyCharOf("*/"), eSingular, eMultiplicativeSuffix), delegate(object v) {
                    IVector a = (IVector)v;
                    IVector tail = (IVector)a[2];
                    return factory.InsertBefore(a[0], factory.InsertBefore(a[1], tail));
                //return v;
                //return ((object[])v)[1];
            }),
                //new EmptyExpression(null));
                new EmptyRule(factory.Empty));
            //eMultiplicative.Expression = new CallbackHandler(new Sequence(eSingular, eMultiplicativeSuffix), DoMultiply);
            eMultiplicative.Expression = new CallbackHandler(new Sequence(eSingular, eMultiplicativeSuffix), delegate(object v) {
                IVector a = (IVector)v;
                IVector tail = (IVector)a[1];
                if (tail == factory.Empty) return a[0];
                return factory.InsertBefore(a[0], tail);
            });
            Rule expr = eAdditive;

            {
                var parser = new Parser(expr, TextCursor.Create("12+3*45+6+7*(6+2)*9+0"), factory);
                Result r = parser.Run();
                //string expected = "(+ 12 (* 3 45) 6 (* 7 8 9) 0)";
                string expected = "(12 + (3 * 45) + 6 + (7 * (6 + 2) * 9) + 0)";
                Assert.IsNotNull(r);
                Assert.IsFalse(r.Cursor.CanPop());
                Assert.AreEqual(expected, Convert.ToString(r.Value).Replace("\"", "").Replace("'", ""));
            }
            {
                var parser = new Parser(expr, TextCursor.Create("120+34*((5*6+7)*8+9)-1"), factory);
                Result r = parser.Run();
                Assert.IsNotNull(r);
                Assert.IsFalse(r.Cursor.CanPop());
                //Assert.AreEqual(120 + 34 * ((5 * 6 + 7) * 8 + 9) - 1, r.Value);
            }
        }

        [Test]
        public void TestCalc2A() {
            LispPrinter.PrintVectorsAsLists = true;//required to compare the result
            TestCalc2(true);//Use ArrayVectorFactory
        }
        [Test]
        public void TestCalc2B() {
            TestCalc2(false);//Use BNodeVectorFactory
        }
        //[Test]//used by TestCalc2A and TestCalc2B
        public void TestCalc2(bool useArray) {
            IVectorFactory factory = useArray ? (IVectorFactory)new ArrayVectorFactory() : (IVectorFactory)new BNodeVectorFactory();
            /*
                E ← T ((‘+’ / ‘-’) T)*
                T ← F (('*' / '/') F)*
                F ← N / '(' E ')'
                N ← [0-9]+
             */
            Rule eDigit = new LiteralAnyCharOf("0123456789");
            Placeholder eNumber = new Placeholder();
            eNumber.Expression = new FirstOf(new CollapseToString(new Sequence(eDigit, eNumber)), new CallbackHandler(eDigit, Convert.ToString));
            Placeholder eAdditive = new Placeholder();
            Placeholder eMultiplicative = new Placeholder();
            Rule eSingular = new FirstOf(eNumber, new ExtractOne(1, new Sequence(new Literal('('), eAdditive, new Literal(')'))));
            //
            var BinaryInfixToPrefix = new Function(delegate(object v) {
                IVector a = (IVector)v;
                IVector tail = (IVector)a[1];
                object x = a[0];
                while (tail != factory.Empty) {
                    x = factory.Create(tail[0], x, tail[1]);
                    tail = (IVector)tail[2];
                }
                return x;
            });
            Placeholder eAdditiveSuffix = new Placeholder();
            eAdditiveSuffix.Expression = new FirstOf(
                new Sequence(new LiteralAnyCharOf("+-"), eMultiplicative, eAdditiveSuffix),
                new EmptyRule(factory.Empty));
            eAdditive.Expression = new CallbackHandler(new Sequence(eMultiplicative, eAdditiveSuffix), BinaryInfixToPrefix);
            Placeholder eMultiplicativeSuffix = new Placeholder();
            eMultiplicativeSuffix.Expression = new FirstOf(
                new Sequence(new LiteralAnyCharOf("*/"), eSingular, eMultiplicativeSuffix),
                new EmptyRule(factory.Empty));
            eMultiplicative.Expression = new CallbackHandler(new Sequence(eSingular, eMultiplicativeSuffix), BinaryInfixToPrefix);
            Rule expr = eAdditive;

            {
                var parser = new Parser(expr, TextCursor.Create("12+3*45+6+7*(6+2)*9+0"), factory);
                Result r = parser.Run();
                string expected = "(+ (+ (+ (+ 12 (* 3 45)) 6) (* (* 7 (+ 6 2)) 9)) 0)";
                Assert.IsNotNull(r);
                Assert.IsFalse(r.Cursor.CanPop());
                Assert.AreEqual(expected, Convert.ToString(r.Value).Replace("\"", "").Replace("'", ""));
                object num = new CalculatorVisitor().Process(r.Value);
                Assert.AreEqual(Convert.ToDouble(12 + 3 * 45 + 6 + 7 * (6 + 2) * 9 + 0), num);
            }
            {
                var parser = new Parser(expr, TextCursor.Create("12+3*4a5+6+7*(6+2)*9+0"), factory);
                Result r = parser.Run();
                //Assert.IsNull(r);
                Assert.IsTrue(r.Cursor.CanPop());
                if (System.Diagnostics.Debugger.IsAttached) {
                    System.Diagnostics.Debug.WriteLine(parser.GetError());
                }
                Assert.AreEqual(6, parser.FailCursor.Position);
            }
            {
                var parser = new Parser(expr, TextCursor.Create("12+3*45+(6+7*(6+2)*9+0"), factory);
                Result r = parser.Run();
                //Assert.IsNull(r);
                Assert.IsTrue(r.Cursor.CanPop());
                if (System.Diagnostics.Debugger.IsAttached) {
                    System.Diagnostics.Debug.WriteLine(parser.GetError());
                }
                Assert.AreEqual(22, parser.FailCursor.Position);
            }
            {
                var parser = new Parser(expr, TextCursor.Create("(12+3*45+(6+7*(6+2)*9+0)"), factory);
                Result r = parser.Run();
                Assert.IsNull(r);
                //Assert.AreNotEqual(TextCursor.EOI, r.Cursor.Peek());
                if (System.Diagnostics.Debugger.IsAttached) {
                    System.Diagnostics.Debug.WriteLine(parser.GetError());
                }
                Assert.AreEqual(24, parser.FailCursor.Position);
            }
        }

        [Test]
        public void TestCalc2RepeatersA() {
            TestCalc2Repeaters(true);
        }
        [Test]
        public void TestCalc2RepeatersB() {
            TestCalc2Repeaters(false);
        }
        public void TestCalc2Repeaters(bool useArray) {
            IVectorFactory factory = useArray ? (IVectorFactory)new ArrayVectorFactory() : (IVectorFactory)new BNodeVectorFactory();
            /*
                E ← T ((‘+’ / ‘-’) T)*
                T ← F (('*' / '/') F)*
                F ← N / '(' E ')'
                N ← [0-9]+
             */
            var eNumber = new CollapseToString(Some.OneOrMore(new LiteralAnyCharOf("0123456789")));
            var factor = new Placeholder();
            var term = new Placeholder();
            var expr = new Placeholder();
            factor.Expression = new FirstOf(eNumber, new ExtractOne(1, new Sequence(new Literal('('), expr, new Literal(')'))));
            Function toPrefix = z => {
                    IVector v = (IVector)z;
                    object v0 = v[0];
                    IVector v1 = (IVector)v[1];
                    if (v1 == factory.Empty) return v0;
                    for (int i = 0; i < v1.Length; i++) {
                        IVector vv = (IVector)v1[i];
                        v0 = factory.Create(vv[0], v0, vv[1]);
                    }
                    return v0;
                };
            term.Expression = new CallbackHandler(new Sequence(factor, Some.ZeroOrMore(new Sequence(new LiteralAnyCharOf("*/"), factor))), toPrefix);
            expr.Expression = new CallbackHandler(new Sequence(term, Some.ZeroOrMore(new Sequence(new LiteralAnyCharOf("+-"), term))), toPrefix);

            {
                var parser = new Parser(expr, TextCursor.Create("12+3*45+6+7*(6+2)*9+0"), factory);
                Result r = parser.Run();
                string expected = "(+ (+ (+ (+ 12 (* 3 45)) 6) (* (* 7 (+ 6 2)) 9)) 0)";
                Assert.IsNotNull(r);
                Assert.IsFalse(r.Cursor.CanPop());
                Assert.AreEqual(expected, Convert.ToString(r.Value).Replace("\"", "").Replace("'", ""));
                object num = new CalculatorVisitor().Process(r.Value);
                Assert.AreEqual(Convert.ToDouble(12 + 3 * 45 + 6 + 7 * (6 + 2) * 9 + 0), num);
            }
            {
                var parser = new Parser(expr, TextCursor.Create("12+3*4a5+6+7*(6+2)*9+0"), factory);
                Result r = parser.Run();
                Assert.IsNotNull(r);
                Assert.IsTrue(r.Cursor.CanPop());
                if (System.Diagnostics.Debugger.IsAttached) {
                    System.Diagnostics.Debug.WriteLine(parser.GetError());
                }
                Assert.AreEqual(6, parser.FailCursor.Position);
            }
            {
                var parser = new Parser(expr, TextCursor.Create("12+3*45+(6+7*(6+2)*9+0"), factory);
                Result r = parser.Run();
                Assert.IsNotNull(r);
                Assert.IsTrue(r.Cursor.CanPop());
                if (System.Diagnostics.Debugger.IsAttached) {
                    System.Diagnostics.Debug.WriteLine(parser.GetError());
                }
                Assert.AreEqual(22, parser.FailCursor.Position);
            }
            {
                var parser = new Parser(expr, TextCursor.Create("(12+3*45+(6+7*(6+2)*9+0)"), factory);
                Result r = parser.Run();
                Assert.IsNull(r);
                if (System.Diagnostics.Debugger.IsAttached) {
                    System.Diagnostics.Debug.WriteLine(parser.GetError());
                }
                Assert.AreEqual(24, parser.FailCursor.Position);
            }
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