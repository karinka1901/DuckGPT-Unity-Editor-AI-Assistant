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
    public static void PlayDuckAudio(string filePath)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(PlayAudioCoroutine(filePath));
    }

    private static IEnumerator  PlayAudioCoroutine(string filePath)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                // Temporary GameObject setup to play audio
                var duckAudioPlayer = new GameObject("DuckAudioPlayer"); 
                var audioSource = duckAudioPlayer.AddComponent<AudioSource>();
                audioSource.clip = clip;
                audioSource.Play();

                while (audioSource.isPlaying) // Wait until playback is finished
                {
                    yield return null;
                }

                DestroyAudio(duckAudioPlayer, filePath);

            }
            else
            {
                Debug.LogError("Failed to load audio: " + www.error);
            }
        }
    }

    private static void DestroyAudio(GameObject audioSourceGameObject, string filePath) // Clean up resources
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(audioSourceGameObject);
            
        else UnityEngine.Object.DestroyImmediate(audioSourceGameObject);
           
        File.Delete(filePath);

        Debug.Log($"Finished playing duck audio and audio files are destroyed");
    }
}
