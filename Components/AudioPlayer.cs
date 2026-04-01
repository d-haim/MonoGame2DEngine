using Microsoft.Xna.Framework.Audio;

namespace MonoGameEngine.Components;

public sealed class AudioPlayer : Component
{
    private SoundEffect _soundEffect = null;
    public SoundEffect SoundEffect
    {
        get
        {
            return _soundEffect;
        }
        set
        {
            _soundEffect = value;
            if (_soundEffect != null && _autoPlay)
            {
                Play();
            }
        }
    }

    private SoundEffectInstance _instance;
    private bool _autoPlay { get; set; } = true;
    private bool IsPlaying => _instance != null && _instance.State == SoundState.Playing;

    public AudioPlayer(SoundEffect soundEffect = null, bool autoPlay = true)
    {
        _autoPlay = autoPlay;
        SoundEffect = soundEffect;
    }

    public void Play()
    {
        if (IsPlaying)
        {
            _instance.Stop();
        }

        if (SoundEffect != null)
        {
            _instance = GameEngine.Audio.PlaySoundEffect(SoundEffect);
        }
    }
}
