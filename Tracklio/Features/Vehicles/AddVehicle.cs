// using FluentValidation;
// using MediatR;
// using Tracklio.Shared.Domain.Dto;
// using Tracklio.Shared.Persistence;
// using Tracklio.Shared.Slices;
//
// namespace Tracklio.Features.Vehicles;
//
// public class AddVehicle : ISlice
// {
//     public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
//     {
//         throw new NotImplementedException();
//     }
//
//     public class AddVehicleCommand : IRequest<GenericResponse<string>>
//     {
//         public string VRM { get; set; } = string.Empty;
//         
//         public string? Make { get; set; }
//         
//         public string? Model { get; set; }
//         
//         public string? Color { get; set; }
//         
//         public int? Year { get; set; }
//     }
//
//     public class AddVehicleCommandValidator : AbstractValidator<AddVehicleCommand>
//     {
//         public AddVehicleCommandValidator()
//         {
//             RuleFor(x => x.VRM)
//                 .NotEmpty()
//                 .WithMessage("Vehicle registration number (VRM) is required")
//                 .Length(2, 10)
//                 .WithMessage("VRM must be between 2 and 10 characters")
//                 .Matches(@"^[A-Z0-9\s]+$")
//                 .WithMessage("VRM can only contain letters, numbers and spaces")
//                 .Must(BeValidUKVRM)
//                 .WithMessage("Invalid UK vehicle registration format");
//
//             RuleFor(x => x.Make)
//                 .MaximumLength(50)
//                 .WithMessage("Make must not exceed 50 characters")
//                 .Matches(@"^[a-zA-Z0-9\s\-&]+$")
//                 .WithMessage("Make can only contain letters, numbers, spaces, hyphens and ampersands")
//                 .When(x => !string.IsNullOrEmpty(x.Make));
//
//             RuleFor(x => x.Model)
//                 .MaximumLength(50)
//                 .WithMessage("Model must not exceed 50 characters")
//                 .Matches(@"^[a-zA-Z0-9\s\-&().]+$")
//                 .WithMessage("Model can only contain letters, numbers, spaces, hyphens, ampersands and brackets")
//                 .When(x => !string.IsNullOrEmpty(x.Model));
//
//             RuleFor(x => x.Color)
//                 .MaximumLength(30)
//                 .WithMessage("Color must not exceed 30 characters")
//                 .Matches(@"^[a-zA-Z\s\-]+$")
//                 .WithMessage("Color can only contain letters, spaces and hyphens")
//                 .When(x => !string.IsNullOrEmpty(x.Color));
//
//             RuleFor(x => x.Year)
//                 .GreaterThanOrEqualTo(1900)
//                 .WithMessage("Year must be 1900 or later")
//                 .LessThanOrEqualTo(DateTime.UtcNow.Year + 1)
//                 .WithMessage($"Year must not exceed {DateTime.UtcNow.Year + 1}")
//                 .When(x => x.Year.HasValue);
//         }
//         
//         private bool BeValidUKVRM(string vrm)
//         {
//             if (string.IsNullOrEmpty(vrm))
//                 return false;
//             
//             vrm = vrm.Replace(" ", "").ToUpperInvariant();
//             
//             var patterns = new[]
//             {
//                 @"^[A-Z]{2}[0-9]{2}[A-Z]{3}$",     
//                 @"^[A-Z][0-9]{1,3}[A-Z]{3}$",      
//                 @"^[A-Z]{3}[0-9]{1,3}[A-Z]$",      
//                 @"^[0-9]{1,4}[A-Z]{1,3}$",         
//                 @"^[A-Z]{1,3}[0-9]{1,4}$"  
//             };
//
//             return patterns.Any(pattern => System.Text.RegularExpressions.Regex.IsMatch(vrm, pattern));
//         }
//     }
//
//     public class AddVehicleCommandHandler(RepositoryContext context) : IRequestHandler<AddVehicleCommand, GenericResponse<string>>
//     {
//         public Task<GenericResponse<string>> Handle(AddVehicleCommand request, CancellationToken cancellationToken)
//         {
//             throw new NotImplementedException();
//         }
//     }
// }