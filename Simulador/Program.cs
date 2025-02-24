using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simulador.Models;
using Simulador.Services;
using Simulador.Helpers;

namespace Simulador
{
    // ------------------------------------------
    // Nova classe para representar cada segmento
    // -------------------------------------------
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


        // Objeto para sincronização de acesso ao console
        private static readonly object _consoleLock = new object();

        static async Task Main(string[] args)
        {


            Console.WriteLine("Iniciando cliente MQTT na console...");

            // Inicializar os serviços
            var loggingService = new LoggingService();
            var mqttService = new MqttService("172.20.0.201", 1884, "Simulador");
            var sensorService = new SensorService(sensorMappings, mqttService, loggingService);

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
            // O dashboard ficará nas primeiras 10 linhas (0 a 9)
            DashboardStartRow = 0;
            DashboardHeight = 10;

            // As restrições ficarão fixadas nas últimas 5 linhas (4 linhas para as restrições + 1 para o prompt)
            PromptRow = Console.WindowHeight - 1; // última linha para o prompt
            RestrictionsStartRow = windowHeight - (RestrictionsHeight + 1); // logo acima do prompt

            // Inicializar o token de cancelamento
            _cancellationTokenSource = new CancellationTokenSource();

            // Exibir os comandos iniciais
            DisplayCommands();

            // Iniciar tarefas assíncronas para atualizar tempo, quilometragem, autonomia
            _ = UpdateTimeAsync(mqttService, loggingService, _cancellationTokenSource.Token);
            _ = UpdateTotalKilometersAsync(mqttService, loggingService, _cancellationTokenSource.Token);
            _ = UpdateAutonomyAsync(mqttService, loggingService, _cancellationTokenSource.Token);

            // Atualiza bateria se estiver em carregamento
            _ = UpdateChargingAsync(mqttService, loggingService, _cancellationTokenSource.Token);

            // Consumo de bateria enquanto se conduz
            _ = UpdateBatteryConsumptionAsync(mqttService, loggingService, _cancellationTokenSource.Token);


            // >>> Nova tarefa: SIMULA ROTA (lógica mais realista)
            _ = UpdateRouteAsync(mqttService, loggingService, sensorService, _cancellationTokenSource.Token);

            // Inicie a tarefa para atualizar o dashboard e o menu
            _ = Task.Run(() => UpdateDashboardContinuouslyAsync(_cancellationTokenSource.Token));

            // Após iniciar as outras tarefas, adicione:
            _ = FluctuateSpeedAsync(mqttService, _cancellationTokenSource.Token);

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
                        await StartMotorcycleAsync(mqttService, loggingService);
                        Console.Clear();
                        break;

                    case "stop":
                        running = false;
                        break;

                    // Controlar carregamento
                    case "charge":
                        if (parts.Length == 2)
                        {
                            string chargeAction = parts[1].ToLower();
                            if (chargeAction == "start")
                            {
                                await StartChargingAsync(mqttService, loggingService);
                            }
                            else if (chargeAction == "stop")
                            {
                                await StopChargingAsync(mqttService, loggingService);
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
                        if (!_isMotorcycleOn)
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
                                    await StartRouteAsync(routeName, mqttService, loggingService);
                                }
                                else
                                {
                                    Console.WriteLine("Uso: route start <rota1|rota2|rota3>");
                                }
                            }
                            else if (action == "stop")
                            {
                                await StopRouteAsync(mqttService, loggingService);
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
                            _ = Task.Run(() => AutoSimulateAsync(routeName, mqttService, loggingService, _cancellationTokenSource.Token));
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
                        if (!_isMotorcycleOn)
                        {
                            Console.WriteLine("É necessário ligar a motocicleta antes de usar este comando. Use 'start' para ligar.");
                            break;
                        }

                        // Bloquear velocidade/marcha se estiver em carregamento
                        if (_isCharging && (parts[0].ToLower() == "speed" || parts[0].ToLower() == "gear"))
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
                                            await UpdateBatteryAsync(parts[1], mqttService, loggingService);
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
                                    await UpdateSpeedAsync(parts[1], mqttService, loggingService);
                                else
                                    Console.WriteLine("Uso: speed +<valor> ou speed -<valor>");
                                break;

                            case "temp":
                                if (parts.Length == 2)
                                    await UpdateTemperatureAsync(parts[1], mqttService, loggingService);
                                else
                                    Console.WriteLine("Uso: temp +<valor> ou temp -<valor>");
                                break;

                            case "gear":
                                if (parts.Length == 2)
                                    await UpdateGearAsync(parts[1], mqttService, loggingService);
                                else
                                    Console.WriteLine("Uso: gear front | gear back | gear neutral | gear park");
                                break;

                            case "indicator":
                                if (parts.Length == 2)
                                    await UpdateIndicatorAsync(parts[1], mqttService, loggingService);
                                else
                                    Console.WriteLine("Uso: indicator left | indicator right | indicator none");
                                break;

                            case "lights":
                                if (parts.Length == 2)
                                    await UpdateLightsAsync(parts[1], mqttService, loggingService);
                                else
                                    Console.WriteLine("Uso: lights on | lights off");
                                break;

                            case "abs":
                                if (parts.Length == 2)
                                    await UpdateAbsAsync(parts[1], mqttService, loggingService);
                                else
                                    Console.WriteLine("Uso: abs on | abs off");
                                break;

                            case "mode":
                                if (parts.Length == 2)
                                    await UpdateModeAsync(parts[1], mqttService, loggingService);
                                else
                                    Console.WriteLine("Uso: mode standard | mode eco | mode sport");
                                break;

                            case "maxlights":
                                if (parts.Length == 2)
                                    await UpdateMaxLightsAsync(parts[1], mqttService, loggingService);
                                else
                                    Console.WriteLine("Uso: maxlights on | maxlights off");
                                break;

                            case "danger":
                                if (parts.Length == 2)
                                    await UpdateDangerAsync(parts[1], mqttService, loggingService);
                                else
                                    Console.WriteLine("Uso: danger on | danger off");
                                break;

                            case "send":
                                if (parts.Length == 2)
                                    await sensorService.SendSensorMessagesAsync(parts[1]);
                                else
                                    Console.WriteLine("Uso: send <sensor>");
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
                //DisplayDashboard();
            }

            // Cancelar todas as tarefas assíncronas
            _cancellationTokenSource?.Cancel();

            // Salvar os registros antes de desconectar
            string logFileName = $"MotorcyclePerformanceLog_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            loggingService.SaveLogsToFile(logFileName);

            await mqttService.DisconnectAsync();
            Console.WriteLine("Desconectado do broker. Encerrando programa.");
        }

