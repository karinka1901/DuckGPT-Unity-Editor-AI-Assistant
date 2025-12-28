using System;
using System.Collections;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Provides functionality to play duck audio files asynchronously.
/// </summary>
public static class DuckSpeechPlayer 
{
    public static bool canPlayAudio = true;
    private static Action<string, int> onAnimationStart;
    private static Action onAnimationStop;

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
        using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG); 
        yield return audioRequest.SendWebRequest();
        
        if (audioRequest.result == UnityWebRequest.Result.Success)
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);

            var duckAudioPlayer = new GameObject("DuckAudioPlayer");
            var audioSource = duckAudioPlayer.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
            
            // Start duck talking animation
            onAnimationStart?.Invoke("talk", 1000);

            while (audioSource.isPlaying)
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
            Debug.LogError("Failed to load audio: " + audioRequest.error);
        }
    }

    private static void DestroyAudio(GameObject audioSourceGameObject, string filePath)
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(audioSourceGameObject);
        else UnityEngine.Object.DestroyImmediate(audioSourceGameObject);
           
        File.Delete(filePath);
        canPlayAudio = true;

        Debug.Log($"Finished playing duck audio and audio files are destroyed");
    }
}
