/********************************************************************
 * Recursive Descent Parser for PEG
 * 
 * (%)2013 KL / HexTex Mayhem
 * 
 * 
 ********************************************************************/
//TODO: automatic left recursion eliminator
//-DONE: ?,+,* operators
//TODO: &,! operators
//TODO: character class selector
//TODO: placeholder remover
//TODO: prefix remover
//-DONE: error handling
//-DONE(via ?/+/*): collapse values in repeaters (recursive calls) - { create a helper, insert it automatically when expanding repeaters }
//-DONE: Calculator example
//TODO: Collect statistics (expression invocations, live states, recursions count,..)
//TODO: AST ?1{functor+arglist} ?2{tuple,one of them is NodeType}
//TODO: ?make expression values optional
//TODO: redesign parser to use no static switches and error helpers

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
        ICursor Pop();
        object Peek();
        int Position { get; }
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
        public static object EOI { get { return eoi; } }
        public ICursor Pop() {
            if (position < text.Length) {
                return new TextCursor(text, position + 1);
            } else {
                return this;
            }
        }
        public object Peek() {
            if (position < text.Length) {
                return text[position];
            } else {
                return TextCursor.EOI;
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

    public abstract class Expression {
        public abstract Result Match(ICursor cursor);
    }

    public class EmptyExpression : Expression {
        private object value;
        public EmptyExpression(object value) {
            this.value = value;
        }
        public override Result Match(ICursor cursor) {
            return new Result(cursor, value);
        }
    }

    public class Placeholder : Expression {
        private Expression expr;
        public Placeholder() { }
        public Expression Expression { get { return expr; } set { expr = value; } }
        public override Result Match(ICursor cursor) {
            ParserHelper.Instance.GoDown();
            Result r = expr.Match(cursor);
            ParserHelper.Instance.GoUp();
            return r;
        }
    }

    public class Literal : Expression {
        private object item;
        public Literal(object item) {
            this.item = item;
        }
        public override Result Match(ICursor cursor) {
            if (Equals(cursor.Peek(), item)) {
                return new Result(cursor.Pop(), item);
            }
            ParserHelper.Instance.RememberFail(cursor, this);
            return null;
        }
    }
    public class LiteralChar : Expression {
        private string chars;
        public LiteralChar(string chars) {
            this.chars = chars;
        }
        public override Result Match(ICursor cursor) {
            object c = cursor.Peek();
            if (!typeof(char).IsInstanceOfType(c)) { ParserHelper.Instance.RememberFail(cursor, this); return null; }
            if (chars.IndexOf((char)c) < 0) { ParserHelper.Instance.RememberFail(cursor, this); return null; }
            return new Result(cursor.Pop(), c);
        }
    }

    public class Sequence : Expression {
        private Expression[] items;
        public Sequence(params Expression[] items) {
            this.items = items;
        }
        public override Result Match(ICursor cursor) {
            ParserHelper.Instance.GoDown();
            Result r;
            if (ParserHelper.VectorFactory is ArrayVectorFactory)
                r = MatchImpl1(cursor);
            else
                r = MatchImpl2(cursor);
            ParserHelper.Instance.GoUp();
            return r;
        }
        private Result MatchImpl1(ICursor cursor) {
            object[] values = new object[items.Length];
            for (int i = 0; i < items.Length; i++) {
                Result r = items[i].Match(cursor);
                if (r == null) return null;
                cursor = r.Cursor;
                values[i] = r.Value;
            }
            return new Result(cursor, ParserHelper.VectorFactory.Create(values));
        }
        private Result MatchImpl2(ICursor cursor) {
            IVector node = ParserHelper.VectorFactory.Empty;
            for (int i = 0; i < items.Length; i++) {
                Result r = items[i].Match(cursor);
                if (r == null) return null;
                cursor = r.Cursor;
                node = ParserHelper.VectorFactory.InsertBefore(r.Value, node);
            }
            return new Result(cursor, BNodeVectorFactory.Reverse(node));
        }
    }

    public class First : Expression {
        private Expression[] items;
        public First(params Expression[] items) {
            this.items = items;
        }
        public override Result Match(ICursor cursor) {
            ParserHelper.Instance.GoDown();
            try {
                for (int i = 0; i < items.Length; i++) {
                    Result r = items[i].Match(cursor);
                    if (r != null) return r;
                }
                return null;
            } finally {
                ParserHelper.Instance.GoUp();
            }
        }
    }

    public class Some : Expression {
        private Expression item;
        private int min, max;
        public Some(Expression item, int min, int max) {
            this.item = item;
            this.min = min;
            this.max = max;
        }
        public override Result Match(ICursor cursor) {
            ParserHelper.Instance.GoDown();
            try {
                IVector node = ParserHelper.VectorFactory.Empty;
                while (max < 0 || node.Length < max) {
                    Result r = item.Match(cursor);
                    if (r == null) {
                        if (node.Length < min) return null;
                        return new Result(cursor, BNodeVectorFactory.Reverse(node));
                    }
                    cursor = r.Cursor;
                    node = ParserHelper.VectorFactory.InsertBefore(r.Value, node);
                }
                return new Result(cursor, BNodeVectorFactory.Reverse(node));
            } finally {
                ParserHelper.Instance.GoUp();
            }
        }
        public static Expression ZeroOrMore(Expression item) {
            return new Some(item, 0, -1);
        }
        public static Expression OneOrMore(Expression item) {
            return new Some(item, 1, -1);
        }
        public static Expression Optional(Expression item) {
            return new Some(item, 0, 1);
        }

    }

    public abstract class Handler : Expression {
        private Expression expr;
        public Handler(Expression expr) {
            this.expr = expr;
        }
        public override Result Match(ICursor cursor) {
            ParserHelper.Instance.GoDown();
            try {
                Result r = expr.Match(cursor);
                if (r != null) return new Result(r.Cursor, ProcessValue(r.Value));
                return null;
            } finally {
                ParserHelper.Instance.GoUp();
            }
        }
        protected abstract object ProcessValue(object value);
    }

    public delegate object Function(object arg);

    public class CallbackHandler : Handler {
        private Function callback;
        public CallbackHandler(Expression expr, Function f)
            : base(expr) {
            this.callback = f;
        }
        protected override object ProcessValue(object value) {
            return callback(value);
        }
    }

    internal class TestHandler : Handler {
        public TestHandler(Expression expr) : base(expr) { }
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
        public CollapseToArray(Expression expr) : base(expr) { }
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
        public CollapseToString(Expression expr) : base(expr) { }
        protected override object ProcessValue(object value) {
            IVector a = (IVector)value;
            if (a.Length == 0) return string.Empty;
            if (a.Length == 1) return Convert.ToString(a[0]);
            return string.Concat(a[0], a[1]);
        }
    }

    public class TailStub : Expression {
        static object[] stub = new object[0];
        public override Result Match(ICursor cursor) {
            return new Result(cursor, stub);
        }
    }

    public class ExtractOne : Handler {
        private int position;
        public ExtractOne(int position, Expression expr)
            : base(expr) {
            this.position = position;
        }
        protected override object ProcessValue(object value) {
            return ((IVector)value)[position];            
        }
    }


    public class ExtractFirstOrSelf : Handler {
        public ExtractFirstOrSelf(Expression expr)
            : base(expr) { }
        protected override object ProcessValue(object value) {
            IVector v = (IVector)value;
            if (v.Length == 0) return v;
            return v[0];
        }
    }

    public class ParserHelper {
        public static ParserHelper Instance;
        static ParserHelper() { Instance = new ParserHelper(); }

        private static IVectorFactory factory = new ArrayVectorFactory();
        public static IVectorFactory VectorFactory { get { return factory; } }
        public static void UseArray(bool v) {
            factory = v ? (IVectorFactory)new ArrayVectorFactory() : (IVectorFactory)new BNodeVectorFactory();
        }

        private int depth;
        private int maxDepth;
        private int failDepth;
        private ICursor failCursor;
        private Expression failExpression;

        public int Depth { get { return depth; } }
        public int MaxDepth { get { return maxDepth; } }
        public ICursor FailCursor { get { return failCursor; } }
        public string GetError() {
            if (failCursor == null) return string.Empty;
            object unexpected = failCursor.Peek();
            if (Equals(TextCursor.EOI, unexpected)) unexpected = "{EOI}";
            return string.Format("Syntax error at {0} (unexpected '{1}')", failCursor.Position, unexpected);
        }
        internal void GoDown() {
            depth++;
            maxDepth = Math.Max(maxDepth, depth);
        }
        internal void GoUp() {
            depth--;
        }
        internal void RememberFail(ICursor cursor, Expression expr) {
            if (failCursor != null && failCursor.Position > cursor.Position) return;
            failDepth = depth;
            failCursor = cursor;
            failExpression = expr;
        }
    }

    //
    // AST
    //

    public interface IASTFactory {
        object CreateNode(object type, params object[] items);
    }

    //public class BNodeFactory : IASTFactory {
    //    //public object Create(object head, object tail) { return new BNode(head, tail); }

    //    #region IASTFactory Members

    //    object IASTFactory.CreateNode(object type, params object[] items) {
    //        throw new NotImplementedException();
    //    }

    //    #endregion
    //}

    //public class ArrayNodeFactory : IASTFactory {
    //    #region IASTFactory Members

    //    object IASTFactory.CreateNode(object type, params object[] items) {
    //        throw new NotImplementedException();
    //    }

    //    #endregion
    //}

}