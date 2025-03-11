using System;
using System.Threading.Tasks;
using Simulador.Services;
using Simulador.Models;

namespace Simulador.Core
{
    public class BackgroundUpdated
    {
        private readonly MqttService _mqttService;
        private readonly LoggingService _loggingService;
        private readonly StartAndUpdate _startAndUpdate;
        private readonly SensorService _sensorService;

        private readonly UpdateMethods _updateMethods;
        private readonly Dashboard _dashboard;

        private readonly Routes _routes;
        private CancellationTokenSource? _cts;

        private const int MIN_BATTERY = 0;

        private Random rand = new Random();



        public BackgroundUpdated(MqttService mqttService, LoggingService loggingService, SensorService sensorService, CancellationTokenSource? cts, StartAndUpdate startAndUpdate, Dashboard dashboard, UpdateMethods updateMethods, Routes routes)
        {
            _mqttService = mqttService;
            _loggingService = loggingService;
            _sensorService = sensorService;
            _cts = cts;
            _startAndUpdate = startAndUpdate;
            _dashboard = dashboard;
            _updateMethods = updateMethods;
            _routes = routes;
        }

        public async Task AutoSimulateAsync(string routeName)
        {
            Console.WriteLine($"[Auto] Iniciando simulação automática para a rota '{routeName}'...");

            if (_cts == null || _cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }

            _ = Task.Run(() => _updateMethods.UpdateDashboardContinuouslyAsync(_cts.Token));


            // 1. Liga moto se estiver desligada
            if (!SimulationState.IsMotorcycleOn)
            {
                await _startAndUpdate.StartMotorcycleAsync();
                _dashboard.DisplayDashboard();
                await Task.Delay(500, _cts.Token);
            }

            // 2. Inicia a rota
            await _routes.StartRouteAsync(routeName);
            _dashboard.DisplayDashboard();
            await Task.Delay(500, _cts.Token);
            _ = Task.Run(() => _routes.UpdateRouteAsync(_sensorService));

            // 3. Marcha front
            if (!SimulationState.Gear.Equals("front", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[Auto] Engrenando marcha frontal...");
                await _updateMethods.UpdateGearAsync("front");
                _dashboard.DisplayDashboard();
                await Task.Delay(500, _cts.Token);

            }

            // 4. Liga luzes
            Console.WriteLine("[Auto] Ligando luzes...");
            await _updateMethods.UpdateLightsAsync("on");
            _dashboard.DisplayDashboard();

            // >>> AQUI ajustamos a “aceleração inicial” de acordo com a rota
            if (_routes._routeConfigs.TryGetValue(routeName, out var config))
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
                string speedCommand = desiredSpeed > SimulationState.Speed
                    ? "+" + (desiredSpeed - SimulationState.Speed)
                    : "-" + (SimulationState.Speed - desiredSpeed);
                await _updateMethods.UpdateSpeedAsync(speedCommand);

            }
            else
            {
                // se a rota não estiver no dicionário, mantém como estava
                Console.WriteLine("[Auto] Rota não configurada, usando default => 60 km/h");
                await _updateMethods.UpdateSpeedAsync("+60");
            }

            await Task.Delay(1000, _cts.Token);

            // Ajusta modo de condução
            // Ex.: se for rota2, forçamos sport; se for rota1, forçamos eco; etc.
            if (routeName == "rota1")
            {
                Console.WriteLine("[Auto] Rota1 => modo 'eco' (urbano)...");
                await _updateMethods.UpdateModeAsync("eco");
                _dashboard.DisplayDashboard();
            }
            else if (routeName == "rota2")
            {
                Console.WriteLine("[Auto] Rota2 => modo 'sport' (autoestrada)...");
                await _updateMethods.UpdateModeAsync("sport");
                _dashboard.DisplayDashboard();
            }
            else
            {
                Console.WriteLine("[Auto] Rota3 => modo 'standard' (rural)...");
                await _updateMethods.UpdateModeAsync("standard");
                _dashboard.DisplayDashboard();
            }

            // Loop enquanto a rota estiver ativa
            while (_routes._isRouteActive && !_cts.IsCancellationRequested)
            {

                // Exemplo: se a rota for rota2 (autoestrada), tenta manter velocidade alta
                if (routeName == "rota2" && SimulationState.Speed < 80)
                {
                    int dif = 80 - SimulationState.Speed;
                    Console.WriteLine($"[Auto] Rota2 => Subir para 80 km/h...");
                    await _updateMethods.UpdateSpeedAsync("+" + dif);
                }

                // se for rota1 e velocidade > 50, reduz
                if (routeName == "rota1" && SimulationState.Speed > 50)
                {
                    int dif = SimulationState.Speed - 50;
                    Console.WriteLine("[Auto] Rota1 => Reduzindo para 50 km/h no trânsito urbano...");
                    await _updateMethods.UpdateSpeedAsync("-" + dif);
                }

                // se for rota3 e velocidade > 70, reduz
                if (routeName == "rota3" && SimulationState.Speed > 70)
                {
                    int dif = SimulationState.Speed - 70;
                    Console.WriteLine("[Auto] Rota3 => Estrada rural => Reduzindo para 70 km/h...");
                    await _updateMethods.UpdateSpeedAsync("-" + dif);
                }




                await Task.Delay(2000, _cts.Token);
            }

            if (_routes._isRouteActive)
            {
                Console.WriteLine("[Auto] Rota ainda em andamento. Simulação não deve terminar prematuramente.");
            }
            else
            {
                Console.WriteLine("[Auto] Rota finalizada. Mudando para modo standard e desligando moto...");
                await _updateMethods.UpdateModeAsync("standard");

                if (SimulationState.IsMotorcycleOn) // 🔍 Evita desligar se a moto ainda estiver ativa
                {
                    await _startAndUpdate.StopMotorcycleAsync();
                }
                Console.Clear();
                _dashboard.DisplayDashboard();
            }

        }


