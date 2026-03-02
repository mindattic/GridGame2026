using System;
using System.Collections;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// SEQUENCECALLBACK - Action callback as a sequence.
    /// 
    /// PURPOSE:
    /// Wraps a simple Action delegate as a SequenceEvent,
    /// allowing callbacks to be queued in the sequence system.
    /// 
    /// USAGE:
    /// ```csharp
    /// g.SequenceManager.Add(new SequenceCallback(() => {
    ///     g.InputManager.InputMode = InputMode.Default;
    /// }));
    /// ```
    /// 
    /// COMMON USES:
    /// - Restore state after sequence batch completes
    /// - Trigger UI updates between sequences
    /// - Log or debug sequence progress
    /// 
    /// RELATED FILES:
    /// - SequenceEvent.cs: Base class
    /// - SequenceManager.cs: Execution
    /// </summary>
    public class SequenceCallback : SequenceEvent
    {
        private readonly Action action;

        public SequenceCallback(Action action)
        {
            this.action = action;
        }

        public override IEnumerator ProcessRoutine()
        {
            action?.Invoke();
            yield return null;
        }
    }
}
