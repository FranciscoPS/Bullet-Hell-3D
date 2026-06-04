using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = new Color(1f, 0.18f, 0.12f, 1f);
    [SerializeField] private float flashDuration = 0.16f;
    [SerializeField] private int flickerCount = 2;
    [SerializeField] private float emissionIntensity = 1.5f;
    [SerializeField] private bool includeInactiveRenderers = true;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private readonly List<RendererFlashState> rendererStates = new List<RendererFlashState>();
    private Coroutine flashRoutine;
    private bool originalsCaptured;

    public float TotalDuration => Mathf.Max(0.01f, flashDuration);

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnDisable()
    {
        StopFlash();
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
            return;

        if (rendererStates.Count == 0)
            CacheRenderers();

        if (rendererStates.Count == 0)
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreOriginalBlocks();
        }

        CaptureOriginalBlocks();
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    public void RefreshRenderers()
    {
        StopFlash();
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        rendererStates.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactiveRenderers);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererToFlash = renderers[i];
            if (rendererToFlash == null)
                continue;

            int materialCount = rendererToFlash.sharedMaterials.Length;
            if (materialCount <= 0)
                continue;

            rendererStates.Add(new RendererFlashState(rendererToFlash, materialCount));
        }
    }

    private IEnumerator FlashRoutine()
    {
        int cycles = Mathf.Max(1, flickerCount);
        float stepDuration = TotalDuration / (cycles * 2f);
        WaitForSeconds wait = new WaitForSeconds(stepDuration);

        for (int i = 0; i < cycles; i++)
        {
            SetFlashColor();
            yield return wait;

            RestoreOriginalBlocks(keepCaptured: true);
            yield return wait;
        }

        RestoreOriginalBlocks();
        flashRoutine = null;
    }

    private void SetFlashColor()
    {
        Color emissionColor = flashColor * Mathf.Max(0f, emissionIntensity);

        for (int rendererIndex = 0; rendererIndex < rendererStates.Count; rendererIndex++)
        {
            RendererFlashState state = rendererStates[rendererIndex];
            if (state.Renderer == null)
                continue;

            for (int materialIndex = 0; materialIndex < state.MaterialCount; materialIndex++)
            {
                MaterialPropertyBlock block = state.WorkingBlocks[materialIndex];
                block.Clear();
                state.Renderer.GetPropertyBlock(block, materialIndex);
                block.SetColor(BaseColorId, flashColor);
                block.SetColor(ColorId, flashColor);
                block.SetColor(EmissionColorId, emissionColor);
                state.Renderer.SetPropertyBlock(block, materialIndex);
            }
        }
    }

    private void CaptureOriginalBlocks()
    {
        for (int rendererIndex = 0; rendererIndex < rendererStates.Count; rendererIndex++)
        {
            RendererFlashState state = rendererStates[rendererIndex];
            if (state.Renderer == null)
                continue;

            for (int materialIndex = 0; materialIndex < state.MaterialCount; materialIndex++)
            {
                MaterialPropertyBlock block = state.OriginalBlocks[materialIndex];
                block.Clear();
                state.Renderer.GetPropertyBlock(block, materialIndex);
                state.OriginalBlocksWereEmpty[materialIndex] = block.isEmpty;
            }
        }

        originalsCaptured = true;
    }

    private void RestoreOriginalBlocks(bool keepCaptured = false)
    {
        if (!originalsCaptured)
            return;

        for (int rendererIndex = 0; rendererIndex < rendererStates.Count; rendererIndex++)
        {
            RendererFlashState state = rendererStates[rendererIndex];
            if (state.Renderer == null)
                continue;

            for (int materialIndex = 0; materialIndex < state.MaterialCount; materialIndex++)
            {
                MaterialPropertyBlock block = state.OriginalBlocksWereEmpty[materialIndex]
                    ? null
                    : state.OriginalBlocks[materialIndex];

                state.Renderer.SetPropertyBlock(block, materialIndex);
            }
        }

        originalsCaptured = keepCaptured;
    }

    private void StopFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        RestoreOriginalBlocks();
    }

    private sealed class RendererFlashState
    {
        public readonly Renderer Renderer;
        public readonly int MaterialCount;
        public readonly MaterialPropertyBlock[] OriginalBlocks;
        public readonly bool[] OriginalBlocksWereEmpty;
        public readonly MaterialPropertyBlock[] WorkingBlocks;

        public RendererFlashState(Renderer renderer, int materialCount)
        {
            Renderer = renderer;
            MaterialCount = materialCount;
            OriginalBlocks = new MaterialPropertyBlock[materialCount];
            OriginalBlocksWereEmpty = new bool[materialCount];
            WorkingBlocks = new MaterialPropertyBlock[materialCount];

            for (int i = 0; i < materialCount; i++)
            {
                OriginalBlocks[i] = new MaterialPropertyBlock();
                WorkingBlocks[i] = new MaterialPropertyBlock();
            }
        }
    }
}
