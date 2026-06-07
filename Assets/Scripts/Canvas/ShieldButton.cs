using System;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Canvas
{
    /// <summary>
    /// SHIELDBUTTON - HUD button anchored bottom-right of the timeline bar. Replaces the old
    /// blinking-dot "Bank" button.
    ///
    /// <para>BEHAVIOR (on press):</para>
    /// <list type="number">
    ///   <item>Grants every hero the <see cref="Scripts.Data.Buffs.Protection"/> buff
    ///   (15% incoming-damage reduction for 1 turn).</item>
    ///   <item>Pushes the timeline forward to the next enemy ready to act — same flow as the old
    ///   Bank button (via <see cref="Scripts.Managers.ManaPoolManager.OnBankButtonClicked"/>).
    ///   This is the "I don't have time to react — let me brace for the next hit" escape valve.</item>
    /// </list>
    /// </summary>
    public sealed class ShieldButton : MonoBehaviour
    {
        /// <summary>Fired after Protection is granted + auto-skip is queued. Subscribe for UI feedback.</summary>
        public event Action OnPressed;

        /// <summary>Wired to the UI Button.onClick by the factory.</summary>
        public void Click()
        {
            // Grant Protection to all heroes.
            Scripts.Managers.BuffSystem.ApplyToAllHeroes(Scripts.Data.Buffs.Protection);
            AnnouncementWindow.Announce("Party raises Shield!");
            g.AudioManager?.Play("Shield");

            // Auto-skip to the next enemy turn — same flow the old Bank button drove.
            g.ManaPoolManager?.OnBankButtonClicked();

            OnPressed?.Invoke();
        }
    }
}
