// Copyright Elliot Bentine, 2018-
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelisationFeature : ScriptableRendererFeature
{
    [Tooltip("Use depth testing for outlines. This prevents outlines appearing when one object intersects another, but requires an extra depth sample.")]
    public bool DepthTestOutlines = true;

    [Tooltip("The threshold value used when depth comparing outlines.")]
    public float DepthTestThreshold = 0.001f;

    class PixelisationPass : ScriptableRenderPass
    {
        public bool DepthTestOutlines;
        public float DepthTestThreshold;
        private int _PixelizationMap;
        private int _OriginalScene;
        private int _CameraColorTexture;
        private int _PixelatedScene;
        private int _CameraDepthAttachment;
        private int _CameraDepthAttachmentTemp;
        private int _CameraDepthTexture;

        private int _Outlines;
        private int _OutlinesTemp;
        static ShaderTagId OutlinesShaderTagID = new ShaderTagId("Outlines");

        Material PixelisingMaterial;
        Material CopyDepthMaterial;
        Material PixelisedPostProcessMaterial;
        Material PixelizationMap;
        Material ApplyPixelizationMap;

        private const string ShaderName = "Hidden/ProPixelizer/SRP/Pixelization Post Process";
        private const string CopyDepthShaderName = "Hidden/ProPixelizer/SRP/BlitCopyDepth"; 
        private const string PixelisedPostProcessShaderName = "Hidden/ProPixelizer/SRP/Screen Post Process";
        private const string PixelizationMapShaderName = "Hidden/ProPixelizer/SRP/Pixelization Map";
        private const string ApplyPixelizationMapShaderName = "Hidden/ProPixelizer/SRP/ApplyPixelizationMap";

        private Vector4 TexelSize;

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _PixelizationMap = Shader.PropertyToID("_PixelizationMap");
            _CameraColorTexture = Shader.PropertyToID("_CameraColorTexture");
            _PixelatedScene = Shader.PropertyToID("_PixelatedScene");
            _OriginalScene = Shader.PropertyToID("_OriginalScene");

            cameraTextureDescriptor.useMipMap = false;
            
            // Issue #3
            //cmd.GetTemporaryRT(_CameraColorTexture, cameraTextureDescriptor);
            cmd.GetTemporaryRT(_PixelatedScene, cameraTextureDescriptor);

            cmd.GetTemporaryRT(_OriginalScene, cameraTextureDescriptor, FilterMode.Point);

            _CameraDepthAttachment = Shader.PropertyToID("_CameraDepthAttachment");
            _CameraDepthAttachmentTemp = Shader.PropertyToID("_CameraDepthAttachmentTemp");
            _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
            var depthDescriptor = cameraTextureDescriptor;
            depthDescriptor.colorFormat = RenderTextureFormat.Depth;
            cmd.GetTemporaryRT(_CameraDepthAttachment, depthDescriptor);
            cmd.GetTemporaryRT(_CameraDepthAttachmentTemp, depthDescriptor);

            var outlineDescriptor = cameraTextureDescriptor;
            outlineDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            outlineDescriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
            _Outlines = Shader.PropertyToID("_Outlines");
            _OutlinesTemp = Shader.PropertyToID("_OutlinesTemp");
            cmd.GetTemporaryRT(_Outlines, outlineDescriptor, FilterMode.Point);
            cmd.GetTemporaryRT(_OutlinesTemp, outlineDescriptor, FilterMode.Point);

            var pixelizationMapDescriptor = cameraTextureDescriptor;
            pixelizationMapDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            pixelizationMapDescriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
            cmd.GetTemporaryRT(_PixelizationMap, pixelizationMapDescriptor);

            if (PixelisingMaterial == null)
                PixelisingMaterial = GetMaterial(ShaderName);
            if (CopyDepthMaterial == null)
                CopyDepthMaterial = GetMaterial(CopyDepthShaderName);
            if (PixelisedPostProcessMaterial == null)
                PixelisedPostProcessMaterial = GetMaterial(PixelisedPostProcessShaderName);
            if (ApplyPixelizationMap == null)
                ApplyPixelizationMap = GetMaterial(ApplyPixelizationMapShaderName);
            if (PixelizationMap == null)
                PixelizationMap = GetMaterial(PixelizationMapShaderName);

            TexelSize = new Vector4(
                1f / cameraTextureDescriptor.width,
                1f / cameraTextureDescriptor.height,
                cameraTextureDescriptor.width,
                cameraTextureDescriptor.height
            );
        }

        public const string PROFILER_TAG = "PIXELISATION";

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer buffer = CommandBufferPool.Get(PROFILER_TAG);
            buffer.name = "Pixelisation";

            if (DepthTestOutlines)
            {
                PixelisedPostProcessMaterial.EnableKeyword("DEPTH_TEST_OUTLINES_ON");
                PixelisedPostProcessMaterial.SetFloat("_OutlineDepthTestThreshold", DepthTestThreshold);
            }
            else
                PixelisedPostProcessMaterial.DisableKeyword("DEPTH_TEST_OUTLINES_ON");

            // Configure keywords for pixelising material.
            if (renderingData.cameraData.camera.orthographic)
            {
                PixelizationMap.EnableKeyword("ORTHO_PROJECTION");
                PixelisingMaterial.EnableKeyword("ORTHO_PROJECTION");
            }
            else
            {
                PixelizationMap.DisableKeyword("ORTHO_PROJECTION");
                PixelisingMaterial.DisableKeyword("ORTHO_PROJECTION");
            }

#if CAMERA_COLOR_TEX_PROP
            RenderTargetIdentifier ColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
#else
            int ColorTarget = _CameraColorTexture;
#endif

            // Blit scene into _OriginalScene - so that we can guarantee point filtering of colors.
            Blit(buffer, ColorTarget, _OriginalScene);

            // Create pixelization map, to determine how to pixelate the screen.
            buffer.SetGlobalTexture("_MainTex", _OriginalScene);
            Blit(buffer, _OriginalScene, _PixelizationMap, PixelizationMap);

            // Pixelise the appearance texture
            buffer.SetGlobalTexture("_MainTex", _OriginalScene);
            buffer.SetGlobalTexture("_PixelizationMap", _PixelizationMap);
            Blit(buffer, ColorTarget, _PixelatedScene, ApplyPixelizationMap);

            // Copy pixelated depth texture
            buffer.SetGlobalTexture("_MainTex", _PixelatedScene, RenderTextureSubElement.Depth);
            buffer.SetRenderTarget(_CameraDepthAttachmentTemp);
            buffer.SetViewMatrix(Matrix4x4.identity);
            buffer.SetProjectionMatrix(Matrix4x4.identity);
            buffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, CopyDepthMaterial);

            // ...then restore transformations:
