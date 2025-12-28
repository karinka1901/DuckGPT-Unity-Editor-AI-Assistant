using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DuckCustomAnimator
{
    private Dictionary<string, List<Texture2D>> animations = new();

    private string currentAnimation = null;
    private int currentFrame = 0;

    private float frameTime;
    private double lastFrameTime;

    public bool isAnimating = false;
    public bool animationFinished = false;

    private int playCount = 1;
    private int playCounter = 0;

    public DuckCustomAnimator(Dictionary<string, (string path, int frameCount)> animationDefs, float frameTime = 0.15f)
    {
        foreach (var kvp in animationDefs)
        {
            var frames = new List<Texture2D>();
            for (int i = 0; i < kvp.Value.frameCount; i++)
            {
                var frame = AssetDatabase.LoadAssetAtPath<Texture2D>($"{kvp.Value.path}/duck_{i}.png");
                if (frame != null)
                    frames.Add(frame);
            }
            animations[kvp.Key] = frames;
        }
        this.frameTime = frameTime;

        animationFinished = false;

        if (animations.Count > 0) currentAnimation = new List<string>(animations.Keys)[0];
        else currentAnimation = null;

        currentFrame = 0;

        lastFrameTime = EditorApplication.timeSinceStartup; // Initialize lastFrameTime
    }

    public void SetAnimation(string name, int playTimes = 1)
    {
        if (!animations.ContainsKey(name)) return;

        currentAnimation = name;
        playCount = Mathf.Max(1, playTimes);
        playCounter = 0;
        currentFrame = 0;
        isAnimating = true;
        animationFinished = false;

        lastFrameTime = EditorApplication.timeSinceStartup;
    }

    public Texture2D GetCurrentFrame() // Returns the current frame of the active animation
    {
        if (currentAnimation == null || !animations.ContainsKey(currentAnimation)) return null;
        var frames = animations[currentAnimation];
        return frames.Count > 0 ? frames[currentFrame] : null;
    }

    public void UpdateFrame()
    {
        if (!isAnimating || animationFinished || currentAnimation == null || !animations.ContainsKey(currentAnimation)) return;
        var frames = animations[currentAnimation];
        if (frames.Count == 0) return;

        double time = EditorApplication.timeSinceStartup;
        if (time - lastFrameTime > frameTime)
        {
            currentFrame++;
            lastFrameTime = time;

            if (currentFrame >= frames.Count)
            {
                playCounter++;
                if (playCounter < playCount)
                {
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = 0;
                    animationFinished = true;
                    isAnimating = false;
                }
            }
        }
    }

    public bool Animate(out Texture2D frame)
    {
        UpdateFrame();
        frame = GetCurrentFrame();
        return animationFinished;
    }

    public static Dictionary<string, (string, int)> GetAllAnimations()
    {
        return new Dictionary<string, (string, int)>
        {        
            { "confuse",("Assets/Editor/DuckGPT/Animations/duck_confused", 8)},
            { "talk", ("Assets/Editor/DuckGPT/Animations/duck_talk", 5) },
            { "squeeze", ("Assets/Editor/DuckGPT/Animations/duck_sqeeze", 4)},
            { "read", ("Assets/Editor/DuckGPT/Animations/duck_reading", 9) },
            { "mute", ("Assets/Editor/DuckGPT/Animations/duck_micOff", 7) },
            { "errors", ("Assets/Editor/DuckGPT/Animations/duck_errors", 9) },
            { "scan", ("Assets/Editor/DuckGPT/Animations/duck_scan", 14) },
            { "micOn", ("Assets/Editor/DuckGPT/Animations/duck_micOn", 11) },
            { "micOff", ("Assets/Editor/DuckGPT/Animations/duck_micOff", 8) },
            { "micDrop", ("Assets/Editor/DuckGPT/Animations/duck_dropMic", 10) },
            { "thinking", ("Assets/Editor/DuckGPT/Animations/duck_thinking", 8) },
        };
    }
}