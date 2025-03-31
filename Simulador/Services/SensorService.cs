using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Simulador.Models;

namespace Simulador.Services
{
    public class SensorService
    {
        private readonly Dictionary<string, (int ArbitrationId, string AlgorithmID)> _sensorMappings;
        private readonly MqttService _mqttService;
        private readonly LoggingService _loggingService;
        private readonly Random _rand = new Random();

        public SensorService(
            Dictionary<string, (int ArbitrationId, string AlgorithmID)> sensorMappings,
            MqttService mqttService,
            LoggingService loggingService)
        {
            _sensorMappings = sensorMappings;
            _mqttService = mqttService;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Envia mensagens do sensor enquanto o perigo (distância medida) estiver abaixo de 100m.
        /// Se a distância ultrapassar 100m, com 90% de chance o perigo cessa.
        /// Caso ocorra uma colisão (distância = 0), o envio é interrompido imediatamente.
        /// Ao final, envia uma mensagem final com status false.
        /// </summary>
        public async Task SendSensorMessagesAsync(string sensorName)
        {
            if (!_sensorMappings.ContainsKey(sensorName))
            {
                Console.WriteLine("Sensor não reconhecido. Sensores disponíveis: " +
                                  string.Join(", ", _sensorMappings.Keys));
                return;
            }

            var (arbitrationId, algorithmID) = _sensorMappings[sensorName];
            Console.WriteLine($"Iniciando envio de mensagens para o sensor '{sensorName}'...");

            // Parâmetros dinâmicos:
            // Para simular o perigo ativo, definimos uma distância inicial aleatória entre 40 e 60m
            int currentDistance = _rand.Next(85, 99);
            int messageCount = 0;
            double errorProbability = 0.3;   // 30% de chance de inverter o status (falso-positivo/negativo)
            double collisionProbability = 0.05; // 5% de chance de ocorrer colisão (distância = 0)

            // Loop: enquanto o perigo estiver ativo
            while (true)
            {
                messageCount++;

                // Aplica um ruído (jitter) aleatório entre -3 e +3 metros à medição
                int jitter = _rand.Next(-5, 5);
                int measuredDistance = Math.Max(currentDistance + jitter, 0);

                // A partir da segunda mensagem, com 10% de chance ocorre colisão (distância = 0)
                if (messageCount >= 2 && _rand.NextDouble() < collisionProbability)
                {
                    measuredDistance = 0;
                }

                // Se ocorrer colisão, interrompe imediatamente
                if (measuredDistance == 0)
                {
                    Console.WriteLine("Colisão detectada (distance = 0). Encerrando envio de mensagens.");
                    await _mqttService.PublishAsync("sim/collision", "true");
                    break;
                }

                // Se a distância medida ultrapassar 100m, com 90% de chance o perigo termina
                if (measuredDistance > 150)
                {
                    if (_rand.NextDouble() < 0.9)
                    {
                        Console.WriteLine("Distância acima de 150m detectada. Perigo cessou.");
                        break;
                    }
                    // Caso contrário, continua enviando mesmo que a distância esteja acima de 100m
                }

                // Para o status: normalmente true, mas com chance de erro de 30%
                bool status = true;
                if (_rand.NextDouble() < errorProbability)
                    status = !status;

                // Define prioridade com base na distância (pode ser ajustada conforme o cenário)
                string priority;
                if (measuredDistance > 150)
                    priority = "0"; // Baixo risco
                else if (measuredDistance > 50)
                    priority = "1"; // Médio risco
                else
                    priority = "2"; // Alto risco

                var canMessage = new CanMessage
                {
                    AlgorithmID = algorithmID,
                    CAN_Message = new CanData
                    {
                        ArbitrationId = arbitrationId,
                        Data = new int[8]
                    }
                };

                canMessage.CAN_Message.Data[0] = status ? 1 : 0; // Status do sensor
                canMessage.CAN_Message.Data[1] = measuredDistance & 0xFF; // Byte baixo da distância
                canMessage.CAN_Message.Data[2] = (measuredDistance >> 8) & 0xFF; // Byte alto da distância

                // Se o sensor requer informação sobre o lado (ex: BlindSpotDetection)
                if (algorithmID == "BlindSpotDetection")
                {
                    // Randomiza o lado entre Left e Right
                    string side = _rand.NextDouble() < 0.5 ? "Left" : "Right";
                    canMessage.CAN_Message.Data[3] = side == "Right" ? 1 : 0;
                }

                // Bytes 4-7 permanecem 0.
                string jsonPayload = System.Text.Json.JsonSerializer.Serialize(canMessage);
                await _mqttService.PublishAsync("sim/canmessages", jsonPayload);
                Console.WriteLine($"Enviado: AlgorithmID={algorithmID}, Distance={measuredDistance}m, Priority={priority}, Status={status}");

                var sensorLogEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Topic = "sim/canmessages",
                    Payload = jsonPayload,
                    MessageType = "Sensor",
                    SensorName = sensorName,
                    ArbitrationId = arbitrationId,
                    Data = canMessage.CAN_Message.Data
                };

                _loggingService.AddSensorLog(sensorLogEntry);

                // Aguarda um intervalo aleatório entre 300 e 500 ms
                int delayMs = _rand.Next(300, 501);
                await Task.Delay(delayMs);

                // Atualiza a distância para a próxima iteração:
                // Simula o objeto se afastando lentamente: incremento aleatório entre 5 e 10 metros
                int increment = _rand.Next(-5, 11);
                currentDistance += increment;


            }

            // Fora do loop: enviar mensagem final com status false (indicando que o perigo cessou)
            var finalMessage = new CanMessage
            {
                AlgorithmID = algorithmID,
                CAN_Message = new CanData
                {
                    ArbitrationId = arbitrationId,
                    Data = new int[8]
                }
            };
            finalMessage.CAN_Message.Data[0] = 0; // Status false
            finalMessage.CAN_Message.Data[1] = 0;
            finalMessage.CAN_Message.Data[2] = 0;
            if (algorithmID == "BlindSpotDetection")
            {
                finalMessage.CAN_Message.Data[3] = 0;
            }
            string finalJson = System.Text.Json.JsonSerializer.Serialize(finalMessage);
            await _mqttService.PublishAsync("sim/canmessages", finalJson);
            Console.WriteLine("Enviado mensagem final com status: false");
            Console.WriteLine($"Finalizado o envio de mensagens para o sensor '{sensorName}'.");
        }
    }
}
