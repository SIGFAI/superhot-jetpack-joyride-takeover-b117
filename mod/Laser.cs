// A Jetpack Joyride laser gate: two hazard pylons blink a red warning line, then a thick beam fires across the room,
// shatters every enemy it touches (they burst into coins) and would kill the player.
using System.Collections.Generic;
using Sigf.Kit;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public static readonly List<Laser> All = new List<Laser>();
    Transform a, b;
    LineRenderer core, glow;
    Light light;
    AudioSource hum;
    float timer, sweep, sweepSpeed, baseY;
    int phase;                       // 0 warning, 1 firing, 2 gone
    Vector3 pa, pb;
    float len;
    const float Warn = 1.6f, Fire = 2.4f;

    public static Laser Create(Vector3 center, Vector3 across, float len, float sweepSpeed = 0f)
    {
        var go = new GameObject("JJLaser");
        go.transform.position = center;
        var l = go.AddComponent<Laser>();
        l.len = len; l.sweepSpeed = sweepSpeed; l.baseY = center.y;
        across.y = 0f;
        l.pa = center + across.normalized * len / 2f;
        l.pb = center - across.normalized * len / 2f;
        l.Build();
        All.Add(l);
        return l;
    }

    void Build()
    {
        a = Pylon("JJLaserA", pa); b = Pylon("JJLaserB", pb);
        core = Line(0.05f, new Color(1f, 0.15f, 0.1f), 3f);
        glow = Line(0.05f, new Color(1f, 0.5f, 0.4f), 2f);
        light = Mix.Glow(transform.position, new Color(1f, 0.15f, 0.1f), 8f, 0f, transform);
        hum = gameObject.AddComponent<AudioSource>();
        hum.clip = Mix.Sound("zap.wav"); hum.loop = true; hum.volume = 0f; hum.pitch = 0.55f;
        hum.spatialBlend = 1f; hum.minDistance = 3f; hum.maxDistance = 35f; hum.Play();
        Mix.Play(Mix.Sound("warn.wav"), transform.position, 0.8f, 1f);
        Hud.AddWarning(transform.position, "LASER!", Warn + 0.2f);
    }

    Transform Pylon(string n, Vector3 p)
    {
        var root = new GameObject(n).transform;
        root.SetParent(transform, true);
        float g = Mathf.Min(Fx.GroundY(p), transform.position.y - 1.2f);
        float h = Mathf.Max(0.5f, transform.position.y - g);
        // a tall striped column from the floor up to the beam, with a glowing emitter on top
        var col = Fx.Prim(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(0.3f, h / 2f, 0.3f), Color.white, "hazard.png", 1f, 0.25f, true, "JJLaserPost");
        col.transform.position = new Vector3(p.x, g + h / 2f, p.z);
        var em = Fx.Prim(PrimitiveType.Sphere, root, Vector3.zero, Vector3.one * 0.42f, new Color(1f, 0.15f, 0.1f), null, 1f, 3f, false, "JJLaserEmitter");
        em.transform.position = new Vector3(p.x, transform.position.y, p.z);
        return root;
    }

    LineRenderer Line(float w, Color c, float g)
    {
        var go = new GameObject("JJLaserBeam");
        go.transform.SetParent(transform, false);
        var l = go.AddComponent<LineRenderer>();
        l.useWorldSpace = true; l.positionCount = 2;
        l.startWidth = l.endWidth = w;
        l.material = Fx.Mat(c, g);
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return l;
    }

    void OnDestroy() { All.Remove(this); }

    float SegDist(Vector3 p, Vector3 s, Vector3 e)
    {
        var ab = e - s;
        float t = Mathf.Clamp01(Vector3.Dot(p - s, ab) / ab.sqrMagnitude);
        return (p - (s + ab * t)).magnitude;
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        float y = baseY;
        if (phase == 1 && sweepSpeed != 0f) { sweep += sweepSpeed * Time.deltaTime; y = baseY + Mathf.Sin(sweep) * 0.9f; }
        var s = new Vector3(pa.x, y, pa.z); var e = new Vector3(pb.x, y, pb.z);
        core.SetPosition(0, s); core.SetPosition(1, e);
        glow.SetPosition(0, s); glow.SetPosition(1, e);
        if (phase == 0)
        {
            bool on = Mathf.Repeat(timer * 7f, 1f) < 0.6f;
            core.enabled = on; glow.enabled = false;
            core.startWidth = core.endWidth = 0.03f;
            light.intensity = on ? 1f : 0f;
            if (timer > Warn) { phase = 1; timer = 0f; core.enabled = glow.enabled = true; Mix.Play(Mix.Sound("zap.wav"), transform.position, 1f, 0.5f); Fx.Kick(transform.position, 1f); }
            return;
        }
        if (phase == 1)
        {
            float pulse = 1f + 0.25f * Mathf.Sin(timer * 60f);
            core.startWidth = core.endWidth = 0.14f * pulse;
            glow.startWidth = glow.endWidth = 0.28f * pulse;
            light.intensity = Random.Range(3f, 6f);
            hum.volume = Mathf.MoveTowards(hum.volume, 0.6f, Time.unscaledDeltaTime * 4f);
            if (Random.value < 0.6f)
            {
                var sp = Vector3.Lerp(s, e, Random.value);
                Ember.Spawn(sp, Random.onUnitSphere * 2.5f, new Color(1f, 0.35f, 0.2f), 0.06f, 0.4f, 4f, 0f, Vector3.down * 3f);
            }
            foreach (var en in G.EnemiesCached())
            {
                if (en == null || en.IsDead) continue;
                var ep = en.transform.position;
                for (float h = 0.2f; h <= 2.2f; h += 0.5f)
                    if (SegDist(ep + Vector3.up * h, s, e) < 0.6f)
                    {
                        Mix.Burst(ep + Vector3.up * h, new Color(1f, 0.3f, 0.2f), 20, 8f, 0.1f, 1f);
                        Mix.Play(Mix.Sound("zap.wav"), ep, 0.9f, 1.3f);
                        G.Shatter(en);
                        Mission.Report("laser");
                        break;
                    }
            }
            var cam = G.Cam;
            if (cam != null && G.Player != null && !G.Player.IsDead() && (SegDist(cam.transform.position, s, e) < 0.6f || SegDist(G.Pos, s, e) < 0.6f))
                G.Player.Kill(cam.transform.position + cam.transform.forward);
            if (timer > Fire) { phase = 2; core.enabled = glow.enabled = false; light.intensity = 0f; hum.volume = 0f; Destroy(gameObject, 1f); }
        }
    }
}
