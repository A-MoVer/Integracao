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
                Console.WriteLine("Falha ao conectar ao broker MQTT. Encerrando programa.");
                return;
            }

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

            bool running = true;
            while (running)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line == null) continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                switch (parts[0].ToLower())
                {
                    case "start":
                        await StartMotorcycleAsync(mqttService, loggingService);
                        break;

                    case "stop":
                        await StopMotorcycleAsync(mqttService, loggingService);
                        break;

                    // Controlar carregamento
                    case "charge":
                        if (parts.Length == 2)
                        {
                            string chargeAction = parts[1].ToLower();
                            if (chargeAction == "start")
                                await StartChargingAsync(mqttService, loggingService);
                            else if (chargeAction == "stop")
                                await StopChargingAsync(mqttService, loggingService);
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
        static void DisplayCommands()
        {
            Console.Clear();
            Console.WriteLine("Comandos disponíveis:");

            if (!_isMotorcycleOn)
            {
                // Comandos disponíveis quando a motocicleta está desligada
                Console.WriteLine("Start:                   start");
                Console.WriteLine("Carregar bateria:        charge start (caso deseje carregar com a moto desligada)");
                Console.WriteLine("Sair do programa:        exit");
            }
            else
            {
                // Comandos disponíveis quando a motocicleta está ligada
                Console.WriteLine("% da Bateria:            battery -<valor>");
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
                Console.WriteLine("Carregar bateria:        charge start / charge stop");
                Console.WriteLine("Stop:                    stop");
                Console.WriteLine("Sair do programa:        exit");

                Console.WriteLine();
                Console.WriteLine("Restrições:");
                Console.WriteLine("- A marcha 'park' só pode ser selecionada quando a velocidade está em 0 km/h.");
                Console.WriteLine("- Quando a marcha está em 'front', a velocidade só pode aumentar.");
                Console.WriteLine("- Quando a marcha está em 'back', a velocidade só pode diminuir.");
                Console.WriteLine("- Se estiver a carregar (charge start), não pode alterar velocidade ou marcha.");
            }

            Console.WriteLine();
        }

        #region Start and Stop Methods

        /// <summary>
        /// Liga a motocicleta, liga as luzes automaticamente e atualiza o estado.
        /// </summary>
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

            // Atualizar comandos disponíveis
            DisplayCommands();
        }

        /// <summary>
        /// Desliga a motocicleta, desliga as luzes, define a velocidade para 0 e atualiza o estado.
        /// </summary>
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

        /// <summary>
        /// Tarefa em segundo plano: se estiver em carregamento, vai aumentando a bateria gradualmente até 100%.
        /// </summary>
        static async Task UpdateChargingAsync(MqttService mqttService, LoggingService loggingService, CancellationToken cancellationToken)
        {
            Console.WriteLine("Tarefa UpdateChargingAsync iniciada.");
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
            Console.WriteLine("Tarefa UpdateChargingAsync encerrada.");
        }

        #endregion

        #region Update Methods

        static async Task UpdateBatteryAsync(string command, MqttService mqttService, LoggingService loggingService)
        {
            if (!CommandParser.TryParseCommand(command, out int delta))
            {
                Console.WriteLine("Uso: battery -<valor>");
                return;
            }

            // Neste ponto, delta é negativo
            int finalValue = _battery + delta;
            finalValue = Math.Clamp(finalValue, MIN_BATTERY, MAX_BATTERY);

            Console.WriteLine($"Alterando bateria de {_battery}% para {finalValue}% ao longo de 5 segundos...");
            await AnimateValueChangeAsync(() => _battery, v => _battery = v, finalValue,
                                          5000, "sim/battery", "battery", mqttService, loggingService);
        }

        /// <summary>
        /// Atualiza a velocidade usando lógica de aceleração/desaceleração gradual.
        /// </summary>
        static async Task UpdateSpeedAsync(string command, MqttService mqttService, LoggingService loggingService)
        {
            // Verificar se a marcha atual permite alterar a velocidade
            if (_gear.Equals("neutral", StringComparison.OrdinalIgnoreCase) ||
                _gear.Equals("park", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Não é possível alterar a velocidade enquanto a marcha está em 'neutral' ou 'park'.");
                return;
            }

            if (!CommandParser.TryParseCommand(command, out int delta))
            {
                Console.WriteLine("Uso: speed +<valor> ou speed -<valor>");
                return;
            }

            // Restringir alterações de velocidade com base na marcha
            if (_gear.Equals("front", StringComparison.OrdinalIgnoreCase) && delta < 0)
            {
                Console.WriteLine("Não é possível diminuir a velocidade enquanto a marcha está em 'front'.");
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

            // Chama o método de aceleração/desaceleração gradual
            await ApplyAccelerationAsync(targetSpeed, mqttService, loggingService);
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
                newLights = true;
            else if (state == "off")
                newLights = false;
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

        #endregion

        #region Helper Methods

        /// <summary>
        /// Aplica aceleração ou desaceleração gradualmente até atingir a velocidade alvo.
        /// </summary>
        static async Task ApplyAccelerationAsync(int targetSpeed, MqttService mqttService, LoggingService loggingService)
        {
            // Exemplo de aceleração base (km/h por segundo), podes alterar consoante o modo
            double accelerationRate;

            // Ajustar a aceleração com base no modo de condução
            switch (_driveMode.ToLower())
            {
                case "eco":
                    accelerationRate = 3.0;  // km/h/s
                    break;
                case "sport":
                    accelerationRate = 8.0; // km/h/s
                    break;
                default:
                    accelerationRate = 6.0;  // km/h/s
                    break;
            }

            // Frequência de atualização (ms)
            int updateInterval = 200; // a cada 200 ms
            double stepsPerSecond = 1000.0 / updateInterval;
            // km/h a alterar por cada updateInterval
            double speedStep = accelerationRate / stepsPerSecond;

            bool isAccelerating = (targetSpeed > _speed);

            while (true)
            {
                // Verifica se já chegou ou passou do alvo
                if ((isAccelerating && _speed >= targetSpeed) ||
                    (!isAccelerating && _speed <= targetSpeed))
                {
                    break;
                }

                double nextSpeedDouble = (double)_speed + (isAccelerating ? speedStep : -speedStep);
                int nextSpeed = (int)Math.Round(nextSpeedDouble);

                // Forçar limites
                nextSpeed = Math.Clamp(nextSpeed, MIN_SPEED, MAX_SPEED);

                // Se passou do alvo, fixa no alvo
                if (isAccelerating && nextSpeed > targetSpeed)
                    nextSpeed = targetSpeed;
                else if (!isAccelerating && nextSpeed < targetSpeed)
                    nextSpeed = targetSpeed;

                // Atribui nova velocidade
                _speed = nextSpeed;

                // Publica e loga se houve alteração
                if (_speed != _lastSpeedPublished)
                {
                    _lastSpeedPublished = _speed;
                    await mqttService.PublishAsync("sim/speed", _speed.ToString());
                    Console.WriteLine($"Velocidade atualizada para {_speed} km/h.");

                    var logEntry = CreateLogEntry();
                    loggingService.AddPerformanceLog(logEntry);
                }

                // Se atingiu mesmo o alvo
                if (_speed == targetSpeed)
                {
                    break;
                }

                // Espera
                await Task.Delay(updateInterval);
            }
        }

        /// <summary>
        /// Anima a mudança de valor de uma propriedade ao longo de um período definido.
        /// (Mantemos para battery e temperature, mas não usamos mais para speed.)
        /// </summary>
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

        /// <summary>
        /// Aplica uma mudança de estado e publica a atualização via MQTT.
        /// </summary>
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

        /// <summary>
        /// Cria um LogEntry com o estado atual da moto.
        /// </summary>
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

        static async Task UpdateTimeAsync(MqttService mqttService, LoggingService loggingService, CancellationToken cancellationToken)
        {
            Console.WriteLine("Tarefa UpdateTimeAsync iniciada.");
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
            Console.WriteLine("Tarefa UpdateTimeAsync encerrada.");
        }

        static async Task UpdateTotalKilometersAsync(MqttService mqttService, LoggingService loggingService, CancellationToken cancellationToken)
        {
            Console.WriteLine("Tarefa UpdateTotalKilometersAsync iniciada.");
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
            Console.WriteLine("Tarefa UpdateTotalKilometersAsync encerrada.");
        }

        static async Task UpdateAutonomyAsync(MqttService mqttService, LoggingService loggingService, CancellationToken cancellationToken)
        {
            Console.WriteLine("Tarefa UpdateAutonomyAsync iniciada.");
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
            Console.WriteLine("Tarefa UpdateAutonomyAsync encerrada.");
        }

        /// <summary>
        /// Tarefa em segundo plano que reduz a bateria conforme a velocidade e o modo de condução.
        /// </summary>
        static async Task UpdateBatteryConsumptionAsync(MqttService mqttService, LoggingService loggingService, CancellationToken cancellationToken)
        {
            Console.WriteLine("Tarefa UpdateBatteryConsumptionAsync iniciada.");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Só consome bateria se estiver ligada, não carregando, e velocidade > 0
                    if (_isMotorcycleOn && !_isCharging && _speed > 0)
                    {
                        double baseConsumptionRate = 0.1;
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

                        double consumptionThisMinute = baseConsumptionRate * (_speed / 60.0);
                        int batteryConsumed = (int)Math.Round(consumptionThisMinute);

                        int newBattery = Math.Max(_battery - batteryConsumed, MIN_BATTERY);
                        if (newBattery != _battery)
                        {
                            _battery = newBattery;
                            await mqttService.PublishAsync("sim/battery", _battery.ToString());
                            Console.WriteLine($"[Consumo] Bateria reduzida para {_battery}% (speed={_speed} km/h, mode={_driveMode}).");

                            loggingService.AddPerformanceLog(CreateLogEntry());
                        }
                    }

                    // Esperar 5 segundos antes de recalcular
                    await Task.Delay(5000, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            Console.WriteLine("Tarefa UpdateBatteryConsumptionAsync encerrada.");
        }

        #endregion
    }
}
