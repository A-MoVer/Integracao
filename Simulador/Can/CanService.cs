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
        private static ushort mbitrate = 500;
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

            string algoritmo = msg.ident switch
            {
                0x100 => "FrontalCollision",
                0x110 => "RearCollision",
                0x120 => "BlindSpotDetection",
                0x105 => "PedestrianDetection",
                _ => $"Desconhecido_{msg.ident:X}"
            };

            string perigo = status == 1 ? "PERIGO" : "Seguro";

            Console.WriteLine($"[CAN] {algoritmo} | Status: {perigo} | Distância: {distancia} cm | Lado: {lado}");

            var payload = new
            {
                status = perigo,
                distancia,
                lado
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
