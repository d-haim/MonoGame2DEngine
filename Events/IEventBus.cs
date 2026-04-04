using System;

namespace MonoGameEngine.Events;

public interface IEventBus
{
    void Subscribe<T>(object sender, Action<T> listener) where T : Event;
    void Unsubscribe<T>(object sender) where T : Event;
    void Publish<T>(T @event) where T : Event;
}
