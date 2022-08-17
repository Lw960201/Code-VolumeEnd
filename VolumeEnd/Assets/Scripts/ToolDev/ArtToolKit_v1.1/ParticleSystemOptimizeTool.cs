#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ArtToolKit
{
    public class ParticleSystemOptimizeToolWindow
    {
        public static void Optimize(bool toggleAutoMaxParticles,bool togglePlayOnAwake)
        {
            Object[] objects = Selection.objects;
            
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].GetType() != typeof(UnityEngine.GameObject))
                {
                    EditorUtility.DisplayDialog("执行优化的文件不是Prefab", "请检查后再试", "关闭");
                    break;
                }

                Transform transform = (objects[i] as GameObject).transform;
                ParticleSystem[] particleSystems = transform.GetComponentsInChildren<ParticleSystem>();
                ParticleSystemRenderer[] particleSystemsRenderers = transform.GetComponentsInChildren<ParticleSystemRenderer>();
                
                if (particleSystems.Length == 0)
                {
                    EditorUtility.DisplayDialog("没有找到ParticleSystem组件", "请检查后再试", "关闭");
                }
                else
                {
                    for (int n = 0; n < particleSystems.Length; n++)
                    {
                        var particleSystem = particleSystems[n];
                        var particleSystemsRenderer = particleSystemsRenderers[n];
                        UnityEngine.Debug.Log(" particleSystem  >> " + particleSystem.name);
                        particleSystem.scalingMode = ParticleSystemScalingMode.Hierarchy;
                        particleSystem.playOnAwake = togglePlayOnAwake;

                        if (!particleSystem.trails.enabled)
                            particleSystemsRenderer.trailMaterial = null;

                        particleSystemsRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        particleSystemsRenderer.receiveShadows = false;

                        int burstTotal = 0;

                        ParticleSystem.Burst[] bursts =
                            new ParticleSystem.Burst[particleSystem.emission.burstCount];
                        for (int m = 0; m < particleSystem.emission.burstCount; m++)
                        {
                            bursts[m] = particleSystem.emission.GetBurst(m);
                            burstTotal += bursts[m].maxCount;
                        }

                        if (toggleAutoMaxParticles)
                        {
                            particleSystem.maxParticles = Mathf.Max((int) (particleSystem.startLifetime * particleSystem.emissionRate),
                                    burstTotal);
                        }

                    }

                    PrefabUtility.SavePrefabAsset(objects[i] as GameObject);
                }
            }
        }


    }
}
#endif  