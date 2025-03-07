// ChargingMethods.cs
using System;
using System.Threading.Tasks;
using Simulador.Services;
using Simulador.Models;

namespace Simulador.Core
{
    public class Dashboard
    {

        private readonly MqttService _mqttService;
        private readonly LoggingService _loggingService;
        private static int DashboardStartRow = 0;
        private static int DashboardHeight = 10;

        private static int RestrictionsStartRow;
        private static int RestrictionsHeight = 4; // ajuste conforme desejar
        private static int PromptRow; // Ex: 21 (ou use Console.WindowHeight - 1)


        // Limites para os estados
        private const int MAX_BATTERY = 100;
        private const int MIN_BATTERY = 0;
        private const int MAX_SPEED = 200;
        private const int MIN_SPEED = 0;
        private const int MAX_TEMPERATURE = 100;
        private const int MIN_TEMPERATURE = -50;

        public Dashboard(MqttService mqttService, LoggingService loggingService)
        {
            _mqttService = mqttService;
            _loggingService = loggingService;
        }

        public void DisplayDashboard()
        {
            for (int i = DashboardStartRow; i < DashboardStartRow + DashboardHeight; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write(new string(' ', Console.WindowWidth));
            }

            // Posiciona o cursor para escrever o dashboard
            Console.WriteLine("╔════════════════════════════════════════════════╗ ╔═════════════════════════════════════════════════════╗");
            Console.WriteLine("║     Simulador da Motocicleta                   ║ ║                      COMANDOS                       ║");
            Console.WriteLine("╠════════════════════════════════════════════════╣ ╠═════════════════════════════════════════════════════╣");
            Console.WriteLine($"║ Bateria:    [{GenerateBar(SimulationState.Battery, MAX_BATTERY)}] {FormatValue(SimulationState.Battery, 3)}%        ║ ║   battery -<valor>                                  ║");
            Console.WriteLine($"║ Velocidade: [{GenerateBar(SimulationState.Speed, MAX_SPEED)}] {FormatValue(SimulationState.Speed, 3)} km/h    ║ ║   speed -/+ <valor>                                 ║");
            Console.WriteLine($"║ Temperatura: {FormatValue(SimulationState.Temperature, 3)}°C                             ║ ║   temp -/+ <valor>                                  ║");
            Console.WriteLine($"║ Marcha: {FormatText(SimulationState.Gear, 9)}                              ║ ║   gear front / gear back / gear neutral / gear park ║");
            Console.WriteLine($"║ Modo: {FormatText(SimulationState.DriveMode, 9)}                                ║ ║   mode standard / mode eco / mode sport             ║");
            Console.WriteLine($"║ Luzes: {FormatText(SimulationState.Lights ? "Ligadas" : "Desligadas", 10)}                              ║ ║   lights on / lights off                            ║");
            Console.WriteLine($"║ Máximos: {FormatText(SimulationState.MaxLights ? "Ligadas" : "Desligadas", 10)}                            ║ ║   maxlights on / maxlights off                      ║");
            Console.WriteLine($"║ Pisca Direito: {FormatText(SimulationState.IndicatorRight ? "On" : "Off", 3)}                             ║ ║   indicator right / indicator none                  ║");
            Console.WriteLine($"║ Pisca Esquerdo: {FormatText(SimulationState.IndicatorLeft ? "On" : "Off", 3)}                            ║ ║   indicator left / indicator none                   ║");
            Console.WriteLine($"║ ABS: {FormatText(SimulationState.Abs ? "Ativo" : "Desativado", 10)}                                ║ ║   abs on / abs off                                  ║");
            Console.WriteLine($"║ Autonomia: {FormatDecimal(SimulationState.Autonomy, 5)} km                            ║ ╚═════════════════════════════════════════════════════╝");
            Console.WriteLine($"║ Total KM: {FormatDecimal(SimulationState.TotalKilometers, 5)} km                             ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Restrições:");
            Console.WriteLine("- A marcha 'park' só pode ser selecionada quando a velocidade está em 0 km/h.");
            Console.WriteLine("- Quando a marcha está em 'front', a velocidade só pode aumentar.");
            Console.WriteLine("- Quando a marcha está em 'back', a velocidade só pode diminuir.");
            Console.WriteLine("- Se estiver a carregar (charge start), não pode alterar velocidade ou marcha.");

        }




        static string FormatValue(double value, int length)
        {
            return value.ToString().PadLeft(length);
        }

        static string FormatText(string text, int length)
        {
            return text.PadRight(length);
        }
        static string FormatDecimal(double value, int length)
        {
            return value.ToString("F2").PadLeft(length); // Usa 1 casa decimal e alinha à direita
        }
        static string GenerateBar(int value, int max)
        {
            int barLength = 20;
            int filled = (int)((value / (double)max) * barLength);
            return new string('█', filled) + new string('-', barLength - filled);
        }

        public void DisplayCommands()
        {
            // Limpa a área do menu (ajuste o número de linhas conforme necessário)
            for (int i = RestrictionsStartRow; i < RestrictionsStartRow + RestrictionsHeight; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write(new string(' ', Console.WindowWidth));
            }

            Console.SetCursorPosition(0, RestrictionsStartRow);

            // Comandos disponíveis quando a motocicleta está desligada
            Console.WriteLine("Start:                   start");
            Console.WriteLine("Carregar bateria:        charge start (caso deseje carregar com a moto desligada)");
            Console.WriteLine("Sair do programa:        exit");

            Console.WriteLine();
        }
    }

}