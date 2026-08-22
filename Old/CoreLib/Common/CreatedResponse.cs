namespace CoreLib.Common;

public class CreatedResponse<T>
{
    public CreatedResponse(bool _isSuccess, string _message, T created)
    {
        isSuccess = _isSuccess;
        Message = _message;
        Data = created;
    }

    public CreatedResponse(bool _isSuccess, string _message)
    {
        isSuccess = _isSuccess;
        Message = _message;
    }

    public CreatedResponse()
    {
    }

    public bool isSuccess { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}