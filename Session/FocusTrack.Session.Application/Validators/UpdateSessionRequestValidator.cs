using FocusTrack.Session.Application.DTOs;
using FluentValidation;

namespace FocusTrack.Session.Application.Validators;

public class UpdateSessionRequestValidator : AbstractValidator<UpdateSessionRequest>
{
    public UpdateSessionRequestValidator()
    {
        RuleFor(x => x.Topic)
            .NotEmpty().WithMessage("Topic is required.")
            .MaximumLength(500).WithMessage("Topic must not exceed 500 characters.");

        RuleFor(x => x.Mode)
            .IsInEnum().WithMessage("Mode must be one of: Reading, Coding, VideoCourse, Practice.");
    }
}
