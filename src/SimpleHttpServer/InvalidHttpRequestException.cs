namespace SimpleHttpServer;

public class InvalidHttpRequestException : Exception
{
    public InvalidHttpRequestException(string message) : base(message) { }
}