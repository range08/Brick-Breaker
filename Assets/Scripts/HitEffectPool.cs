using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small ParticleSystem pool for block impact flashes. It creates a bounded
/// number of lightweight effects and drops new requests when the pool is full.
/// </summary>
public class HitEffectPool : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField, Range(1, 16)] private int initialPoolSize = 6;
    [SerializeField, Range(1, 24)] private int maximumPoolSize = 12;
    [SerializeField, Range(1, 12)] private int particlesPerHit = 5;

    private readonly List<ParticleSystem> pool = new();
    private Material particleMaterial;

    public int PoolCount => pool.Count;

    public void Initialize()
    {
        int targetSize = Mathf.Min(initialPoolSize, maximumPoolSize);

        while (pool.Count < targetSize)
            pool.Add(CreateEffect(pool.Count));
    }

    public void Play(Vector2 worldPosition, Color color)
    {
        ParticleSystem effect = FindAvailableEffect();

        if (effect == null && pool.Count < maximumPoolSize)
        {
            effect = CreateEffect(pool.Count);
            pool.Add(effect);
        }

        if (effect == null)
            return;

        ParticleSystem.MainModule main = effect.main;
        main.startColor = color;
        effect.transform.position = worldPosition;
        effect.Clear(true);
        effect.Emit(particlesPerHit);
    }

    private ParticleSystem FindAvailableEffect()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            ParticleSystem effect = pool[i];

            if (effect != null && !effect.IsAlive(true))
                return effect;
        }

        return null;
    }

    private ParticleSystem CreateEffect(int index)
    {
        GameObject effectObject = new($"HitEffect_{index + 1}");
        effectObject.transform.SetParent(transform, false);

        ParticleSystem effect = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = effect.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.12f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.maxParticles = 12;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = effect.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = effect.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.03f;

        ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 30;

        if (particleMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader != null)
                particleMaterial = new Material(shader);
        }

        if (particleMaterial != null)
            renderer.sharedMaterial = particleMaterial;

        return effect;
    }
}
