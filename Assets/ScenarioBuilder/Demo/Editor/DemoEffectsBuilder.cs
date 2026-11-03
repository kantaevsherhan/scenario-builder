using System.Linq;
using ScenarioBuilder.Core;
using ScenarioBuilder.Feedback;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace ScenarioBuilder.Demo.Editor
{
    /// <summary>Particle effects and lights of the demo, started by actions through <see cref="ActionReaction"/>.</summary>
    internal static class DemoEffectsBuilder
    {
        private static Material particleMaterial;

        public static void Build(DemoAssets assets, DemoWorld world)
        {
            particleMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            ActionDefinition Action(string key) => assets.Actions[key];

            Vector3 pipeTop = world.ValvePipe.transform.position + new Vector3(0f, 0.45f, 0.3f);
            ParticleSystem steam = Steam("ValveSteam", world.ValvePipe.transform, pipeTop, 18f, 0.6f);
            Loop(steam, starts: Action("OpenValve"), stops: Action("CloseValve"));
            Burst(Steam("PressurePuff", world.ValvePipe.transform, pipeTop, 0f, 1.4f, burst: 60), Action("PressureMax"));
            Burst(Sparks("PressureRelease", world.ValvePipe.transform, pipeTop, UiFactory.Hex("8FD3FF"), 30, 2.5f),
                Action("PressureZero"));

            Vector3 pumpOutlet = world.Pump.position + new Vector3(0.75f, 0.65f, 0f);
            ParticleSystem water = Water("PumpWater", world.Pump, pumpOutlet);
            Loop(water, starts: Action("StartPump"), stops: Action("PressEmergency"));

            Vector3 lampTop = world.Lamp.transform.position;
            Burst(Sparks("PowerSparks", world.Cabinet, lampTop, UiFactory.Hex("FFD27A"), 40, 3f), Action("PressPower"));
            Burst(Sparks("EmergencySparks", world.Cabinet, lampTop, UiFactory.Hex("FF5C5C"), 70, 4.5f), Action("PressEmergency"));
            Switch(PointLight("CabinetLamp", world.Cabinet, lampTop + Vector3.up * 0.4f, UiFactory.Hex("FFD27A"), 2.5f, 4f),
                Action("PressPower"));
            Switch(PointLight("WorkLightWest", world.Root, new Vector3(-4.5f, 4f, 0.5f), UiFactory.Hex("FFF1D6"), 1.4f, 11f),
                Action("PressLights"));
            Switch(PointLight("WorkLightEast", world.Root, new Vector3(4.5f, 4f, 0.5f), UiFactory.Hex("FFF1D6"), 1.4f, 11f),
                Action("PressLights"));

            foreach (GameObject post in world.Posts)
            {
                Color color = post.GetComponent<Renderer>().sharedMaterial.color;
                Burst(Sparks("TouchSparks", post.transform, post.transform.position + Vector3.up * 1.1f, color, 45, 3.5f),
                    post.GetComponent<Sources.CollisionActionSource>().Actions.First());
            }

            foreach (GameObject zone in world.Zones)
            {
                Color color = zone.GetComponent<Renderer>().sharedMaterial.color;
                Burst(Ring("EnterRing", zone.transform, zone.transform.position + Vector3.up * 0.1f, color),
                    zone.GetComponent<Sources.TriggerActionSource>().Actions.First());
            }

            GameObject exit = world.Zones.Last();
            Burst(Ring("FinishRing", exit.transform, exit.transform.position + Vector3.up * 0.1f, UiFactory.Hex("FFFFFF"), 120),
                Action("PressFinish"));
        }

        private static ActionReaction Reaction(GameObject host, ActionDefinition action)
        {
            var reaction = host.AddComponent<ActionReaction>();
            SerializedWiring.SetList(reaction, "actions", new Object[] { action });
            return reaction;
        }

        private static void Burst(ParticleSystem particles, ActionDefinition action)
        {
            ActionReaction reaction = Reaction(particles.gameObject, action);
            UnityEventTools.AddVoidPersistentListener(reaction.Performed, particles.Play);
            AddReset(reaction.RunStarted, particles);
        }

        private static void Loop(ParticleSystem particles, ActionDefinition starts, ActionDefinition stops)
        {
            ActionReaction start = Reaction(particles.gameObject, starts);
            SerializedWiring.Set(start, "skipOnViolation", true);
            UnityEventTools.AddVoidPersistentListener(start.Performed, particles.Play);
            AddReset(start.RunStarted, particles);

            ActionReaction stop = Reaction(particles.gameObject, stops);
            UnityEventTools.AddVoidPersistentListener(stop.Performed, particles.Stop);
        }

        private static void Switch(Light light, ActionDefinition action)
        {
            ActionReaction reaction = Reaction(light.gameObject, action);
            var setEnabled = (UnityAction<bool>)System.Delegate.CreateDelegate(typeof(UnityAction<bool>), light, "set_enabled");
            UnityEventTools.AddBoolPersistentListener(reaction.Performed, setEnabled, true);
            UnityEventTools.AddBoolPersistentListener(reaction.RunStarted, setEnabled, false);
        }

        private static void AddReset(UnityEvent runStarted, ParticleSystem particles)
        {
            UnityEventTools.AddVoidPersistentListener(runStarted, particles.Stop);
            UnityEventTools.AddVoidPersistentListener(runStarted, particles.Clear);
        }

        private static ParticleSystem Steam(string name, Transform parent, Vector3 position, float rate, float size, int burst = 0)
        {
            ParticleSystem particles = Create(name, parent, position, Quaternion.Euler(-90f, 0f, 0f), loop: burst == 0);
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new Color(0.92f, 0.95f, 1f, 0.55f);
            main.gravityModifier = -0.05f;

            Emit(particles, rate, burst);
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.08f;
            FadeAndGrow(particles, 2.2f);
            return particles;
        }

        private static ParticleSystem Water(string name, Transform parent, Vector3 position)
        {
            ParticleSystem particles = Create(name, parent, position, Quaternion.Euler(-35f, 90f, 0f), loop: true);
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = 0.9f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new Color(0.45f, 0.75f, 1f, 0.9f);
            main.gravityModifier = 1.2f;

            Emit(particles, 70f, 0);
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.05f;
            FadeAndGrow(particles, 1f);
            return particles;
        }

        private static ParticleSystem Sparks(string name, Transform parent, Vector3 position, Color color, int count, float speed)
        {
            ParticleSystem particles = Create(name, parent, position, Quaternion.identity, loop: false);
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = color;
            main.gravityModifier = 0.6f;

            Emit(particles, 0f, count);
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            FadeAndGrow(particles, 0.6f);
            return particles;
        }

        private static ParticleSystem Ring(string name, Transform parent, Vector3 position, Color color, int count = 80)
        {
            ParticleSystem particles = Create(name, parent, position, Quaternion.Euler(-90f, 0f, 0f), loop: false);
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            main.startColor = color;

            Emit(particles, 0f, count);
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.4f;
            shape.radiusThickness = 0f;
            FadeAndGrow(particles, 0.5f);
            return particles;
        }

        private static ParticleSystem Create(string name, Transform parent, Vector3 position, Quaternion rotation, bool loop)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, true);
            host.transform.SetPositionAndRotation(position, rotation);
            host.transform.localScale = Vector3.one;

            var particles = host.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = loop;
            main.duration = loop ? 1f : 0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 400;
            host.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
            return particles;
        }

        private static void Emit(ParticleSystem particles, float rate, int burst)
        {
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = rate;
            emission.SetBursts(burst > 0 ? new[] { new ParticleSystem.Burst(0f, (short)burst) } : new ParticleSystem.Burst[0]);
        }

        private static void FadeAndGrow(ParticleSystem particles, float endSize)
        {
            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, endSize));
        }

        private static Light PointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, true);
            light.transform.position = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.enabled = false;
            return light;
        }
    }
}
