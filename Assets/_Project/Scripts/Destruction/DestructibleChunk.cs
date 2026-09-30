using System.Collections;
using JJKDemo.Combat.Physics;
using UnityEngine;

namespace JJKDemo.Combat.Destruction
{
    public class DestructibleChunk : MonoBehaviour, IDamageable
    {
        [Header("Dissolve Settings")]
        [Tooltip("The material created from the PurpleDissolve shader")]
        [SerializeField] public Material dissolveMaterial;
        
        [Tooltip("How fast the sphere expands (meters per second)")]
        [SerializeField] private float expandSpeed = 6f;

        private bool isDestroying = false;

        public void ApplyDamage(float amount, ImpactPayload payload)
        {
            if (payload.EffectMode == JJKDemo.Combat.Abilities.Data.AbilityEffectMode.Destroy)
            {
                DeleteChunkInstantly(payload.Point);
            }
        }

        public void DeleteChunkInstantly(Vector3 impactPoint = default)
        {
            // Prevent running the coroutine multiple times if hit multiple frames in a row
            if (isDestroying) return;
            
            if (dissolveMaterial != null && TryGetComponent<MeshRenderer>(out var renderer))
            {
                StartCoroutine(DissolveRoutine(renderer, impactPoint == default ? transform.position : impactPoint));
            }
            else
            {
                // Fallback if no material is assigned
                Destroy(gameObject);
            }
        }

        private IEnumerator DissolveRoutine(MeshRenderer renderer, Vector3 impactPoint)
        {
            isDestroying = true;

            // Carry the original colour and texture over to the dissolve material.
            Color oldColor = Color.white;
            Texture oldTex = null;
            var oldMat = renderer.sharedMaterial;

            if (oldMat != null)
            {
                if (oldMat.HasProperty("_BaseColor")) oldColor = oldMat.GetColor("_BaseColor");
                else if (oldMat.HasProperty("_Color")) oldColor = oldMat.GetColor("_Color");

                if (oldMat.HasProperty("_BaseMap")) oldTex = oldMat.GetTexture("_BaseMap");
                else if (oldMat.HasProperty("_MainTex")) oldTex = oldMat.GetTexture("_MainTex");
            }

            // 1. Swap the material to our dissolve material
            renderer.material = dissolveMaterial;
            
            // Reapply the original color and texture to the new dissolve material
            if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", oldColor);
            if (renderer.material.HasProperty("_BaseMap") && oldTex != null) renderer.material.SetTexture("_BaseMap", oldTex);
            
            // Seed the dissolve from the exact impact point.
            renderer.material.SetVector("_ImpactPosition", impactPoint);

            // Calculate exactly how far the sphere needs to travel to fully engulf this specific object
            float maxRadius = renderer.bounds.extents.magnitude + Vector3.Distance(renderer.bounds.center, impactPoint);
            
            // Time = Distance / Speed. This ensures large blocks take longer and small blocks are faster, but the edge always travels at exactly 'expandSpeed'
            float duration = maxRadius / expandSpeed;

            float elapsedTime = 0f;

            // 2. Animate the _ExpandRadius from 0 to Max over the calculated duration
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                
                float currentRadius = Mathf.Lerp(0f, maxRadius, progress);
                renderer.material.SetFloat("_ExpandRadius", currentRadius);
                
                yield return null;
            }

            // 3. Finally delete the object once it has visually burned away
            Destroy(gameObject);
        }
    }
}
