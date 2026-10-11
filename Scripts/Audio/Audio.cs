using Godot;
using System.Collections.Generic;

public partial class Audio : Node
{
    public static Audio Instance { get; private set; }

    [Export] private int sfxVoices = 12;
    [Export] private float musicFadeTime = 1f;

    private readonly List<AudioStreamPlayer> sfxPool = new();
    private readonly RandomNumberGenerator rng = new();
    private AudioStreamPlayer musicA, musicB, currentMusic;

    public override void _Ready()
    {
        Instance = this;
        rng.Randomize();

        for (int i = 0; i < sfxVoices; i++)
        {
            var player = new AudioStreamPlayer { Bus = "SFX" };
            AddChild(player);
            sfxPool.Add(player);
        }

        musicA = new AudioStreamPlayer { Bus = "Music" };
        musicB = new AudioStreamPlayer { Bus = "Music" };
        AddChild(musicA);
        AddChild(musicB);
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

    // Crossfade to a new track; does nothing if that track is already playing
    public void PlayMusic(AudioStream stream)
    {
        if (stream == null) return;
        if (currentMusic != null && currentMusic.Stream == stream && currentMusic.Playing) return;

        AudioStreamPlayer next = currentMusic == musicA ? musicB : musicA;
        AudioStreamPlayer previous = currentMusic;

        next.Stream = stream;
        next.VolumeDb = -40f;
        next.Play();

        Tween tween = CreateTween().SetParallel();
        tween.TweenProperty(next, "volume_db", 0f, musicFadeTime);
        if (previous != null)
        {
            tween.TweenProperty(previous, "volume_db", -40f, musicFadeTime);
            tween.Chain().TweenCallback(Callable.From(previous.Stop));
        }

        currentMusic = next;
    }
}