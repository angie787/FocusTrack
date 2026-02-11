using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusTrack.Session.Application.DTOs
{
    public record CreateSessionRequest(
    string Topic,
    DateTimeOffset StartTime,
    FocusTrack.Session.Domain.Models.SessionMode Mode
);
}
