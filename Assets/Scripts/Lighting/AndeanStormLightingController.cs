using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SobrevivenciaAndina.Lighting
{
    [DisallowMultipleComponent, RequireComponent(typeof(Light2D))]
    public sealed class AndeanStormLightingController : MonoBehaviour
    {
        [SerializeField] private Color stormColor = new Color32(43, 53, 66, 255);
        [SerializeField] private Color ashColor = new Color32(58, 51, 44, 255);
        [SerializeField, Range(0.25f, 0.35f)] private float baseIntensity = 0.3f;
        [SerializeField] private Vector2 lightningInterval = new Vector2(3f, 7f);
        [SerializeField] private Vector2 lightningIntensity = new Vector2(2f, 2.6f);
        [SerializeField] private Color lightningColor = new Color32(215, 232, 255, 255);
        [SerializeField] private bool lightningEnabled = true;

        private Light2D ambient;
        private float flashWeight;
        private float flashPeak;
        private float noiseOffset;

        private void OnEnable()
        {
            ambient = GetComponent<Light2D>();
            if (ambient.lightType != Light2D.LightType.Global)
            {
                Debug.LogError("La tormenta requiere una Global Light 2D.", this);
                enabled = false;
                return;
            }
            noiseOffset = Random.Range(0f, 1000f);
            flashWeight = 0f;
            StartCoroutine(Storm());
            Update();
        }

        private void Update()
        {
            float density = Mathf.PerlinNoise(noiseOffset, Time.time * 0.075f);
            float intensity = Mathf.Clamp(baseIntensity + (density - 0.5f) * 0.04f, 0.25f, 0.35f);
            Color color = Color.Lerp(stormColor, ashColor, density * 0.18f);
            float weight = lightningEnabled ? flashWeight : 0f;
            ambient.intensity = Mathf.Lerp(intensity, flashPeak, weight);
            ambient.color = Color.Lerp(color, lightningColor, weight);
        }

        private IEnumerator Storm()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(lightningInterval.x, lightningInterval.y));
                if (!lightningEnabled) continue;
                int pulses = Random.Range(2, 4);
                for (int pulse = 0; pulse < pulses; pulse++)
                {
                    flashPeak = Random.Range(lightningIntensity.x, lightningIntensity.y);
                    flashWeight = 1f;
                    yield return new WaitForSeconds(Random.Range(0.035f, 0.075f));
                    float duration = pulse == pulses - 1 ? 0.45f : 0.09f;
                    for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
                    {
                        flashWeight = 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
                        yield return null;
                    }
                    flashWeight = 0f;
                    if (pulse < pulses - 1)
                        yield return new WaitForSeconds(Random.Range(0.045f, 0.12f));
                }
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            flashWeight = 0f;
            if (ambient != null && ambient.lightType == Light2D.LightType.Global)
            {
                ambient.color = stormColor;
                ambient.intensity = baseIntensity;
            }
        }

        private void OnValidate()
        {
            baseIntensity = Mathf.Clamp(baseIntensity, 0.25f, 0.35f);
            lightningInterval.x = Mathf.Max(0.5f, lightningInterval.x);
            lightningInterval.y = Mathf.Max(lightningInterval.x, lightningInterval.y);
            lightningIntensity.x = Mathf.Max(baseIntensity, lightningIntensity.x);
            lightningIntensity.y = Mathf.Max(lightningIntensity.x, lightningIntensity.y);
        }
    }
}
