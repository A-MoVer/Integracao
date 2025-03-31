using System;
using System.Threading.Tasks;
using Simulador.Services;
using Simulador.Models;
using Simulador.Helpers;

namespace Simulador.Core
{
    public class Routes
    {
        private readonly MqttService _mqttService;
        private readonly LoggingService _loggingService;

        private readonly SensorService _sensorService;

        private readonly Dashboard _dashboard;

        private UpdateMethods _updateMethods;

        private BackgroundUpdated _backgroundUpdated;

        private readonly Auxiliares _auxiliares;

        private CancellationTokenSource? _cts;

        public bool _isRouteActive = false;

        public string _activeRouteName = string.Empty;

        private List<(double Lat, double Lng)> _currentRoute = new List<(double, double)>();


        public List<RouteSegment> _currentRouteSegments = new List<RouteSegment>();

        public int _segmentIndex = 0;
        private double _distanceInSegment = 0.0; // em km

        private bool _accidentTriggered = false;
        private static Random _rand = new Random();

        private const int MAX_SPEED = 200;

        public double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }


        public readonly Dictionary<string, (int maxSpeed, double accelerationRate, double extraConsumption)> _routeConfigs
            = new Dictionary<string, (int, double, double)>(StringComparer.OrdinalIgnoreCase)
        {
            { "rota1", (50, 3.0, 1.0) },

            { "rota2", (120, 8.0, 1.5) },

            { "rota3", (70, 5.0, 1.3) }
        };

        private readonly Dictionary<string, List<(double, double)>> _routes
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
                    (41.3000, -7.7400), // Ponto inicial: Vila Real
                    (41.2950, -7.7700),
                    (41.2900, -7.8000),
                    (41.2850, -7.8300),
                    (41.2800, -7.8600),
                    (41.2750, -7.8900),
                    (41.2700, -7.9200),
                    (41.2650, -7.9500),
                    (41.2600, -7.9800),
                    (41.2550, -8.0100),
                    (41.2500, -8.0400),
                    (41.2450, -8.0700),
                    (41.2400, -8.1000),
                    (41.2300, -8.4000),
                    (41.2200, -8.5000),
                    (41.2100, -8.5500),
                    (41.2000, -8.6000),
                    (41.1900, -8.6200),
                    (41.1579, -8.6291) // Ponto final: Porto
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

        public double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
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

        public Routes(MqttService mqttService, LoggingService loggingService, SensorService sensorService, Dashboard dashboard, UpdateMethods updateMethods, BackgroundUpdated backgroundUpdated, Auxiliares auxiliares)
        {
            _mqttService = mqttService;
            _loggingService = loggingService;
            _sensorService = sensorService;
            _dashboard = dashboard;
            _updateMethods = updateMethods;
            _backgroundUpdated = backgroundUpdated;
            _auxiliares = auxiliares;
            _cts = new CancellationTokenSource();
        }

        public void SetUpdateMethods(UpdateMethods updateMethods)
        {
            _updateMethods = updateMethods;
        }

        public void SetBackgroundUpdated(BackgroundUpdated backgroundUpdated)
        {
            _backgroundUpdated = backgroundUpdated;
        }

        /// <summary>
        /// Método que inicia uma rota, converte a lista simples de pontos em lista de segmentos,
        /// e reseta os índices/variáveis de controle.
        /// </summary>
        public async Task StartRouteAsync(string routeName)
        {
            if (!_routeConfigs.ContainsKey(routeName))
            {
                Console.WriteLine($"Rota '{routeName}' não existe. As rotas disponíveis são: rota1, rota2, rota3.");
                return;
            }

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
            _currentRouteSegments = BuildRouteSegments(_currentRoute, routeName);
            _isRouteActive = true;
            // Reiniciamos os controles de segmentação
            _segmentIndex = 0;
            _distanceInSegment = 0.0;

            _isRouteActive = true;

            _accidentTriggered = false;


            Console.WriteLine($"Rota '{routeName}' iniciada com {_currentRoute.Count} pontos.");
            _ = Task.Run(() => UpdateRouteAsync(_sensorService)); // 🔄 Certifica que é iniciado em segundo plano!
            await _mqttService.PublishAsync("sim/route/status", $"started:{routeName}");

            // Log
            _loggingService.AddPerformanceLog(CreateLogEntry());
            _dashboard.DisplayDashboard();
        }

