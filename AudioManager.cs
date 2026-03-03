using System.Collections.Generic;
using ExtensionMethods;
using UnityEngine;
using UnityEngine.Audio;


public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField]
    Sound[] m_sounds;
    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one AudioManager in scene");
        }
        else
        {
            instance = this;
        }

        for (int i = 0; i < m_sounds.Length; i++)
        {
            GameObject go = new GameObject("Sound_" + i + "_" + m_sounds[i].m_name.ToString());
            go.transform.SetParent(transform);
            AudioSource source = go.AddComponent<AudioSource>();
            m_sounds[i].SetSource(source);
        }


    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            for (int i = 0; i < m_sounds.Length; i++)
            {
                m_sounds[i].UpdateValues();
            }
        }
    }
#endif

    public void StopSound(SoundName name)
    {
        Sound requestedSound = GetSound(name);
        if (requestedSound == null)
        {
            Debug.LogWarning("AudioManager: Sound name not found on list: " + name.ToString());
        }
        else
        {
            requestedSound.Stop();
        }
    }

    public void PlaySound(SoundName name, bool oneShot = false)
    {
        Sound requestedSound = GetSound(name);
        if (requestedSound == null)
        {
            Debug.LogWarning("AudioManager: Sound name not found on list: " + name.ToString());
        }
        else
        {
            requestedSound.Play(oneShot);
        }
    }

    public void PlaySoundAtPosition(SoundName name, Vector3 position, bool oneShot = false)
    {
        Sound requestedSound = GetSound(name);
        if (requestedSound == null)
        {
            Debug.LogWarning("AudioManager: Sound name not found on list: " + name.ToString());
        }
        else
        {
            //print("Playing " + name.ToString());
            requestedSound.PlayAtPosition(position, oneShot);
        }
    }

    private Sound GetSound(SoundName name)
    {
        for (int i = 0; i < m_sounds.Length; i++)
        {
            if (m_sounds[i].m_name == name)
            {
                return m_sounds[i];
            }
        }

        return null;
    }
}


[System.Serializable]
public class Sound
{
    public SoundName m_name;
    public AudioClip[] m_clips;
    public AudioMixerGroup output;
    [Range(0f, 1f)]
    public float volume = 1.0f;
    [Range(0f, 3f)]
    public float pitch = 1.0f;
    public bool loop;
    public Vector2 m_randomVolumeRange = new Vector2(1.0f, 1.0f);
    public Vector2 m_randomPitchRange = new Vector2(1.0f, 1.0f);
    public ClipSelectionOption clipSelectionOption = ClipSelectionOption.Random;

    private AudioSource m_source;

    int selectedClipIndex = 0;
    List<AudioClip> clipsLeft;

    public void SetSource(AudioSource source)
    {
        m_source = source;
        m_source.loop = loop;
        int randomClip = Random.Range(0, m_clips.Length - 1);
        m_source.clip = m_clips[randomClip];
        m_source.outputAudioMixerGroup = output;
    }

    public void UpdateValues()
    {
        if (m_source != null)
        {
            m_source.loop = loop;
            int randomClip = Random.Range(0, m_clips.Length);
            m_source.clip = m_clips[randomClip];
            m_source.outputAudioMixerGroup = output;
            m_source.volume = volume * Random.Range(m_randomVolumeRange.x, m_randomVolumeRange.y);
            m_source.pitch = pitch * Random.Range(m_randomPitchRange.x, m_randomPitchRange.y);
        }
    }

    public void Play(bool oneShot)
    {
        if (m_clips.Length > 1)
        {
            int randomClip = Random.Range(0, m_clips.Length);
            m_source.clip = m_clips[randomClip];
        }
        m_source.volume = volume * Random.Range(m_randomVolumeRange.x, m_randomVolumeRange.y);
        m_source.pitch = pitch * Random.Range(m_randomPitchRange.x, m_randomPitchRange.y);
        m_source.spatialBlend = 0;
        if (!oneShot) m_source.Play();
        else m_source.PlayOneShot(m_source.clip);
    }

    public void Stop()
    {
        m_source.Stop();
    }

    public void PlayAtPosition(Vector3 position, bool oneShot)
    {
        if (m_clips.Length > 1)
        {
            int randomClip = Random.Range(0, m_clips.Length);
            m_source.clip = m_clips[randomClip];
        }
        m_source.volume = volume * Random.Range(m_randomVolumeRange.x, m_randomVolumeRange.y);
        m_source.pitch = pitch * Random.Range(m_randomPitchRange.x, m_randomPitchRange.y);
        m_source.transform.position = position;
        m_source.spatialBlend = 1;
        if (!oneShot) m_source.Play();
        else m_source.PlayOneShot(m_source.clip);
    }

    private AudioClip GetClip()
    {
        switch (clipSelectionOption)
        {
            case ClipSelectionOption.Random:
                selectedClipIndex = Random.Range(0, m_clips.Length);
                return m_clips[selectedClipIndex];
            case ClipSelectionOption.RandomCycle:
                if (clipsLeft == null || clipsLeft.Count == 0)
                {
                    clipsLeft = new List<AudioClip>(m_clips);
                }
                AudioClip randomClip = ArrayExtensions.GetRandom<AudioClip>(clipsLeft.ToArray());
                clipsLeft.Remove(randomClip);
                return randomClip;
            case ClipSelectionOption.Cycle:
                selectedClipIndex = (selectedClipIndex + 1) % m_clips.Length;
                return m_clips[selectedClipIndex];
            default:
                return null;
        }
    }

}

public enum ClipSelectionOption
{
    Random,
    RandomCycle,
    Cycle
}

public enum SoundName
{
}