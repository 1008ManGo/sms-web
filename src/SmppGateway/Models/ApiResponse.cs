namespace SmppGateway.Models;

public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data, string message = "success")
    {
        return new ApiResponse<T> { Code = 0, Message = message, Data = data };
    }

    public static ApiResponse<T> Fail(int code, string message)
    {
        return new ApiResponse<T> { Code = code, Message = message };
    }
}

public class ApiResponse
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ApiResponse Success(string message = "success")
    {
        return new ApiResponse { Code = 0, Message = message };
    }

    public static ApiResponse Fail(int code, string message)
    {
        return new ApiResponse { Code = code, Message = message };
    }
}
