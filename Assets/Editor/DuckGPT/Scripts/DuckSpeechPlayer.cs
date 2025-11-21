using System.Collections;
using System;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Networking;
/// <summary>
/// Provides functionality to play duck audio files asynchronously.
/// </summary>
/// <remarks>This class is designed to handle the playback of audio files in a Unity environment.  The audio file
/// is loaded from the specified file path, played using a temporary  <see cref="GameObject"/> with an <see
/// cref="AudioSource"/>, and then cleaned up  after playback is complete. The audio file is deleted from the file
/// system once playback finishes.</remarks>
public static class DuckSpeechPlayer 
{
    public static bool canPlayAudio = true; // Flag to prevent overlapping audio playback


    public static void PlayTTSAudio(string filePath)
    {
        if (canPlayAudio) EditorCoroutineUtility.StartCoroutineOwnerless(PlayAudioCoroutine(filePath));
        else return;
    }

    private static IEnumerator PlayAudioCoroutine(string filePath)
    {
        // Load the audio clip from the specified file path
        using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG); 
        yield return audioRequest.SendWebRequest();
        if (audioRequest.result == UnityWebRequest.Result.Success)
        {
            // Get the audio clip from the download handler
            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);

            // create a temporary GameObject with AudioSource component to play the audio
            var duckAudioPlayer = new GameObject("DuckAudioPlayer");
            var audioSource = duckAudioPlayer.AddComponent<AudioSource>();
            audioSource.clip = clip;
            //audioSource.pitch = 1.4f;
            audioSource.Play();

            while (audioSource.isPlaying) // Wait until playback is finished
            {
                canPlayAudio = false;
                yield return null;
            }

            DestroyAudio(duckAudioPlayer, filePath);

        }
        else
        {
            Debug.LogError("Failed to load audio: " + audioRequest.error);
        }
    }

    private static void DestroyAudio(GameObject audioSourceGameObject, string filePath) // Clean up resources
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(audioSourceGameObject);
            
        else UnityEngine.Object.DestroyImmediate(audioSourceGameObject);
           
        File.Delete(filePath);

        canPlayAudio = true;

        Debug.Log($"Finished playing duck audio and audio files are destroyed");
    }
}
