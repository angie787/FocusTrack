using FocusTrack.Session.Application.DTOs;
using FocusTrack.Session.Domain.Models;
using FluentValidation;

namespace FocusTrack.Session.Application.Validators;

public class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    public CreateSessionRequestValidator()
    {
        RuleFor(x => x.Topic)
            .NotEmpty().WithMessage("Topic is required.")
            .MaximumLength(500).WithMessage("Topic must not exceed 500 characters.");

        RuleFor(x => x.StartTime)
            .NotEqual(default(DateTimeOffset)).WithMessage("StartTime is required.");

        RuleFor(x => x.Mode)
            .IsInEnum().WithMessage("Mode must be one of: Reading, Coding, VideoCourse, Practice.");
    }
}
