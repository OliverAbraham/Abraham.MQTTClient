using MQTTnet;
using MQTTnet.Client;

namespace Abraham.MQTTClient;
public class MQTTClient
{
    #region ------------- Types ---------------------------------------------------------------
    private class Subscription
    {
        public string? Topic { get; set; }
        public Action<string>? Callback { get; set; }
        public Func<MqttApplicationMessageReceivedEventArgs, Task>? Receiver { get; internal set; }
    }
    #endregion



    #region ------------- Fields --------------------------------------------------------------
    private string                       _url = "";
    private string                       _username = "";
    private string                       _password = "";
    private int                          _timeoutInSeconds = 60;
    private Action<string>               _logger = delegate(string message) {};
    private MqttFactory                  _mqttFactory = new MqttFactory();
    private MqttClientOptions?           _mqttClientOptions;
    private MqttClientDisconnectOptions? _mqttClientDisconnectOptions;
    private IMqttClient?                 _mqttClient;
    private List<Subscription>           _subscriptions = new();
    #endregion



    #region ------------- Init ----------------------------------------------------------------
    public MQTTClient()
    {
    }
    #endregion



    #region ------------- Methods -------------------------------------------------------------
    public MQTTClient UseUrl(string url)
	{
		_url = url ?? throw new ArgumentNullException(nameof(url));
		return this;
	}

    public MQTTClient UseUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username));
        _username = username;
		return this;
    }

    public MQTTClient UsePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentNullException(nameof(password));
        _password = password;
		return this;
    }

    public MQTTClient UseTimeout(int timeoutInSeconds)
    {
        if (timeoutInSeconds <= 0)
            throw new ArgumentNullException(nameof(timeoutInSeconds));
        _timeoutInSeconds = timeoutInSeconds;
		return this;
    }

    public MQTTClient UseLogger(Action<string> logger)
    {
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));
        _logger = logger;
		return this;
    }

    public MQTTClient Build()
    {
        if (string.IsNullOrWhiteSpace(_url))      throw new Exception($"The _url cannot be null. Call the UseUrl() method first.");
        if (string.IsNullOrWhiteSpace(_username)) throw new Exception($"The _username cannot be null. Call the UseUsername() method first.");
        if (string.IsNullOrWhiteSpace(_password)) throw new Exception($"The _password cannot be null. Call the UsePassword() method first.");

        _mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_url)
            .WithCredentials(_username, _password)
            .WithTimeout(TimeSpan.FromSeconds(_timeoutInSeconds))
            .Build();

        _mqttClientDisconnectOptions = _mqttFactory
            .CreateClientDisconnectOptionsBuilder()
            .Build();

        _mqttClient = _mqttFactory.CreateMqttClient();

        _logger($"Client initialized");
        return this;
    }

    public MqttClientPublishResult Publish(string topic, string value)
    {
        return PublishAsync(topic, value).GetAwaiter().GetResult();
    }

    public async Task<MqttClientPublishResult> PublishAsync(string topic, string value)
    {
        if (string.IsNullOrWhiteSpace(topic)) throw new Exception($"The topic cannot be null.");
        if (string.IsNullOrWhiteSpace(value)) throw new Exception($"The value cannot be null.");
        if (_mqttClient  is null)             throw new Exception($"The MQTTClient has not been built. Call the Build() method first.");

        var response = await _mqttClient.ConnectAsync(_mqttClientOptions, CancellationToken.None);
        if (response.ResultCode != MqttClientConnectResultCode.Success)
            throw new Exception($"Could not connect to MQTT broker. ResultCode={response.ResultCode}");
        _logger($"Client is connected. ResultCode={response.ResultCode}");
           

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(value)
            .Build();
        var result = await _mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

        _logger($"Published. ResultCode={result.ReasonCode}");

        await _mqttClient.DisconnectAsync(_mqttClientDisconnectOptions, CancellationToken.None);
        return result;
    }

    public MqttClientSubscribeResult Subscribe(string topic, Action<string> callback)
    {
        return SubscribeAsync(topic, callback).GetAwaiter().GetResult();
    }

    public async Task<MqttClientSubscribeResult> SubscribeAsync(string topic, Action<string> callback)
    {
        if (string.IsNullOrWhiteSpace(topic)) throw new Exception($"The topic cannot be null.");
        if (callback is null)                 throw new Exception($"The value cannot be null.");
        if (_mqttClient  is null)             throw new Exception($"The MQTTClient has not been built. Call the Build() method first.");

        var sub = new Subscription();
        sub.Topic = topic;
        sub.Callback = callback;

        var response = await _mqttClient.ConnectAsync(_mqttClientOptions, CancellationToken.None);
        if (response.ResultCode != MqttClientConnectResultCode.Success)
            throw new Exception($"Could not connect to MQTT broker. ResultCode={response.ResultCode}");
        _logger($"Client is connected. ResultCode={response.ResultCode}");

        sub.Receiver = delegate(MqttApplicationMessageReceivedEventArgs e)
            {
                _logger($"Received application message: {e}");
                sub.Callback(e.ApplicationMessage.ConvertPayloadToString());
                return Task.CompletedTask;
            };
        _mqttClient.ApplicationMessageReceivedAsync += sub.Receiver;

        var mqttSubscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(
                f =>
                {
                    f.WithTopic(topic);
                })
            .Build();

        var result = await _mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(result.ReasonString))
            throw new Exception($"Could not subscribe to topic {topic}. Reason={result.ReasonString}");

        _logger($"subscribed to topic {topic}");

        _subscriptions.Add(sub);

        return result;
    }

    public void StopAllSubscriptions()
    {
        if (_mqttClient  is null) throw new Exception($"The MQTTClient has not been built. Call the Build() method first.");

        foreach (var sub in _subscriptions)
        {
            _mqttClient.ApplicationMessageReceivedAsync -= sub.Receiver;
            _mqttClient.UnsubscribeAsync(sub.Topic);
        }

        _subscriptions.Clear();
    }
    #endregion



    #region ------------- Implementation ------------------------------------------------------
    #endregion
}
