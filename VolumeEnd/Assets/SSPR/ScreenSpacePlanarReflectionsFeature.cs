﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;


public class ScreenSpacePlanarReflectionsFeature : ScriptableRendererFeature
{
    [System.Serializable, ReloadGroup]
    public class ScreenSpacePlanarReflectionsSettings
    {
        [Reload("Shaders/ReflectionShader.compute")]
        public ComputeShader reflectionCS;

        [Reload("Shaders/ReflectionShader.shader")]
        public Shader reflectionShader;

        public Quaternion PlaneRotation;
        public Vector3 PlaneLocation;

        public bool ApplyEdgeStretch = true;
        public float StretchThreshold = 0.95f;
        public float StretchIntensity = 1.0f;
        public bool ApplyBlur = true;


        public bool RenderReflectiveLayer = true;
        //[ConditionalField("RenderReflectiveLayer")]
        public LayerMask ReflectiveSurfaceLayer;

        public bool StencilOptimization = true;
        [Range(1,255)]
        public int StencilValue = 255;

        //public StencilStateData stencilSettings = new StencilStateData();
        public bool needsStencilPass
        {
            get { return StencilOptimization && ReflectiveSurfaceLayer != 0; }
        }

        public bool needsRenderReflective
        {
            get { return RenderReflectiveLayer && ReflectiveSurfaceLayer != 0; }
        }

        internal bool EnqueuedStencilPass = false;
        



    }

    class StencilRenderPass : ScriptableRenderPass
    {
        private const string m_ProfilerTag = "ReflectionsFeature_Stencil";
        private ProfilingSampler m_ProfilerSampler = new ProfilingSampler(m_ProfilerTag);
        private ScreenSpacePlanarReflectionsSettings m_Settings;
        RenderStateBlock m_RenderStateBlock;
        FilteringSettings m_FilteringSettings;

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        RenderTargetIdentifier m_CameraColorTarget;
        RenderTargetIdentifier m_CameraDepthTarget;

        RenderTargetHandle m_DepthTexture;


        public StencilRenderPass(ScreenSpacePlanarReflectionsSettings settings)
        {
            m_Settings = settings;

            m_RenderStateBlock = new RenderStateBlock();
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(false, CompareFunction.Always);

            m_RenderStateBlock.mask |= RenderStateMask.Stencil;

            StencilState stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(CompareFunction.Always);
            stencilState.SetPassOperation(StencilOp.Replace);
            stencilState.SetFailOperation(StencilOp.Zero);
            stencilState.SetZFailOperation(StencilOp.Keep);

            m_RenderStateBlock.stencilReference = settings.StencilValue;
            m_RenderStateBlock.stencilState = stencilState;

            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings.ReflectiveSurfaceLayer);

            m_ShaderTagIdList.Add(new ShaderTagId("DepthOnly"));

            m_DepthTexture.Init("_CameraDepthTexture");

        }

        bool CanCopyDepth(ref CameraData cameraData)
        {
            bool msaaEnabledForCamera = cameraData.cameraTargetDescriptor.msaaSamples > 1;
            bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
            bool supportsDepthTarget = RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
            bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);

