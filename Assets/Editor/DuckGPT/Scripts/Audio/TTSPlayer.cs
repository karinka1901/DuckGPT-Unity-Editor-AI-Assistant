using System;
using System.Collections;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Provides static methods to play duck speech audio clips and manage associated animation callbacks within the Unity
/// Editor.
/// </summary>
public static class TTSPlayer 
{
    public static bool canPlayAudio = true;
    private static Action<string, int> onAnimationStart; // Action callback for starting animations, takes animation name and duration as parameters
    private static Action onAnimationStop; // Action callback for stopping animations

    public static void Initialize(Action<string, int> animationStartCallback, Action animationStopCallback) 
    {
        onAnimationStart = animationStartCallback;
        onAnimationStop = animationStopCallback;
    }

    public static void PlayTTSAudio(string filePath)
    {
        if (canPlayAudio) EditorCoroutineUtility.StartCoroutineOwnerless(PlayAudioCoroutine(filePath));
        else return;
    }

    private static IEnumerator PlayAudioCoroutine(string filePath) 
    {
        using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG); //load local audio file as a web request
        yield return audioRequest.SendWebRequest();
        
        if (audioRequest.result == UnityWebRequest.Result.Success)
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest); // Extract the AudioClip from the web request

            GameObject duckAudioPlayer = new("DuckAudioPlayer");
            AudioSource audioSource = duckAudioPlayer.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
            
            // Start duck talking animation
            onAnimationStart?.Invoke("talk", 1000);

            while (audioSource.isPlaying) // Wait until the audio finishes playing
            {
                canPlayAudio = false;
                yield return null;
            }

            // Stop animation
            onAnimationStop?.Invoke(); 
            DestroyAudio(duckAudioPlayer, filePath);
        }
        else
        {
            Debug.LogError("[DuckGPT] Failed to load audio: " + audioRequest.error);
        }
    }

    private static void DestroyAudio(GameObject audioSourceGameObject, string filePath) // Clean up the audio source and delete the audio file after playback
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(audioSourceGameObject); //play mode
        else UnityEngine.Object.DestroyImmediate(audioSourceGameObject); //editor mode

        File.Delete(filePath);

        canPlayAudio = true;

        DebugColor.Log($"[DuckGPT] Finished playing duck audio and audio files are destroyed", "orange");
    }
}
