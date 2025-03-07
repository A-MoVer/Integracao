using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simulador.Models;
using Simulador.Services;
using Simulador.Helpers;
using Simulador.Core;

namespace Simulador
{
    public class RouteSegment
    {
        public double LatStart { get; set; }
        public double LngStart { get; set; }
        public double LatEnd { get; set; }
        public double LngEnd { get; set; }
        public double Distance { get; set; } // em km
        // Novo campo para direção (em graus, 0 a 360)
        public double Heading { get; set; }
        public double Slope { get; set; } // já existente
    }

    class Program
    {
        // Mapeamento dos sensores para ArbitrationId e AlgorithmID
        private static readonly Dictionary<string, (int ArbitrationId, string AlgorithmID)> sensorMappings
            = new Dictionary<string, (int, string)>(StringComparer.OrdinalIgnoreCase)
            {
                { "blindspot",        (0x100, "BlindSpotDetection") },
                { "pedestrian",       (0x101, "PedestrianDetection") },
                { "frontalcollision", (0x102, "FrontalCollisionDetection") },
                { "rearcollision",    (0x103, "RearCollisionDetection") },
                // Adicione outros sensores conforme necessário
            };


        // Token de cancelamento para tarefas assíncronas
        private static CancellationTokenSource? _cancellationTokenSource;
        private static int PromptRow;


        // Objeto para sincronização de acesso ao console
        private static readonly object _consoleLock = new object();

        static async Task Main(string[] args)
        {


            Console.WriteLine("Iniciando cliente MQTT na console...");

            // Inicializar os serviços
            var loggingService = new LoggingService();
            var mqttService = new MqttService("172.20.0.201", 1884, "Simulador");
            var sensorService = new SensorService(sensorMappings, mqttService, loggingService);
            var startAndUpdate = new StartAndUpdate(mqttService, loggingService, null);
            var dashboard = new Dashboard(mqttService, loggingService);
            var chargingMethods = new ChargingMethods(mqttService, loggingService, dashboard);
            var auxiliar = new Auxiliares(mqttService, loggingService, dashboard);
            var updateMethods = new UpdateMethods(mqttService, loggingService, dashboard, auxiliar, null);
            var routes = new Routes(mqttService, loggingService, sensorService, dashboard, null, null, auxiliar);
            var backgroundUpdated = new BackgroundUpdated(mqttService, loggingService, sensorService, _cancellationTokenSource, startAndUpdate, dashboard, updateMethods, routes);

            startAndUpdate.SetAux(auxiliar);
            updateMethods.SetRoutes(routes);
            routes.SetUpdateMethods(updateMethods);
            routes.SetBackgroundUpdated(backgroundUpdated);
            try
            {
                await mqttService.ConnectAsync();
            }
            catch
            {
                Console.WriteLine("Falha ao conectar ao broker MQTT. Encerrando programa.");
                return;
            }

            Console.Clear();


            int windowHeight = Console.WindowHeight;

            // As restrições ficarão fixadas nas últimas 5 linhas (4 linhas para as restrições + 1 para o prompt)
            PromptRow = Console.WindowHeight - 1; // última linha para o prompt

            // Inicializar o token de cancelamento
            _cancellationTokenSource = new CancellationTokenSource();

            // Exibir os comandos iniciais
            dashboard.DisplayCommands();

            // Iniciar tarefas assíncronas para atualizar tempo, quilometragem, autonomia
            _ = backgroundUpdated.UpdateTimeAsync(_cancellationTokenSource.Token);
            _ = backgroundUpdated.UpdateTotalKilometersAsync(_cancellationTokenSource.Token);
            _ = backgroundUpdated.UpdateAutonomyAsync(_cancellationTokenSource.Token);

            // Atualiza bateria se estiver em carregamento
            _ = chargingMethods.UpdateChargingAsync(_cancellationTokenSource.Token);

            // Consumo de bateria enquanto se conduz
            _ = backgroundUpdated.UpdateBatteryConsumptionAsync(_cancellationTokenSource.Token);


            // >>> Nova tarefa: SIMULA ROTA (lógica mais realista)
            _ = routes.UpdateRouteAsync(sensorService);

            // Inicie a tarefa para atualizar o dashboard e o menu
            _ = Task.Run(() => updateMethods.UpdateDashboardContinuouslyAsync(_cancellationTokenSource.Token));

            // Após iniciar as outras tarefas, adicione:
            _ = backgroundUpdated.FluctuateSpeedAsync(_cancellationTokenSource.Token);

            bool running = true;
            while (running)
            {
                lock (_consoleLock)
                {
                    Console.SetCursorPosition(0, PromptRow);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, PromptRow);
                    Console.Write("> ");
                }
                var line = Console.ReadLine();
                if (line == null) { continue; }

                lock (_consoleLock)
                {
                    // Reposiciona corretamente o cursor após o input do utilizador
                    Console.SetCursorPosition(0, PromptRow);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, PromptRow);
                    Console.Write("> ");
                }

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) { continue; }

