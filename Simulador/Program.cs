using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simulador.Models;
using Simulador.Services;
using Simulador.Helpers;
<<<<<<< HEAD

namespace Simulador
{
    // ------------------------------------------
    // Nova classe para representar cada segmento
    // -------------------------------------------
=======
using Simulador.Core;

namespace Simulador
{
>>>>>>> tijo
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

<<<<<<< HEAD
    class Program
    {
        // Estados da simulação
        private static int _battery = 100;
        private static int _speed = 0;
        private static int _temperature = 25;
        private static string _gear = "neutral";
        private static bool _lights = false;
        private static bool _abs = false;
        private static bool _indicatorLeft = false;
        private static bool _indicatorRight = false;
        private static string _driveMode = "standard";
        private static bool _maxLights = false;
        private static bool _dangerLights = false;
        private static double _totalKilometers = 0.0;
        private static double _autonomy = 0.0;


        // Novo estado para controlar se a motocicleta está ligada
        private static bool _isMotorcycleOn = false;

        // Variável que controla se a bateria está em carregamento
        private static bool _isCharging = false;

        // Limites para os estados
        private const int MAX_BATTERY = 100;
        private const int MIN_BATTERY = 0;
        private const int MAX_SPEED = 200;
        private const int MIN_SPEED = 0;
        private const int MAX_TEMPERATURE = 100;
        private const int MIN_TEMPERATURE = -50;

        // Variáveis para armazenar o último valor publicado
        private static int _lastBatteryPublished = _battery;
        private static int _lastSpeedPublished = _speed;
        private static int _lastTemperaturePublished = _temperature;
        private static string _activeRouteName = string.Empty;
        private static bool _accidentTriggered = false;
        private static int _previousSpeed = 0;

        // -------------------------
        //          ROTAS
        // -------------------------

        // Dicionário com a lista de coordenadas de cada rota
        private static readonly Dictionary<string, List<(double, double)>> _routes
            = new Dictionary<string, List<(double, double)>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "rota1",
                new List<(double, double)>
                {
                    (38.736946, -9.142685),
                    (38.737500, -9.140000),
                    (38.738000, -9.137000),
                    (38.738500, -9.135000),
                }
            },
            {
                "rota2",
                new List<(double, double)>
                {
                    (41.157944, -8.629105),
                    (41.158500, -8.628000),
                    (41.159000, -8.626500),
                    (41.159500, -8.625000),
                    (41.160000, -8.623500),
                    (41.161000, -8.621000),
                    (41.162000, -8.619000),
                    (41.163000, -8.617000),
                    (41.164000, -8.615000)
                }
            },
            {
                "rota3",
                new List<(double, double)>
                {
                    (37.017953, -7.930834),
                    (37.018500, -7.929500),
                    (37.019000, -7.928000),
                    (37.019500, -7.927000),
                }
            }
        };
        // Exemplo de 3 rotas, cada uma com uma lista de coordenadas (Lat, Lng)
        private static readonly Dictionary<string, (int maxSpeed, double accelerationRate, double extraConsumption)> _routeConfigs
             = new Dictionary<string, (int, double, double)>(StringComparer.OrdinalIgnoreCase)
         {
            // rota1 => zona urbana
            //   - maxSpeed = 50
            //   - aceleração suave => 3.0
            //   - extraConsumption => 1.0 (ou seja, sem adicional)
            { "rota1", (50, 3.0, 1.0) },

            // rota2 => autoestrada
            //   - maxSpeed = 120
            //   - aceleração agressiva => 8.0
            //   - extraConsumption => 1.5 (ex.: 50% a mais)
            { "rota2", (120, 8.0, 1.5) },

            // rota3 => estrada rural ou montanhosa
            //   - maxSpeed = 70
            //   - aceleração média => 5.0
            //   - extraConsumption => 1.3 (ex.: 30% a mais)
            { "rota3", (70, 5.0, 1.3) }
         };

        // Controla o estado da rota atual
        private static bool _isRouteActive = false;
        private static List<(double Lat, double Lng)> _currentRoute = new List<(double, double)>();
        private static int _currentRouteIndex = 0; // Mantido para não alterar interface

        // >>> NOVAS ESTRUTURAS para segmentos mais realistas
        private static List<RouteSegment> _currentRouteSegments = new List<RouteSegment>();
        private static int _segmentIndex = 0;
        private static double _distanceInSegment = 0.0; // em km

=======
    static class Program
    {
>>>>>>> tijo
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


<<<<<<< HEAD
        private static double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            double rlat1 = ToRadians(lat1);
            double rlat2 = ToRadians(lat2);
            double dLon = ToRadians(lon2 - lon1);

            double y = Math.Sin(dLon) * Math.Cos(rlat2);
            double x = Math.Cos(rlat1) * Math.Sin(rlat2) - Math.Sin(rlat1) * Math.Cos(rlat2) * Math.Cos(dLon);
            double bearingRad = Math.Atan2(y, x);
            double bearingDeg = (bearingRad * (180.0 / Math.PI) + 360) % 360;
            return bearingDeg;
        }

        // Token de cancelamento para tarefas assíncronas
        private static CancellationTokenSource? _cancellationTokenSource;

        private static int DashboardStartRow = 0;
        private static int DashboardHeight = 10;

        private static int RestrictionsStartRow;
        private static int RestrictionsHeight = 4; // ajuste conforme desejar
        private static int PromptRow; // Ex: 21 (ou use Console.WindowHeight - 1)


        private static bool _isUserTyping = false;
=======
        // Token de cancelamento para tarefas assíncronas
        private static CancellationTokenSource? _cancellationTokenSource;
        private static int PromptRow;
>>>>>>> tijo


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
                            {
                                Console.WriteLine("Uso: charge start | charge stop");
                            }
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