        /// <summary>
        /// Exibe os comandos disponíveis com base no estado da motocicleta.
        /// </summary>

        // Método para desenhar o dashboard na região reservada (colunas 0 a 40)
        static void DisplayDashboard()
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
            Console.WriteLine($"║ Bateria:    [{GenerateBar(_battery, MAX_BATTERY)}] {FormatValue(_battery, 3)}%        ║ ║   battery -<valor>                                  ║");
            Console.WriteLine($"║ Velocidade: [{GenerateBar(_speed, MAX_SPEED)}] {FormatValue(_speed, 3)} km/h    ║ ║   speed -/+ <valor>                                 ║");
            Console.WriteLine($"║ Temperatura: {FormatValue(_temperature, 3)}°C                             ║ ║   temp -/+ <valor>                                  ║");
            Console.WriteLine($"║ Marcha: {FormatText(_gear, 9)}                              ║ ║   gear front / gear back / gear neutral / gear park ║");
            Console.WriteLine($"║ Modo: {FormatText(_driveMode, 9)}                                ║ ║   mode standard / mode eco / mode sport             ║");
            Console.WriteLine($"║ Luzes: {FormatText(_lights ? "Ligadas" : "Desligadas", 10)}                              ║ ║   lights on / lights off                            ║");
            Console.WriteLine($"║ Máximos: {FormatText(_maxLights ? "Ligadas" : "Desligadas", 10)}                            ║ ║   maxlights on / maxlights off                      ║");
            Console.WriteLine($"║ Pisca Direito: {FormatText(_indicatorRight ? "On" : "Off", 3)}                             ║ ║   indicator right / indicator none                  ║");
            Console.WriteLine($"║ Pisca Esquerdo: {FormatText(_indicatorLeft ? "On" : "Off", 3)}                            ║ ║   indicator left / indicator none                   ║");
            Console.WriteLine($"║ ABS: {FormatText(_abs ? "Ativo" : "Desativado", 10)}                                ║ ║   abs on / abs off                                  ║");
            Console.WriteLine($"║ Autonomia: {FormatDecimal(_autonomy, 5)} km                            ║ ╚═════════════════════════════════════════════════════╝");
            Console.WriteLine($"║ Total KM: {FormatDecimal(_totalKilometers, 5)} km                             ║");
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

        static void DisplayCommands()
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

        #region Start and Stop Methods

        static async Task StartMotorcycleAsync(MqttService mqttService, LoggingService loggingService)
        {
            if (_isMotorcycleOn)
            {
                Console.WriteLine("A motocicleta já está ligada.");
                return;
            }

            _isMotorcycleOn = true;
            Console.WriteLine("A motocicleta foi ligada.");

            // Ligar as luzes automaticamente
            _lights = true;
            Console.WriteLine("As luzes foram ligadas automaticamente.");
            await mqttService.PublishAsync("sim/lights", _lights.ToString());

            // Registrar no log
            var logEntry = CreateLogEntry();
            loggingService.AddPerformanceLog(logEntry);

            await PublishAllStatesAsync(mqttService);

            // Atualizar comandos disponíveis
            DisplayCommands();
        }

        static async Task StopMotorcycleAsync(MqttService mqttService, LoggingService loggingService)
        {
            if (!_isMotorcycleOn)
            {
                Console.WriteLine("A motocicleta já está desligada.");
                return;
            }

            _isMotorcycleOn = false;
            Console.WriteLine("A motocicleta foi desligada.");

            // Desligar as luzes
            _lights = false;
            Console.WriteLine("As luzes foram desligadas automaticamente.");
            await mqttService.PublishAsync("sim/lights", _lights.ToString());

            // Definir a velocidade para 0
            _speed = 0;
            Console.WriteLine("A velocidade foi definida para 0 km/h.");
            await mqttService.PublishAsync("sim/speed", _speed.ToString());

            // Registrar no log
            var logEntry = CreateLogEntry();
            loggingService.AddPerformanceLog(logEntry);

            // Atualizar comandos
            DisplayCommands();
        }

        #endregion

        #region Charging Methods

        static async Task StartChargingAsync(MqttService mqttService, LoggingService loggingService)
        {
            if (_isCharging)
            {
                Console.WriteLine("A bateria já está em carregamento.");
                return;
            }

            _isCharging = true;
            Console.WriteLine("Carregamento da bateria iniciado.");

            await mqttService.PublishAsync("sim/charging", _isCharging.ToString());

            loggingService.AddPerformanceLog(CreateLogEntry());
            DisplayCommands();
        }

        static async Task StopChargingAsync(MqttService mqttService, LoggingService loggingService)
        {
            if (!_isCharging)
            {
                Console.WriteLine("A bateria não está em processo de carregamento.");
                return;
            }

            _isCharging = false;
            Console.WriteLine("Carregamento da bateria parado.");

            await mqttService.PublishAsync("sim/charging", _isCharging.ToString());

            loggingService.AddPerformanceLog(CreateLogEntry());
            DisplayCommands();
        }

        static async Task UpdateChargingAsync(MqttService mqttService, LoggingService loggingService, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_isCharging && _battery < MAX_BATTERY)
                    {
                        // Exemplo: carrega 1% a cada 2 segundos
                        _battery = Math.Min(_battery + 1, MAX_BATTERY);

                        if (_battery != _lastBatteryPublished)
                        {
                            _lastBatteryPublished = _battery;
                            await mqttService.PublishAsync("sim/battery", _battery.ToString());
                            Console.WriteLine($"Bateria carregada para {_battery}%.");

                            loggingService.AddPerformanceLog(CreateLogEntry());
                        }
                    }

