using HexTex.Data.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace HexTex.Dypa.PEG {
    [TestFixture]
    public class RDParserTests2 {
        [Test]
        public void TestComplex() {
            Function toString = delegate(object r) {
                IVector v = (IVector)r;
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < v.Length; i++) {
                    sb.Append(v[i]);
                }
                return sb.ToString();
            };
            {
                var _digits = new LiteralAnyCharOf("0123456789");
                //var _letters = new LiteralAnyCharOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
                var _letters = new FirstOf(
                    new LiteralCharCategory(System.Globalization.UnicodeCategory.LowercaseLetter),
                    new LiteralCharCategory(System.Globalization.UnicodeCategory.UppercaseLetter),
                    new LiteralCharCategory(System.Globalization.UnicodeCategory.TitlecaseLetter),
                    new LiteralCharCategory(System.Globalization.UnicodeCategory.LetterNumber),
                    new LiteralCharCategory(System.Globalization.UnicodeCategory.ModifierLetter),
                    new LiteralCharCategory(System.Globalization.UnicodeCategory.OtherLetter)
                    );
                var _number = new CallbackHandler(Some.OneOrMore(_digits), toString);
                var _ident = new CallbackHandler(new Sequence(_letters, new CallbackHandler(Some.ZeroOrMore(new FirstOf(_digits, _letters)), toString)), toString);
                var _whitespace = new LiteralAnyCharOf(" \t\n\r\f\v");
                var _comment = new Sequence(new LiteralString("/*"), Some.ZeroOrMore(new Sequence(Predicate.Not(new LiteralString("*/")), new LiteralAny())), new LiteralString("*/"));
                var _atmosphere = new FirstOf(_whitespace, _comment);
                var _lexeme = new FirstOf(new LiteralAnyCharOf("{"), new LiteralAnyCharOf("}"), new LiteralAnyCharOf(";"),
                    new ExtractOne(0, new Sequence(new FirstOf(_ident, _number), Predicate.And(new FirstOf(new LiteralAnyCharOf("{};"), _atmosphere, new LiteralEOI())))));
                var q = new ExtractOne(1, new Sequence(Some.ZeroOrMore(_atmosphere), Some.ZeroOrMore(new ExtractOne(0, new Sequence(_lexeme, Some.ZeroOrMore(_atmosphere)))), new LiteralEOI()));
                {
                    var parser = new Parser(q, TextCursor.Create("   a1 set plus 32434 Ratio; /* ffwe34kfn3k4u3f$#$df \n34f2$F@$F$F#$DF#E#44  432#@$# $%@$***/ define edit {f 2 xz /*****/ e} /*\r*/\r;\r"));
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    IVector v = r.Value as IVector;
                    Assert.IsNotNull(v);
                    Assert.AreEqual(15, v.Length);
                    Assert.AreEqual("a1", v[0]);
                    Assert.AreEqual('{', v[8]);
                    Assert.AreEqual('}', v[13]);
                    Assert.AreEqual(';', v[14]);
                }
            }
        }
        [Test]
        public void TestNestedA() {
            TestNested(true);
        }
        [Test]
        public void TestNestedB() {
            TestNested(false);
        }
        public void TestNested(bool useArray) {
            IVectorFactory factory = useArray ? (IVectorFactory)new ArrayVectorFactory() : (IVectorFactory)new BNodeVectorFactory();
            Function toString = delegate(object r) {
                StringBuilder sb = new StringBuilder();
                foreach (object o in factory.AsEnumerable((IVector)r)) {
                    sb.Append(o);
                }
                return sb.ToString();
            };
            Function toList = delegate(object r) {
                List<object> list = new List<object>();
                foreach (object o in factory.AsEnumerable((IVector)r)) list.Add(o);
                return list;
            };
            {
                var _digits = new LiteralAnyCharOf("0123456789");
                var _letters = new LiteralAnyCharOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
                var _number = new CallbackHandler(Some.OneOrMore(_digits), (v) => { return new Token("number", toString(v)); });
                var _ident = new CallbackHandler(new Sequence(_letters, new CallbackHandler(Some.ZeroOrMore(new FirstOf(_digits, _letters)), toString)),
                    (v) => { return new Token("ident", toString(v)); });
                var _whitespace = new LiteralAnyCharOf(" \t\n\r\f\v");
                var _comment = new Sequence(new LiteralString("/*"), Some.ZeroOrMore(new Sequence(
                    Predicate.Not(new LiteralString("*/")), new LiteralAny())), new LiteralString("*/"));
                var _atmosphere = new FirstOf(_whitespace, _comment);
                var _lexeme = new FirstOf(
                    new CallbackHandler(new FirstOf(new LiteralAnyCharOf("{"), new LiteralAnyCharOf("}"), new LiteralAnyCharOf("|")),
                        (v) => { string s = Convert.ToString(v); return new Token(s, s); }),
                    new ExtractOne(0, new Sequence(new FirstOf(_ident, _number), 
                        Predicate.And(new FirstOf(new LiteralAnyCharOf("{}|"), _atmosphere, new LiteralEOI())))));
                var q = new ExtractOne(1, new Sequence(Some.ZeroOrMore(_atmosphere), 
                    Some.ZeroOrMore(new ExtractOne(0, new Sequence(_lexeme, Some.ZeroOrMore(_atmosphere)))), new LiteralEOI()));
                {
                    var parser = new Parser(q, TextCursor.Create("   a1 set plus 32434 Ratio| /* ffwe34kfn3k4u3f$#$df \n34f2$F@$F$F#$DF#E#44  432#@$# $%@$***/ define edit {f 2 xz /*****/ e} /*\r*/\r|\r"),
                        factory);
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    IVector v = r.Value as IVector;
                    Assert.IsNotNull(v);
                    Assert.AreEqual(15, v.Length);
                    List<Token> list = new VectorFactoryHelper(factory).ToList<Token>(v);
                    Assert.AreEqual("ident", list[0].ID);
                    Assert.AreEqual("a1", list[0].Value);
                    Assert.AreEqual("number", list[3].ID);
                    Assert.AreEqual("32434", list[3].Value);
                    Assert.AreEqual("{", list[8].ID);
                    Assert.AreEqual("{", list[8].Value);
                    Assert.AreEqual("}", list[13].ID);
                    Assert.AreEqual("}", list[13].Value);
                    Assert.AreEqual("|", list[14].ID);
                    Assert.AreEqual("|", list[14].Value);
                }
                var _sym = new CallbackHandler(new LiteralToken("ident"), x => ((Token)x).Value);
                var _num = new CallbackHandler(new LiteralToken("number"), x => Convert.ToInt32(((Token)x).Value));
                var _list = new Placeholder();
                var _expr = new FirstOf(_list, _sym, _num);
                _list.Expression = new FirstOf(
                    new CallbackHandler(new Sequence(new LiteralToken("{"), Some.ZeroOrMore(_expr), new LiteralToken("}")), x => toNode((IVector)((IVector)x)[1], null)),
                    new CallbackHandler(new Sequence(new LiteralToken("{"), Some.OneOrMore(_expr), new LiteralToken("|"), _expr, new LiteralToken("}")), x => toNode((IVector)((IVector)x)[1], ((IVector)x)[3]))
                    );
                var q2 = new ExtractOne(0, new Sequence(_list, new LiteralEOI()));
                {
                    var parser = new Parser(q,
                        new Cursor<char>("   { any 90 {set {plus 32434 Ratio}|/* ffwetrash34kfn3k4u3f$#$df \n34f2$F@$F$F#$DF#E#44  432#@$# $%@$***/ define} {edit {f 2 {}xz /*****/ e} /*\r*/|\rr}\r    \n111} \t"),
                        factory);
                    var r = parser.Run();
                    Assert.IsNotNull(r);
                    IVector v = r.Value as IVector;
                    Assert.IsNotNull(v);
                    List<Token> list = new VectorFactoryHelper(factory).ToList<Token>(v);
                    var parser2 = new Parser(q2, new Cursor<Token>(list), factory);
                    var r2 = parser2.Run();
                    Assert.IsNotNull(r2);
                    BNode n1 = r2.Value as BNode;
                    Assert.IsNotNull(n1);
                    Assert.AreEqual(5, n1.Length);
                    Assert.AreEqual("(any #System.Int32(90) (set (plus #System.Int32(32434) Ratio) . define) (edit (f #System.Int32(2) () xz e) . r) #System.Int32(111))", n1.ToString().Replace("\"", ""));
                }
            }
        }
        static object toNode(IVector v, object tail) {
            if (tail == null) tail = BNodeNil.Instance;
            for (int i = v.Length; i > 0; i--) {
                tail = new BNode(v[i - 1], tail);
            }
            return tail;
        }

    }

    public struct Token {
        private string id;
        private object value;
        public Token(string id, object value) {
            this.id = id;
            this.value = value;
        }
        public string ID { get { return id; } }
        public object Value { get { return value; } }
    }

    public class LiteralToken : Literal<Token> {
        private string id;
        public LiteralToken(string id) {
            this.id = id;
        }
        protected override bool MatchImpl(Token item) {
            return item.ID == id;
        }
    }
}
