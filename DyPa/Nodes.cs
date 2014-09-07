using System;
using System.Text;

namespace HexTex.Data.Common {

    //
    // Vectors
    //

    public interface IVector {
        int Length { get; }
        object this[int index] { get; }
    }

    public interface IVectorFactory {
        IVector Empty { get; }
        IVector Create(params object[] values);
        IVector InsertBefore(object o, IVector v);
    }

    #region ArrayVector

    class Vector : IVector {
        public object[] array;
        public Vector(object[] items) {
            this.array = new object[items.Length];
            Array.Copy(items, array, items.Length);
        }
        public Vector(object o, object[] rest) {
            this.array = new object[rest.Length + 1];
            array[0] = o;
            Array.Copy(rest, 0, array, 1, rest.Length);
        }
        public override string ToString() {
            return LispPrinter.ToString(this);
        }

        #region IVector Members

        public int Length { get { return array.Length; } }
        public object this[int index] { get { return array[index]; } }

        #endregion
    }

    public class ArrayVectorFactory : IVectorFactory {

        //private static IVectorFactory instance = new ArrayVectorFactory();
        //public static IVectorFactory Instance { get { return instance; } }

        private Vector empty = new Vector(new object[0]);

        #region IVectorFactory Members

        public IVector Empty { get { return empty; } }
        public IVector InsertBefore(object o, IVector v) {
            if (v is Vector) {
                return new Vector(o, ((Vector)v).array);
            }
            throw new NotSupportedException();
        }
        public IVector Create(params object[] values) {
            return new Vector(values);
        }

        #endregion
    }

    #endregion

    #region BNodeVector

    class BNodeNil : IVector {
        private static BNodeNil instance = new BNodeNil();
        public static BNodeNil Instance { get { return instance; } }
        private BNodeNil() { }
        public override string ToString() {
            return LispPrinter.ToString(this);
        }

        #region IVector Members

        public int Length { get { return 0; } }
        public object this[int index] { get { throw new IndexOutOfRangeException(); } }

        #endregion
    }
    class BNode : IVector {
        private object head, tail;
        protected internal BNode(object head, object tail) {
            this.head = head;
            this.tail = tail;
        }
        public object Head { get { return head; } }
        public object Tail { get { return tail; } }
        public override string ToString() {
            return LispPrinter.ToString(this);
        }

        #region IVector Members

        public int Length { get { return (Tail is BNode) ? (1 + ((BNode)Tail).Length) : 1; } }
        public object this[int index] {
            get {
                if (index == 0) return Head;
                if (Tail is BNode) return ((BNode)Tail)[index - 1];
                throw new IndexOutOfRangeException();
            }
        }

        #endregion
    }

    public class BNodeVectorFactory : IVectorFactory {

        //private static BNodeVectorFactory instance = new BNodeVectorFactory();
        //public static BNodeVectorFactory Instance { get { return instance; } }

        public static object Reverse(object node) {
            object rnode = BNodeNil.Instance;
            BNode bnode = node as BNode;
            while (bnode != null) {
                rnode = new BNode(bnode.Head, rnode);
                bnode = bnode.Tail as BNode;
            }
            return rnode;
        }
        #region IVectorFactory Members

        public IVector Empty { get { return BNodeNil.Instance; } }
        public IVector InsertBefore(object o, IVector v) {
            return new BNode(o, v);
        }
        public IVector Create(params object[] values) {
            IVector node = BNodeNil.Instance;
            for (int i = values.Length; i > 0; i--) {
                node = new BNode(values[i - 1], node);
            }
            return node;
        }

        #endregion
    }

    #endregion

    public static class LispPrinter {
        public static bool PrintVectorsAsLists = false;
        public static string ToString(object o) {
            if (o == null) return "#NULL";
            if (o is BNodeNil) return "()";
            if (o is char) return string.Format("'{0}'", o);
            if (o is string) return string.Format("\"{0}\"", Escape((string)o));
            if (o is BNode) return string.Concat("(", BodyToString((BNode)o), ")");
            if (o is Vector) return string.Concat((PrintVectorsAsLists ? "" : "#"), "(", BodyToString((Vector)o), ")");
            return string.Format("#{0}({1})", o.GetType().FullName, o);
        }
        private static string BodyToString(Vector vector) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < vector.Length; i++) {
                if (sb.Length != 0) sb.Append(" ");
                sb.Append(LispPrinter.ToString(vector[i]));
            }
            return sb.ToString();
        }
        private static string BodyToString(BNode node) {
            string shead = LispPrinter.ToString(node.Head);
            if (node.Tail is BNodeNil) {
                return shead;
            } else if (node.Tail is BNode) {
                return string.Concat(shead, " ", BodyToString((BNode)node.Tail));
            } else {
                return string.Concat(shead, " . ", LispPrinter.ToString(node.Tail));
            }
        }
        public static string Escape(string s) {
            return s.Replace("\"", "\\\"");
        }
    }
}