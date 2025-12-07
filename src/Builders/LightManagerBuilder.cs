using DarkArmsProto.VFX;

namespace DarkArmsProto.Builders
{
    public class LightManagerBuilder
    {
        private string _vertexShaderPath = "resources/shaders/lighting.vs";
        private string _fragmentShaderPath = "resources/shaders/lighting.fs";
        private float[] _ambientLight = new float[] { 0.05f, 0.05f, 0.05f, 1.0f };

        public LightManagerBuilder WithVertexShader(string path)
        {
            _vertexShaderPath = path;
            return this;
        }

        public LightManagerBuilder WithFragmentShader(string path)
        {
            _fragmentShaderPath = path;
            return this;
        }

        public LightManagerBuilder WithAmbientLight(float r, float g, float b, float a = 1.0f)
        {
            _ambientLight = new float[] { r, g, b, a };
            return this;
        }

        public LightManager Build()
        {
            var lightManager = new LightManager
            {
                VertexShaderPath = _vertexShaderPath,
                FragmentShaderPath = _fragmentShaderPath,
                AmbientLight = _ambientLight,
            };

            lightManager.Initialize();
            return lightManager;
        }
    }
}