#if CAMERADATA_MATRICES
            buffer.SetViewMatrix(renderingData.cameraData.GetViewMatrix());
            buffer.SetProjectionMatrix(renderingData.cameraData.GetProjectionMatrix());
#else
            buffer.SetViewMatrix(renderingData.cameraData.camera.worldToCameraMatrix);
            buffer.SetProjectionMatrix(renderingData.cameraData.camera.projectionMatrix);
#endif

            // Render outlines into a render target.
            buffer.SetRenderTarget(_OutlinesTemp);
            buffer.ClearRenderTarget(true, true, Color.white);
            context.ExecuteCommandBuffer(buffer);
            CommandBufferPool.Release(buffer);

            var sort = new SortingSettings(renderingData.cameraData.camera);
            var drawingSettings = new DrawingSettings(OutlinesShaderTagID, sort);
            var filteringSettings = new FilteringSettings(RenderQueueRange.all);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

            buffer = CommandBufferPool.Get(PROFILER_TAG);
            buffer.name = "PixelisationPostProcess";

            // Pixelise the outline metadata
            buffer.SetGlobalTexture("_MainTex", _OutlinesTemp);
            buffer.SetGlobalTexture("_PixelizationMap", _PixelizationMap);
            Blit(buffer, _OutlinesTemp, _Outlines, ApplyPixelizationMap);
            
            // Perform the final post process.
            buffer.SetGlobalTexture("_Outlines", _Outlines);
            buffer.SetGlobalTexture("_MainTex", _PixelatedScene); // original unpixelated scene
            buffer.SetGlobalTexture("_Pixelised", _PixelatedScene);
            buffer.SetGlobalVector("_TexelSize", TexelSize);
            buffer.SetGlobalTexture("_Depth", _CameraDepthAttachmentTemp);
            buffer.SetRenderTarget(ColorTarget);
            buffer.SetViewMatrix(Matrix4x4.identity);
            buffer.SetProjectionMatrix(Matrix4x4.identity);
            buffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, PixelisedPostProcessMaterial);

            //// Blit pixelised depth into the used depth texture
            Blit(buffer, _CameraDepthAttachmentTemp, _CameraDepthTexture, CopyDepthMaterial);
            Blit(buffer, _CameraDepthAttachmentTemp, _CameraDepthAttachment, CopyDepthMaterial);
            
            buffer.SetGlobalTexture("_CameraDepthTexture", _CameraDepthTexture);

            // ...and restore transformations:
#if CAMERADATA_MATRICES
            buffer.SetViewMatrix(renderingData.cameraData.GetViewMatrix());
            buffer.SetProjectionMatrix(renderingData.cameraData.GetProjectionMatrix());
#else
            buffer.SetViewMatrix(renderingData.cameraData.camera.worldToCameraMatrix);
            buffer.SetProjectionMatrix(renderingData.cameraData.camera.projectionMatrix);
#endif
            context.ExecuteCommandBuffer(buffer);
            CommandBufferPool.Release(buffer);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_PixelizationMap);
            // Issue #3
            //cmd.ReleaseTemporaryRT(_CameraColorTexture);
            cmd.ReleaseTemporaryRT(_CameraDepthAttachment);
            cmd.ReleaseTemporaryRT(_CameraDepthAttachmentTemp);
            // Don't release camera depth - causes bugs.
            //cmd.ReleaseTemporaryRT(_CameraDepthTexture);
            cmd.ReleaseTemporaryRT(_Outlines);
            cmd.ReleaseTemporaryRT(_OutlinesTemp);
            cmd.ReleaseTemporaryRT(_PixelatedScene);
            cmd.ReleaseTemporaryRT(_OriginalScene);
        }

#region Materials
        private Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public Material GetMaterial(string shaderName)
        {
            Material material;
            if (Materials.TryGetValue(shaderName, out material))
            {
                return material;
            }
            else
            {
                Shader shader = Shader.Find(shaderName);

                if (shader == null)
                {
                    Debug.LogError("Shader not found (" + shaderName + "), check if missed shader is in Shaders folder if not reimport this package. If this problem occurs only in build try to add all shaders in Shaders folder to Always Included Shaders (Project Settings -> Graphics -> Always Included Shaders)");
                }

                Material NewMaterial = new Material(shader);
                NewMaterial.hideFlags = HideFlags.HideAndDontSave;
                Materials.Add(shaderName, NewMaterial);
                return NewMaterial;
            }
        }
#endregion
    }

    PixelisationPass _PixelisationPass;

    public override void Create()
    {
        _PixelisationPass = new PixelisationPass();
        _PixelisationPass.DepthTestOutlines = DepthTestOutlines;
        _PixelisationPass.DepthTestThreshold = DepthTestThreshold;
        _PixelisationPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_PixelisationPass);
    }
}