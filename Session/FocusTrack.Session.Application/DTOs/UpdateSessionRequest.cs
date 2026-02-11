using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusTrack.Session.Application.DTOs
{
    public record UpdateSessionRequest(
        string Topic,
        DateTimeOffset? EndTime,
        FocusTrack.Session.Domain.Models.SessionMode Mode
    );
}
