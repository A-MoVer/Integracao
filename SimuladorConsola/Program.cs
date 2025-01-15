using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simulador.Models;
using Simulador.Services;
using Simulador.Helpers;

namespace Simulador
{
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

        // Limites para os estados
        private const int MAX_BATTERY = 100;
        private const int MIN_BATTERY = 0;
        private const int MAX_SPEED = 200; // Exemplo: velocidade máxima de 200 km/h
        private const int MIN_SPEED = 0;
        private const int MAX_TEMPERATURE = 100; // Exemplo: temperatura máxima de 100°C
        private const int MIN_TEMPERATURE = -50; // Exemplo: temperatura mínima de -50°C

        // Variáveis para armazenar o último valor publicado
        private static int _lastBatteryPublished = _battery;
        private static int _lastSpeedPublished = _speed;
        private static int _lastTemperaturePublished = _temperature;

        // Mapeamento dos sensores para ArbitrationId e AlgorithmID
        private static readonly Dictionary<string, (int ArbitrationId, string AlgorithmID)> sensorMappings = new Dictionary<string, (int, string)>(StringComparer.OrdinalIgnoreCase)
        {
            { "blindspot", (0x100, "BlindSpotDetection") },
            { "pedestrian", (0x101, "PedestrianDetection") },
            { "frontalcollision", (0x102, "FrontalCollisionDetection") },
            { "rearcollision", (0x103, "RearCollisionDetection") },
            // Adicione outros sensores conforme necessário
        };

        // Token de cancelamento para tarefas assíncronas
        private static CancellationTokenSource? _cancellationTokenSource;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Iniciando cliente MQTT na console...");

            // Inicializar os serviços
            var loggingService = new LoggingService();
            var mqttService = new MqttService("192.168.28.96", 1884, "Simulador");
            var sensorService = new SensorService(sensorMappings, mqttService, loggingService);

            try
            {
                await mqttService.ConnectAsync();
            }
            catch
            {
                return;
            }

            // Iniciar tarefas assíncronas para atualizar tempo, quilometragem e autonomia
            _ = UpdateTimeAsync(mqttService, loggingService);
            _ = UpdateTotalKilometersAsync(mqttService, loggingService);
            _ = UpdateAutonomyAsync(mqttService, loggingService);

            // Inicializar o token de cancelamento
            _cancellationTokenSource = new CancellationTokenSource();

            DisplayCommands();