            // TODO:  We don't have support to highp Texture2DMS currently and this breaks depth precision.
            // currently disabling it until shader changes kick in.
            //bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;
            bool msaaDepthResolve = false;
            return supportsDepthCopy || msaaDepthResolve;
        }

        public bool Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_CameraColorTarget = renderer.cameraColorTarget;
            m_CameraDepthTarget = renderer.cameraDepth;

            // if it matches with Camera Target then it should be equal to whatever CameraColorTarget is
            if (m_CameraDepthTarget.Equals(RenderTargetHandle.CameraTarget.Identifier()))
            {
                m_CameraDepthTarget = m_CameraColorTarget;
            }

            bool requiresDepthTexture = renderingData.cameraData.requiresDepthTexture;
            // Depth prepass is generated in the following cases:
            // - Scene view camera always requires a depth texture. We do a depth pre-pass to simplify it and it shouldn't matter much for editor.
            // - If game or offscreen camera requires it we check if we can copy the depth from the rendering opaques pass and use that instead.
            bool requiresDepthPrepass = renderingData.cameraData.isSceneViewCamera;
            requiresDepthPrepass |= (requiresDepthTexture && !CanCopyDepth(ref renderingData.cameraData));

            bool createDepthTexture = requiresDepthTexture && !requiresDepthPrepass;

            if(requiresDepthPrepass)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPrePasses; // we want to render into the depth texture after its been created
                return true;
            }
            else if(createDepthTexture)
            {
                if (renderingData.cameraData.requiresDepthTexture && createDepthTexture)
                {
                    renderPassEvent = RenderPassEvent.BeforeRenderingSkybox; // we want to render into the depth texture after its been copied
                    return true;
                }
                    
            }
            return false;

        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {

           // cmd.GetTemporaryRT(m_DepthTexture.id, cameraTextureDescriptor, FilterMode.Point);
            ConfigureTarget(m_DepthTexture.Identifier());
            ConfigureClear(ClearFlag.None, Color.black);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            // Update the Value
            m_RenderStateBlock.stencilReference = m_Settings.StencilValue;
            m_FilteringSettings.layerMask = m_Settings.ReflectiveSurfaceLayer;

            // Draw settings
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            // Render any 'reflective surfaces' with the stencil value,
            // will use this to generate the texture later
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingScope(cmd, m_ProfilerSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings,
                    ref m_RenderStateBlock);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }


    class ReflectionsRenderPass : ScriptableRenderPass
    {
        private const string m_ProfilerTag = "ReflectionsFeature_Render";
        private ProfilingSampler m_ProfilerSampler = new ProfilingSampler(m_ProfilerTag);

        private ScreenSpacePlanarReflectionsSettings m_Settings;

        RenderTargetHandle m_ScreenSpacePlanarReflection;
        RenderTargetHandle m_ScreenSpacePlanarReflectionBuffer;
        RenderTargetHandle m_DebugBuffer;
        RenderTexture m_ScreenSpacePlanarReflectionTexture;
        RenderTexture m_ScreenSpacePlanarReflectionTextureBuffer;
        RenderTextureDescriptor m_RenderTextureDescriptor;
        RenderTextureDescriptor m_RenderTextureBufferDescriptor;
        RenderTextureDescriptor m_DebugBufferDescriptor;

        RenderTargetIdentifier m_CameraColorTarget;
        RenderTargetIdentifier m_CameraDepthTarget;
        RenderTargetHandle m_DepthTexture;
        ComputeBuffer m_MetalBuffer;
        int m_BufferStride;

        private const string _NO_MSAA = "_NO_MSAA";
        private const string _MSAA_2 = "_MSAA_2";
        private const string _MSAA_4 = "_MSAA_4";
        private const string COLOR_ATTACHMENT = "COLOR_ATTACHMENT";


        RenderTargetHandle[] m_Temp = new RenderTargetHandle[2];

        Vector2Int m_Size;
        Vector2Int m_ThreadSize;

        ComputeShader m_ReflectionShaderCS;
        Shader m_ReflectionShader;
        Material m_ReflectionMaterial;

        int m_ClearKernal;
        int m_RenderKernal;
        int m_RenderStretchKernal;
        int m_DebugKernal;
        int m_MSSA2Kernal;
        int m_MSSA4Kernal;
        int m_MSSA8Kernal;
        int m_PropertyResult;
        int m_PropertyResultSize;
        int m_PropertyDepth;
        int m_PropertyDepth_MSAA2;
        int m_PropertyDepth_MSAA4;
        int m_PropertyDepth_MSAA8;
        int m_PropertyInvVP;
        int m_PropertyVP;
        int m_PropertyReflectionData;
        int m_PropertySSPRBufferRange;
        int m_PropertyMainTex;
        int m_PropertyDebugTex;
        int m_PropertyBufferStep;
        int m_PropertyStencil;

        Matrix4x4 m_InvVP;
        Matrix4x4 m_VP;
        Vector4[] m_ReflectionData;

        RenderStateBlock m_RenderStateBlock;

        bool m_MSAA = true;
        int m_MSAACount = 0;

        const bool bDebug = false;

        public ReflectionsRenderPass(ScreenSpacePlanarReflectionsSettings settings)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            m_Settings = settings;
            m_ScreenSpacePlanarReflection.Init("_ScreenSpacePlanarReflectionTexture");
            m_ScreenSpacePlanarReflectionBuffer.Init("_ScreenSpacePlanarReflectionBuffer");

            m_Temp[0].Init("_SSPRTempA");
            m_Temp[1].Init("_SSPRTempB");

            m_RenderTextureBufferDescriptor = new RenderTextureDescriptor(512,512,RenderTextureFormat.RInt, 0);
            m_RenderTextureBufferDescriptor.enableRandomWrite = true;

            m_DebugBufferDescriptor = new RenderTextureDescriptor(512, 512, RenderTextureFormat.ARGBFloat, 0);
            m_DebugBufferDescriptor.enableRandomWrite = true;

            m_RenderTextureDescriptor = new RenderTextureDescriptor(512, 512, RenderTextureFormat.ARGB32, 0);

            m_DepthTexture.Init("_CameraDepthTexture");
            m_RenderTextureDescriptor.msaaSamples = 1;
            m_RenderTextureDescriptor.bindMS = false;
            m_Size = new Vector2Int(512,512);
            m_ThreadSize = new Vector2Int(1, 1);
            m_ReflectionShaderCS = settings.reflectionCS;
            m_ReflectionShader = settings.reflectionShader;

            if(m_ReflectionShaderCS!=null)
            {
                m_ClearKernal = m_ReflectionShaderCS.FindKernel("CSClear");
                m_RenderKernal = m_ReflectionShaderCS.FindKernel("CSMain");
                m_RenderStretchKernal = m_ReflectionShaderCS.FindKernel("CSMain_Stretch");
                m_DebugKernal = m_ReflectionShaderCS.FindKernel("CSDebug");

                m_MSSA2Kernal = m_ReflectionShaderCS.FindKernel("CSMain_MS2");
                m_MSSA4Kernal = m_ReflectionShaderCS.FindKernel("CSMain_MS4");
                m_MSSA8Kernal = m_ReflectionShaderCS.FindKernel("CSMain_MS8");
            }

            if(m_ReflectionShader != null)
            {
                m_ReflectionMaterial = new Material(m_ReflectionShader);
            }

            m_PropertyResult = Shader.PropertyToID("Result");
            m_PropertyResultSize = Shader.PropertyToID("ResultSize");
            m_PropertyDepth = Shader.PropertyToID("_CameraDepthTexture");
            m_PropertyDepth_MSAA2 = Shader.PropertyToID("CameraDepth_MSAA2");
            m_PropertyDepth_MSAA4 = Shader.PropertyToID("CameraDepth_MSAA4");
            m_PropertyDepth_MSAA8 = Shader.PropertyToID("CameraDepth_MSAA8");
            m_PropertyInvVP = Shader.PropertyToID("InverseViewProjection");
            m_PropertyVP = Shader.PropertyToID("ViewProjection");
            m_PropertyReflectionData = Shader.PropertyToID("ReflectionData");
            m_PropertySSPRBufferRange = Shader.PropertyToID("_SSPRBufferRange");
            m_PropertyMainTex = Shader.PropertyToID("_MainTex");
            m_PropertyDebugTex = Shader.PropertyToID("WorldPositionAndPlaneDistance");
            m_PropertyBufferStep = Shader.PropertyToID("_SSPRBufferStride");

            m_PropertyStencil = Shader.PropertyToID("_StencilRef");

            m_InvVP = new Matrix4x4();
            m_VP = new Matrix4x4();
            m_ReflectionData = new Vector4[3] { new Vector4(), new Vector4(), new Vector4() };
            m_MSAA = false;
            m_MSAACount = 1;
        }

        public void SetTargets(ScriptableRenderer renderer)
        {
            m_CameraColorTarget = renderer.cameraColorTarget;
            m_CameraDepthTarget = renderer.cameraDepth;

            // if it matches with Camera Target then it should be equal to whatever CameraColorTarget is
            if(m_CameraDepthTarget.Equals(RenderTargetHandle.CameraTarget.Identifier()))
            {
                m_CameraDepthTarget = m_CameraColorTarget;
            }
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            m_Size.x = cameraTextureDescriptor.width;
            m_Size.y = cameraTextureDescriptor.height;
            m_RenderTextureBufferDescriptor.width = m_Size.x;
            m_RenderTextureBufferDescriptor.height = m_Size.y;
            m_RenderTextureDescriptor.width = m_Size.x;
            m_RenderTextureDescriptor.height = m_Size.y;
            m_DebugBufferDescriptor.width = m_Size.x;
            m_DebugBufferDescriptor.height = m_Size.y;
            //m_RenderTextureDescriptor.colorFormat = cameraTextureDescriptor.colorFormat;

            cmd.GetTemporaryRT(m_ScreenSpacePlanarReflection.id, m_RenderTextureDescriptor);

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                m_BufferStride = ((m_Size.x/4) + (m_Size.x%4 > 0 ? 1 : 0)) * 16;
                int Size = ((m_Size.y/4) + (m_Size.y % 4 > 0 ? 1 : 0)) * m_BufferStride;

                if(m_MetalBuffer == null || m_MetalBuffer.count < Size)
                {
                    if(m_MetalBuffer!=null)
                    {
                        //Debug.Log("Releasing Buffer, old size "+m_MetalBuffer.count + " new size "+Size);
                        m_MetalBuffer.Release();
                    }
                    m_MetalBuffer = new ComputeBuffer(Size, 4, ComputeBufferType.Default);
                }

            }
            else
            {
                cmd.GetTemporaryRT(m_ScreenSpacePlanarReflectionBuffer.id, m_RenderTextureBufferDescriptor);
            }

            if(m_Settings.ApplyBlur)
            {
                cmd.GetTemporaryRT(m_Temp[0].id, m_RenderTextureDescriptor);
            }

            if (bDebug)
            {
                cmd.GetTemporaryRT(m_DebugBuffer.id, m_DebugBufferDescriptor);
            }

            // if were using MSAA then we would have to resolve the depth texture again to get the stencil values
            m_MSAA = cameraTextureDescriptor.msaaSamples > 1;
            m_MSAACount = cameraTextureDescriptor.msaaSamples;

            m_ThreadSize.x = m_Size.x / 32 + (m_Size.x % 32 > 0 ? 1 : 0);
            m_ThreadSize.y = m_Size.y / 32 + (m_Size.y % 32 > 0 ? 1 : 0);

            //Debug.Log("Thread Size " + m_ThreadSize.x + ", " + m_ThreadSize.y);

            if (m_Settings.reflectionCS != null && m_Settings.reflectionCS!=m_ReflectionShaderCS)
            {
                m_ReflectionShaderCS = m_Settings.reflectionCS;
            }

            if (m_Settings.reflectionShader != null && m_Settings.reflectionShader != m_ReflectionShader)
            {
                m_ReflectionShader = m_Settings.reflectionShader;
                m_ReflectionMaterial = new Material(m_ReflectionShader);
            }
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_ReflectionShaderCS == null || m_ReflectionShader == null)
            {
                return;
            }

            if (m_ReflectionMaterial == null || m_ReflectionMaterial.shader == null)
            {
                m_ReflectionMaterial = new Material(m_ReflectionShader);
            }

            Camera camera = renderingData.cameraData.camera;

            // Calculate Our Plane and Matrices
            Vector3 temp;
            temp = m_Settings.PlaneRotation * Vector3.up;
            temp.Normalize();
            m_ReflectionData[0].x = temp.x;
            m_ReflectionData[0].y = temp.y;
            m_ReflectionData[0].z = temp.z;
            m_ReflectionData[0].w = -Vector3.Dot(temp, m_Settings.PlaneLocation);
            m_ReflectionData[1].x = 1.0f / m_Size.x;
            m_ReflectionData[1].y = 1.0f / m_Size.y;
            m_ReflectionData[1].z = m_ReflectionData[1].x * 0.5f;
            m_ReflectionData[1].w = m_ReflectionData[1].y * 0.5f;
            m_ReflectionData[2].x = camera.transform.forward.z;
            m_ReflectionData[2].y = m_Settings.StretchThreshold;
            m_ReflectionData[2].z = m_Settings.StretchIntensity;


            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
            Matrix4x4 worldToCamera = camera.cameraToWorldMatrix;
            Vector4 zaxis  = worldToCamera.GetRow(2);
            worldToCamera.SetRow(2, -zaxis);


            m_VP = camera.projectionMatrix;// * worldToCamera;
            m_VP = camera.projectionMatrix;
            m_VP = gpuProj * camera.worldToCameraMatrix;
            m_InvVP = m_VP.inverse;

            bool UseCameraDepth = !renderingData.cameraData.requiresDepthTexture && !renderingData.cameraData.isSceneViewCamera;


            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingScope(cmd, m_ProfilerSampler))
            {
                
                // need to run compute shader to clear
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                {
                    cmd.SetComputeBufferParam(m_ReflectionShaderCS, m_ClearKernal, m_PropertyResult, m_MetalBuffer);
                    cmd.SetComputeIntParam(m_ReflectionShaderCS, m_PropertyBufferStep, m_BufferStride);
                }
                else
                {
                    cmd.SetComputeTextureParam(m_ReflectionShaderCS, m_ClearKernal, m_PropertyResult, m_ScreenSpacePlanarReflectionBuffer.Identifier());
                }
                cmd.SetComputeIntParams(m_ReflectionShaderCS, m_PropertyResultSize, m_Size.x, m_Size.y);
                cmd.DispatchCompute(m_ReflectionShaderCS, m_ClearKernal, m_ThreadSize.x, m_ThreadSize.y, 1);
                {
                    //GraphicsFence fence = cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.ComputeProcessing);
                    //cmd.WaitOnAsyncGraphicsFence(fence);
                }
                
                if (bDebug && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal)
                {
                    cmd.SetComputeTextureParam(m_ReflectionShaderCS, m_DebugKernal, m_PropertyResult, m_ScreenSpacePlanarReflectionBuffer.Identifier());
                    cmd.SetComputeTextureParam(m_ReflectionShaderCS, m_DebugKernal, m_PropertyDebugTex, m_DebugBuffer.Identifier());
                    cmd.SetComputeIntParams(m_ReflectionShaderCS, m_PropertyResultSize, m_Size.x, m_Size.y);
                    cmd.SetComputeMatrixParam(m_ReflectionShaderCS, m_PropertyInvVP, m_InvVP);
                    cmd.SetComputeMatrixParam(m_ReflectionShaderCS, m_PropertyVP, m_VP);
                    cmd.SetComputeVectorArrayParam(m_ReflectionShaderCS, m_PropertyReflectionData, m_ReflectionData);

                    cmd.DispatchCompute(m_ReflectionShaderCS, m_DebugKernal, m_ThreadSize.x, m_ThreadSize.y, 1);
                }
                else
                {
                    int Kernal = m_Settings.ApplyEdgeStretch ? m_RenderStretchKernal : m_RenderKernal;

                    if(UseCameraDepth)
                    {
                        if (m_MSAA)
                        {
                            // okay change the kernal to
                            switch(m_MSAACount)
                            {
                                case 2: Kernal = m_MSSA2Kernal; cmd.SetComputeTextureParam(m_ReflectionShaderCS, Kernal, m_PropertyDepth_MSAA2, m_CameraDepthTarget); break;
                                case 4: Kernal = m_MSSA4Kernal; cmd.SetComputeTextureParam(m_ReflectionShaderCS, Kernal, m_PropertyDepth_MSAA4, m_CameraDepthTarget); break;
                                case 8: Kernal = m_MSSA8Kernal; cmd.SetComputeTextureParam(m_ReflectionShaderCS, Kernal, m_PropertyDepth_MSAA8, m_CameraDepthTarget); break;
                                default:
                                    Debug.LogError("Undefined MSAA Level");
                                    break;
                            }
                        }
                        else
                        {
                            cmd.SetComputeTextureParam(m_ReflectionShaderCS, Kernal, m_PropertyDepth, m_CameraDepthTarget);
                        }
                    }
                    else
                    {
                        cmd.SetComputeTextureParam(m_ReflectionShaderCS, Kernal, m_PropertyDepth, m_DepthTexture.Identifier());
                    }
                    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                    {
                        cmd.SetComputeBufferParam(m_ReflectionShaderCS, Kernal, m_PropertyResult, m_MetalBuffer);
                        cmd.SetComputeIntParam(m_ReflectionShaderCS, m_PropertyBufferStep, m_BufferStride);
                    }
                    else
                    {
                        cmd.SetComputeTextureParam(m_ReflectionShaderCS, Kernal, m_PropertyResult, m_ScreenSpacePlanarReflectionBuffer.Identifier());
                    }

                    
                    //cmd.SetComputeTextureParam(m_ReflectionShaderCS, Kernal, m_PropertyDepth, m_DepthTexture.Identifier());
                    //cmd.SetComputeTextureParam(m_ReflectionShaderCS, m_RenderKernal, m_PropertyDepth, BuiltinRenderTextureType.Depth);
                    cmd.SetComputeIntParams(m_ReflectionShaderCS, m_PropertyResultSize, m_Size.x, m_Size.y);
                    cmd.SetComputeMatrixParam(m_ReflectionShaderCS, m_PropertyInvVP, m_InvVP);
                    cmd.SetComputeMatrixParam(m_ReflectionShaderCS, m_PropertyVP, m_VP);
                    cmd.SetComputeVectorArrayParam(m_ReflectionShaderCS, m_PropertyReflectionData, m_ReflectionData);

                    cmd.DispatchCompute(m_ReflectionShaderCS, Kernal, m_ThreadSize.x, m_ThreadSize.y, 1);
                }
                {
                    GraphicsFence fence = cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.ComputeProcessing);
                    cmd.WaitOnAsyncGraphicsFence(fence);
                }
                
                
                if (m_Settings.ApplyBlur)
                {
                    // now we can render into the temporary texture where the stencil is set or full screen depending if the optimisation is on
                    RenderReflection(cmd, m_Temp[0].Identifier(), camera);
                    // render blur
                    RenderBlur(cmd, m_ScreenSpacePlanarReflection.Identifier(), m_Temp[0].Identifier(), camera);
                }
                else
                {
                    // now we can render into the temporary texture where the stencil is set or full screen depending if the optimisation is on
                    RenderReflection(cmd, m_ScreenSpacePlanarReflection.Identifier(), camera);
                }
                


                // restore target state
                cmd.SetRenderTarget(m_CameraColorTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, m_CameraDepthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                cmd.SetGlobalTexture(m_ScreenSpacePlanarReflection.id, m_ScreenSpacePlanarReflection.Identifier());

            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new System.ArgumentNullException("cmd");

            cmd.ReleaseTemporaryRT(m_ScreenSpacePlanarReflection.id);
            cmd.ReleaseTemporaryRT(m_ScreenSpacePlanarReflectionBuffer.id);

            if (m_Settings.ApplyBlur)
            {
                cmd.ReleaseTemporaryRT(m_Temp[0].id);
            }
        }

        void RenderReflection(CommandBuffer cmd, RenderTargetIdentifier target, Camera camera)
        {
            

            if(m_Settings.EnqueuedStencilPass)
            {
                // set the stencil value
                m_ReflectionMaterial.SetInt(m_PropertyStencil, m_Settings.StencilValue);
                // if were MSAA lets use the depth texture other wise we can re-use the camera depth texture
                cmd.SetRenderTarget(target, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_DepthTexture.Identifier(), RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                cmd.ClearRenderTarget(false, true, Color.black);
            }
            else
            {
                cmd.SetRenderTarget(target, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.ClearRenderTarget(false, true, Color.black);
            }

            cmd.SetGlobalVector(m_PropertySSPRBufferRange, new Vector4(m_Size.x, m_Size.y, 0, 0));
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                cmd.SetGlobalBuffer(m_ScreenSpacePlanarReflectionBuffer.id, m_MetalBuffer);
                cmd.SetGlobalInt(m_PropertyBufferStep, m_BufferStride);
            }
            else
            {
                cmd.SetGlobalTexture(m_ScreenSpacePlanarReflectionBuffer.id, m_ScreenSpacePlanarReflectionBuffer.Identifier());
            }
            cmd.SetGlobalTexture(m_PropertyMainTex, m_CameraColorTarget);

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.SetViewport(camera.pixelRect);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_ReflectionMaterial, 0, m_Settings.EnqueuedStencilPass ? 2 : 0);

            if (!m_Settings.ApplyBlur)
            {
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            }

        }

        void RenderBlur(CommandBuffer cmd, RenderTargetIdentifier target, RenderTargetIdentifier source, Camera camera)
        {
            if (m_Settings.EnqueuedStencilPass)
            {
                // if were MSAA lets use the depth texture other wise we can re-use the camera depth texture
                cmd.SetRenderTarget(target, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_DepthTexture.Identifier(), RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare);
            }
            else
            {
                cmd.SetRenderTarget(target, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            }

            cmd.ClearRenderTarget(false, true, Color.black);
            cmd.SetGlobalTexture(m_PropertyMainTex, source);

            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_ReflectionMaterial, 0, m_Settings.EnqueuedStencilPass ? 3 : 1);

            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            
        }
    }


    class DrawReflectiveLayerRenderPass : ScriptableRenderPass
    {
        private const string m_ProfilerTag = "ReflectionsFeature_DrawPass";
        private ProfilingSampler m_ProfilerSampler = new ProfilingSampler(m_ProfilerTag);
        private ScreenSpacePlanarReflectionsSettings m_Settings;

        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        public DrawReflectiveLayerRenderPass(ScreenSpacePlanarReflectionsSettings settings)
        {
            m_Settings = settings;

            m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, m_Settings.ReflectiveSurfaceLayer);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingScope(cmd, m_ProfilerSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Camera camera = renderingData.cameraData.camera;
                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortFlags);


                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings, ref m_RenderStateBlock);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public ScreenSpacePlanarReflectionsSettings settings = new ScreenSpacePlanarReflectionsSettings();
    StencilRenderPass m_StencilPass;
    ReflectionsRenderPass m_ReflectionsPass;
    DrawReflectiveLayerRenderPass m_DrawReflectivePass;

    public override void Create()
    {
#if UNITY_EDITOR
        ResourceReloader.TryReloadAllNullIn(settings, "Assets/");
#endif

        m_StencilPass = new StencilRenderPass(settings);
        m_ReflectionsPass = new ReflectionsRenderPass(settings);
        m_DrawReflectivePass = new DrawReflectiveLayerRenderPass(settings);

        // Configures where the render pass should be injected.
        m_StencilPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        m_ReflectionsPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        m_DrawReflectivePass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    bool CanSupportSSPR(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // do we has a depth texture or support using the camera depth
        return SystemInfo.supportsComputeShaders // we need compute
            && (renderingData.cameraData.requiresDepthTexture || renderer.cameraDepth != RenderTargetHandle.CameraTarget.Identifier()) // we need a depth texture to read
            && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2 // we dont support open gles 2
            && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3; // we dont support open gles 3
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // we dont bother if we dont support this
        if(!CanSupportSSPR(renderer, ref renderingData))
        {
            return;
        }

        settings.EnqueuedStencilPass = false;

        if (settings.needsStencilPass)
        {
            if(m_StencilPass.Setup(renderer, ref renderingData))
            {
                renderer.EnqueuePass(m_StencilPass);
                settings.EnqueuedStencilPass = true;
            }
        }

        m_ReflectionsPass.SetTargets(renderer);
        renderer.EnqueuePass(m_ReflectionsPass);

        if(settings.needsRenderReflective)
        {
            renderer.EnqueuePass(m_DrawReflectivePass);
        }
        
    }

}


