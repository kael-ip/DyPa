/********************************************************************
 * Recursive Descent Parser for PEG
 * 
 * (%)2013 KL / HexTex Mayhem
 * 
 * 
 ********************************************************************/
//TODO: automatic left recursion eliminator
//-DONE: ?,+,* operators
//-DONE: &,! operators
//TODO: character class selector
//TODO: placeholder remover
//TODO: prefix remover
//-DONE: error handling
//-DONE(via ?/+/*): collapse values in repeaters (recursive calls) - { create a helper, insert it automatically when expanding repeaters }
//-DONE: Calculator example
//TODO: Collect statistics (expression invocations, live states, recursions count,..)
//TODO: AST ?1{functor+arglist} ?2{tuple,one of them is NodeType}
//TODO: ?make expression values optional
//-DONE: redesign parser to use no static switches and error helpers

using System.Collections.Generic;
using System;
using HexTex.Data.Common;

namespace HexTex.Dypa.PEG {

    /*
     * Recursive Descent Parser
     * 
     * process variants:
     * 1. Direct - recursively call nested expressions.
     *  expr call argument is the input position only.
     *  a successfully matched expr should rerurn the new input position and a value,
     *  otherwise nothing is modified.
     * 2. Passive - allows step-by-step processing by returning continuations.
     * 
     */

    public interface ICursor {
        bool CanPop();
        ICursor Pop();
        object Peek();
        int Position { get; }
    }

    public class Cursor<T> : ICursor {
        private IEnumerator<T> nseq;
        private Cursor<T> next;
        private T current;
        private bool hasValue;
        private int position;
        public Cursor(IEnumerable<T> e) : this(e.GetEnumerator(), 0) { }
        public Cursor(IEnumerator<T> seq, int position) {
            this.hasValue = seq.MoveNext();
            if (hasValue) {
                this.current = seq.Current;
            }
            this.nseq = seq;
            this.position = position;
        }
        public bool CanPop() { return hasValue; }
        public ICursor Pop() {
            if (CanPop()) {
                if (next == null) {
                    next = new Cursor<T>(nseq, position + 1);
                }
                return next;
            } else {
                return this;
            }
        }
        object ICursor.Peek() {
            if (CanPop()) {
                return current;
            } else {
                return null;
            }
        }
        public T Peek() {
            if (CanPop()) {
                return current;
            } else {
                return default(T);
            }
        }
        public int Position { get { return position; } }
    }

    public class TextCursor : ICursor {
        private string text;
        private int position;
        private static object eoi = new object();
        private TextCursor(string text, int position) {
            this.text = text;
            this.position = position;
        }
        public static TextCursor Create(string text) {
            if (text == null) throw new ArgumentNullException("text");
            return new TextCursor(text, 0);
        }
        public bool CanPop() { return position < text.Length; }
        public ICursor Pop() {
            if (CanPop()) {
                return new TextCursor(text, position + 1);
            } else {
                return this;
            }
        }
        public object Peek() {
            if (CanPop()) {
                return text[position];
            } else {
                return eoi;
            }
        }
        public int Position { get { return position; } }
    }

    public class Result {
        private ICursor cursor;
        private object value;
        public Result(ICursor cursor, object value) {
            this.cursor = cursor;
            this.value = value;
        }
        public ICursor Cursor { get { return cursor; } }
        public object Value { get { return value; } }
    }

    public abstract class Rule {
        public abstract Result Match(Parser parser, ICursor cursor);
    }

    public class EmptyRule : Rule {
        private object value;
        public EmptyRule(object value) {
            this.value = value;
        }
        public override Result Match(Parser parser, ICursor cursor) {
            return new Result(cursor, value);
        }
    }

    public class Placeholder : Rule {
        private Rule expr;
        public Placeholder() { }
        public Rule Expression { get { return expr; } set { expr = value; } }
        public override Result Match(Parser parser, ICursor cursor) {
            parser.GoDown();
            Result r = expr.Match(parser, cursor);
            parser.GoUp();
            return r;
        }
    }

    #region Primitives

    public class LiteralEOI : Rule {
        public override Result Match(Parser parser, ICursor cursor) {
            if (!cursor.CanPop()) {
                return new Result(cursor, null);
            }
            parser.RememberFail(cursor, this);
            return null;
        }
    }

    public class LiteralAny : Rule {
        public override Result Match(Parser parser, ICursor cursor) {
            if (!cursor.CanPop()) { parser.RememberFail(cursor, this); return null; }
            return MatchSome(parser, cursor);
        }
        protected virtual Result MatchSome(Parser parser, ICursor cursor) {
            object t = cursor.Peek();
            return new Result(cursor.Pop(), t);
        }
    }

