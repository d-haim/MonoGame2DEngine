using System;

namespace MonoGameEngine.Events;

public abstract class Event
{
    public DateTime TimeStamp { get; private set; }
    public string Name { get; private set; }

    public Event(string name = "Event")
    {
        Name = name;
        TimeStamp = DateTime.Now;
    }
}
