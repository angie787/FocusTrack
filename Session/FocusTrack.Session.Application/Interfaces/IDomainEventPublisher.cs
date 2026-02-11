using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusTrack.Session.Application.Interfaces
{
    public interface IDomainEventPublisher
    {
        Task PublishAsync<T>(T domainEvent) where T : class;
    }
}
