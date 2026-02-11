namespace FocusTrack.Session.Application.DTOs;

public record SetUserStatusRequest(string Status); // "Active" | "Suspended" | "Deactivated"