        public async Task StopRouteAsync()
        {
            if (!_isRouteActive)
            {
                Console.WriteLine("Não existe rota em andamento.");
                return;
            }

            _isRouteActive = false;
            Console.WriteLine("Rota parada.");
            await _mqttService.PublishAsync("sim/route/status", "stopped");

            _loggingService.AddPerformanceLog(CreateLogEntry());
        }

        /// <summary>
        /// Converte uma lista de (Lat, Lng) em uma lista de segmentos, cada segmento com start/end e distância.
        /// </summary>
        public List<RouteSegment> BuildRouteSegments(List<(double Lat, double Lng)> routePoints, string routeName)
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


        private double GenerateRandomSlope(double min, double max)
        {
            var rand = new Random();
            return rand.NextDouble() * (max - min) + min;
        }


        /// <summary>
        /// Calcula distância aproximada (em km) entre duas coordenadas lat/long usando fórmula de Haversine.
        /// </summary>
        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
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


        /// <summary>
        /// Método que atualiza a posição na rota a cada ciclo, de acordo com a velocidade e a distância do segmento.
        /// </summary>
        public async Task UpdateRouteAsync(SensorService sensorService)
        {


            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    if (SimulationState.IsMotorcycleOn && _isRouteActive &&
                        _currentRouteSegments.Count > 0 &&
                        _segmentIndex < _currentRouteSegments.Count)
                    {
                        // Obtém o segmento atual
                        var segment = _currentRouteSegments[_segmentIndex];

                        // Calcula a distância percorrida neste ciclo
                        double speedKmS = Math.Max(SimulationState.Speed, 5) / 3600.0;
                        double distanceThisLoop = speedKmS * 1.0; // 1 segundo por loop
                        _distanceInSegment += distanceThisLoop;

                        // Se a distância percorrida ultrapassar a distância do segmento, passa para o próximo
                        if (_distanceInSegment >= segment.Distance)
                        {
                            if (SimulationState.Speed > 0)
                            {
                                _distanceInSegment -= segment.Distance;
                                _segmentIndex++;
                                if (_segmentIndex >= _currentRouteSegments.Count && _isRouteActive)
                                {
                                    Console.WriteLine("[DEBUG] _isRouteActive definido como false. Confirma se a rota terminou realmente.");
                                    _isRouteActive = false;
                                    Console.WriteLine("Rota concluída (chegaste ao último ponto).");
                                    await _mqttService.PublishAsync("sim/route/status", "completed");
                                }
                            }
                            else
                            {
                                // Está parado (por ex. no semáforo), não avança na rota
                                Console.WriteLine("[INFO] Moto parada. Segmento não avança até retomar movimento.");
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
                            await _mqttService.PublishAsync("sim/gps/latitude", lat.ToString("F6"));
                            await _mqttService.PublishAsync("sim/gps/longitude", lng.ToString("F6"));

                            // -- Bloco de semáforo para rota1 --
                            // Bloco para simular paradas na rota1 (urbana)
                            if (_activeRouteName == "rota1")
                            {
                                // Se o progresso do segmento estiver próximo do final (>= 80%) e a velocidade for maior que 0
                                if (fraction >= 0.8 && SimulationState.Speed > 0)
                                {
                                    double randomVal = _rand.NextDouble();
                                    // 20% de chance de disparar o evento de semáforo vermelho
                                    if (randomVal < 0.2)
                                    {
                                        Console.WriteLine("[DEBUG] Evento de semáforo vermelho na rota1.");
                                        Console.WriteLine("[URBANO] Semáforo vermelho detectado! Decelerando para parada...");
                                        // Desacelera suavemente até 0 km/h
                                        await AdjustSpeedAsync(0);

                                        // Escolhe uma duração aleatória entre 3000 e 7000 ms para a parada
                                        int stopDuration = _rand.Next(3000, 7000);
                                        Console.WriteLine($"[URBANO] Parado por {stopDuration} ms no semáforo vermelho.");
                                        await Task.Delay(stopDuration);

                                        Console.WriteLine("[URBANO] Semáforo verde! Acelerando...");
                                        // Acelera suavemente até 20 km/h (velocidade mínima para retomar a rota)
                                        await _auxiliares.ApplyAccelerationAsync(20);
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
                                    _ = _updateMethods.AutoActivateIndicatorAsync(indicatorDirection, 3000);
                                }
                            }

                            // Adicione, somente para rota2, a lógica de ultrapassagem
                            if (_activeRouteName == "rota2" && SimulationState.Speed >= 80)
                            {
                                // Usamos um gerador de números aleatórios (pode ser um membro estático para evitar reinicializações)
                                double chance = _rand.NextDouble(); // valor entre 0 e 1
                                // Com 5% de chance, dispara a ultrapassagem
                                if (chance < 0.05)
                                {
                                    Console.WriteLine("[AUTO] Ultrapassagem iniciada pela esquerda na rota2!");
                                    // Aciona o pisca esquerdo
                                    await _updateMethods.UpdateIndicatorAsync("left");
                                    // Guarda a velocidade atual para retornar depois
                                    int velocidadeOriginal = SimulationState.Speed;
                                    // Aumenta a velocidade temporariamente (por exemplo, +20 km/h, respeitando o limite máximo)
                                    int velocidadeUltra = Math.Min(SimulationState.Speed + 20, MAX_SPEED);

                                    if (_routeConfigs.TryGetValue(_activeRouteName, out var config))
                                    {
                                        int maxPermitida = config.maxSpeed;
                                        if (velocidadeUltra > maxPermitida)
                                        {
                                            Console.WriteLine($"[ULTRAPASSAGEM] Velocidade pretendida ({velocidadeUltra}) excede o limite da rota ({maxPermitida}). Corrigindo para {maxPermitida} km/h.");
                                            velocidadeUltra = maxPermitida;
                                        }
                                    }
                                    await _auxiliares.ApplyAccelerationAsync(velocidadeUltra);
                                    // Mantém essa velocidade por um tempo (por exemplo, 3 segundos)
                                    await Task.Delay(3000);
                                    // Desliga o pisca
                                    await _updateMethods.UpdateIndicatorAsync("right");
                                    // Mantém essa velocidade por um tempo (por exemplo segundos)
                                    await Task.Delay(2000);
                                    // Desliga o pisca
                                    await _updateMethods.UpdateIndicatorAsync("none");
                                    // Retorna à velocidade original (ou, se preferir, decelera gradualmente)
                                    await _auxiliares.ApplyAccelerationAsync(velocidadeOriginal);
                                }
                            }

                            // -- Bloco de lógica de acidente para rota2 (já existente) --
                            if (!_accidentTriggered && _activeRouteName == "rota2" && SimulationState.Speed > 0)
                            {
                                double progress = (double)_segmentIndex / _currentRouteSegments.Count;
                                if (progress >= 0.5)
                                {
                                    _accidentTriggered = true;
                                    string[] sensors = { "blindspot", "pedestrian", "frontalcollision", "rearcollision" };
                                    string sensorToActivate = sensors[_rand.Next(sensors.Length)];
                                    Console.WriteLine($"**[ACIDENTE SIMULADO]** Disparando sensor {sensorToActivate}...");
                                    await sensorService.SendSensorMessagesAsync(sensorToActivate);
                                    if (sensorToActivate == "frontalcollision" || sensorToActivate == "pedestrian")
                                    {
                                        Console.WriteLine($"[ALERTA CRÍTICO] Acidente '{sensorToActivate}' ocorreu! Parando a moto...");
                                        SimulationState.Speed = 0;
                                        await _mqttService.PublishAsync("sim/speed", "0");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[ACIDENTE MENOR] Evento '{sensorToActivate}' detectado, mas moto continua em movimento.");
                                    }
                                }
                            }


                            // Bloco específico para a rota3 (estrada rural ou montanhosa)
                            if (_activeRouteName == "rota3")
                            {
                                // Se a inclinação for alta (subida íngreme) e a velocidade for maior que 30 km/h, reduza a velocidade
                                if (segment.Slope > 0.08 && SimulationState.Speed > 30)
                                {
                                    Console.WriteLine("[ROTA3] Subida íngreme detectada. Reduzindo velocidade para segurança.");
                                    await AdjustSpeedAsync(SimulationState.Speed - 10);

                                }
                                // Se estiver em descida e a velocidade estiver muito alta, limite a velocidade para 70 km/h
                                else if (segment.Slope < -0.02 && SimulationState.Speed > 70)
                                {
                                    Console.WriteLine("[ROTA3] Descida acentuada detectada. Ajustando velocidade para segurança.");
                                    await _auxiliares.ApplyAccelerationAsync(70);
                                }
                                // Simulação de trecho irregular ou estreito com uma chance de 10%
                                if (_rand.NextDouble() < 0.1 && SimulationState.Speed > 20)
                                {
                                    Console.WriteLine("[ROTA3] Trecho irregular ou estreito detectado. Reduzindo velocidade temporariamente.");
                                    await AdjustSpeedAsync(SimulationState.Speed - 10);

                                    // Mantém a velocidade reduzida por 2 segundos
                                    await Task.Delay(2000);
                                    Console.WriteLine("[ROTA3] Condições normais retomadas. Acelerando novamente.");
                                    // Retoma a aceleração (aqui você pode definir um valor mínimo para retomar, por exemplo, 30 km/h)
                                    await _auxiliares.ApplyAccelerationAsync(30);
                                }
                            }

                        }

                    }
                    // Aguarda 1 segundo antes de repetir o ciclo
                    await Task.Delay(1000, _cts.Token);

                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private async Task AdjustSpeedAsync(int targetSpeed)
        {
            int currentSpeed = SimulationState.Speed;

            if (targetSpeed < currentSpeed)
            {
                Console.WriteLine($"[TRAVAGEM] Reduzindo de {currentSpeed} km/h para {targetSpeed} km/h...");
                await _auxiliares.ApplyBrakingAsync(targetSpeed);
                Console.WriteLine($"[TRAVAGEM] Velocidade estabilizada em {SimulationState.Speed} km/h.");
            }
            else if (targetSpeed > currentSpeed)
            {
                Console.WriteLine($"[ACELERAÇÃO] Aumentando de {currentSpeed} km/h para {targetSpeed} km/h...");
                await _auxiliares.ApplyAccelerationAsync(targetSpeed);
                Console.WriteLine($"[ACELERAÇÃO] Velocidade estabilizada em {SimulationState.Speed} km/h.");
            }
            else
            {
                Console.WriteLine($"[VELOCIDADE] Já estás a {currentSpeed} km/h. Nenhuma alteração necessária.");
            }
        }



        private LogEntry CreateLogEntry()
        {
            return new LogEntry
            {
                Timestamp = DateTime.Now,
                MessageType = "Performance",
                Battery = SimulationState.Battery,
                Speed = SimulationState.Speed,
                Temperature = SimulationState.Temperature,
                Gear = SimulationState.Gear,
                Lights = SimulationState.Lights,
                Abs = SimulationState.Abs,
                IndicatorLeft = SimulationState.IndicatorLeft,
                IndicatorRight = SimulationState.IndicatorRight,
                DriveMode = SimulationState.DriveMode,
                MaxLights = SimulationState.MaxLights,
                DangerLights = SimulationState.DangerLights,
                TotalKilometers = SimulationState.TotalKilometers,
                Autonomy = SimulationState.Autonomy
            };
        }
    }

}