namespace Tracklio.Shared.Domain.Dto;

public class GenericResponse<T>
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = "";
    public T? Data { get; set; }


    public static GenericResponse<T> Success(string message, T data)
    {
        return new GenericResponse<T>
        {
            StatusCode = 200,
            Message = message,
            Data = data
        };
    }

    public static GenericResponse<T> Error(int statusCode, string message, T data = default!)
    {
        return new GenericResponse<T>
        {
            StatusCode = statusCode,
            Message = message,
            Data = data

        };
    }
    
    
}