            bool running = true;
            while (running)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line == null)
                {
                    continue;
                }

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                switch (parts[0].ToLower())
                {
                    case "battery":
                        if (parts.Length == 2)
                            await UpdateBatteryAsync(parts[1], mqttService, loggingService);
                        break;

                    case "speed":
                        if (parts.Length == 2)
                            await UpdateSpeedAsync(parts[1], mqttService, loggingService);
                        break;

                    case "temp":
                        if (parts.Length == 2)
                            await UpdateTemperatureAsync(parts[1], mqttService, loggingService);
                        break;

                    case "gear":
                        if (parts.Length == 2)
                            await UpdateGearAsync(parts[1], mqttService, loggingService);
                        break;

                    case "indicator":
                        if (parts.Length == 2)
                            await UpdateIndicatorAsync(parts[1], mqttService, loggingService);
                        break;

                    case "lights":
                        if (parts.Length == 2)
                            await UpdateLightsAsync(parts[1], mqttService, loggingService);
                        break;

                    case "abs":
                        if (parts.Length == 2)
                            await UpdateAbsAsync(parts[1], mqttService, loggingService);
                        break;

                    case "mode":
                        if (parts.Length == 2)
                            await UpdateModeAsync(parts[1], mqttService, loggingService);
                        break;

                    case "maxlights":
                        if (parts.Length == 2)
                            await UpdateMaxLightsAsync(parts[1], mqttService, loggingService);
                        break;

                    case "danger":
                        if (parts.Length == 2)
                            await UpdateDangerAsync(parts[1], mqttService, loggingService);
                        break;

                    case "send":
                        if (parts.Length == 2)
                            await sensorService.SendSensorMessagesAsync(parts[1]);
                        break;

                    case "exit":
                        running = false;
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

        static void DisplayCommands()
        {
            Console.WriteLine("Comandos disponíveis:");
            Console.WriteLine("% da Bateria:            battery +<valor> / battery -<valor>");
            Console.WriteLine("Velocidade:              speed +<valor> / speed -<valor>");
            Console.WriteLine("Temperatura da moto:     temp +<valor> / temp -<valor>");
            Console.WriteLine("Marcha:                  gear front / gear back / gear neutral / gear park");
            Console.WriteLine("Piscas:                  indicator left / indicator right / indicator none");
            Console.WriteLine("Luzes médias:            lights on / lights off");
            Console.WriteLine("ABS:                     abs on / abs off");
            Console.WriteLine("Modo de condução:        mode standard / mode eco / mode sport");
            Console.WriteLine("Luzes máximas:           maxlights on / maxlights off");
            Console.WriteLine("Luzes de Perigo:         danger on / danger off");
            Console.WriteLine("Enviar Sensor:           send <sensor>");
            Console.WriteLine("Sair do programa:        exit");
            Console.WriteLine();
        }

        #region Update Methods

        static async Task UpdateBatteryAsync(string command, MqttService mqttService, LoggingService loggingService)
        {
            if (!CommandParser.TryParseCommand(command, out int delta))
            {
                Console.WriteLine("Uso: battery +<valor> ou battery -<valor>");
                return;
            }

            int finalValue = _battery + delta;
            finalValue = Math.Clamp(finalValue, MIN_BATTERY, MAX_BATTERY);

            Console.WriteLine($"Alterando bateria de {_battery}% para {finalValue}% ao longo de 5 segundos...");
            await AnimateValueChangeAsync(() => _battery, v => _battery = v, finalValue, 5000, "sim/battery", "battery", mqttService, loggingService);
        }

        static async Task UpdateSpeedAsync(string command, MqttService mqttService, LoggingService loggingService)
        {
            if (!CommandParser.TryParseCommand(command, out int delta))
            {
                Console.WriteLine("Uso: speed +<valor> ou speed -<valor>");
                return;
            }

            int finalValue = _speed + delta;
            finalValue = Math.Clamp(finalValue, MIN_SPEED, MAX_SPEED);

            Console.WriteLine($"Alterando velocidade de {_speed} km/h para {finalValue} km/h ao longo de 5 segundos...");
            await AnimateValueChangeAsync(() => _speed, v => _speed = v, finalValue, 5000, "sim/speed", "speed", mqttService, loggingService);
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
            await AnimateValueChangeAsync(() => _temperature, v => _temperature = v, finalValue, 5000, "sim/temperature", "temperature", mqttService, loggingService);
        }

        static async Task UpdateGearAsync(string position, MqttService mqttService, LoggingService loggingService)
        {
            position = position.ToLower();
            if (position == "front" || position == "back" || position == "neutral" || position == "park")
            {
                await ApplyStateChangeAsync(() => _gear = position, "Marcha", "sim/gear", position, mqttService, loggingService);
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
            "Pisca Esquerdo",
            "sim/indicator_left",
            left.ToString(),
            mqttService,
            loggingService);

            await ApplyStateChangeAsync(() =>
            {
                _indicatorLeft = left;
                _indicatorRight = right;
            },
            "Pisca Direito",
            "sim/indicator_right",
            right.ToString(),
            mqttService,
            loggingService);
        }

        static async Task UpdateLightsAsync(string state, MqttService mqttService, LoggingService loggingService)
        {
            state = state.ToLower();
            bool newLights;
            if (state == "on")
                newLights = true;
            else if (state == "off")
                newLights = false;
            else
            {
                Console.WriteLine("Uso: lights on | lights off");
                return;
            }

            await ApplyStateChangeAsync(() => _lights = newLights, "Luzes médias", "sim/lights", newLights.ToString(), mqttService, loggingService);
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

            await ApplyStateChangeAsync(() => _abs = newAbs, "ABS", "sim/abs", newAbs.ToString(), mqttService, loggingService);
        }

        static async Task UpdateModeAsync(string mode, MqttService mqttService, LoggingService loggingService)
        {
            mode = mode.ToLower();
            if (mode == "standard" || mode == "eco" || mode == "sport")
            {
                await ApplyStateChangeAsync(() => _driveMode = mode, "Modo de condução", "sim/drive_mode", mode, mqttService, loggingService);
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

            await ApplyStateChangeAsync(() => _maxLights = newMax, "Luzes máximas", "sim/maxlights", newMax.ToString(), mqttService, loggingService);
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

            await ApplyStateChangeAsync(() => _dangerLights = newDanger, "Luzes de perigo", "sim/danger-lights", newDanger.ToString(), mqttService, loggingService);
        }

        #endregion

        #region Helper Methods

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

                // Verificar se o valor mudou em relação ao último valor publicado
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

            // Adicionar registro após a animação completa
            var performanceLogEntry = new LogEntry
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

            loggingService.AddPerformanceLog(performanceLogEntry);
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

            // Adicionar registro após a alteração
            var performanceLogEntry = new LogEntry
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

            loggingService.AddPerformanceLog(performanceLogEntry);
        }

        static async Task UpdateTimeAsync(MqttService mqttService, LoggingService loggingService)
        {
            while (true)
            {
                var currentTime = DateTime.Now.ToString("hh:mm tt");
                await mqttService.PublishAsync("sim/time", currentTime);

                // Adicionar registro após atualizar o tempo
                var performanceLogEntry = new LogEntry
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

                loggingService.AddPerformanceLog(performanceLogEntry);

                await Task.Delay(60000); // Atualizar a cada minuto
            }
        }

        static async Task UpdateTotalKilometersAsync(MqttService mqttService, LoggingService loggingService)
        {
            while (true)
            {
                _totalKilometers += _speed / 3600.0; // Supondo que a velocidade está em km/h e atualização a cada minuto
                await mqttService.PublishAsync("sim/total_kilometers", _totalKilometers.ToString("F2"));

                // Adicionar registro após atualizar a quilometragem
                var performanceLogEntry = new LogEntry
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

                loggingService.AddPerformanceLog(performanceLogEntry);

                await Task.Delay(60000); // Atualizar a cada minuto
            }
        }

        static async Task UpdateAutonomyAsync(MqttService mqttService, LoggingService loggingService)
        {
            while (true)
            {
                _autonomy = _battery * 10.0 / (_speed + 1.0);
                await mqttService.PublishAsync("sim/autonomy", _autonomy.ToString("F2"));

                // Adicionar registro após atualizar a autonomia
                var performanceLogEntry = new LogEntry
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

                loggingService.AddPerformanceLog(performanceLogEntry);

                await Task.Delay(60000); // Atualizar a cada minuto
            }
        }

        #endregion
    }
}
