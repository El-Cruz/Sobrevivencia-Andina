using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SobrevivenciaAndina.Lighting.Editor
{
    public static class AndeanLightingSetup
    {
        private const string Menu = "Tools/Sobrevivencia Andina/Configurar Iluminacion Escena";

        [MenuItem(Menu)]
        public static void ConfigureScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "SampleScene" || EditorApplication.isPlayingOrWillChangePlaymode
                || PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogWarning("Abre SampleScene fuera de Play Mode y del modo Prefab.");
                return;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configurar iluminacion andina");
            try
            {
                Configure(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("Iluminacion configurada. Guarda SampleScene con Ctrl+S. Los cambios admiten Undo.");
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
            finally { Undo.CollapseUndoOperations(group); }
        }

        internal static void Configure(Scene scene)
        {
            Transform[] objects = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform player = objects.SingleOrDefault(item => item.name == "Player");
            Light2D ambient = objects.Select(item => item.GetComponent<Light2D>())
                .Where(light => light != null && light.lightType == Light2D.LightType.Global)
                .SingleOrDefault();
            if (player == null || ambient == null)
                throw new InvalidOperationException("Se requiere un Player y una unica Global Light 2D en SampleScene.");

            Undo.RecordObject(ambient, "Clima pesado");
            ambient.color = new Color32(43, 53, 66, 255);
            ambient.intensity = 0.3f;
            ambient.enabled = true;
            Record(ambient);
            var storm = Component<AndeanStormLightingController>(ambient.gameObject);
            // Actualiza tambien componentes existentes con valores ya serializados.
            var stormSettings = new SerializedObject(storm);
            stormSettings.FindProperty("lightningInterval").vector2Value = new Vector2(3f, 7f);
            stormSettings.FindProperty("lightningIntensity").vector2Value = new Vector2(2f, 2.6f);
            stormSettings.ApplyModifiedProperties();
            Record(storm);

            // ChivaController multiplica Vertical por speed = -12: W avanza hacia -Y local.
            Bounds bounds = player.GetComponent<SpriteRenderer>()?.localBounds
                ?? new Bounds(Vector3.zero, new Vector3(2.13f, 4.51f, 0f));
            float front = bounds.min.y + 0.12f;
            float separation = bounds.extents.x * 0.65f;
            Color warm = new Color32(255, 226, 154, 255);
            Light2D left = Light(player, "Faro_Izquierdo", new Vector3(bounds.center.x + separation, front), warm, 2.2f, 9f, true);
            Light2D right = Light(player, "Faro_Derecho", new Vector3(bounds.center.x - separation, front), warm, 2.2f, 9f, true);
            Light(player, "Halo_Chiva", bounds.center, warm, 0.6f, 2.5f, false);
            var headlights = Component<ChivaHeadlightsController>(player.gameObject);
            Undo.RecordObject(headlights, "Referencias de faros");
            headlights.SetHeadlights(left, right);
            Record(headlights);

            foreach (Transform item in objects)
            {
                if (item.name.IndexOf("Roca", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var caster = Component<ShadowCaster2D>(item.gameObject);
                    Undo.RecordObject(caster, "Sombras de roca");
                    caster.castsShadows = true;
                    caster.selfShadows = false;
                    Record(caster);
                }
                if (item.name.StartsWith("Enemigo_Cucurucho", StringComparison.OrdinalIgnoreCase))
                    Light(item, "Aura_Cucurucho", Vector3.zero, new Color32(255, 59, 31, 255), 0.8f, 1.8f, false);
            }
        }

        private static T Component<T>(GameObject owner) where T : Behaviour
        {
            T component = owner.GetComponent<T>();
            if (component == null) component = Undo.AddComponent<T>(owner);
            Undo.RecordObject(component, "Activar iluminacion");
            component.enabled = true;
            Record(component);
            return component;
        }

        private static Light2D Light(Transform parent, string name, Vector3 position,
            Color color, float intensity, float radius, bool cone)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                var created = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(created, "Crear luz");
                SceneManager.MoveGameObjectToScene(created, parent.gameObject.scene);
                Undo.SetTransformParent(created.transform, parent, "Emparentar luz");
                child = created.transform;
            }
            Undo.RecordObject(child, "Posicionar luz");
            child.localPosition = position;
            child.localRotation = cone ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
            child.localScale = Vector3.one;
            Record(child);
            Light2D light = Component<Light2D>(child.gameObject);
            Undo.RecordObject(light, "Configurar luz");
            // URP muestra Spot en el Inspector; su enum publico sigue llamandose Point.
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = 0f;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerAngle = cone ? 18f : 360f;
            light.pointLightOuterAngle = cone ? 42f : 360f;
            light.falloffIntensity = 0.6f;
            light.shadowsEnabled = cone;
            light.shadowIntensity = 1f;
            light.blendStyleIndex = 0;
            light.targetSortingLayers = SortingLayer.layers.Select(layer => layer.id).ToArray();
            Record(light);
            return light;
        }

        private static void Record(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