    public abstract class Literal<T> : Rule {
        public override Result Match(Parser parser, ICursor cursor) {
            if (cursor.CanPop()) {
                if (typeof(T).IsInstanceOfType(cursor.Peek())) {
                    T item = (T)cursor.Peek();
                    if (MatchImpl(item)) {
                        return new Result(cursor.Pop(), item);
                    }
                }
            }
            parser.RememberFail(cursor, this);
            return null;
        }
        protected abstract bool MatchImpl(T item);
    }

    public class Literal : Literal<object> {
        private object item;
        public Literal(object item) {
            this.item = item;
        }
        protected override bool MatchImpl(object item) {
            return Equals(this.item, item);
        }
    }

    public class LiteralAnyCharOf : Literal<char> {
        private string chars;
        public LiteralAnyCharOf(string chars) {
            this.chars = chars;
        }
        protected override bool MatchImpl(char item) {
            return chars.IndexOf(item) >= 0;
        }
    }

    public class LiteralCharCategory : Literal<char> {
        private System.Globalization.UnicodeCategory category;
        public LiteralCharCategory(System.Globalization.UnicodeCategory category) {
            this.category = category;
        }
        protected override bool MatchImpl(char item) {
            return Equals(category, char.GetUnicodeCategory(item));
        }
    }

    public class LiteralString : Rule {
        private string chars;
        public LiteralString(string chars) {
            this.chars = chars;
        }
        public override Result Match(Parser parser, ICursor cursor) {
            ICursor cur = cursor;
            for (int i = 0; i < chars.Length; i++) {
                if (!cur.CanPop()) { parser.RememberFail(cursor, this); return null; }
                object c = cur.Peek();
                if (!typeof(char).IsInstanceOfType(c)) { parser.RememberFail(cursor, this); return null; }
                if (!Equals(chars[i], (char)c)) { parser.RememberFail(cursor, this); return null; }
                cur = cur.Pop();
            }
            return new Result(cur, chars);
        }
    }

    #endregion

    #region Compositions

    public class Sequence : Rule {
        private Rule[] items;
        public Sequence(params Rule[] items) {
            this.items = items;
        }
        public override Result Match(Parser parser, ICursor cursor) {
            parser.GoDown();
            Result r;
            if (parser.VectorFactory is ArrayVectorFactory)
                r = MatchImpl1(parser, cursor);
            else
                r = MatchImpl2(parser, cursor);
            parser.GoUp();
            return r;
        }
        private Result MatchImpl1(Parser parser, ICursor cursor) {
            object[] values = new object[items.Length];
            for (int i = 0; i < items.Length; i++) {
                Result r = items[i].Match(parser, cursor);
                if (r == null) return null;
                cursor = r.Cursor;
                values[i] = r.Value;
            }
            return new Result(cursor, parser.VectorFactory.Create(values));
        }
        private Result MatchImpl2(Parser parser, ICursor cursor) {
            IVector node = parser.VectorFactory.Empty;
            for (int i = 0; i < items.Length; i++) {
                Result r = items[i].Match(parser, cursor);
                if (r == null) return null;
                cursor = r.Cursor;
                node = parser.VectorFactory.InsertBefore(r.Value, node);
            }
            return new Result(cursor, parser.VectorFactory.Reverse(node));
        }
    }

    public class FirstOf : Rule {
        private Rule[] items;
        public FirstOf(params Rule[] items) {
            this.items = items;
        }
        public override Result Match(Parser parser, ICursor cursor) {
            parser.GoDown();
            try {
                for (int i = 0; i < items.Length; i++) {
                    Result r = items[i].Match(parser, cursor);
                    if (r != null) return r;
                }
                return null;
            } finally {
                parser.GoUp();
            }
        }
    }

    public class Some : Rule {
        private Rule item;
        private int min, max;
        public Some(Rule item, int min, int max) {
            this.item = item;
            this.min = min;
            this.max = max;
        }
        public override Result Match(Parser parser, ICursor cursor) {
            parser.GoDown();
            try {
                IVector node = parser.VectorFactory.Empty;
                while (max < 0 || node.Length < max) {
                    Result r = item.Match(parser, cursor);
                    if (r == null) {
                        if (node.Length < min) return null;
                        return new Result(cursor, parser.VectorFactory.Reverse(node));
                    }
                    cursor = r.Cursor;
                    node = parser.VectorFactory.InsertBefore(r.Value, node);
                }
                return new Result(cursor, parser.VectorFactory.Reverse(node));
            } finally {
                parser.GoUp();
            }
        }
        public static Rule ZeroOrMore(Rule item) {
            return new Some(item, 0, -1);
        }
        public static Rule OneOrMore(Rule item) {
            return new Some(item, 1, -1);
        }
        public static Rule Optional(Rule item) {
            return new Some(item, 0, 1);
        }

    }

    public class Predicate : Rule {
        private Rule item;
        private bool inverse;
        public Predicate(Rule item, bool inverse) {
            this.item = item;
            this.inverse = inverse;
        }
        public override Result Match(Parser parser, ICursor cursor) {
            Result r = item.Match(parser, cursor);
            if (r == null && inverse || r != null && !inverse) {
                return new Result(cursor, null);
            } else {
                return null;
            }
        }
        public static Rule And(Rule item) {
            return new Predicate(item, false);
        }
        public static Rule Not(Rule item) {
            return new Predicate(item, true);
        }
    }

