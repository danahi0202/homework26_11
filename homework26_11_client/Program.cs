using System.Net.Sockets;

var tcp = new TcpClient();
await tcp.ConnectAsync("127.0.0.1", 5000);
var reader = new StreamReader(tcp.GetStream());
var writer = new StreamWriter(tcp.GetStream()) { AutoFlush = true };

_ = Receive();

while (true)
{
    var text = Console.ReadLine();
    if (text == null) break;
    await writer.WriteLineAsync(text);
    if (text == "/quit") break;
}

async Task Receive()
{
    while (await reader.ReadLineAsync() is string line)
        Console.WriteLine(line);
    Console.WriteLine("Connection closed");
    Environment.Exit(0);
}