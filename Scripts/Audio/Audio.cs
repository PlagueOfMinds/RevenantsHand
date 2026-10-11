using Godot;
using System.Collections.Generic;

public partial class Audio : Node
{
    public static Audio Instance { get; private set; }

    // ---- Background music (plays for the whole game) ----
    private const string BackgroundMusicPath = "res://AudioFX/Music/one.mp3";
    private const float MusicVolumeDb = -6f;

    // ---- Button hover sound (added to every button automatically) ----
    private const string UIHoverSoundPath = "res://AudioFX/UIElements/basicUISound.mp3";
    private const string NoSoundGroup = "no_ui_sound"; // add a button to this group to keep it silent
    private const ulong UIHoverCooldownMs = 80;        // stops a fast swipe across buttons from stacking sounds
    private const float UIHoverVolumeDb = -4f;

    private const int SfxVoices = 12;

    private readonly List<AudioStreamPlayer> sfxPool = new();
    private readonly RandomNumberGenerator rng = new();
    private AudioStreamPlayer music;
    private AudioStream uiHoverSound;
    private ulong lastUIHoverMs;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always; // keeps playing if the game is ever paused
        rng.Randomize();

        for (int i = 0; i < SfxVoices; i++)
        {
            var player = new AudioStreamPlayer { Bus = "SFX" };
            AddChild(player);
            sfxPool.Add(player);
        }

        StartBackgroundMusic();

        // Hook up every button that enters the game from now on, in any scene
        if (ResourceLoader.Exists(UIHoverSoundPath))
            uiHoverSound = GD.Load<AudioStream>(UIHoverSoundPath);
        else
            GD.PushWarning($"Audio: UI hover sound not found at {UIHoverSoundPath}");

        GetTree().NodeAdded += OnNodeAdded;
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

    // Every node added to the game passes through here; buttons get the hover sound
    private void OnNodeAdded(Node node)
    {
        if (node is not BaseButton button) return;
        if (button.IsInGroup(NoSoundGroup) || button.HasMeta("ui_hover_sound")) return;

        button.SetMeta("ui_hover_sound", true); // never hook the same button twice
        button.MouseEntered += () =>
        {
            if (!button.Disabled) PlayUIHover();
        };
    }

    // The shared UI hover sound; other scripts can call this too
    public void PlayUIHover()
    {
        ulong now = Time.GetTicksMsec();
        if (now - lastUIHoverMs < UIHoverCooldownMs) return;
        lastUIHoverMs = now;

        PlaySfx(uiHoverSound, volumeDb: UIHoverVolumeDb, pitchVariance: 0.30f, bus: "UI");
    }

    // One-shot sound with a slight random pitch so repeated sounds don't feel robotic
    public void PlaySfx(AudioStream stream, float volumeDb = 0f, float pitchVariance = 0.08f,
                        string bus = "SFX", float pitch = 1f)
    {
        if (stream == null) return;

        AudioStreamPlayer player = sfxPool.Find(p => !p.Playing) ?? sfxPool[0]; // reuse one if all are busy
        player.Stream = stream;
        player.Bus = bus;
        player.VolumeDb = volumeDb;
        player.PitchScale = pitch * (1f + rng.RandfRange(-pitchVariance, pitchVariance));
        player.Play();
    }
}