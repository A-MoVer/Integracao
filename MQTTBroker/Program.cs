using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;

namespace MQTTBroker
{
    internal class Program
    {
        private static int currentSpeed = 0;;
        private static int currentBattery = 100;
        private static double totalKilometers = 0.0;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Iniciando o broker MQTT...");

            var mqttFactory = new MqttFactory();

            var mqttServerOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(1884)
                .Build();

            var mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);

            mqttServer.ValidatingConnectionAsync += e =>
            {
                Console.WriteLine($"Cliente conectado: {e.ClientId}");
                return Task.CompletedTask;
            };

            mqttServer.ClientDisconnectedAsync += async e =>
            {
                Console.WriteLine($"Cliente desconectado: {e.ClientId}");
            };

            mqttServer.InterceptingPublishAsync += async e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                if (topic.StartsWith("sim/"))
                {
                    if (topic == "sim/canmessages")
                    {
                        try
                        {
                            var mqttPayload = JsonSerializer.Deserialize<CanMessageSimulator>(payload);
                            if (mqttPayload != null)
                            {
                                Console.WriteLine($"Arbitration ID: {mqttPayload.CAN_Message.ArbitrationId}");
                                Console.WriteLine($"Data Bytes: {string.Join(", ", mqttPayload.CAN_Message.Data)}");

                                var arbitrationToTopic = new System.Collections.Generic.Dictionary<int, string>
                                {
                                    { 0x100, "simsensor/blindspot" },
                                    { 0x101, "simsensor/pedestrian" },
                                    { 0x102, "simsensor/frontalcollision" },
                                    { 0x103, "simsensor/rearcollision" }
                                };

                                var jsonMessage = CanToJsonSim(mqttPayload);
                                var jsonPayload = JsonSerializer.Serialize(jsonMessage);

                                if (arbitrationToTopic.TryGetValue(mqttPayload.CAN_Message.ArbitrationId, out var targetTopic))
                                {
                                    var frontendMessage = new MqttApplicationMessageBuilder()
                                        .WithTopic(targetTopic)
                                        .WithPayload(jsonPayload)
                                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                                        .WithRetainFlag()
                                        .Build();

                                    await mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(frontendMessage));

                                    Console.WriteLine($"Mensagem redirecionada para o tópico {targetTopic}");
                                }
                                else
                                {
                                    Console.WriteLine("ArbitrationId não mapeado para nenhum tópico específico.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erro ao processar mensagem: {ex.Message}");
                        }
                    }else{
                        Console.WriteLine($"Mensagem recebida do tópico {topic}: {payload}");
                        var newTopic = "moto/" + topic.Substring(4);

                        var forwardMessage = new MqttApplicationMessageBuilder()
                            .WithTopic(newTopic)
                            .WithPayload(payload)
                            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                            .WithRetainFlag()
                            .Build();

                        await mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(forwardMessage));

                        Console.WriteLine($"(Simulação) Tópico: {topic} -> Redirecionado para: {newTopic} com valor: {payload}");
                    }
                }

