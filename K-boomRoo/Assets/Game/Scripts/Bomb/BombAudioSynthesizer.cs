using UnityEngine;

namespace DefusalGame.Bomb
{
    [RequireComponent(typeof(AudioSource))]
    public class BombAudioSynthesizer : MonoBehaviour
    {
        private AudioSource audioSource;

        private AudioClip tickClip;
        private AudioClip urgentTickClip;
        private AudioClip buttonClickClip;
        private AudioClip errorBuzzClip;
        private AudioClip successJingleClip;
        private AudioClip explosionClip;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D Spatial Audio in VR
            audioSource.minDistance = 0.5f;
            audioSource.maxDistance = 15.0f;

            GenerateClips();
        }

        private void GenerateClips()
        {
            tickClip = CreateBeep(1200f, 0.05f);
            urgentTickClip = CreateBeep(1800f, 0.06f);
            buttonClickClip = CreateClick(0.04f);
            errorBuzzClip = CreateBuzz(140f, 0.45f);
            successJingleClip = CreateJingle();
            explosionClip = CreateExplosion(1.8f);
        }

        public void PlayTick(bool isUrgent)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(isUrgent ? urgentTickClip : tickClip, isUrgent ? 0.9f : 0.6f);
            }
        }

        public void PlayButton()
        {
            if (audioSource != null && buttonClickClip != null)
            {
                audioSource.PlayOneShot(buttonClickClip, 0.7f);
            }
        }

        public void PlayError()
        {
            if (audioSource != null && errorBuzzClip != null)
            {
                audioSource.PlayOneShot(errorBuzzClip, 0.95f);
            }
        }

        public void PlaySuccess()
        {
            if (audioSource != null && successJingleClip != null)
            {
                audioSource.PlayOneShot(successJingleClip, 1.0f);
            }
        }

        public void PlayExplosion()
        {
            if (audioSource != null && explosionClip != null)
            {
                audioSource.spatialBlend = 0.0f; // 2D para que resuene fuerte sin importar la distancia
                audioSource.PlayOneShot(explosionClip, 1.0f);
            }
        }

        private AudioClip CreateBeep(float frequency, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - (t / duration));
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("Beep", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateClick(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 80f);
                float noise = (Random.value * 2f - 1f) * 0.4f;
                float tone = Mathf.Sin(2f * Mathf.PI * 800f * t) * 0.6f;
                samples[i] = (tone + noise) * env;
            }

            AudioClip clip = AudioClip.Create("Click", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateBuzz(float frequency, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Sawtooth / square wave for buzzer
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t));
                float env = (i < sampleCount * 0.1f) ? (i / (sampleCount * 0.1f)) : (1f - (float)i / sampleCount);
                samples[i] = wave * env * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Buzz", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateJingle()
        {
            // Acorde mayor arpegiado (C5, E5, G5, C6)
            int sampleRate = 44100;
            float totalDuration = 0.9f;
            int sampleCount = Mathf.CeilToInt(sampleRate * totalDuration);
            float[] samples = new float[sampleCount];

            float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDur = totalDuration / notes.Length;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t / noteDur), 0, notes.Length - 1);
                float noteT = t - (noteIndex * noteDur);
                float freq = notes[noteIndex];
                float env = Mathf.Clamp01(1f - (noteT / noteDur));
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Jingle", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateExplosion(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 3.5f);
                float noise = (Random.value * 2f - 1f);
                float lowRumble = Mathf.Sin(2f * Mathf.PI * (50f - t * 15f) * t) * 0.8f;
                samples[i] = (noise * 0.7f + lowRumble) * env;
            }

            AudioClip clip = AudioClip.Create("Explosion", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
