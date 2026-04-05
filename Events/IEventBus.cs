using System;

namespace MonoGameEngine.Events;

public interface IEventBus
{
    void Subscribe<T>(object sender, Action<T> listener) where T : EventHandler;
    void Unsubscribe<T>(object sender) where T : EventHandler;
    void Publish<T>(T @event) where T : EventHandler;
}
