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

        public SensorService(
            Dictionary<string, (int ArbitrationId, string AlgorithmID)> sensorMappings,
            MqttService mqttService,
            LoggingService loggingService)
        {
            _sensorMappings = sensorMappings;
            _mqttService = mqttService;
            _loggingService = loggingService;
        }

        public async Task SendSensorMessagesAsync(string sensorName)
        {
            if (!_sensorMappings.ContainsKey(sensorName))
            {
                Console.WriteLine("Sensor não reconhecido. Sensores disponíveis: " + string.Join(", ", _sensorMappings.Keys));
                return;
            }

            var (arbitrationId, algorithmID) = _sensorMappings[sensorName];

            Console.WriteLine($"Enviando mensagens para o sensor '{sensorName}'...");

            int initialDistance = 100; // Distância inicial em metros
            int decrement = 10; // Redução de distância por mensagem
            int numMessages = 10; // Número total de mensagens a serem enviadas

            string side = "Left"; // Definir o lado, pode ser randomizado ou configurado


            for (int i = 0; i < numMessages; i++)
            {
                int currentDistance = initialDistance - (i * decrement);
                if (currentDistance < 0) { currentDistance = 0; } // Não permitir distância negativa
                bool status = true; // Assume que há um perigo se a mensagem está sendo enviada
                // Definir prioridade com base na distância
                string priority;

                if (currentDistance > 150)
                {
                    priority = "0"; // Baixo risco
                }
                else if (currentDistance > 50)
                {
                    priority = "1"; // Médio risco
                }
                else
                {
                    priority = "2"; // Alto risco
                }

                var canMessage = new CanMessage
                {
                    AlgorithmID = algorithmID,
                    CAN_Message = new CanData
                    {
                        ArbitrationId = arbitrationId,
                        Data = new int[8]
                    }
                };

                canMessage.CAN_Message.Data[0] = status ? 1 : 0; // Status
                canMessage.CAN_Message.Data[1] = currentDistance & 0xFF; // Distance low byte
                canMessage.CAN_Message.Data[2] = (currentDistance >> 8) & 0xFF; // Distance high byte

                // Se o sensor requer informação sobre o lado (ex: BlindSpot)
                if (algorithmID == "BlindSpotDetection")
                {
                    canMessage.CAN_Message.Data[3] = side == "Right" ? 1 : 0; // Side: 1 = Right, 0 = Left
                }

                // Bytes 4-7 permanecem como 0 (dados extras)

                string jsonPayload = System.Text.Json.JsonSerializer.Serialize(canMessage);
                await _mqttService.PublishAsync("sim/canmessages", jsonPayload); // Enviando para "sim/canmessages"

                Console.WriteLine($"Enviado: AlgorithmID={algorithmID}, Distance={currentDistance}m, Priority={priority}, Side={side}");

                // Criar e adicionar um registro de log para a mensagem do sensor
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


                // Pequena pausa para simular envio rápido, mas controlado
                await Task.Delay(400); // 200 ms entre mensagens
            }

            // Após o loop de envio de mensagens
            bool finalStatus = false;
            var finalMessage = new CanMessage
            {
                AlgorithmID = algorithmID,
                CAN_Message = new CanData
                {
                    ArbitrationId = arbitrationId,
                    Data = new int[8]
                }
            };

            // Define o status como false (0)
            finalMessage.CAN_Message.Data[0] = finalStatus ? 1 : 0;

            // Se necessário, você pode definir outros dados (como distância ou lado) como 0
            finalMessage.CAN_Message.Data[1] = 0;
            finalMessage.CAN_Message.Data[2] = 0;
            // Por exemplo, para o lado, se for BlindSpot, defina como 0 (ou conforme sua lógica)
            if (algorithmID == "BlindSpotDetection")
            {
                finalMessage.CAN_Message.Data[3] = 0;
            }

            string finalJson = System.Text.Json.JsonSerializer.Serialize(finalMessage);
            await _mqttService.PublishAsync("sim/canmessages", finalJson);

            Console.WriteLine($"Enviado mensagem final com status: {finalStatus}");

            //Console.WriteLine($"Status: {status}");
            Console.WriteLine($"Finalizado o envio de mensagens para o sensor '{sensorName}'.");
        }
    }
}
