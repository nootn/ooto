namespace OOTO.Core.Domain.Interface
{
    //Originally from https://github.com/andrewabest/EventSourcing101
    public interface IAggregateRoot : IIdentifiable, IHaveFacts
    {
    }
}