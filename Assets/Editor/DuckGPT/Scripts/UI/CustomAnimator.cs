using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Frame-based texture animator for use within the Unity Editor.
/// </summary>
public class CustomAnimator
{
    private readonly Dictionary<string, List<Texture2D>> animations = new();

    private string currentAnimation;
    private int currentFrame;
    private int playCount;
    private int playCounter;

    private readonly float frameTime;
    private double lastFrameTime;

    public bool IsAnimating { get; private set; }
    public bool AnimationFinished { get; private set; }

    public CustomAnimator(Dictionary<string, (string path, int frameCount)> animationRegistry, float frameTime = 0.15f) // Initializes the animator by loading all specified animations from the registry
    {
        foreach (var (name, (path, frameCount)) in animationRegistry) 
        {
            var frames = new List<Texture2D>();
            for (int i = 0; i < frameCount; i++)
            {
                Texture2D frame = AssetDatabase.LoadAssetAtPath<Texture2D>($"{path}/duck_{i}.png"); 
                if (frame != null) frames.Add(frame);
            }
            animations[name] = frames;
        }
        this.frameTime = frameTime;
        lastFrameTime = EditorApplication.timeSinceStartup;
    }

    public void SetAnimation(string name, int playTimes = 1) //starts animation, plays specified number of times
    {
        if (!animations.ContainsKey(name)) return;

        currentAnimation = name;
        playCount = Mathf.Max(1, playTimes);

        playCounter = 0;// Reset play counter for new animation
        currentFrame = 0;// Reset frame index and play counter for new animation

        IsAnimating = true;
        AnimationFinished = false;

        lastFrameTime = EditorApplication.timeSinceStartup;// Start frame timer 
    }

    /// <summary>
    /// Advances the animation and outputs the current frame.
    /// Returns true when all play cycles have completed.
    /// </summary>
    public bool Animate(out Texture2D frame)
    {
        UpdateFrame();
        frame = GetCurrentFrame();
        return AnimationFinished;
    }

    private Texture2D GetCurrentFrame()
    {
        if (currentAnimation == null || !animations.TryGetValue(currentAnimation, out var frames))
            return null;

        return frames.Count > 0 ? frames[currentFrame] : null;
    }

    private void UpdateFrame()
    {
        if (!IsAnimating || AnimationFinished || currentAnimation == null) return;
        if (!animations.TryGetValue(currentAnimation, out var frames) || frames.Count == 0) return;

        double time = EditorApplication.timeSinceStartup;// Check if enough time has passed to advance to the next frame
        if (time - lastFrameTime <= frameTime) return; 

        currentFrame++;
        lastFrameTime = time;

        if (currentFrame < frames.Count) return;

        currentFrame = 0;
        playCounter++;

        if (playCounter >= playCount)
        {
            AnimationFinished = true;
            IsAnimating = false;
        }
    }

    public static Dictionary<string, (string, int)> GetAllAnimations() => new()
    {
        { "confuse",  ("Assets/Editor/DuckGPT/Animations/duck_confused",  8)  },
        { "talk",     ("Assets/Editor/DuckGPT/Animations/duck_talk",      5)  },
        { "squeeze",  ("Assets/Editor/DuckGPT/Animations/duck_sqeeze",    4)  },
        { "read",     ("Assets/Editor/DuckGPT/Animations/duck_reading",   9)  },
        { "mute",     ("Assets/Editor/DuckGPT/Animations/duck_micOff",    7)  },
        { "errors",   ("Assets/Editor/DuckGPT/Animations/duck_errors",    9)  },
        { "scan",     ("Assets/Editor/DuckGPT/Animations/duck_scan",      14) },
        { "micOn",    ("Assets/Editor/DuckGPT/Animations/duck_micOn",     11) },
        { "micOff",   ("Assets/Editor/DuckGPT/Animations/duck_micOff",    8)  },
        { "micDrop",  ("Assets/Editor/DuckGPT/Animations/duck_dropMic",   10) },
        { "thinking", ("Assets/Editor/DuckGPT/Animations/duck_thinking",  8)  },
    };
}