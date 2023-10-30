using Abraham.MQTTClient;

namespace SubscribeDemo;

internal class Program
{
    private static string _url      = "<YOUR MQTT BROKER URL WITHOUT PROTOCOL WITHOUT PORT>";
    private static string _username = "<YOUR MQTT BROKER USERNAME>";
    private static string _password = "<YOUR MQTT BROKER PASSWORD>";

    static void Main(string[] args)
    {
        Console.WriteLine("Connecting to MQTT broker and subscribing to a topic (listening to value changes)");

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

        Console.WriteLine("Now listening to topic 'garden/temperature'...  press any key to end the program.");

        client.Subscribe("garden/temperature",
            delegate(string topic, string value)
            {
                Console.WriteLine($"New event received: {topic}={value}");
            });

        Console.ReadKey();
        client.StopAllSubscriptions();
    }
}
