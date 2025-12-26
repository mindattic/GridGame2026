using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public class HeroManager : MonoBehaviour
{
    public void Glow()
    {
        foreach (var x in g.Actors.Heroes.Where(x => x != null && x.IsPlaying))
        {
            if (x.Glow != null) x.Glow.Play();
        }
    }
}
