using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SobrevivenciaAndina.Lighting.Editor
{
    public static class AndeanLightingChecks
    {
        // Ejecutar con Unity -batchmode -executeMethod ...AndeanLightingChecks.Run.
        // No guarda la escena: comprueba montaje, repeticion y Undo sobre SampleScene.
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            Func<Transform[]> objects = () => scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            int originalObjects = objects().Length;
            int colliders = objects().Count(item => item.GetComponent<Collider2D>() != null);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            AndeanLightingSetup.Configure(scene);
            int configuredObjects = objects().Length;
            AndeanLightingSetup.Configure(scene);
            Require(objects().Length == configuredObjects, "El montaje duplico objetos.");
            Require(objects().Count(item => item.GetComponent<Collider2D>() != null) == colliders, "Se alteraron colliders.");
            var player = objects().Single(item => item.name == "Player");
            Require(player.GetComponents<ChivaHeadlightsController>().Length == 1, "Controlador duplicado.");
            foreach (string name in new[] { "Faro_Izquierdo", "Faro_Derecho" })
            {
                var light = player.Find(name).GetComponent<Light2D>();
                Require(light.shadowsEnabled && light.pointLightOuterRadius == 9f
                    && light.pointLightInnerAngle == 18f && light.pointLightOuterAngle == 42f
                    && Mathf.Approximately(light.intensity, 2.2f), "Parametros incorrectos en " + name);
                // W produce fuerza local (0, -12) en el grafo ChivaController.
                Vector3 forward = player.TransformDirection(Vector3.down);
                Require(Vector3.Dot(light.transform.up, forward) > 0.999f, "El faro no apunta hacia W.");
                Bounds bounds = player.GetComponent<SpriteRenderer>().localBounds;
                Require(Mathf.Abs(light.transform.localPosition.y - (bounds.min.y + 0.12f)) < 0.001f,
                    "El faro no esta montado en el frente del vehiculo.");
            }
            foreach (var rock in objects().Where(item => item.name.Contains("Roca")))
            {
                var caster = rock.GetComponent<ShadowCaster2D>();
                Require(caster != null && caster.castsShadows && !caster.selfShadows, "Roca sin sombra correcta.");
            }
            Require(objects().Any(item => item.name == "Aura_Cucurucho"), "Falta aura del enemigo.");
            var ambient = objects().Select(item => item.GetComponent<Light2D>())
                .Single(light => light != null && light.lightType == Light2D.LightType.Global);
            Require(Mathf.Approximately(ambient.intensity, 0.3f), "Ambiente incorrecto.");
            Require(ambient.GetComponents<AndeanStormLightingController>().Length == 1, "Tormenta duplicada.");
            Undo.RevertAllDownToGroup(group);
            Require(objects().Length == originalObjects, "Undo no restauro la jerarquia.");
            Debug.Log("ANDEAN_LIGHTING_CHECKS_PASSED: montaje, idempotencia, sombras, orientacion, colliders y Undo.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