                    await Task.Delay(2000, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        #endregion

        #region Rotas - Start/Stop + Lógica de Segmentos

        /// <summary>
        /// Método que inicia uma rota, converte a lista simples de pontos em lista de segmentos,
        /// e reseta os índices/variáveis de controle.
        /// </summary>
        private static async Task StartRouteAsync(string routeName, MqttService mqttService, LoggingService loggingService)
        {
            if (_isRouteActive)
            {
                Console.WriteLine("Já existe uma rota em andamento. Pare a rota atual primeiro (route stop).");
                return;
            }

            if (!_routes.ContainsKey(routeName))
            {
                Console.WriteLine($"Rota '{routeName}' não existe. As rotas disponíveis são: rota1, rota2, rota3.");
                return;
            }

            // Guarda o nome atual
            _activeRouteName = routeName.ToLower();
            // Carregamos a rota (lista de pontos) e convertendo para segmentos
            _currentRoute = _routes[routeName];
            _currentRouteIndex = 0; // por compatibilidade com a interface existente
            _currentRouteSegments = BuildRouteSegments(_currentRoute, routeName);

            // Reiniciamos os controles de segmentação
            _segmentIndex = 0;
            _distanceInSegment = 0.0;

            _isRouteActive = true;

            _accidentTriggered = false;


            Console.WriteLine($"Rota '{routeName}' iniciada com {_currentRoute.Count} pontos.");
            await mqttService.PublishAsync("sim/route/status", $"started:{routeName}");

            // Log
            loggingService.AddPerformanceLog(CreateLogEntry());
            DisplayDashboard();
        }

        private static async Task StopRouteAsync(MqttService mqttService, LoggingService loggingService)
        {
            if (!_isRouteActive)
            {
                Console.WriteLine("Não existe rota em andamento.");
                return;
            }

            _isRouteActive = false;
            Console.WriteLine("Rota parada.");
            await mqttService.PublishAsync("sim/route/status", "stopped");

            loggingService.AddPerformanceLog(CreateLogEntry());
        }

        /// <summary>
        /// Converte uma lista de (Lat, Lng) em uma lista de segmentos, cada segmento com start/end e distância.
        /// </summary>
        private static List<RouteSegment> BuildRouteSegments(List<(double Lat, double Lng)> routePoints, string routeName)
        {
            var segments = new List<RouteSegment>();

            for (int i = 0; i < routePoints.Count - 1; i++)
            {
                var (lat1, lng1) = routePoints[i];
                var (lat2, lng2) = routePoints[i + 1];

                double dist = CalculateDistance(lat1, lng1, lat2, lng2);
                double heading = CalculateBearing(lat1, lng1, lat2, lng2);

                // Definir slope: somente para rota3 (montanhosa)
                double slopeValue = 0.0;
                if (routeName.Equals("rota3", StringComparison.OrdinalIgnoreCase))
                {
                    slopeValue = GenerateRandomSlope(-0.05, 0.10); // entre -5% e +10%
                }
                else
                {
                    slopeValue = 0.0;
                }

                segments.Add(new RouteSegment
                {
                    LatStart = lat1,
                    LngStart = lng1,
                    LatEnd = lat2,
                    LngEnd = lng2,
                    Distance = dist,
                    Heading = heading,
                    Slope = slopeValue
                });
            }
            return segments;
        }


        private static double GenerateRandomSlope(double min, double max)
        {
            var rand = new Random();
            return rand.NextDouble() * (max - min) + min;
        }


        /// <summary>
        /// Calcula distância aproximada (em km) entre duas coordenadas lat/long usando fórmula de Haversine.
        /// </summary>
        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Raio da Terra em km
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        /// <summary>
        /// Método que atualiza a posição na rota a cada ciclo, de acordo com a velocidade e a distância do segmento.
        /// </summary>
        private static async Task UpdateRouteAsync(MqttService mqttService, LoggingService loggingService, SensorService sensorService, CancellationToken cancellationToken)
        {

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_isMotorcycleOn && _isRouteActive &&
                        _currentRouteSegments.Count > 0 &&
                        _segmentIndex < _currentRouteSegments.Count)
                    {
                        // Obtém o segmento atual
                        var segment = _currentRouteSegments[_segmentIndex];

                        // Calcula a distância percorrida neste ciclo
                        double speedKmS = _speed / 3600.0;
                        double distanceThisLoop = speedKmS * 1.0; // 1 segundo por loop
                        _distanceInSegment += distanceThisLoop;

                        // Se a distância percorrida ultrapassar a distância do segmento, passa para o próximo
                        if (_distanceInSegment >= segment.Distance)
                        {
                            _distanceInSegment -= segment.Distance;
                            _segmentIndex++;
                            if (_segmentIndex >= _currentRouteSegments.Count)
                            {
                                _isRouteActive = false;
                                Console.WriteLine("Rota concluída (chegaste ao último ponto).");
                                await mqttService.PublishAsync("sim/route/status", "completed");
                            }
                        }

                        if (_isRouteActive && _segmentIndex < _currentRouteSegments.Count)
                        {
                            // Re-obtem o segmento atual (caso tenha mudado)
                            segment = _currentRouteSegments[_segmentIndex];

                            // Calcula o progresso (fraction) do segmento atual
                            double fraction = _distanceInSegment / segment.Distance;
                            fraction = Math.Clamp(fraction, 0.0, 1.0);

                            double lat = segment.LatStart + (segment.LatEnd - segment.LatStart) * fraction;
                            double lng = segment.LngStart + (segment.LngEnd - segment.LngStart) * fraction;

                            // Publica a posição
                            await mqttService.PublishAsync("sim/gps/latitude", lat.ToString("F6"));
                            await mqttService.PublishAsync("sim/gps/longitude", lng.ToString("F6"));

                            // -- Bloco de semáforo para rota1 --
                            if (_activeRouteName == "rota1")
                            {
                                if (fraction >= 0.9 && _speed > 0)
                                {
                                    double randomVal = new Random().NextDouble();
                                    if (randomVal < 0.1)
                                    {
                                        Console.WriteLine("[URBANO] Semáforo vermelho detectado! Parando por 5 segundos...");
                                        _speed = 0;
                                        await mqttService.PublishAsync("sim/speed", "0");
                                        await Task.Delay(5000);
                                        Console.WriteLine("[URBANO] Semáforo verde! Retomando a velocidade mínima.");
                                        _speed = 20;
                                        await mqttService.PublishAsync("sim/speed", "20");
                                    }
                                }
                            }

                            // -- Bloco para ativação automática dos piscas (para curvas e mudanças de faixa) --
                            if (_activeRouteName != "rota2" && _segmentIndex < _currentRouteSegments.Count - 1)
                            {
                                double currentHeading = segment.Heading;
                                double nextHeading = _currentRouteSegments[_segmentIndex + 1].Heading;
                                double angleDiff = nextHeading - currentHeading;
                                if (angleDiff > 180) { angleDiff -= 360; }
                                if (angleDiff < -180) { angleDiff += 360; }

                                if (Math.Abs(angleDiff) >= 20 && fraction >= 0.8)
                                {
                                    string indicatorDirection = angleDiff > 0 ? "left" : "right";
                                    Console.WriteLine($"[AUTO-INDICATOR] Curva detectada ({indicatorDirection}, diff={angleDiff:0.0}°). Ativando pisca por 3000 ms...");
                                    // Chama o método para ativar o pisca automaticamente
                                    _ = AutoActivateIndicatorAsync(indicatorDirection, 3000, mqttService, loggingService);
                                }
                            }

                            // -- Bloco de lógica de acidente para rota2 (já existente) --
                            if (!_accidentTriggered && _activeRouteName == "rota2")
                            {
                                double progress = (double)_segmentIndex / _currentRouteSegments.Count;
                                if (progress >= 0.5)
                                {
                                    _accidentTriggered = true;
                                    Console.WriteLine("**[ACIDENTE SIMULADO]** Disparando sensor frontalcollision...");
                                    await sensorService.SendSensorMessagesAsync("frontalcollision");
                                    _speed = 0;
                                    await mqttService.PublishAsync("sim/speed", _speed.ToString());
                                }
                            }
                        }

                    }
                    // Aguarda 1 segundo antes de repetir o ciclo
                    await Task.Delay(1000, cancellationToken);

                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        #endregion

        #region Update Methods (Battery, Speed, etc.)


        static async Task UpdateDashboardContinuouslyAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (_consoleLock)
                {
                    // Guarda a posição do cursor antes de atualizar
                    int cursorLeft = Console.CursorLeft;
                    int cursorTop = Console.CursorTop;

                    // Evita que o dashboard limpe o prompt ao atualizar
                    if (_isMotorcycleOn)
                    {

                        DisplayDashboard();
                    }

                    // Restaura a posição do cursor para onde estava antes
                    Console.SetCursorPosition(cursorLeft, cursorTop);
                }
                await Task.Delay(500, cancellationToken);
            }

        }


        static async Task UpdateBatteryAsync(string command, MqttService mqttService, LoggingService loggingService)
        {
            if (!CommandParser.TryParseCommand(command, out int delta))
            {
                Console.WriteLine("Uso: battery -<valor>");
                return;
            }

            int finalValue = _battery + delta;
            finalValue = Math.Clamp(finalValue, MIN_BATTERY, MAX_BATTERY);

            Console.WriteLine($"Alterando bateria de {_battery}% para {finalValue}% ao longo de 5 segundos...");
            await AnimateValueChangeAsync(() => _battery, v => _battery = v, finalValue,
                                          5000, "sim/battery", "battery", mqttService, loggingService);
        }

        static async Task UpdateSpeedAsync(string command, MqttService mqttService, LoggingService loggingService)
        {
            // Verificar se a marcha atual permite alterar a velocidade
            if (_gear.Equals("neutral", StringComparison.OrdinalIgnoreCase) ||
                _gear.Equals("park", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Não é possível alterar a velocidade enquanto a marcha está em 'neutral' ou 'park'.");
                return;
            }

            if (_gear.Equals("neutral", StringComparison.OrdinalIgnoreCase) && _speed > 20)
            {
                Console.WriteLine("[AUTO-GEAR] Velocidade acima de 20 km/h e marcha em neutral. Alterando para front.");
                await UpdateGearAsync("front", mqttService, loggingService);
            }


            if (!CommandParser.TryParseCommand(command, out int delta))
            {
                Console.WriteLine("Uso: speed +<valor> ou speed -<valor>");
                return;
            }
            else if (_gear.Equals("back", StringComparison.OrdinalIgnoreCase) && delta > 0)
            {
                Console.WriteLine("Não é possível aumentar a velocidade enquanto a marcha está em 'back'.");
                return;
            }

            int targetSpeed = _speed + delta;
            targetSpeed = Math.Clamp(targetSpeed, MIN_SPEED, MAX_SPEED);

            Console.WriteLine($"A {(delta > 0 ? "acelerar" : "desacelerar")} " +
                              $"de {_speed} km/h para {targetSpeed} km/h...");

            await ApplyAccelerationAsync(targetSpeed, mqttService, loggingService);

            // Após a aceleração, se o ABS estiver desligado e a velocidade caiu bruscamente, exibir alerta
            if (!_abs) // se ABS está desligado
            {
                int speedDrop = _previousSpeed - _speed;
                if (speedDrop >= 30)
                {
                    Console.WriteLine("[ALERTA ABS] Possível derrapagem detectada devido a travagem brusca!");
                    // Aqui você pode disparar um sensor ou log adicional
                }
            }
            _previousSpeed = _speed;

            if (_routeConfigs.TryGetValue(_activeRouteName, out var config))
            {
                int routeMaxSpeed = config.maxSpeed;
                if (_speed > routeMaxSpeed)
                {
                    Console.WriteLine($"[LIMITE] Excedeu maxSpeed para {_activeRouteName} (limite={routeMaxSpeed}). " +
                                    $"Forçando {_speed} => {routeMaxSpeed}");
                    _speed = routeMaxSpeed;  // Reduz forçadamente
                    await mqttService.PublishAsync("sim/speed", _speed.ToString());


                    var logEntry = new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Topic = "sim/speed",
                        Payload = _speed.ToString(),
                        MessageType = "Performance",
                        Battery = _battery,
                        Speed = _speed,
                        Temperature = _temperature,
                        Gear = _gear,
                        Lights = _lights,
                        Abs = _abs,
                        IndicatorLeft = _indicatorLeft,
                        IndicatorRight = _indicatorRight,
                        DriveMode = _driveMode,
                        MaxLights = _maxLights,
                        DangerLights = _dangerLights,
                        TotalKilometers = _totalKilometers,
                        Autonomy = _autonomy
                    };

                    loggingService.AddPerformanceLog(logEntry);
                }

                // Se for rota2 e speed > 120 => exibe ALERT
                if (_activeRouteName == "rota2" && _speed > 120)
                {
                    Console.WriteLine("[ALERTA] Excedeu limite (120 km/h) na rota2!");
                }
            }
        }

        static async Task UpdateTemperatureAsync(string command, MqttService mqttService, LoggingService loggingService)
        {
            if (!CommandParser.TryParseCommand(command, out int delta))
            {
                Console.WriteLine("Uso: temp +<valor> ou temp -<valor>");
                return;
            }

            int finalValue = _temperature + delta;
            finalValue = Math.Clamp(finalValue, MIN_TEMPERATURE, MAX_TEMPERATURE);

            Console.WriteLine($"Alterando temperatura de {_temperature}°C para {finalValue}°C ao longo de 5 segundos...");
            await AnimateValueChangeAsync(() => _temperature, v => _temperature = v, finalValue,
                                          5000, "sim/temperature", "temperature", mqttService, loggingService);
        }

        static async Task UpdateGearAsync(string position, MqttService mqttService, LoggingService loggingService)
        {
            position = position.ToLower();
            if (position == "front" || position == "back" || position == "neutral")
            {
                await ApplyStateChangeAsync(() => _gear = position,
                                            "Marcha", "sim/gear", position,
                                            mqttService, loggingService);
            }
            else if (position == "park")
            {
                if (_speed == 0)
                {
                    await ApplyStateChangeAsync(() => _gear = position,
                                                "Marcha", "sim/gear", position,
                                                mqttService, loggingService);
                }
                else
                {
                    Console.WriteLine("Não é possível mudar para 'park' enquanto a velocidade não está em 0 km/h.");
                }
            }
            else
            {
                Console.WriteLine("Uso: gear front | gear back | gear neutral | gear park");
            }
        }



        static async Task UpdateIndicatorAsync(string direction, MqttService mqttService, LoggingService loggingService)
        {
            direction = direction.ToLower();
            bool left = _indicatorLeft;
            bool right = _indicatorRight;

            if (direction == "left")
            {
                left = true;
                right = false;
            }
            else if (direction == "right")
            {
                left = false;
                right = true;
            }
            else if (direction == "none")
            {
                left = false;
                right = false;
            }
            else
            {
                Console.WriteLine("Uso: indicator left | indicator right | indicator none");
                return;
            }

            // Atualizar indicadores imediatamente
            await ApplyStateChangeAsync(() =>
            {
                _indicatorLeft = left;
                _indicatorRight = right;
            },
            "Pisca Esquerdo", "sim/indicator_left", left.ToString(),
            mqttService, loggingService);

            await ApplyStateChangeAsync(() =>
            {
                _indicatorLeft = left;
                _indicatorRight = right;
            },
            "Pisca Direito", "sim/indicator_right", right.ToString(),
            mqttService, loggingService);
        }

        static async Task UpdateLightsAsync(string state, MqttService mqttService, LoggingService loggingService)
        {
            state = state.ToLower();
            bool newLights;
            if (state == "on")
            {
                newLights = true;
            }
            else if (state == "off")
            {
                newLights = false;
            }
            else
            {
                Console.WriteLine("Uso: lights on | lights off");
                return;
            }

            await ApplyStateChangeAsync(() => _lights = newLights,
                                        "Luzes médias", "sim/lights", newLights.ToString(),
                                        mqttService, loggingService);
        }

        static async Task UpdateAbsAsync(string state, MqttService mqttService, LoggingService loggingService)
        {
            state = state.ToLower();
            bool newAbs;
            if (state == "on")
                newAbs = true;
            else if (state == "off")
                newAbs = false;
            else
            {
                Console.WriteLine("Uso: abs on | abs off");
                return;
            }

            await ApplyStateChangeAsync(() => _abs = newAbs,
                                        "ABS", "sim/abs", newAbs.ToString(),
                                        mqttService, loggingService);
        }

        static async Task UpdateModeAsync(string mode, MqttService mqttService, LoggingService loggingService)
        {
            mode = mode.ToLower();
            if (mode == "standard" || mode == "eco" || mode == "sport")
            {
                await ApplyStateChangeAsync(() => _driveMode = mode,
                                            "Modo de condução", "sim/drive_mode", mode,
                                            mqttService, loggingService);
            }
            else
            {
                Console.WriteLine("Uso: mode standard | mode eco | mode sport");
            }
        }

        static async Task UpdateMaxLightsAsync(string state, MqttService mqttService, LoggingService loggingService)
        {
            state = state.ToLower();
            bool newMax;
            if (state == "on")
                newMax = true;
            else if (state == "off")
                newMax = false;
            else
            {
                Console.WriteLine("Uso: maxlights on | maxlights off");
                return;
            }

            await ApplyStateChangeAsync(() => _maxLights = newMax,
                                        "Luzes máximas", "sim/maxlights", newMax.ToString(),
                                        mqttService, loggingService);
        }

        static async Task UpdateDangerAsync(string state, MqttService mqttService, LoggingService loggingService)
        {
            state = state.ToLower();
            bool newDanger;
            if (state == "on")
                newDanger = true;
            else if (state == "off")
                newDanger = false;
            else
            {
                Console.WriteLine("Uso: danger on | danger off");
                return;
            }

            await ApplyStateChangeAsync(() => _dangerLights = newDanger,
                                        "Luzes de perigo", "sim/danger-lights", newDanger.ToString(),
                                        mqttService, loggingService);
        }

        static async Task AutoActivateIndicatorAsync(string direction, int durationMs, MqttService mqttService, LoggingService loggingService)
        {
            // Ativa o pisca (left ou right)
            await UpdateIndicatorAsync(direction, mqttService, loggingService);
            // Espera o tempo definido (ex.: 3000 ms)
            await Task.Delay(durationMs);
            // Desliga o pisca
            await UpdateIndicatorAsync("none", mqttService, loggingService);
        }


        #endregion

        #region Aceleração e Auxiliares

        static async Task PublishAllStatesAsync(MqttService mqttService)
        {
            await mqttService.PublishAsync("sim/battery", _battery.ToString());
            await mqttService.PublishAsync("sim/speed", _speed.ToString());
            await mqttService.PublishAsync("sim/gear", _gear);
            await mqttService.PublishAsync("sim/lights", _lights.ToString());
            await mqttService.PublishAsync("sim/abs", _abs.ToString());
            await mqttService.PublishAsync("sim/indicator_left", _indicatorLeft.ToString());
            await mqttService.PublishAsync("sim/indicator_right", _indicatorRight.ToString());
            await mqttService.PublishAsync("sim/drive_mode", _driveMode);
            await mqttService.PublishAsync("sim/maxlights", _maxLights.ToString());
            await mqttService.PublishAsync("sim/danger-lights", _dangerLights.ToString());
            await mqttService.PublishAsync("sim/total_kilometers", _totalKilometers.ToString("F2"));
            await mqttService.PublishAsync("sim/autonomy", _autonomy.ToString("F2"));
        }

        // Crie uma instância compartilhada de Random para evitar que seja reinicializada a cada chamada
        static Random _rand = new Random();

        static async Task ApplyAccelerationAsync(int targetSpeed, MqttService mqttService, LoggingService loggingService)
        {
            // Exemplo de aceleração base (km/h por segundo) conforme modo de condução
            double accelerationRate;
            switch (_driveMode.ToLower())
            {
                case "eco": accelerationRate = 3.0; break;
                case "sport": accelerationRate = 8.0; break;
                default: accelerationRate = 6.0; break;
            }

            int updateInterval = 200; // atualiza a cada 200 ms
            double stepsPerSecond = 1000.0 / updateInterval;
            double speedStep = accelerationRate / stepsPerSecond;

            bool isAccelerating = (targetSpeed > _speed);

            while (true)
            {
                if ((isAccelerating && _speed >= targetSpeed) ||
                    (!isAccelerating && _speed <= targetSpeed))
                {
                    break;
                }

                // Calcula a nova velocidade baseada na aceleração
                double nextSpeedDouble = _speed + (isAccelerating ? speedStep : -speedStep);
                int nextSpeed = (int)Math.Round(nextSpeedDouble);

                // Adiciona uma variação aleatória entre -1 e +1 km/h para simular pequenas flutuações
                int randomAdjustment = _rand.Next(-1, 2); // gera -1, 0 ou 1
                nextSpeed += randomAdjustment;

                // Garante que a nova velocidade não ultrapasse os limites
                nextSpeed = Math.Clamp(nextSpeed, MIN_SPEED, MAX_SPEED);

                // Se ultrapassar o target, força o target
                if (isAccelerating && nextSpeed > targetSpeed)
                {
                    nextSpeed = targetSpeed;
                }
                else if (!isAccelerating && nextSpeed < targetSpeed)
                {
                    nextSpeed = targetSpeed;
                }

                _speed = nextSpeed;

                if (_speed != _lastSpeedPublished)
                {
                    _lastSpeedPublished = _speed;
                    DisplayDashboard();
                    await mqttService.PublishAsync("sim/speed", _speed.ToString());
                    //Console.WriteLine($"Velocidade atualizada para {_speed} km/h.");
                    var logEntry = CreateLogEntry();
                    loggingService.AddPerformanceLog(logEntry);
                }

                if (_speed == targetSpeed)
                {
                    break;
                }

                await Task.Delay(updateInterval);
            }
        }


        static async Task AnimateValueChangeAsync(
            Func<int> getCurrentValue,
            Action<int> setValue,
            int targetValue,
            int durationMs,
            string topic,
            string valueType,
            MqttService mqttService,
            LoggingService loggingService)
        {
            int startValue = getCurrentValue();
            int steps = 10;
            int interval = durationMs / steps;
            int difference = targetValue - startValue;
            double stepValue = (double)difference / steps;

            for (int i = 1; i <= steps; i++)
            {
                int newValue = (int)Math.Round(startValue + stepValue * i);
                setValue(newValue);

                bool shouldPublish = false;
                switch (valueType)
                {
                    case "battery":
                        if (newValue != _lastBatteryPublished)
                        {
                            _lastBatteryPublished = newValue;
                            shouldPublish = true;
                        }
                        break;
                    case "speed":
                        if (newValue != _lastSpeedPublished)
                        {
                            _lastSpeedPublished = newValue;
                            shouldPublish = true;
                        }
                        break;
                    case "temperature":
                        if (newValue != _lastTemperaturePublished)
                        {
                            _lastTemperaturePublished = newValue;
                            shouldPublish = true;
                        }
                        break;
                    default:
                        shouldPublish = false;
                        break;
                }

                if (shouldPublish)
                {
                    Console.WriteLine($"Atualizando {topic.Replace("sim/", "")}: {newValue}");
                    await mqttService.PublishAsync(topic, newValue.ToString());
                }

                await Task.Delay(interval);
            }

            loggingService.AddPerformanceLog(CreateLogEntry());
        }

        static async Task ApplyStateChangeAsync(
            Action applyChange,
            string description,
            string topic,
            string payload,
            MqttService mqttService,
            LoggingService loggingService)
        {
            applyChange();
            Console.WriteLine($"{description} alterado para {payload}");
            await mqttService.PublishAsync(topic, payload);

            loggingService.AddPerformanceLog(CreateLogEntry());
        }

        static LogEntry CreateLogEntry()
        {
            return new LogEntry
            {
                Timestamp = DateTime.Now,
                MessageType = "Performance",
                Battery = _battery,
                Speed = _speed,
                Temperature = _temperature,
                Gear = _gear,
                Lights = _lights,
                Abs = _abs,
                IndicatorLeft = _indicatorLeft,
                IndicatorRight = _indicatorRight,
                DriveMode = _driveMode,
                MaxLights = _maxLights,
                DangerLights = _dangerLights,
                TotalKilometers = _totalKilometers,
                Autonomy = _autonomy
            };
        }

        #endregion

        #region Background Update Methods

        static async Task AutoSimulateAsync(string routeName, MqttService mqttService, LoggingService loggingService, CancellationToken ct)
        {
            Console.WriteLine($"[Auto] Iniciando simulação automática para a rota '{routeName}'...");

            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            _ = Task.Run(() => UpdateDashboardContinuouslyAsync(_cancellationTokenSource.Token));


            // 1. Liga moto se estiver desligada
            if (!_isMotorcycleOn)
            {
                await StartMotorcycleAsync(mqttService, loggingService);
                DisplayDashboard();
                await Task.Delay(500, ct);
            }

            // 2. Inicia a rota
            await StartRouteAsync(routeName, mqttService, loggingService);
            DisplayDashboard();
            await Task.Delay(500, ct);

            // 3. Marcha front
            if (!_gear.Equals("front", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[Auto] Engrenando marcha frontal...");
                await UpdateGearAsync("front", mqttService, loggingService);
                DisplayDashboard();
                await Task.Delay(500, ct);

            }

            // 4. Liga luzes
            Console.WriteLine("[Auto] Ligando luzes...");
            await UpdateLightsAsync("on", mqttService, loggingService);
            DisplayDashboard();

            // >>> AQUI ajustamos a “aceleração inicial” de acordo com a rota
            if (_routeConfigs.TryGetValue(routeName, out var config))
            {
                int desiredSpeed;
                if (routeName.Equals("rota2", StringComparison.OrdinalIgnoreCase))
                {
                    // Para rota2 (autoestrada) queremos iniciar em 110 km/h
                    desiredSpeed = 110;
                }
                else
                {
                    // Para as outras rotas, usamos o mínimo entre o maxSpeed configurado e 60 km/h
                    desiredSpeed = (int)Math.Min(config.maxSpeed, 60);
                }
                Console.WriteLine($"[Auto] Acelerando até {desiredSpeed} km/h...");
                string speedCommand = desiredSpeed > _speed
                    ? "+" + (desiredSpeed - _speed)
                    : "-" + (_speed - desiredSpeed);
                await UpdateSpeedAsync(speedCommand, mqttService, loggingService);
                //DisplayDashboard();

            }
            else
            {
                // se a rota não estiver no dicionário, mantém como estava
                Console.WriteLine("[Auto] Rota não configurada, usando default => 60 km/h");
                await UpdateSpeedAsync("+60", mqttService, loggingService);
                //DisplayDashboard();
            }

            await Task.Delay(1000, ct);

            // Ajusta modo de condução
            // Ex.: se for rota2, forçamos sport; se for rota1, forçamos eco; etc.
            if (routeName == "rota1")
            {
                Console.WriteLine("[Auto] Rota1 => modo 'eco' (urbano)...");
                await UpdateModeAsync("eco", mqttService, loggingService);
                DisplayDashboard();
            }
            else if (routeName == "rota2")
            {
                Console.WriteLine("[Auto] Rota2 => modo 'sport' (autoestrada)...");
                await UpdateModeAsync("sport", mqttService, loggingService);
                DisplayDashboard();
            }
            else
            {
                Console.WriteLine("[Auto] Rota3 => modo 'standard' (rural)...");
                await UpdateModeAsync("standard", mqttService, loggingService);
                DisplayDashboard();
            }

            // Loop enquanto a rota estiver ativa
            while (_isRouteActive && !ct.IsCancellationRequested)
            {

                // Exemplo: se a rota for rota2 (autoestrada), tenta manter velocidade alta
                if (routeName == "rota2" && _speed < 80)
                {
                    int dif = 80 - _speed;
                    Console.WriteLine($"[Auto] Rota2 => Subir para 80 km/h...");
                    await UpdateSpeedAsync("+" + dif, mqttService, loggingService);
                    //DisplayDashboard();
                }

                // se for rota1 e velocidade > 50, reduz
                if (routeName == "rota1" && _speed > 50)
                {
                    int dif = _speed - 50;
                    Console.WriteLine("[Auto] Rota1 => Reduzindo para 50 km/h no trânsito urbano...");
                    await UpdateSpeedAsync("-" + dif, mqttService, loggingService);
                    //DisplayDashboard();
                }

                // se for rota3 e velocidade > 70, reduz
                if (routeName == "rota3" && _speed > 70)
                {
                    int dif = _speed - 70;
                    Console.WriteLine("[Auto] Rota3 => Estrada rural => Reduzindo para 70 km/h...");
                    await UpdateSpeedAsync("-" + dif, mqttService, loggingService);
                    //DisplayDashboard();
                }




                await Task.Delay(2000, ct);
            }

            // Ao final da rota
            Console.WriteLine("[Auto] Rota finalizada. Mudando para modo standard e desligando moto...");
            await UpdateModeAsync("standard", mqttService, loggingService);
            await StopMotorcycleAsync(mqttService, loggingService);
            Console.Clear();
            DisplayDashboard();
        }


        static async Task UpdateTimeAsync(MqttService mqttService, LoggingService loggingService, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var currentTime = DateTime.Now.ToString("hh:mm tt");
                await mqttService.PublishAsync("sim/time", currentTime);

                loggingService.AddPerformanceLog(CreateLogEntry());

                try
                {
                    await Task.Delay(60000, cancellationToken); // Atualizar a cada minuto
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        static async Task UpdateTotalKilometersAsync(MqttService mqttService, LoggingService loggingService, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_isMotorcycleOn)
                {
                    // velocidade em km/h => km por minuto
                    _totalKilometers += _speed / 60.0;
                    await mqttService.PublishAsync("sim/total_kilometers", _totalKilometers.ToString("F2"));

                    loggingService.AddPerformanceLog(CreateLogEntry());
                }

                try
                {
                    await Task.Delay(60000, cancellationToken); // Atualizar a cada minuto
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        static async Task UpdateAutonomyAsync(MqttService mqttService, LoggingService loggingService, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_isMotorcycleOn)
                {
                    if (_speed > 0)
                    {
                        _autonomy = _battery * 10.0 / _speed;
                        await mqttService.PublishAsync("sim/autonomy", _autonomy.ToString("F2"));
                    }
                    else
                    {
                        // Parado => "infinita"
                        await mqttService.PublishAsync("sim/autonomy", "∞");
                    }

                    // Publicar novamente para garantir
                    await mqttService.PublishAsync("sim/autonomy", _autonomy.ToString("F2"));

                    loggingService.AddPerformanceLog(CreateLogEntry());
                }

                try
                {
                    await Task.Delay(60000, cancellationToken); // Atualizar a cada minuto
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        // Método para simular flutuações naturais na velocidade
        static async Task FluctuateSpeedAsync(MqttService mqttService, CancellationToken cancellationToken)
        {
            // Instância compartilhada de Random para gerar variações
            Random rand = new Random();

            while (!cancellationToken.IsCancellationRequested)
            {
                // Se a moto estiver ligada, não estiver carregando e tiver velocidade > 0:
                if (_isMotorcycleOn && !_isCharging && _speed > 0)
                {
                    // Gera um delta aleatório entre -2 e +2 km/h (você pode ajustar esse valor)
                    int delta = rand.Next(-2, 3); // -2, -1, 0, 1 ou 2
                    // Aplica a variação, garantindo que a velocidade não fique negativa
                    int newSpeed = Math.Max(_speed + delta, 0);
                    // Atualiza a velocidade (aqui você pode também forçar que não ultrapasse algum valor máximo se necessário)
                    _speed = newSpeed;
                    DisplayDashboard();
                    // Publica a nova velocidade
                    await mqttService.PublishAsync("sim/speed", _speed.ToString());
                }
                // Aguarda 1 segundo antes de repetir
                await Task.Delay(1000, cancellationToken);
            }
        }


        // Guardar velocidade anterior
        private static int _previousSpeedForRegen = 0;
        static async Task UpdateBatteryConsumptionAsync(MqttService mqttService, LoggingService loggingService, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_isMotorcycleOn && !_isCharging)
                    {
                        // 1. Consumo base dependendo do modo de condução
                        double baseConsumptionRate;
                        switch (_driveMode.ToLower())
                        {
                            case "eco":
                                baseConsumptionRate = 0.2;
                                break;
                            case "sport":
                                baseConsumptionRate = 1.0;
                                break;
                            default:
                                baseConsumptionRate = 0.5;
                                break;
                        }

                        // 2. Fator extra definido pela rota
                        double extraFactor = 1.0;
                        if (_routeConfigs.TryGetValue(_activeRouteName, out var config))
                        {
                            extraFactor = config.extraConsumption;
                        }

                        // 3. Calcular consumo base (consumo por minuto proporcional à velocidade)
                        double consumptionThisMinute = baseConsumptionRate * (_speed / 60.0) * extraFactor;

                        // 4. Se estivermos na rota3, incorporar o efeito da inclinação
                        double slopeFactor = 1.0;
                        if (_activeRouteName.Equals("rota3", StringComparison.OrdinalIgnoreCase))
                        {
                            if (_segmentIndex < _currentRouteSegments.Count)
                            {
                                double slope = _currentRouteSegments[_segmentIndex].Slope;
                                slopeFactor = 1.0 + slope;
                                if (slopeFactor < 0.5)
                                {
                                    slopeFactor = 0.5;
                                }
                            }
                        }
                        consumptionThisMinute *= slopeFactor;

                        // 5. Calcular o consumo para o ciclo (a cada 5 segundos)
                        int batteryConsumed = (int)Math.Round(consumptionThisMinute);
                        int newBattery = Math.Max(_battery - batteryConsumed, MIN_BATTERY);
                        if (newBattery != _battery)
                        {
                            _battery = newBattery;
                            await mqttService.PublishAsync("sim/battery", _battery.ToString());
                            Console.WriteLine($"[Consumo] Bateria => {_battery}% (speed={_speed} km/h, mode={_driveMode}, route={_activeRouteName}, slopeFactor={slopeFactor:0.00}).");
                            loggingService.AddPerformanceLog(CreateLogEntry());
                        }

                        // 6. Verifica se a bateria está esgotada
                        if (_battery <= MIN_BATTERY)
                        {
                            Console.WriteLine("[ALERTA] Bateria esgotada! A motocicleta deve ser carregada.");
                            // Força a velocidade a zero
                            _speed = 0;
                            await mqttService.PublishAsync("sim/speed", "0");
                            // Opcional: interromper a rota e desligar a moto
                            _isRouteActive = false;
                            _isMotorcycleOn = false;
                            // Você também pode forçar a exibição de um alerta ou chamar um método para iniciar o carregamento
                            break;
                        }

                        // 7. (Opcional) Simular regeneração para rota1
                        if (_activeRouteName.Equals("rota1", StringComparison.OrdinalIgnoreCase))
                        {
                            int speedDrop = _previousSpeedForRegen - _speed;
                            if (speedDrop >= 15 && _battery < MAX_BATTERY)
                            {
                                _battery = Math.Min(_battery + 1, MAX_BATTERY);
                                Console.WriteLine("[Regen] Rota1: Recuperou 1% da bateria devido à travagem brusca.");
                                await mqttService.PublishAsync("sim/battery", _battery.ToString());
                            }
                        }
                        _previousSpeedForRegen = _speed;
                    }

                    // Aguardar 5 segundos antes do próximo ciclo
                    await Task.Delay(5000, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
        #endregion
    }
}

