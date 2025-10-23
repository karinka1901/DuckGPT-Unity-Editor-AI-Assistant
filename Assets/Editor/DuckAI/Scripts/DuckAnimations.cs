using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DuckAnimations
{
    private Dictionary<string, List<Texture2D>> animations = new();
    private string currentAnimation = null;
    private int currentFrame = 0;
    private float frameTime;
    private double lastFrameTime;

    public bool isAnimating = false;
    public bool finished = false;

    private int playCount = 1;
    private int playCounter = 0;

    public DuckAnimations(Dictionary<string, (string path, int frameCount)> animationDefs, float frameTime = 0.15f)
    {
        foreach (var kvp in animationDefs)
        {
            var frames = new List<Texture2D>();
            for (int i = 0; i < kvp.Value.frameCount; i++)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{kvp.Value.path}/duck_{i}.png");
                if (tex != null)
                    frames.Add(tex);
            }
            animations[kvp.Key] = frames;
        }
        this.frameTime = frameTime;
        finished = false;
        currentAnimation = animations.Count > 0 ? new List<string>(animations.Keys)[0] : null;
        currentFrame = 0;
        lastFrameTime = EditorApplication.timeSinceStartup;
    }

    public void SetAnimation(string name, int playTimes = 1)
    {
        if (!animations.ContainsKey(name)) return;
        currentAnimation = name;
        playCount = Mathf.Max(1, playTimes);
        playCounter = 0;
        currentFrame = 0;
        isAnimating = true;
        finished = false;
        lastFrameTime = EditorApplication.timeSinceStartup;
    }

    public Texture2D GetCurrentFrame()
    {
        if (currentAnimation == null || !animations.ContainsKey(currentAnimation)) return null;
        var frames = animations[currentAnimation];
        return frames.Count > 0 ? frames[currentFrame] : null;
    }

    public void UpdateFrame()
    {
        if (!isAnimating || finished || currentAnimation == null || !animations.ContainsKey(currentAnimation)) return;
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
                    finished = true;
                    isAnimating = false;
                }
            }
        }
    }

    public bool Animate(out Texture2D frame)
    {
        UpdateFrame();
        frame = GetCurrentFrame();
        return finished;
    }

    public static Dictionary<string, (string, int)> GetAllAnimations()
    {
        return new Dictionary<string, (string, int)>
        {
            { "jump", ("Assets/Editor/DuckAI/Animations/duck_jump", 4) },
  
        };
    }
}