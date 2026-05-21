// =============================================================
// MultiDelayedCameraSystem.cs
//
// ACM THRI revision — "System Design and Validation of an Ego-Exocentric
// Mixed Reality Framework in Compensating Delay for ROV Teleoperation"
// (Manuscript ID THRI-2025-0268).
//
// Paper reference: §4 (delay injection)
//
// Implements the per-trial delay on the visual channel. Each CameraFeed writes timestamped RenderTextures into a FIFO buffer; the consumer pops frames older than `delayInSeconds`, producing a constant 0 / 0.2 / 0.5 / 1.0 s delay between rendering and display.
//
// Released under the MIT License — see ../../LICENSE.
// =============================================================
﻿using System.Collections.Generic;
using UnityEngine;

public class MultiDelayedCameraSystem : MonoBehaviour
{
    [System.Serializable]
    public class CameraFeed
    {
        public Camera sourceCamera;
        public float delayInSeconds = 2f;
        public List<MeshRenderer> screenRenderers;

        [HideInInspector] public RenderTexture liveRT;
        [HideInInspector] public Queue<FrameData> buffer = new Queue<FrameData>();
        [HideInInspector] public Queue<RenderTexture> renderTexturePool = new Queue<RenderTexture>();
    }

    [System.Serializable]
    public class FrameData
    {
        public RenderTexture texture;
        public float timestamp;
    }

    [Header("Render Texture Settings")]
    public int renderWidth = 720;
    public int renderHeight = 720;

    public List<CameraFeed> cameraFeeds;

    void Start()
    {
        foreach (var feed in cameraFeeds)
        {
            // Create render texture for live preview
            feed.liveRT = new RenderTexture(renderWidth, renderHeight, 16, RenderTextureFormat.DefaultHDR);
            feed.liveRT.Create();
            feed.sourceCamera.targetTexture = feed.liveRT;

            // Pre-allocate render textures based on delay
            int poolSize = Mathf.CeilToInt(feed.delayInSeconds * 30f); // assume 30 FPS
            for (int i = 0; i < poolSize; i++)
            {
                RenderTexture rt = new RenderTexture(renderWidth, renderHeight, 0, RenderTextureFormat.DefaultHDR);
                rt.Create();
                feed.renderTexturePool.Enqueue(rt);
            }

            // Adjust screen sizes for aspect ratio
            float aspect = (float)renderWidth / renderHeight;
            foreach (var renderer in feed.screenRenderers)
            {
                Vector3 scale = renderer.transform.localScale;
                scale.y = 1f;
                scale.x = aspect;
                renderer.transform.localScale = scale;
            }
        }
    }

    void LateUpdate()
    {
        float currentTime = Time.time;

        foreach (var feed in cameraFeeds)
        {
            RenderTexture copy;

            // Try to dequeue a reusable RenderTexture
            if (feed.renderTexturePool.Count > 0)
            {
                copy = feed.renderTexturePool.Dequeue();
            }
            else
            {
                // Fallback: allocate if needed (shouldn't happen often)
                copy = new RenderTexture(renderWidth, renderHeight, 0, RenderTextureFormat.DefaultHDR);
                copy.Create();
            }

            // Capture current camera view into the copy
            Graphics.Blit(feed.liveRT, copy);

            // Store frame with timestamp
            feed.buffer.Enqueue(new FrameData
            {
                texture = copy,
                timestamp = currentTime
            });

            // Display the oldest delayed frame if delay time has passed
            if (feed.buffer.Count > 0 && currentTime - feed.buffer.Peek().timestamp >= feed.delayInSeconds)
            {
                FrameData delayedFrame = feed.buffer.Peek(); // do not dequeue yet

                if (delayedFrame.texture != null)
                {
                    foreach (var renderer in feed.screenRenderers)
                    {
                        renderer.material.SetTexture("_BaseColorMap", delayedFrame.texture);
                    }
                }

                // Only dequeue and recycle if more than one frame is buffered
                if (feed.buffer.Count > 1)
                {
                    FrameData oldFrame = feed.buffer.Dequeue();
                    if (oldFrame.texture != null)
                    {
                        feed.renderTexturePool.Enqueue(oldFrame.texture);
                    }
                }
            }
        }
    }

    void OnDestroy()
    {
        foreach (var feed in cameraFeeds)
        {
            if (feed.liveRT != null)
            {
                feed.liveRT.Release();
                Destroy(feed.liveRT);
            }

            foreach (var frame in feed.buffer)
            {
                if (frame.texture != null)
                {
                    frame.texture.Release();
                    Destroy(frame.texture);
                }
            }

            feed.buffer.Clear();

            while (feed.renderTexturePool.Count > 0)
            {
                var tex = feed.renderTexturePool.Dequeue();
                if (tex != null)
                {
                    tex.Release();
                    Destroy(tex);
                }
            }
        }
    }
}
