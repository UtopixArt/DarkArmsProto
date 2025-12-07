using System.Numerics;
using DarkArmsProto.VFX;
using Raylib_cs;

namespace DarkArmsProto.Builders
{
    public class DynamicLightBuilder
    {
        private readonly LightManager _manager;
        private Vector3 _position;
        private Color _color = Color.White;
        private float _intensity = 1.0f;
        private float _radius = 5.0f;
        private float _lifetime = 1.0f;
        private bool _flicker = false;

        public DynamicLightBuilder(LightManager manager, Vector3 position)
        {
            _manager = manager;
            _position = position;
        }

        public DynamicLightBuilder WithColor(Color color)
        {
            _color = color;
            return this;
        }

        public DynamicLightBuilder WithIntensity(float intensity)
        {
            _intensity = intensity;
            return this;
        }

        public DynamicLightBuilder WithRadius(float radius)
        {
            _radius = radius;
            return this;
        }

        public DynamicLightBuilder WithLifetime(float lifetime)
        {
            _lifetime = lifetime;
            return this;
        }

        public DynamicLightBuilder WithFlicker(bool flicker)
        {
            _flicker = flicker;
            return this;
        }

        // Presets
        public DynamicLightBuilder AsExplosion()
        {
            _intensity = 0.5f;
            _radius = 2f;
            _lifetime = 0.4f;
            _flicker = true;
            return this;
        }

        public DynamicLightBuilder AsMuzzleFlash()
        {
            _intensity = 0.01f;
            _radius = 0.1f;
            _lifetime = 0.1f;
            _flicker = true;
            return this;
        }

        public DynamicLightBuilder AsImpact()
        {
            _intensity = 0.1f;
            _radius = 2f;
            _lifetime = 0.3f;
            _flicker = false;
            return this;
        }

        public void Spawn()
        {
            _manager.AddLight(_position, _color, _intensity, _radius, _lifetime, _flicker);
        }
    }
}
