// Jetpack Joyride missions: three goals on the HUD at a time. Finish one and it pays out a coin shower, then a new goal appears.
using System.Collections.Generic;
using Sigf.Kit;
using UnityEngine;

public class MissionGoal
{
    public string Kind, Text;
    public int Need, Have;
    public bool Done => Have >= Need;
    public float FlashUntil;
}

public static class Mission
{
    public static readonly List<MissionGoal> Active = new List<MissionGoal>();
    public static int Completed;
    static readonly MissionGoal[] pool =
    {
        new MissionGoal { Kind = "coin",  Text = "Collect {0} coins",              Need = 60 },
        new MissionGoal { Kind = "kill",  Text = "Shatter {0} jetpack goons",      Need = 6 },
        new MissionGoal { Kind = "jet",   Text = "Fly {0} seconds on your jetpack", Need = 8 },
        new MissionGoal { Kind = "laser", Text = "Fry {0} goons with a laser",     Need = 2 },
        new MissionGoal { Kind = "zap",   Text = "Zap {0} goons on a zapper",      Need = 3 },
        new MissionGoal { Kind = "pack",  Text = "Blow up {0} loose jetpacks",     Need = 2 },
    };
    static int nextIdx;

    public static void Reset()
    {
        Active.Clear(); Completed = 0; nextIdx = 0;
        for (int i = 0; i < 3; i++) Pull();
    }

    static void Pull()
    {
        var t = pool[nextIdx++ % pool.Length];
        // two copies of the same kind never sit on the board together
        foreach (var a in Active) if (a.Kind == t.Kind) { Pull(); return; }
        Active.Add(new MissionGoal { Kind = t.Kind, Need = t.Need + (nextIdx > pool.Length ? 2 : 0), Text = string.Format(t.Text, t.Need + (nextIdx > pool.Length ? 2 : 0)) });
    }

    public static void Report(string kind, int n = 1)
    {
        for (int i = 0; i < Active.Count; i++)
        {
            var m = Active[i];
            if (m.Kind != kind || m.Done) continue;
            m.Have = Mathf.Min(m.Need, m.Have + n);
            m.FlashUntil = Time.unscaledTime + 0.4f;
            if (m.Done) Complete(i);
        }
    }

    static void Complete(int i)
    {
        Completed++;
        var m = Active[i];
        Mix.Say("MISSION COMPLETE", 2.5f, Fx.Gold, 0.3f, 76);
        Mix.Say(m.Text, 2.5f, Color.white, 0.42f, 34);
        Mix.Play(Mix.Sound("coin.wav"), null, 0.9f, 0.7f);
        G.Words("MISSION;COMPLETE");
        if (G.Cam != null)
        {
            var c = G.Cam.transform;
            Coin.Burst(c.position + c.forward * 4f + Vector3.up * 1.2f, 14);
        }
        Mix.After(2.2f, () => { Active.Remove(m); Pull(); }, "mission" + Completed);
    }
}
