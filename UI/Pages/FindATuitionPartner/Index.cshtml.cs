using Application;
using Application.Extensions;
using Domain;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UI.Pages.FindATuitionPartner;

public partial class Index : PageModel
{
    private readonly IMediator _mediator;
    public Index(IMediator mediator) => _mediator = mediator;

    [BindProperty(SupportsGet = true)]
    public Command Data { get; set; } = new Command();

    public record Command : SearchModel, IRequest<Domain.IResult> { }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid) return Page();

        var validation = await _mediator.Send(Data);

        if (validation.IsSuccess)
        {
            return RedirectToPage(nameof(WhichKeyStages), Data);
        }

        if(validation is ErrorResult error)
            ModelState.AddModelError("Data.Postcode", error.ToString());

        return Page();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(m => m.Postcode)
                .NotEmpty()
                .WithMessage("Enter a postcode");

            RuleFor(m => m.Postcode)
                .Matches(@"[a-zA-Z]{1,2}([0-9]{1,2}|[0-9][a-zA-Z])\s*[0-9][a-zA-Z]{2}")
                .WithMessage("Enter a valid postcode");
        }
    }

    public class Handler : IRequestHandler<Command, Domain.IResult>
    {
        private readonly ILocationFilterService locationService;

        public Handler(ILocationFilterService location) => locationService = location;

        public async Task<Domain.IResult> Handle(Command request, CancellationToken cancellationToken)
        {
            var location = await locationService.GetLocationFilterParametersAsync(request.Postcode!);
            return location.TryValidate();
        }
    }
}