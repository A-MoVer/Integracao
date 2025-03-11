// ChargingMethods.cs
using System;
using System.Threading.Tasks;
using Simulador.Services;
using Simulador.Models;

namespace Simulador.Core
{
    public class ChargingMethods
    {
        private readonly MqttService _mqttService;
        private readonly LoggingService _loggingService;
        private readonly Dashboard _dashboard;
        private int _lastBatteryPublished = SimulationState.Battery;

        private const int MAX_BATTERY = 100;

        public ChargingMethods(MqttService mqttService, LoggingService loggingService, Dashboard dashboard)
        {
            _mqttService = mqttService;
            _loggingService = loggingService;
            _dashboard = dashboard;
        }

        public async Task StartChargingAsync()
        {
            if (SimulationState.IsCharging)
            {
                Console.WriteLine("O motociclo já está a carregar.");
                return;
            }

            SimulationState.IsCharging = true;
            Console.WriteLine("O carregamento foi iniciado.");
            await _mqttService.PublishAsync("sim/charging", "on");
            _loggingService.AddPerformanceLog(CreateLogEntry());
        }

        public async Task StopChargingAsync()
        {
            if (!SimulationState.IsCharging)
            {
                Console.WriteLine("O motociclo não está a carregar.");
                return;
            }

            SimulationState.IsCharging = false;
            Console.WriteLine("O carregamento foi interrompido.");
            await _mqttService.PublishAsync("sim/charging", "off");
            _loggingService.AddPerformanceLog(CreateLogEntry());
        }

        public async Task UpdateChargingAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (SimulationState.IsCharging && SimulationState.Battery < MAX_BATTERY)
                    {
                        // Exemplo: carrega 1% a cada 2 segundos
                        SimulationState.Battery = Math.Min(SimulationState.Battery + 1, MAX_BATTERY);

                        if (SimulationState.Battery != _lastBatteryPublished)
                        {
                            _lastBatteryPublished = SimulationState.Battery;
                            await _mqttService.PublishAsync("sim/battery", SimulationState.Battery.ToString());
                            _dashboard.DisplayCommands();

                            _loggingService.AddPerformanceLog(CreateLogEntry());
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

        private LogEntry CreateLogEntry()
        {
            return new LogEntry
            {
                Timestamp = DateTime.Now,
                MessageType = "Charging",
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