    #endregion

    #region Handlers

    public abstract class Handler : Rule {
        private Rule expr;
        public Handler(Rule expr) {
            this.expr = expr;
        }
        public override Result Match(Parser parser, ICursor cursor) {
            parser.GoDown();
            try {
                Result r = expr.Match(parser, cursor);
                if (r != null) return new Result(r.Cursor, ProcessValue(r.Value));
                return null;
            } finally {
                parser.GoUp();
            }
        }
        protected abstract object ProcessValue(object value);
    }

    public delegate object Function(object arg);

    public class CallbackHandler : Handler {
        private Function callback;
        public CallbackHandler(Rule expr, Function f)
            : base(expr) {
            this.callback = f;
        }
        protected override object ProcessValue(object value) {
            return callback(value);
        }
    }

    internal class TestHandler : Handler {
        public TestHandler(Rule expr) : base(expr) { }
        protected override object ProcessValue(object value) {
            return value;
        }
    }

    //
    // Value helpers
    //

    //public static class ValueHelper {
    //    public static Expression CollapseToArray(Expression expr) { return new CollapseToArray(expr); }
    //}

    // expr = Sequence(item, accumulator)
    public class CollapseToArray : Handler {
        public CollapseToArray(Rule expr) : base(expr) { }
        protected override object ProcessValue(object value) {
            object[] a = (object[])value;
            if (a[1] == null) return new object[] { a[0] };
            object[] tail = (object[])a[1];
            object[] values = new object[tail.Length + 1];
            values[0] = a[0];
            Array.Copy(tail, 0, values, 1, tail.Length);
            return values;
            //return string.Concat(a[0], a[1]);
        }
    }

    // expr = Sequence(item, string)
    public class CollapseToString : Handler {
        IVectorFactory factory;
        public CollapseToString(Rule expr) : this(null, expr) { }
        public CollapseToString(IVectorFactory factory, Rule expr)
            : base(expr) {
            this.factory = factory;
        }
        protected override object ProcessValue(object value) {
            IVector a = (IVector)value;
            int length = a.Length;
            if (length == 0) return string.Empty;
            if (length == 1) return Convert.ToString(a[0]);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (factory == null) {
                for (int i = 0; i < a.Length; i++) {
                    sb.Append(a[i]);
                }
            } else {
                foreach (object x in factory.AsEnumerable(a)) {
                    sb.Append(x);
                }
            }
            return sb.ToString();
        }
    }

    public class TailStub : Rule {
        static object[] stub = new object[0];
        public override Result Match(Parser parser, ICursor cursor) {
            return new Result(cursor, stub);
        }
    }

    public class ExtractOne : Handler {
        private int position;
        public ExtractOne(int position, Rule expr)
            : base(expr) {
            this.position = position;
        }
        protected override object ProcessValue(object value) {
            return ((IVector)value)[position];            
        }
    }


    public class ExtractFirstOrSelf : Handler {
        public ExtractFirstOrSelf(Rule expr)
            : base(expr) { }
        protected override object ProcessValue(object value) {
            IVector v = (IVector)value;
            if (v.Length == 0) return v;
            return v[0];
        }
    }

    #endregion

    public class Parser {
        private IVectorFactory factory;
        public IVectorFactory VectorFactory { get { return factory; } }

        protected Rule rule;
        protected ICursor cursor;
        private int depth;
        private int maxDepth;
        private int failDepth;
        private ICursor failCursor;
        private Rule failExpression;

        public int Depth { get { return depth; } }
        public int MaxDepth { get { return maxDepth; } }
        public ICursor FailCursor { get { return failCursor; } }
        public string GetError() {
            if (failCursor == null) return string.Empty;
            object unexpected;
            if (failCursor.CanPop()) {
                unexpected = failCursor.Peek();
            } else {
                unexpected = "{EOI}";
            }
            return string.Format("Syntax error at {0} (unexpected '{1}')", failCursor.Position, unexpected);
        }
        public void GoDown() {
            depth++;
            maxDepth = Math.Max(maxDepth, depth);
        }
        public void GoUp() {
            depth--;
        }
        public void RememberFail(ICursor cursor, Rule expr) {
            if (failCursor != null && failCursor.Position > cursor.Position) return;
            failDepth = depth;
            failCursor = cursor;
            failExpression = expr;
        }
        public Parser(Rule rule, ICursor cursor)
            : this(rule, cursor, new ArrayVectorFactory()) {
        }
        public Parser(Rule rule, ICursor cursor, IVectorFactory factory) {
            this.factory = factory;
            this.rule = rule;
            this.cursor = cursor;
        }
        public Result Run() {
            return rule.Match(this, cursor);
        }
    }

}