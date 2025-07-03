using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        const int port = 8080;
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Server is listening on http://localhost:{port}");

        while (true)
        {
            using var client = listener.AcceptTcpClient();
            using var stream = client.GetStream();

            // Đọc request
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string requestText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Request:");
            Console.WriteLine(requestText);

            // Chuẩn bị response
            string responseBody = "hello";
            string response =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/plain\r\n" +
                $"Content-Length: {responseBody.Length}\r\n" +
                "\r\n" +
                responseBody;

            // Gửi response
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            stream.Write(responseBytes, 0, responseBytes.Length);
        }
    }
}