using System.Net;

namespace ConsoleApp1.Router;

public interface IBaseRouter
{
    Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response);

}