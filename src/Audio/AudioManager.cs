using System;
using System.Collections.Generic;
using Raylib_cs;

namespace DarkArmsProto.Audio
{
    public enum SoundType
    {
        Shoot,
        Hit,
        Kill,
        SoulCollect,
        WeaponEvolve,
        PlayerHit,
        Teleport,
        Explosion,
    }

    public class AudioManager
    {
        private static AudioManager? instance;
        private Dictionary<SoundType, Sound> sounds;
        private bool isInitialized;

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AudioManager();
                }
                return instance;
            }
        }

        private AudioManager()
        {
            sounds = new Dictionary<SoundType, Sound>();
            isInitialized = false;
        }

        public void Initialize()
        {
            if (isInitialized)
                return;

            Raylib.InitAudioDevice();
            isInitialized = true;

            // Génération de sons procéduraux (pas besoin de fichiers!)
            GenerateProceduralSounds();
        }

        private void GenerateProceduralSounds()
        {
            // Système audio prêt mais sans sons pour l'instant
            // Pour ajouter des sons plus tard, place des fichiers .wav/.ogg dans un dossier Sounds/
            // et charge-les avec: LoadSound(SoundType.Shoot, "Sounds/shoot.wav");
        }

        public void PlaySound(SoundType soundType, float volume = 1.0f)
        {
            if (!isInitialized)
                return;

            if (sounds.TryGetValue(soundType, out Sound sound))
            {
                Raylib.SetSoundVolume(sound, volume);
                Raylib.PlaySound(sound);
            }
        }

        public void LoadSound(SoundType soundType, string filePath)
        {
            if (!isInitialized)
                return;

            Sound sound = Raylib.LoadSound(filePath);
            if (sounds.ContainsKey(soundType))
            {
                Raylib.UnloadSound(sounds[soundType]);
            }
            sounds[soundType] = sound;
        }

        public void SetMasterVolume(float volume)
        {
            if (!isInitialized)
                return;

            Raylib.SetMasterVolume(volume);
        }

        public void Cleanup()
        {
            if (!isInitialized)
                return;

            foreach (var sound in sounds.Values)
            {
                Raylib.UnloadSound(sound);
            }
            sounds.Clear();

            Raylib.CloseAudioDevice();
            isInitialized = false;
        }
    }
}
