namespace GameServerApi.Models.Responses
{
    /// <summary>
    /// Unified response wrapper for all API endpoints.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool    Success   { get; set; }
        public T?      Data      { get; set; }
        public string? Error     { get; set; }
        public int     ErrorCode { get; set; }

        public static ApiResponse<T> Ok(T data) =>
            new() { Success = true, Data = data };

        public static ApiResponse<T> Fail(string error, int code = 400) =>
            new() { Success = false, Error = error, ErrorCode = code };
    }

    /// <summary>Non-generic convenience for void/empty responses.</summary>
    public class ApiResponse : ApiResponse<object?>
    {
        public static ApiResponse Ok(string? message = null) =>
            new() { Success = true, Data = message != null ? (object)message : null };

        public static new ApiResponse Fail(string error, int code = 400) =>
            new() { Success = false, Error = error, ErrorCode = code };
    }
}