                switch (parts[0].ToLower())
                {
                    case "start":
                        await startAndUpdate.StartMotorcycleAsync();
                        SimulationState.IsMotorcycleOn = true;
                        Console.Clear();
                        // Atualizar comandos disponíveis
                        dashboard.DisplayDashboard();
                        break;

                    case "stop":
                        await startAndUpdate.StopMotorcycleAsync();
                        SimulationState.IsMotorcycleOn = false;
                        Console.Clear();
                        break;

                    // Controlar carregamento
                    case "charge":
                        if (parts.Length == 2)
                        {
                            string chargeAction = parts[1].ToLower();
                            if (chargeAction == "start")
                            {
                                await chargingMethods.StartChargingAsync();
                                dashboard.DisplayDashboard();
                            }
                            else if (chargeAction == "stop")
                            {
                                await chargingMethods.StopChargingAsync();
                                dashboard.DisplayDashboard();
                            }
                            else
                                Console.WriteLine("Uso: charge start | charge stop");
                        }
                        else
                        {
                            Console.WriteLine("Uso: charge start | charge stop");
                        }
                        break;

                    case "exit":
                        running = false;
                        break;

                    // Comando de rota
                    case "route":
                        if (!SimulationState.IsMotorcycleOn)
                        {
                            Console.WriteLine("É necessário ligar a motocicleta antes de usar este comando.");
                            break;
                        }
                        if (parts.Length >= 2)
                        {
                            string action = parts[1].ToLower();
                            if (action == "start")
                            {
                                // route start <nome>
                                if (parts.Length == 3)
                                {
                                    string routeName = parts[2].ToLower();
                                    await routes.StartRouteAsync(routeName);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: route start <rota1|rota2|rota3>");
                                }
                            }
                            else if (action == "stop")
                            {
                                await routes.StopRouteAsync();
                            }
                            else
                            {
                                Console.WriteLine("Uso: route start <nome> | route stop");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Uso: route start <nome> | route stop");
                        }
                        break;

                    case "auto":
                        if (parts.Length == 2)
                        {
                            string routeName = parts[1].ToLower();
                            // Lançar o “cenário automático”
                            _ = Task.Run(() => backgroundUpdated.AutoSimulateAsync(routeName));

                        }
                        else
                        {
                            Console.WriteLine("Uso: auto <rota1|rota2|rota3>");
                        }
                        break;


                    // Verificar se a motocicleta está ligada antes de processar outros comandos
                    case "battery":
                    case "speed":
                    case "temp":
                    case "gear":
                    case "indicator":
                    case "lights":
                    case "abs":
                    case "mode":
                    case "maxlights":
                    case "danger":
                    case "send":
                        if (!SimulationState.IsMotorcycleOn)
                        {
                            Console.WriteLine("É necessário ligar a motocicleta antes de usar este comando. Use 'start' para ligar.");
                            break;
                        }

                        // Bloquear velocidade/marcha se estiver em carregamento
                        if (SimulationState.IsCharging && (parts[0].ToLower() == "speed" || parts[0].ToLower() == "gear"))
                        {
                            Console.WriteLine("A motocicleta está em carregamento. Pare o carregamento antes de alterar velocidade ou marchas.");
                            break;
                        }

                        // Processar outros comandos
                        switch (parts[0].ToLower())
                        {
                            case "battery":
                                if (parts.Length == 2)
                                {
                                    if (CommandParser.TryParseCommand(parts[1], out int delta))
                                    {
                                        // Se for positivo, não permite
                                        if (delta > 0)
                                        {
                                            Console.WriteLine("Não é possível aumentar manualmente a bateria. Use 'charge start' para carregar.");
                                        }
                                        else
                                        {
                                            // Se for negativo, podemos chamar o UpdateBatteryAsync normalmente
                                            await updateMethods.UpdateBatteryAsync(parts[1]);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Uso: battery -<valor>");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Uso: battery -<valor>");
                                }
                                break;

                            case "speed":
                                if (parts.Length == 2)
                                {
                                    await updateMethods.UpdateSpeedAsync(parts[1]);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: speed +<valor> ou speed -<valor>");
                                }
                                break;

                            case "temp":
                                if (parts.Length == 2)
                                {
                                    await updateMethods.UpdateTemperatureAsync(parts[1]);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: temp +<valor> ou temp -<valor>");
                                }
                                break;

                            case "gear":
                                if (parts.Length == 2)
                                {
                                    await updateMethods.UpdateGearAsync(parts[1]);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: gear front | gear back | gear neutral | gear park");
                                }
                                break;

                            case "indicator":
                                if (parts.Length == 2)
                                {
                                    await updateMethods.UpdateIndicatorAsync(parts[1]);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: indicator left | indicator right | indicator none");
                                }
                                break;

                            case "lights":
                                if (parts.Length == 2)
                                {
                                    await updateMethods.UpdateLightsAsync(parts[1]);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: lights on | lights off");
                                }
                                break;

                            case "abs":
                                if (parts.Length == 2)
                                {
                                    await updateMethods.UpdateAbsAsync(parts[1]);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: abs on | abs off");
                                }
                                break;

                            case "mode":
                                if (parts.Length == 2)
                                {
                                    await updateMethods.UpdateModeAsync(parts[1]);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: mode standard | mode eco | mode sport");
                                }
                                break;

                            case "maxlights":
                                if (parts.Length == 2)
                                {
                                    await updateMethods.UpdateMaxLightsAsync(parts[1]);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: maxlights on | maxlights off");
                                }
                                break;

                            case "danger":
                                if (parts.Length == 2)
                                {
                                    await updateMethods.UpdateDangerAsync(parts[1]);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: danger on | danger off");
                                }
                                break;

                            case "send":
                                if (parts.Length == 2)
                                {
                                    await sensorService.SendSensorMessagesAsync(parts[1]);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: send <sensor>");
                                }
                                break;

                            default:
                                Console.WriteLine("Comando não reconhecido.");
                                break;
                        }
                        break;

                    default:
                        Console.WriteLine("Comando não reconhecido.");
                        break;
                }
            }

            // Cancelar todas as tarefas assíncronas
            _cancellationTokenSource?.Cancel();

            // Salvar os registros antes de desconectar
            string logFileName = $"MotorcyclePerformanceLog_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            loggingService.SaveLogsToFile(logFileName);

            await mqttService.DisconnectAsync();
            Console.WriteLine("Desconectado do broker. Encerrando programa.");
        }

    }
}