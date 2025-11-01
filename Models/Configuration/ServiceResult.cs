namespace HISWEBAPI.Models
{
    public class ServiceResult<T>
    {
        public bool Result { get; set; }
        public T Data { get; set; }
        public string MessageType { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public object Errors { get; set; }

        public static ServiceResult<T> Success(T data, string messageType, string message, int statusCode = 200)
        {
            return new ServiceResult<T>
            {
                Result = true,
                Data = data,
                MessageType = messageType,
                Message = message,
                StatusCode = statusCode
            };
        }

        public static ServiceResult<T> Failure(string messageType, string message, int statusCode = 400, object errors = null)
        {
            return new ServiceResult<T>
            {
                Result = false,
                MessageType = messageType,
                Message = message,
                StatusCode = statusCode,
                Errors = errors
            };
        }
    }
}