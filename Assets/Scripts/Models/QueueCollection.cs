using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Models
{

    public enum InsertOrder
    {
        Before,
        After
    }

    public class QueueCollection<T>
    {
        //Properties
        public int Count => queue.Count;

        //Fields
        private LinkedList<T> queue = new LinkedList<T>();

        public void Add(T item) => queue.AddLast(item); // Normal enqueue

        public void AddFirst(T item) => queue.AddFirst(item); // Add to top


        public void Insert(T item, T node, InsertOrder order = InsertOrder.Before)
        {
            var nodeRef = queue.Find(node);
            if (nodeRef == null)
                throw new UnityException($"Node `{nodeRef}` not found.");

            if (order == InsertOrder.Before)
                queue.AddBefore(nodeRef, item);
            else
                queue.AddAfter(nodeRef, item);
        }

        public T Remove()
        {
            if (queue.Count == 0) return default;
            T value = queue.First.Value;
            queue.RemoveFirst();
            return value;
        }

        public void Clear()
        {
            queue.Clear();
        }

    }

}
