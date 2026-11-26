using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

var clients = new ConcurrentDictionary<string, StreamWriter>();
var sendLock = new SemaphoreSlim(1);

var listener = new TcpListener(IPAddress.Any, 5000);
listener.Start();
Console.WriteLine("Server started");

while (true)
{
    var tcp = await listener.AcceptTcpClientAsync();
    _ = Serve(tcp);
}

async Task Serve(TcpClient tcp)
{
    var reader = new StreamReader(tcp.GetStream());
    var writer = new StreamWriter(tcp.GetStream()) { AutoFlush = true };
    string? name = null;
    try
    {
        await Send(writer, "Enter your name:");
        var input = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(input) || !clients.TryAdd(input, writer))
        {
            await Send(writer, "Name is empty or already taken");
            return;
        }
        name = input;
        await SendAll($"{name} joined the chat");

        while (await reader.ReadLineAsync() is string text)
        {
            if (text == "/quit") break;

            if (text.StartsWith("/w "))
            {
                var parts = text.Split(' ', 3);
                if (parts.Length == 3 && clients.TryGetValue(parts[1], out var target))
                    await Send(target, $"[private from {name}] {parts[2]}");
                else
                    await Send(writer, "No such user");
            }
            else
                await SendAll($"{name}: {text}");
        }
    }
    catch { }
    finally
    {
        if (name != null)
        {
            clients.TryRemove(name, out _);
            await SendAll($"{name} left the chat");
        }
        tcp.Close();
    }
}

async Task SendAll(string text)
{
    foreach (var w in clients.Values)
        await Send(w, text);
}

async Task Send(StreamWriter w, string text)
{
    await sendLock.WaitAsync();
    try { await w.WriteLineAsync(text); }
    catch { }
    finally { sendLock.Release(); }
}