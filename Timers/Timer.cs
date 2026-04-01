using System;
using Microsoft.Xna.Framework;

namespace MonoGameEngine.Timers;

public sealed class Timer : GameComponent, IDisposable
{
    public float Time { get; private set; }
    public bool IsActive { get; private set; }
    public float Interval { get; set; }
    public bool Repeating { get; set; }
    public bool Elapsed => Time >= Interval;

    public event Action OnTick;
    public event Action OnElapsed;

    public Timer(float interval) : base(GameEngine.Instance)
    {
        Time = 0f;
        Interval = interval;
        GameEngine.Instance.Components.Add(this);
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsActive)
            return;

        Time += (float)gameTime.ElapsedGameTime.TotalSeconds;
        OnTick?.Invoke();
        if (Time >= Interval)
        {
            Time = Interval;
            OnElapsed?.Invoke();
            if (Repeating == false)
            {
                Stop();
            }
            Reset();
        }
    }

    public void Start()
    {
        IsActive = true;
    }

    public void Stop()
    {
        IsActive = false;
        Time = 0;
    }

    public void Reset()
    {
        Time = 0;
    }

    public void Pause()
    {
        IsActive = false;
    }

    protected override void Dispose(bool disposing)
    {
        GameEngine.Instance.Components.Remove(this);
        base.Dispose(disposing);
    }
}