using Tracklio.Shared.Domain.Dto;

namespace Tracklio.Shared.Slices;

public static class ResponseMapper
{
    public static IResult ReturnedResponse<T>(this GenericResponse<T> response)
    {

        return response.StatusCode switch
        {
            200 => Results.Ok(response),
            400 => Results.BadRequest(response),
            404 => Results.NotFound(response),
            409 => Results.Conflict(response),
            _ => Results.Problem(response.Message),
        };
            
       
    }
}