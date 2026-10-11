using Godot;
using System.Collections.Generic;

public partial class Audio : Node
{
    public static Audio Instance { get; private set; }

    // The track that plays for the whole game (right-click the file in FileSystem → Copy Path)
    private const string BackgroundMusicPath = "res://AudioFX/Music/one.mp3";
    private const float MusicVolumeDb = -6f;
    private const int SfxVoices = 12;

    private readonly List<AudioStreamPlayer> sfxPool = new();
    private readonly RandomNumberGenerator rng = new();
    private AudioStreamPlayer music;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always; // keeps playing if the game is ever paused
        rng.Randomize();

        // Sound effect players, reused for overlapping one-shot sounds
        for (int i = 0; i < SfxVoices; i++)
        {
            var player = new AudioStreamPlayer { Bus = "SFX" };
            AddChild(player);
            sfxPool.Add(player);
        }

        StartBackgroundMusic();
    }

    private void StartBackgroundMusic()
    {
        if (!ResourceLoader.Exists(BackgroundMusicPath))
        {
            GD.PushWarning($"Audio: background music not found at {BackgroundMusicPath}");
            return;
        }

        music = new AudioStreamPlayer
        {
            Stream = GD.Load<AudioStream>(BackgroundMusicPath),
            Bus = "Music",
            VolumeDb = MusicVolumeDb,
        };
        AddChild(music);
        music.Play();
    }

    // One-shot sound with a slight random pitch so repeated sounds don't feel robotic
    public void PlaySfx(AudioStream stream, float volumeDb = 0f, float pitchVariance = 0.08f, string bus = "SFX")
    {
        if (stream == null) return;

        AudioStreamPlayer player = sfxPool.Find(p => !p.Playing) ?? sfxPool[0]; // reuse one if all are busy
        player.Stream = stream;
        player.Bus = bus;
        player.VolumeDb = volumeDb;
        player.PitchScale = 1f + rng.RandfRange(-pitchVariance, pitchVariance);
        player.Play();
    }
}