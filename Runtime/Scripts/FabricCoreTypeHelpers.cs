using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fabric.Player
{
    /*
     * SerializableDictionary
     * - By default, Unity does not serialize Dictionaries
     * - Use this as a replacement for Dictionary if you want the stored data to persist when Unity reloads script assemblies
     * - It copies the keys & values into Lists before the assemblies reload, and then copies the data back into the dictionary
     */
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();

        [SerializeField]
        private List<TValue> values = new List<TValue>();
        
        public SerializableDictionary() { }
        public SerializableDictionary(SerializableDictionary<TKey, TValue> copy)
        {
            keys = copy.keys;
            values = copy.values;
        }
        
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            this.Clear();

            if (keys.Count != values.Count)
            {
                Debug.LogError(string.Format("there are {0} keys and {1} values after deserialization. Check that both key/value types are serializable."
                    , keys.Count
                    , values.Count));
            }

            for (int i = 0; i < keys.Count; i++)
            {
                this.Add(keys[i], values[i]);
            }
        }
    }

    /*
     * NestedList
     * - By default, Unity does not serialize nested Lists (i.e. List<List<T>> or Dictionary<TKey, List<T>>, etc.)
     * - Use this as a replacement for a nested List if you want the stored data to persist when Unity reloads script assemblies
     * - This is a wrapper class for List. Only part of the List API has been implemented, extend when needed
     */
    [Serializable]
    public class NestedList<T> : IEnumerable
    {
        [SerializeField]
        public List<T> list = new List<T>();

        public NestedList() { }
        public NestedList(IEnumerable<T> collection)
        {
            this.list = collection.ToList<T>();
        }

        public T this[int index]
        {
            get
            {
                return list[index];
            }
            set
            {
                list[index] = value;
            }
        }

        public void Add(T item) { list.Add(item); }
        public void AddRange(IEnumerable<T> items) { list.AddRange(items); }
        public void Clear() { list.Clear(); }
        public bool Contains(T item) { return list.Contains(item); }
        public T Find(Predicate<T> predicate) { return list.Find(predicate); }
        public List<T>.Enumerator GetEnumerator() { return list.GetEnumerator(); }
        public int IndexOf(T item) { return list.IndexOf(item); }
        public void Insert(int index, T item) { list.Insert(index, item); }
        public void Remove(T item) { list.Remove(item); }
        public void RemoveAt(int index) { list.RemoveAt(index); }
        public void RemoveRange(int index, int count) { list.RemoveRange(index, count); }
        public void Sort() { list.Sort(); }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public T[] ToArray()
        {
            return list.ToArray();
        }

        public int Count { get { return list.Count; } }
    }
}
