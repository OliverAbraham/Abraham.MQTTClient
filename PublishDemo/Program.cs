using Abraham.MQTTClient;

namespace PublishDemo;

internal class Program
{
    private static string _url      = "<YOUR MQTT BROKER URL WITHOUT PROTOCOL WITHOUT PORT>";
    private static string _username = "<YOUR MQTT BROKER USERNAME>";
    private static string _password = "<YOUR MQTT BROKER PASSWORD>";

    static void Main(string[] args)
    {
        Console.WriteLine("Connecting to MQTT broker and publishing (sending) a value to it.");

        #if DEBUG
        _url      = File.ReadAllText(@"C:\Credentials\mosquitto_url.txt");
        _username = File.ReadAllText(@"C:\Credentials\mosquitto_username.txt");
        _password = File.ReadAllText(@"C:\Credentials\mosquitto_password.txt");
        #endif

        var client = new MQTTClient()
            .UseUrl(_url)
            .UseUsername(_username)
            .UsePassword(_password)
            .Build();

        var result = client.Publish("garden/temperature", "19.1");

        Console.WriteLine($"Value '\"19.1\" sent to the MQTT broker. Result={result.ReasonCode} {result.ReasonString}");
    }
}
