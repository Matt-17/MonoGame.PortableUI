using System;

namespace MonoGame.PortableUI.Input
{
    public interface IGameClock
    {
        TimeSpan TotalTime { get; }
    }
}