                if (topic == "can/messages")
                {
                    try
                    {
                        var mqttPayload = JsonSerializer.Deserialize<CanMessage>(payload);
                        if (mqttPayload != null)
                        {
                            Console.WriteLine($"Arbitration ID: {mqttPayload.CAN_Message.ArbitrationId}");
                            Console.WriteLine($"Data Bytes: {string.Join(", ", mqttPayload.CAN_Message.Data)}");

                            /*var arbitrationToTopic = new System.Collections.Generic.Dictionary<int, string>
                            {
                                { 0x100, "sensor/sensordetector" },
                                { 0x101, "sensor/sensordetector" },
                                { 0x102, "sensor/sensordetector" },
                                { 0x103, "sensor/sensordetector" }
                            };*/
                            

                            var jsonMessage = CanToJson(mqttPayload);
                            var jsonPayload = JsonSerializer.Serialize(jsonMessage);

                            string targetTopic = "sensor/sensordetector";


                            //if (arbitrationToTopic.TryGetValue(mqttPayload.CAN_Message.ArbitrationId, out var targetTopic))
                            //{
                                var frontendMessage = new MqttApplicationMessageBuilder()
                                    .WithTopic(targetTopic)
                                    .WithPayload(jsonPayload)
                                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                                    .WithRetainFlag()
                                    .Build();

                                await mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(frontendMessage));

                                Console.WriteLine($"Mensagem redirecionada para o tópico {targetTopic}");
                            //}
                            //else
                            //{
                              //  Console.WriteLine("ArbitrationId não mapeado para nenhum tópico específico.");
                            //}
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao processar mensagem: {ex.Message}");
                    }
                }

                
            };

            await mqttServer.StartAsync();
            Console.WriteLine("Broker MQTT iniciado. Aguardando conexões...");

            Console.WriteLine("Pressione qualquer tecla para encerrar.");
            Console.ReadKey();

            await mqttServer.StopAsync();
        }

        // Função para traduzir CAN para JSON
        public static JsonMessage CanToJson(CanMessage canMessage)
        {
            int arbitrationId = canMessage.CAN_Message.ArbitrationId;
            int[] data = canMessage.CAN_Message.Data;

            var algorithmId = !string.IsNullOrEmpty(canMessage.AlgorithmID) ? canMessage.AlgorithmID : "Unknown";

            var status = data[0] == 1;
            var distance = (data[2] << 8) | data[1]; 

            var side = algorithmId == "BlindSpotDetection" && data.Length > 3 && data[3] == 1 ? "Direita" : "Esquerda";

            // Montar a mensagem JSON final
            var jsonMessage = new JsonMessage
            {
                AlgorithmID = algorithmId,
                Timestamp = DateTime.UtcNow.ToString("o"), // Formato ISO 8601
                Status = status,
                Data = algorithmId == "BlindSpotDetection"
                    ? new { Side = side, DistanceToVehicle = distance / 100.0 }
                    : new { DistanceToVehicle = distance / 100.0 }
            };

            return jsonMessage;
        }

                // Função para traduzir CAN para JSON
        public static JsonMessage CanToJsonSim(CanMessageSimulator canMessage)
        {
            int arbitrationId = canMessage.CAN_Message.ArbitrationId;
            int[] data = canMessage.CAN_Message.Data;

            var algorithmId = !string.IsNullOrEmpty(canMessage.AlgorithmID) ? canMessage.AlgorithmID : "Unknown";

            var status = data[0] == 1;
            var distance = (data[2] << 8) | data[1]; 

            var side = algorithmId == "BlindSpotDetection" && data.Length > 3 && data[3] == 1 ? "Direita" : "Esquerda";

            // Montar a mensagem JSON final
            var jsonMessage = new JsonMessage
            {
                AlgorithmID = algorithmId,
                Timestamp = DateTime.UtcNow.ToString("o"), // Formato ISO 8601
                Status = status,
                Data = algorithmId == "BlindSpotDetection"
                    ? new { Side = side, DistanceToVehicle = distance / 100.0 }
                    : new { DistanceToVehicle = distance / 100.0 }
            };

            return jsonMessage;
        }

        public class CanMessage
        {
            public string AlgorithmID { get; set; } = string.Empty;
            public CanData CAN_Message { get; set; } = new CanData();
        }

        public class CanMessageSimulator
        {
            [JsonPropertyName("algorithm_id")]
            public string AlgorithmID { get; set; } = string.Empty;

            [JsonPropertyName("can_message")]
            public CanData CAN_Message { get; set; } = new CanData();
        }

        public class CanData
        {
            [JsonPropertyName("arbitration_id")]
            public int ArbitrationId { get; set; }

            [JsonPropertyName("data")]
            public int[] Data { get; set; }
        }

        // Estrutura para a mensagem JSON
        public class JsonMessage
        {
            public string AlgorithmID { get; set; }
            public string Timestamp { get; set; }
            public bool Status { get; set; }
            public object Data { get; set; }
        }
    }
}
