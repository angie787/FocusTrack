using FluentValidation;
using FocusTrack.Session.Application.DTOs;

namespace FocusTrack.Session.Application.Validators;

public class ShareSessionRequestValidator : AbstractValidator<ShareSessionRequest>
{
    public ShareSessionRequestValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one user ID is required.");
    }
}
