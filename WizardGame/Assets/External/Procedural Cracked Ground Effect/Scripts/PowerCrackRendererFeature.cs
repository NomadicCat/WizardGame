using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PowerCrackRendererFeature : ScriptableRendererFeature
{
    [SerializeField]
    private LayerMask layerMask;
    [SerializeField]
    private Material overrideMaterial;
    [SerializeField]
    private ComputeShader computeShader;
    [SerializeField]
    private int textureResolution = 64;
    [SerializeField]
    private int numPoints = 10;
    [SerializeField]
    private int seed = 1;

    class PowerCrackRenderPass : ScriptableRenderPass
    {
        private ProfilingSampler m_profilingSampler;
        private FilteringSettings m_filteringSettings;
        private List<ShaderTagId> m_shaderTagIds = new List<ShaderTagId>();
        private Material m_overrideMaterial;
        private RenderTexture m_crackTexture;
        private ComputeShader m_computeShader;
        private Vector3[] m_points;

        public PowerCrackRenderPass(ComputeShader computeShader, LayerMask layerMask, Material overrideMaterial, int textureResolution, int numPoints, int seed)
        {
            m_overrideMaterial = overrideMaterial;
            m_computeShader = computeShader;

            m_profilingSampler = new ProfilingSampler("Power Crack");
            m_filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);

            m_shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
            m_shaderTagIds.Add(new ShaderTagId("UniversalForward"));
            m_shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));

            ConfigureInput(ScriptableRenderPassInput.Depth);

            // create procedural texture

            m_crackTexture = new RenderTexture(textureResolution, textureResolution, 0);
            m_crackTexture.enableRandomWrite = true;
            m_crackTexture.dimension = TextureDimension.Tex3D;
            m_crackTexture.volumeDepth = textureResolution;
            m_crackTexture.wrapMode = TextureWrapMode.Repeat;
            m_crackTexture.filterMode = FilterMode.Trilinear;
            m_crackTexture.format = RenderTextureFormat.ARGBFloat;
            m_crackTexture.Create();

            m_points = new Vector3[numPoints];

            Random.InitState(seed);

            // create points for procedural texture
            for (int i = 0; i < m_points.Length; i++)
            {
                m_points[i].x = Random.Range(0.0f, 1.0f);
                m_points[i].y = Random.Range(0.0f, 1.0f);
                m_points[i].z = Random.Range(0.0f, 1.0f);
            }

            ComputeBuffer pointsBuffer = new ComputeBuffer(m_points.Length, sizeof(float) * 3);
            pointsBuffer.SetData(m_points);

            int numThreadGroups = textureResolution / 8;

            m_computeShader.SetTexture(0, "result", m_crackTexture);
            m_computeShader.SetInt("resolution", textureResolution);
            m_computeShader.SetInt("numPoints", m_points.Length);
            m_computeShader.SetBuffer(0, "points", pointsBuffer);

            m_computeShader.Dispatch(0, numThreadGroups, numThreadGroups, numThreadGroups);

            pointsBuffer.Dispose();
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_profilingSampler))
            {
                // make sure the command buffer is empty
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // setup parameters for selecting volumes
                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = CreateDrawingSettings(m_shaderTagIds, ref renderingData, sortingCriteria);

                // setup override parameters for drawing volumes
                drawingSettings.overrideMaterial = m_overrideMaterial;
                drawingSettings.overrideMaterial.SetTexture("_MainTex", m_crackTexture);

                // draw volumes
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_filteringSettings);
            }

            // execute the command buffer
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public void Dispose()
        {
            m_crackTexture.DiscardContents();
        }
    }

    PowerCrackRenderPass m_renderPass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_renderPass = new PowerCrackRenderPass(computeShader, layerMask, overrideMaterial, textureResolution, numPoints, seed);

        // configures where the render pass should be injected
        m_renderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_renderPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_renderPass.Dispose();
    }
}


