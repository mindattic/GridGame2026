using System.Collections;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    public class HideTargetIndicatorSequence : SequenceEvent
    {
        public override IEnumerator ProcessRoutine()
        {
            g.Actors.TargetActor.Render.SetTargetIndicatorEnabled(false);
            g.Actors.TargetActor = null;
            g.InputManager.InputMode = InputMode.PlayerTurn;
            yield return Wait.None();
        }
    }


}
