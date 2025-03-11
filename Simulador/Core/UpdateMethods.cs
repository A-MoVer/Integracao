using System;
using System.Threading.Tasks;
using Simulador.Services;
using Simulador.Models;
using Simulador.Helpers;

namespace Simulador.Core
{
    public class UpdateMethods
    {
        private readonly MqttService _mqttService;
        private readonly LoggingService _loggingService;

        private readonly Dashboard _dashboard;

        private readonly Auxiliares _auxiliares;

        private Routes _routes;





        private const int MAX_BATTERY = 100;
        private const int MIN_BATTERY = 0;

        private const int MAX_SPEED = 200;
        private const int MIN_SPEED = 0;

        private const int MAX_TEMPERATURE = 100;
        private const int MIN_TEMPERATURE = -50;

        private int _previousSpeed = 0;

        private static readonly object _consoleLock = new object();

        private static int DashboardStartRow = 0;

        public UpdateMethods(MqttService mqttService, LoggingService loggingService, Dashboard dashboard, Auxiliares auxiliares, Routes routes)
        {
            _mqttService = mqttService;
            _loggingService = loggingService;
            _dashboard = dashboard;
            _auxiliares = auxiliares;
            _routes = routes;
        }

        public void SetRoutes(Routes routes)
        {
            _routes = routes;
        }


        public async Task UpdateDashboardContinuouslyAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (_consoleLock)
                {
                    // Guarda a posição do cursor antes de atualizar
                    int cursorLeft = Console.CursorLeft;
                    int cursorTop = Console.CursorTop;

                    // Evita que o dashboard limpe o prompt ao atualizar
                    if (SimulationState.IsMotorcycleOn)
                    {
                        Console.SetCursorPosition(0, DashboardStartRow);
                        _dashboard.DisplayDashboard();
                    }

                    // Restaura a posição do cursor para onde estava antes
                    Console.SetCursorPosition(cursorLeft, cursorTop);
                }
                await Task.Delay(500, cancellationToken);
            }

        }


        public async Task UpdateBatteryAsync(string command)
        {
            if (!CommandParser.TryParseCommand(command, out int delta))
            {
                Console.WriteLine("Uso: battery -<valor>");
                return;
            }

            int finalValue = SimulationState.Battery + delta;
            finalValue = Math.Clamp(finalValue, MIN_BATTERY, MAX_BATTERY);

            Console.WriteLine($"Alterando bateria de {SimulationState.Battery}% para {finalValue}% ao longo de 5 segundos...");
            await _auxiliares.AnimateValueChangeAsync(() => SimulationState.Battery, v => SimulationState.Battery = v, finalValue,
                                          5000, "sim/battery", "battery", _mqttService, _loggingService);
        }

        public async Task UpdateSpeedAsync(string command)
        {
            // Verificar se a marcha atual permite alterar a velocidade
            if (SimulationState.Gear.Equals("neutral", StringComparison.OrdinalIgnoreCase) ||
                SimulationState.Gear.Equals("park", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Não é possível alterar a velocidade enquanto a marcha está em 'neutral' ou 'park'.");
                return;
            }

            if (SimulationState.Gear.Equals("neutral", StringComparison.OrdinalIgnoreCase) && SimulationState.Speed > 20)
            {
                Console.WriteLine("[AUTO-GEAR] Velocidade acima de 20 km/h e marcha em neutral. Alterando para front.");
                await UpdateGearAsync("front");
            }


            if (!CommandParser.TryParseCommand(command, out int delta))
            {
                Console.WriteLine("Uso: speed +<valor> ou speed -<valor>");
                return;
            }
            else if (SimulationState.Gear.Equals("back", StringComparison.OrdinalIgnoreCase) && delta > 0)
            {
                Console.WriteLine("Não é possível aumentar a velocidade enquanto a marcha está em 'back'.");
                return;
            }

            int targetSpeed = SimulationState.Speed + delta;
            targetSpeed = Math.Clamp(targetSpeed, MIN_SPEED, MAX_SPEED);

            Console.WriteLine($"A {(delta > 0 ? "acelerar" : "desacelerar")} " +
                              $"de {SimulationState.Speed} km/h para {targetSpeed} km/h...");

            await _auxiliares.ApplyAccelerationAsync(targetSpeed);

            // Após a aceleração, se o ABS estiver desligado e a velocidade caiu bruscamente, exibir alerta
            if (!SimulationState.Abs) // se ABS está desligado
            {
                int speedDrop = _previousSpeed - SimulationState.Speed;
                if (speedDrop >= 30)
                {
                    Console.WriteLine("[ALERTA ABS] Possível derrapagem detectada devido a travagem brusca!");
                    // Aqui você pode disparar um sensor ou log adicional
                }
            }
            _previousSpeed = SimulationState.Speed;

            if (_routes._routeConfigs.TryGetValue(_routes._activeRouteName, out var config))
            {
                int routeMaxSpeed = config.maxSpeed;
                if (SimulationState.Speed > routeMaxSpeed)
                {
                    Console.WriteLine($"[LIMITE] Excedeu maxSpeed para {_routes._activeRouteName} (limite={routeMaxSpeed}). " +
                                    $"Forçando {SimulationState.Speed} => {routeMaxSpeed}");
                    SimulationState.Speed = routeMaxSpeed;  // Reduz forçadamente
                    await _mqttService.PublishAsync("sim/speed", SimulationState.Speed.ToString());


                    var logEntry = new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Topic = "sim/speed",
                        Payload = SimulationState.Speed.ToString(),
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

                    _loggingService.AddPerformanceLog(logEntry);
                }

                // Se for rota2 e speed > 120 => exibe ALERT
                if (_routes._activeRouteName == "rota2" && SimulationState.Speed > 120)
                {
                    Console.WriteLine("[ALERTA] Excedeu limite (120 km/h) na rota2!");
                }
            }
        }

        public async Task UpdateTemperatureAsync(string command)
        {
            if (!CommandParser.TryParseCommand(command, out int delta))
            {
                Console.WriteLine("Uso: temp +<valor> ou temp -<valor>");
                return;
            }

            int finalValue = SimulationState.Temperature + delta;
            finalValue = Math.Clamp(finalValue, MIN_TEMPERATURE, MAX_TEMPERATURE);

            Console.WriteLine($"Alterando temperatura de {SimulationState.Temperature}°C para {finalValue}°C ao longo de 5 segundos...");
            await _auxiliares.AnimateValueChangeAsync(() => SimulationState.Temperature, v => SimulationState.Temperature = v, finalValue,
                                          5000, "sim/temperature", "temperature", _mqttService, _loggingService);
        }

        public async Task UpdateGearAsync(string position)
        {
            position = position.ToLower();
            if (position == "front" || position == "back" || position == "neutral")
            {
                await _auxiliares.ApplyStateChangeAsync(() => SimulationState.Gear = position,
                                            "Marcha", "sim/gear", position,
                                            _mqttService, _loggingService);
            }
            else if (position == "park")
            {
                if (SimulationState.Speed == 0)
                {
                    await _auxiliares.ApplyStateChangeAsync(() => SimulationState.Gear = position,
                                                "Marcha", "sim/gear", position,
                                                _mqttService, _loggingService);
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



        public async Task UpdateIndicatorAsync(string direction)
        {
            direction = direction.ToLower();
            bool left = SimulationState.IndicatorLeft;
            bool right = SimulationState.IndicatorRight;

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
            await _auxiliares.ApplyStateChangeAsync(() =>
            {
                SimulationState.IndicatorLeft = left;
                SimulationState.IndicatorRight = right;
            },
            "Pisca Esquerdo", "sim/indicator_left", left.ToString(),
            _mqttService, _loggingService);

            await _auxiliares.ApplyStateChangeAsync(() =>
            {
                SimulationState.IndicatorLeft = left;
                SimulationState.IndicatorRight = right;
            },
            "Pisca Direito", "sim/indicator_right", right.ToString(),
            _mqttService, _loggingService);
        }

        public async Task UpdateLightsAsync(string state)
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

            await _auxiliares.ApplyStateChangeAsync(() => SimulationState.Lights = newLights,
                                        "Luzes médias", "sim/lights", newLights.ToString(),
                                        _mqttService, _loggingService);
        }

        public async Task UpdateAbsAsync(string state)
        {
            state = state.ToLower();
            bool newAbs;
            if (state == "on")
            {
                newAbs = true;
            }
            else if (state == "off")
            {
                newAbs = false;
            }
            else
            {
                Console.WriteLine("Uso: abs on | abs off");
                return;
            }

            await _auxiliares.ApplyStateChangeAsync(() => SimulationState.Abs = newAbs,
                                        "ABS", "sim/abs", newAbs.ToString(),
                                        _mqttService, _loggingService);
        }

        public async Task UpdateModeAsync(string mode)
        {
            mode = mode.ToLower();
            if (mode == "standard" || mode == "eco" || mode == "sport")
            {
                await _auxiliares.ApplyStateChangeAsync(() => SimulationState.DriveMode = mode,
                                            "Modo de condução", "sim/drive_mode", mode,
                                            _mqttService, _loggingService);
            }
            else
            {
                Console.WriteLine("Uso: mode standard | mode eco | mode sport");
            }
        }

        public async Task UpdateMaxLightsAsync(string state)
        {
            state = state.ToLower();
            bool newMax;
            if (state == "on")
            {
                newMax = true;
            }
            else if (state == "off")
            {
                newMax = false;
            }
            else
            {
                Console.WriteLine("Uso: maxlights on | maxlights off");
                return;
            }

            await _auxiliares.ApplyStateChangeAsync(() => SimulationState.MaxLights = newMax,
                                        "Luzes máximas", "sim/maxlights", newMax.ToString(),
                                        _mqttService, _loggingService);
        }

        public async Task UpdateDangerAsync(string state)
        {
            state = state.ToLower();
            bool newDanger;
            if (state == "on")
            {
                newDanger = true;
            }
            else if (state == "off")
            {
                newDanger = false;
            }
            else
            {
                Console.WriteLine("Uso: danger on | danger off");
                return;
            }

            await _auxiliares.ApplyStateChangeAsync(() => SimulationState.DangerLights = newDanger,
                                        "Luzes de perigo", "sim/danger-lights", newDanger.ToString(),
                                        _mqttService, _loggingService);
        }

        public async Task AutoActivateIndicatorAsync(string direction, int durationMs)
        {
            // Ativa o pisca (left ou right)
            await UpdateIndicatorAsync(direction);
            // Espera o tempo definido (ex.: 3000 ms)
            await Task.Delay(durationMs);
            // Desliga o pisca
            await UpdateIndicatorAsync("none");
        }


    }
}