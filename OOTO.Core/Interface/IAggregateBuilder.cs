using OOTO.Core.Domain.Interface;

namespace OOTO.Core.Interface
{
    //Originally from https://github.com/andrewabest/EventSourcing101
    public interface IAggregateBuilder
    {
        void Apply<T>(T aggregateRoot, IFact fact) where T : IAggregateRoot;
    }
}