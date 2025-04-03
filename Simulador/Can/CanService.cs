using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IXXAT;
using Simulador.Services;
using static IXXAT.SimplyCAN;

namespace CAN
{
    public class CanService
    {
        private static char[] mserial_port = "COM5\0".ToCharArray();
        private static ushort mbitrate = 1000;
        private readonly MqttService _mqttService;

        public CanService(MqttService mqttService)
        {
            _mqttService = mqttService;
        }

        private void ErrorExit()
        {
            Console.WriteLine("Erro: " + simply_get_last_error());
            simply_close();
        }

        private async Task InterpretarMensagem(can_msg_t msg)
        {
            if (msg.dlc < 4) return;

            byte status = msg.payload[0];
            int distancia = msg.payload[1] + (msg.payload[2] << 8);
            string lado = msg.payload[3] == 1 ? "Direita" : "Esquerda";

            uint tipoSensor = msg.ident & 0xFF;  // Máscara para obter apenas o tipo
            int prioridade = (int)((msg.ident & 0xF00) >> 8);
            string algoritmo = tipoSensor switch
            {
                0x00 => "FrontalCollision",
                0x10 => "RearCollision",
                0x20 => "BlindSpotDetection",
                0x05 => "PedestrianDetection",
                _ => $"Desconhecido_{msg.ident:X}"
            };

            string perigo = status == 1 ? "PERIGO" : "Seguro";

            Console.WriteLine($"[CAN] {algoritmo} | Prio: {prioridade} | Status: {perigo} | Distância: {distancia} cm | Lado: {lado}");

            var payload = new
            {
                status = perigo,
                distancia,
                lado,
                prioridade
            };

            string topic = $"sim/can/{algoritmo}";
            string json = JsonSerializer.Serialize(payload);

            await _mqttService.PublishAsync(topic, json);
        }

        public async Task StartAsync(CancellationToken token)
        {
            if (!simply_open(mserial_port) || !simply_initialize_can(mbitrate) || !simply_start_can())
            {
                ErrorExit();
                return;
            }

            Console.WriteLine("[CAN] Ligação CAN iniciada.");

            await Task.Run(async () =>
            {
                can_msg_t rx = new can_msg_t();
                while (!token.IsCancellationRequested)
                {
                    sbyte res = simply_receive(ref rx);
                    if (res == 1)
                        await InterpretarMensagem(rx);
                    else if (res == -1)
                        ErrorExit();

                    Thread.Sleep(1);
                }
            }, token);

            simply_stop_can();
            simply_close();
            Console.WriteLine("[CAN] Ligação CAN terminada.");
        }
    }
}
