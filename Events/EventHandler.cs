using System;

namespace MonoGameEngine.Events;

public abstract class EventHandler
{
    public DateTime TimeStamp { get; private set; }
    public string Name { get; private set; }

    public EventHandler(string name = "Event")
    {
        Name = name;
        TimeStamp = DateTime.Now;
    }
}