        public async Task UpdateTimeAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var currentTime = DateTime.Now.ToString("hh:mm tt");
                await _mqttService.PublishAsync("sim/time", currentTime);

                _loggingService.AddPerformanceLog(CreateLogEntry());

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

        public async Task UpdateTotalKilometersAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (SimulationState.IsMotorcycleOn)
                {
                    // velocidade em km/h => km por minuto
                    SimulationState.TotalKilometers += SimulationState.Speed / 60.0;
                    await _mqttService.PublishAsync("sim/total_kilometers", SimulationState.TotalKilometers.ToString("F2"));

                    _loggingService.AddPerformanceLog(CreateLogEntry());
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

        public async Task UpdateAutonomyAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (SimulationState.IsMotorcycleOn)
                {
                    if (SimulationState.Speed > 0)
                    {
                        SimulationState.Autonomy = SimulationState.Battery * 10.0 / SimulationState.Speed;
                        await _mqttService.PublishAsync("sim/autonomy", SimulationState.Autonomy.ToString("F2"));
                    }
                    else
                    {
                        // Parado => "infinita"
                        await _mqttService.PublishAsync("sim/autonomy", "∞");
                    }

                    // Publicar novamente para garantir
                    await _mqttService.PublishAsync("sim/autonomy", SimulationState.Autonomy.ToString("F2"));

                    _loggingService.AddPerformanceLog(CreateLogEntry());
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
        public async Task FluctuateSpeedAsync(CancellationToken cancellationToken)
        {

            while (!cancellationToken.IsCancellationRequested)
            {
                // Se a moto estiver ligada, não estiver carregando e tiver velocidade > 0:
                if (SimulationState.IsMotorcycleOn && !SimulationState.IsCharging && SimulationState.Speed > 0)
                {
                    // Gera um delta aleatório entre -2 e +2 km/h (você pode ajustar esse valor)
                    int delta = rand.Next(-2, 3); // -2, -1, 0, 1 ou 2
                    // Aplica a variação, garantindo que a velocidade não fique negativa
                    int newSpeed = Math.Max(SimulationState.Speed + delta, 0);
                    // Atualiza a velocidade (aqui você pode também forçar que não ultrapasse algum valor máximo se necessário)
                    SimulationState.Speed = newSpeed;
                    _dashboard.DisplayDashboard();
                    // Publica a nova velocidade
                    await _mqttService.PublishAsync("sim/speed", SimulationState.Speed.ToString());
                }
                // Aguarda 1 segundo antes de repetir
                await Task.Delay(1000, cancellationToken);
            }
        }


        public async Task UpdateBatteryConsumptionAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (SimulationState.IsMotorcycleOn && !SimulationState.IsCharging)
                    {
                        // 1. Consumo base dependendo do modo de condução
                        double baseConsumptionRate;
                        switch (SimulationState.DriveMode.ToLower())
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
                        if (_routes._routeConfigs.TryGetValue(_routes._activeRouteName, out var config))
                        {
                            extraFactor = config.extraConsumption;
                        }

                        // 3. Calcular consumo base (consumo por minuto proporcional à velocidade)
                        double consumptionThisMinute = baseConsumptionRate * (SimulationState.Speed / 60.0) * extraFactor;

                        // 4. Se estivermos na rota3, incorporar o efeito da inclinação
                        double slopeFactor = 1.0;
                        if (_routes._activeRouteName.Equals("rota3", StringComparison.OrdinalIgnoreCase))
                        {
                            if (_routes._segmentIndex < _routes._currentRouteSegments.Count)
                            {
                                double slope = _routes._currentRouteSegments[_routes._segmentIndex].Slope;
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
                        int newBattery = Math.Max(SimulationState.Battery - batteryConsumed, MIN_BATTERY);
                        if (newBattery != SimulationState.Battery)
                        {
                            SimulationState.Battery = newBattery;
                            await _mqttService.PublishAsync("sim/battery", SimulationState.Battery.ToString());
                            Console.WriteLine($"[Consumo] Bateria => {SimulationState.Battery}% (speed={SimulationState.Speed} km/h, mode={SimulationState.DriveMode}, route={_routes._activeRouteName}, slopeFactor={slopeFactor:0.00}).");
                            _loggingService.AddPerformanceLog(CreateLogEntry());
                        }

                        // 6. Verifica se a bateria está esgotada
                        if (SimulationState.Battery <= MIN_BATTERY)
                        {
                            Console.WriteLine("[ALERTA] Bateria baixa! Moto em modo de segurança.");

                            if (_routes._isRouteActive)
                            {
                                Console.WriteLine("[ALERTA] A moto está numa rota ativa, reduzindo velocidade para economizar bateria.");
                                SimulationState.Speed = Math.Max(SimulationState.Speed - 10, 0);
                            }
                            else
                            {
                                Console.WriteLine("[ALERTA] Bateria esgotada! Desligando moto...");
                                SimulationState.Speed = 0;
                                await _mqttService.PublishAsync("sim/speed", "0");
                                SimulationState.IsMotorcycleOn = false;  // 🔍 Apenas desligamos se não estiver numa rota ativa
                            }
                        }

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


        public LogEntry CreateLogEntry()
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