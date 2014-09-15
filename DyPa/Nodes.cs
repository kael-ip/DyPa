using System;
using System.Collections;
using System.Collections.Generic;
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
        IVector Reverse(IVector v);
        IEnumerable AsEnumerable(IVector v);
    }

    #region ArrayVector

    class Vector : IVector {
        private object[] items;
        public object[] Items { get { return items; } }
        public Vector(object[] items) {
            this.items = new object[items.Length];
            System.Array.Copy(items, this.items, items.Length);
        }
        public Vector(object o, object[] rest) {
            this.items = new object[rest.Length + 1];
            this.items[0] = o;
            System.Array.Copy(rest, 0, this.items, 1, rest.Length);
        }
        public override string ToString() {
            return LispPrinter.ToString(this);
        }

        #region IVector Members

        public int Length { get { return items.Length; } }
        public object this[int index] { get { return items[index]; } }

        #endregion
    }

    public class ArrayVectorFactory : IVectorFactory {
        private Vector empty = new Vector(new object[0]);

        #region IVectorFactory Members

        public IVector Empty { get { return empty; } }
        public IVector InsertBefore(object o, IVector v) {
            if (v is Vector) {
                return new Vector(o, ((Vector)v).Items);
            }
            throw new NotSupportedException();
        }
        public IVector Create(params object[] values) {
            return new Vector(values);
        }
        public IVector Reverse(IVector v) {
            if(v.Length <= 1) return v;
            object[] a = new object[v.Length];
            for (int i = 0; i < a.Length; i++) {
                a[a.Length - i - 1] = v[i];
            }
            return this.Create(a);
        }
        public IEnumerable AsEnumerable(IVector v) {
            return ((Vector)v).Items;
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

        static object Reverse(object node) {
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
        public IVector Reverse(IVector v) {
            return (IVector)Reverse((object)v);
        }
        public IEnumerable AsEnumerable(IVector v) {
            while (v is BNode) {
                yield return ((BNode)v).Head;
                v = ((BNode)v).Tail as IVector;
            }
            yield break;
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

    public class VectorFactoryHelper {
        IVectorFactory factory;
        public VectorFactoryHelper(IVectorFactory factory){
            this.factory = factory;
        }
        public string ToString(IVector v) {
            StringBuilder sb = new StringBuilder();
            foreach (object o in factory.AsEnumerable(v)) {
                sb.Append(o);
            }
            return sb.ToString();
        }
        public List<T> ToList<T>(IVector v) {
            List<T> list = new List<T>();
            foreach (T o in factory.AsEnumerable(v)) list.Add(o);
            return list;
        }
    }
}