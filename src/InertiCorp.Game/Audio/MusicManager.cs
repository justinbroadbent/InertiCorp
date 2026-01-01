using Godot;

namespace InertiCorp.Game.Audio;

/// <summary>
/// Manages background music playback for the game.
/// Provides elevator/corporate lobby music that shuffles and loops.
/// </summary>
public partial class MusicManager : Node
{
    private static MusicManager? _instance;

    private AudioStreamPlayer? _player;
    private readonly List<string> _trackPaths = new();
    private readonly List<int> _shuffleOrder = new();
    private int _currentTrackIndex;
    private readonly Random _rng = new();

    private float _volume = 0.5f;
    private bool _muted;

    /// <summary>
    /// Gets the singleton instance. Returns null if not initialized.
    /// </summary>
    public static MusicManager? Instance => _instance;

    /// <summary>
    /// Whether music is currently muted.
    /// </summary>
    public bool IsMuted
    {
        get => _muted;
        set
        {
            _muted = value;
            ApplyVolume();
            SaveSettings();
        }
    }

    /// <summary>
    /// Music volume from 0.0 to 1.0.
    /// </summary>
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Mathf.Clamp(value, 0f, 1f);
            ApplyVolume();
            SaveSettings();
        }
    }

    /// <summary>
    /// Whether music is currently playing.
    /// </summary>
    public bool IsPlaying => _player?.Playing ?? false;

    public override void _Ready()
    {
        _instance = this;

        // Create audio player
        _player = new AudioStreamPlayer
        {
            Bus = "Master",
            ProcessMode = ProcessModeEnum.Always // Keep playing during pause
        };
        AddChild(_player);
        _player.Finished += OnTrackFinished;

        // Discover tracks
        DiscoverTracks();

        // Load settings
        LoadSettings();

        // Start playing if we have tracks
        if (_trackPaths.Count > 0)
        {
            ShufflePlaylist();
            PlayCurrentTrack();
        }
    }

    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void DiscoverTracks()
    {
        _trackPaths.Clear();

        // Known track list - more reliable than directory scanning which can fail
        // before Godot imports the files
        var knownTracks = new[]
        {
            "res://Audio/executive_loop.mp3",
            "res://Audio/executive_loop_2.mp3",
            "res://Audio/lobby_loop.mp3",
            "res://Audio/lobby_loop_2.mp3",
            "res://Audio/lobby_skyline.mp3",
            "res://Audio/lobby_skyline_2.mp3",
            "res://Audio/quarterly_sunrise.mp3",
            "res://Audio/quarterly_sunrise_2.mp3"
        };

        foreach (var trackPath in knownTracks)
        {
            // Verify file exists before adding
            if (ResourceLoader.Exists(trackPath))
            {
                _trackPaths.Add(trackPath);
                GD.Print($"[Music] Found track: {trackPath}");
            }
            else
            {
                GD.Print($"[Music] Track not yet imported: {trackPath}");
            }
        }

        // Fallback: try directory scan if known tracks not found
        if (_trackPaths.Count == 0)
        {
            GD.Print("[Music] No known tracks found, trying directory scan...");
            var audioDir = "res://Audio/";
            using var dir = DirAccess.Open(audioDir);

            if (dir != null)
            {
                dir.ListDirBegin();
                var fileName = dir.GetNext();

                while (!string.IsNullOrEmpty(fileName))
                {
                    if (!dir.CurrentIsDir() && fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                    {
                        _trackPaths.Add(audioDir + fileName);
                        GD.Print($"[Music] Found track via scan: {fileName}");
                    }
                    fileName = dir.GetNext();
                }
                dir.ListDirEnd();
            }
        }

        GD.Print($"[Music] Total tracks available: {_trackPaths.Count}");
    }

    private void ShufflePlaylist()
    {
        _shuffleOrder.Clear();
        for (int i = 0; i < _trackPaths.Count; i++)
        {
            _shuffleOrder.Add(i);
        }

        // Fisher-Yates shuffle
        for (int i = _shuffleOrder.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (_shuffleOrder[i], _shuffleOrder[j]) = (_shuffleOrder[j], _shuffleOrder[i]);
        }

        _currentTrackIndex = 0;
    }

    private void PlayCurrentTrack()
    {
        if (_player == null || _trackPaths.Count == 0) return;

        var trackIndex = _shuffleOrder[_currentTrackIndex];
        var trackPath = _trackPaths[trackIndex];

        var stream = GD.Load<AudioStream>(trackPath);
        if (stream == null)
        {
            GD.PrintErr($"[Music] Failed to load: {trackPath}");
            return;
        }

        _player.Stream = stream;
        ApplyVolume();
        _player.Play();
    }

    private void OnTrackFinished()
    {
        // Move to next track
        _currentTrackIndex++;

        // If we've played all tracks, reshuffle
        if (_currentTrackIndex >= _shuffleOrder.Count)
        {
            ShufflePlaylist();
        }

        PlayCurrentTrack();
    }

    private void ApplyVolume()
    {
        if (_player == null) return;

        if (_muted)
        {
            _player.VolumeDb = -80f; // Effectively silent
        }
        else
        {
            // Convert linear volume to dB (0.0 = -40dB, 1.0 = 0dB)
            _player.VolumeDb = Mathf.LinearToDb(_volume);
        }
    }

    /// <summary>
    /// Starts music playback if not already playing.
    /// </summary>
    public void Play()
    {
        if (_player == null || IsPlaying) return;

        if (_trackPaths.Count == 0)
        {
            DiscoverTracks();
            if (_trackPaths.Count == 0) return;
            ShufflePlaylist();
        }

        PlayCurrentTrack();
    }

    /// <summary>
    /// Stops music playback.
    /// </summary>
    public void Stop()
    {
        _player?.Stop();
    }

    /// <summary>
    /// Toggles mute state.
    /// </summary>
    public void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    private void LoadSettings()
    {
        var config = new ConfigFile();
        var error = config.Load("user://settings.cfg");

        if (error == Error.Ok)
        {
            _volume = (float)config.GetValue("audio", "music_volume", 0.5f);
            _muted = (bool)config.GetValue("audio", "music_muted", false);
            ApplyVolume();
        }
    }

    private void SaveSettings()
    {
        var config = new ConfigFile();
        config.Load("user://settings.cfg"); // Load existing to preserve other settings

        config.SetValue("audio", "music_volume", _volume);
        config.SetValue("audio", "music_muted", _muted);
        config.Save("user://settings.cfg");
    }
}
