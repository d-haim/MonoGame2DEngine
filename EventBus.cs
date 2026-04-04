using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using MonoGameEngine.Events;

namespace MonoGameEngine;

public class EventBus : IEventBus
{
    private Dictionary<Type, List<(object sender, Action<object> action)>> _listeners;

    public EventBus()
    {
        _listeners = [];
    }

    public void Publish<T>(T @event) where T : Event
    {
        if (!_listeners.ContainsKey(@event.GetType()))
        {
            return;
        }

        foreach (var (sender, action) in _listeners[@event.GetType()])
        {
            action?.Invoke(@event);
        }
    }

    public void Subscribe<T>(object sender, Action<T> listener) where T : Event
    {
        var type = typeof(T);
        if (!_listeners.ContainsKey(type))
        {
            _listeners[type] = new();
        }
        _listeners[type].Add((sender, (o) => listener(o as T)));
    }

    public void Unsubscribe<T>(object sender) where T : Event
    {
        var type = typeof(T);
        if (!_listeners.ContainsKey(type))
        {
            return;
        }
        var listener = _listeners[type].FirstOrDefault(l => l.sender == sender);
        if (listener != default)
        {
            _listeners[type].Remove(listener);
        }
    }
}