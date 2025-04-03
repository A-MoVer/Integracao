using System;
using System.Threading.Tasks;
using Simulador.Services;
using Simulador.Models;
using Simulador.Helpers;

namespace Simulador.Core
{
    public class Auxiliares
    {
        private readonly MqttService _mqttService;
        private readonly LoggingService _loggingService;

        private readonly Dashboard _dashboard;


        public Auxiliares(MqttService mqttService, LoggingService loggingService, Dashboard dashboard)
        {
            _mqttService = mqttService;
            _loggingService = loggingService;
            _dashboard = dashboard;
        }

        private int _lastSpeedPublished = SimulationState.Speed;

        private int _lastBatteryPublished = SimulationState.Battery;

        private int _lastTemperaturePublished = SimulationState.Temperature;

        private const int MAX_SPEED = 200;
        private const int MIN_SPEED = 0;

        public async Task PublishAllStatesAsync()
        {
            await _mqttService.PublishAsync("sim/battery", SimulationState.Battery.ToString());
            await _mqttService.PublishAsync("sim/speed", SimulationState.Speed.ToString());
            await _mqttService.PublishAsync("sim/gear", SimulationState.Gear);
            await _mqttService.PublishAsync("sim/lights", SimulationState.Lights.ToString());
            await _mqttService.PublishAsync("sim/abs", SimulationState.Abs.ToString());
            await _mqttService.PublishAsync("sim/indicator_left", SimulationState.IndicatorLeft.ToString());
            await _mqttService.PublishAsync("sim/indicator_right", SimulationState.IndicatorRight.ToString());
            await _mqttService.PublishAsync("sim/drive_mode", SimulationState.DriveMode);
            await _mqttService.PublishAsync("sim/maxlights", SimulationState.MaxLights.ToString());
            await _mqttService.PublishAsync("sim/danger-lights", SimulationState.DangerLights.ToString());
            await _mqttService.PublishAsync("sim/total_kilometers", SimulationState.TotalKilometers.ToString("F2"));
            await _mqttService.PublishAsync("sim/autonomy", SimulationState.Autonomy.ToString("F2"));
            await _mqttService.PublishAsync("sim/brake", SimulationState.Brake.ToString());
        }

        private static Random _rand = new Random();

        public async Task ApplyAccelerationAsync(int targetSpeed)
        {
            // Exemplo de aceleração base (km/h por segundo) conforme modo de condução
            double accelerationRate;
            switch (SimulationState.DriveMode.ToLower())
            {
                case "eco": accelerationRate = 3.0; break;
                case "sport": accelerationRate = 8.0; break;
                default: accelerationRate = 6.0; break;
            }

            int updateInterval = 200; // atualiza a cada 200 ms
            double stepsPerSecond = 1000.0 / updateInterval;
            double speedStep = accelerationRate / stepsPerSecond;

            bool isAccelerating = (targetSpeed > SimulationState.Speed);

            while (true)
            {
                if ((isAccelerating && SimulationState.Speed >= targetSpeed) ||
                    (!isAccelerating && SimulationState.Speed <= targetSpeed))
                {
                    break;
                }

                // Calcula a nova velocidade baseada na aceleração
                double nextSpeedDouble = SimulationState.Speed + (isAccelerating ? speedStep : -speedStep);
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

                SimulationState.Speed = nextSpeed;

                if (SimulationState.Speed != _lastSpeedPublished)
                {
                    _lastSpeedPublished = SimulationState.Speed;
                    _dashboard.DisplayDashboard();
                    await _mqttService.PublishAsync("sim/speed", SimulationState.Speed.ToString());
                    var logEntry = CreateLogEntry();
                    _loggingService.AddPerformanceLog(logEntry);
                }

                if (SimulationState.Speed == targetSpeed)
                {
                    break;
                }

                await Task.Delay(updateInterval);
            }
        }


        public async Task AnimateValueChangeAsync(
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

        public async Task ApplyStateChangeAsync(
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


        public async Task ApplyBrakingAsync(int targetSpeed)
        {
            double brakingRate;
            switch (SimulationState.DriveMode.ToLower())
            {
                case "eco": brakingRate = 5.0; break;
                case "sport": brakingRate = 10.0; break;
                default: brakingRate = 7.0; break;
            }

            int updateInterval = 200; // ms
            double stepsPerSecond = 1000.0 / updateInterval;
            double speedStep = brakingRate / stepsPerSecond;
            SimulationState.Brake = true;
            await _mqttService.PublishAsync("sim/brake", SimulationState.Brake.ToString());

            while (SimulationState.Speed > targetSpeed)
            {
                double nextSpeedDouble = SimulationState.Speed - speedStep;
                int nextSpeed = (int)Math.Round(nextSpeedDouble);
                nextSpeed = Math.Clamp(nextSpeed, 0, 200);

                SimulationState.Speed = nextSpeed;

                if (SimulationState.Speed != _lastSpeedPublished)
                {
                    _lastSpeedPublished = SimulationState.Speed;
                    _dashboard.DisplayDashboard();
                    await _mqttService.PublishAsync("sim/speed", SimulationState.Speed.ToString());
                    _loggingService.AddPerformanceLog(CreateLogEntry());
                }

                if (SimulationState.Speed <= targetSpeed)
                {
                    break;
                }

                await Task.Delay(updateInterval);
            }
            SimulationState.Brake = false;
            await _mqttService.PublishAsync("sim/brake", SimulationState.Brake.ToString());
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