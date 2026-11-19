using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SobrevivenciaAndina.Lighting
{
    [DisallowMultipleComponent]
    public sealed class ChivaHeadlightsController : MonoBehaviour
    {
        [SerializeField] private Light2D leftHeadlight;
        [SerializeField] private Light2D rightHeadlight;
        [SerializeField, Min(0f)] private float intensity = 2.2f;
        [SerializeField, Range(0f, 0.1f)] private float flickerAmount = 0.035f;
        private float noiseOffset;
        private float nextDip;
        private float dipRemaining;

        public void SetHeadlights(Light2D left, Light2D right)
        {
            leftHeadlight = left;
            rightHeadlight = right;
        }

        private void OnEnable()
        {
            noiseOffset = Random.Range(0f, 1000f);
            nextDip = Time.time + Random.Range(3f, 8f);
            dipRemaining = 0f;
        }

        private void Update()
        {
            if (Time.time >= nextDip)
            {
                dipRemaining = Random.Range(0.06f, 0.14f);
                nextDip = Time.time + Random.Range(3f, 8f);
            }
            dipRemaining = Mathf.Max(0f, dipRemaining - Time.deltaTime);
            float alternator = dipRemaining > 0f ? 0.88f : 1f;
            Apply(leftHeadlight, alternator, noiseOffset);
            Apply(rightHeadlight, alternator, noiseOffset + 17f);
        }

        private void Apply(Light2D headlight, float alternator, float seed)
        {
            if (headlight == null) return;
            float noise = Mathf.PerlinNoise(seed, Time.time * 19f) * 2f - 1f;
            headlight.intensity = intensity * alternator * (1f + noise * flickerAmount);
        }

        private void OnDisable()
        {
            if (leftHeadlight != null) leftHeadlight.intensity = intensity;
            if (rightHeadlight != null) rightHeadlight.intensity = intensity;
        }
    }
}
