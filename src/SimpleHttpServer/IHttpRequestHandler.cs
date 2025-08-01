namespace SimpleHttpServer;

public interface IHttpRequestHandler
{
    Task<HttpResponse> Handle(HttpRequest request);
}