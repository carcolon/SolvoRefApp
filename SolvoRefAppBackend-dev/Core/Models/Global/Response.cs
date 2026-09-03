using System.Net;

namespace Core.Models.Global
{
    public class Response<T>
    {
        public bool Success { get; set; }
        public List<string>? Errors { get; set; } = [];
        public T? Data { get; set; } = default;
        public HttpStatusCode StatusCode { get; set; }

        public static Response<T> SuccessResponse(T data, HttpStatusCode statusCode)
        {
            return new Response<T>
            {
                Success = true,
                Data = data,
                StatusCode = statusCode
            };
        }
        public static Response<T> ErrorResponse(List<string> errors, HttpStatusCode statusCode)
        {
            return new Response<T>
            {
                Success = false,
                Errors = errors,
                StatusCode = statusCode
            };
        }
    }
}
