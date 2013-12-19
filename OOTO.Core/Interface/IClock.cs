using System;

namespace OOTO.Core.Interface
{
    //Originally from https://github.com/andrewabest/EventSourcing101
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}