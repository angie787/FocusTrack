namespace FocusTrack.Session.Application.DTOs;

public record ShareSessionRequest(IReadOnlyList<string> UserIds);
