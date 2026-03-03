using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
{
    /// <summary>Insertion order for queue operations.</summary>
    public enum InsertOrder
    {
        Before,
        After
    }

    /// <summary>
    /// QUEUECOLLECTION<T> - LinkedList-based queue with insertion.
    /// 
    /// PURPOSE:
    /// Provides queue operations (FIFO) with ability to insert
    /// items at specific positions relative to existing items.
    /// 
    /// OPERATIONS:
    /// - Add: Enqueue at end
    /// - AddFirst: Enqueue at front
    /// - Insert: Insert before/after existing item
    /// - Remove: Dequeue from front
    /// 
    /// USAGE:
    /// Used by SequenceManager for sequence event ordering.
    /// 
    /// RELATED FILES:
    /// - SequenceManager.cs: Uses for event queue
    /// - SequenceEvent.cs: Queue item type
    /// </summary>
    public class QueueCollection<T>
    {
        public int Count => queue.Count;

        private LinkedList<T> queue = new LinkedList<T>();

        /// <summary>Add.</summary>
        public void Add(T item) => queue.AddLast(item);

        /// <summary>Add first.</summary>
        public void AddFirst(T item) => queue.AddFirst(item);

        /// <summary>Insert.</summary>
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

        /// <summary> remove..Groups[0].Value.ToUpper() emove.</summary>
        public T Remove()
        {
            if (queue.Count == 0) return default;
            T value = queue.First.Value;
            queue.RemoveFirst();
            return value;
        }

        /// <summary> clear..Groups[0].Value.ToUpper() lear.</summary>
        public void Clear()
        {
            queue.Clear();
        }

    }

}
