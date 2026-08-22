namespace Web.Components.ForgetPassword;

public class ValidUserResponse
{
    public bool Valid { get; set; }
    public string Message { get; set; }
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}