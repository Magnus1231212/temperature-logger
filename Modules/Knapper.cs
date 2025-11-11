using System;
using System.Device.Gpio;
using System.Diagnostics;

namespace temperature_logger.Modules
{
    /// <summary>
    /// Knapper: +, -, Reset/Wake
    /// - Reset: kort tryk = WakeRequested; langt tryk = StartApSetupRequested
    /// - Opdaterer Lysdioder efter hver ændring, så LED-status matcher tolerance-logikken.
    /// </summary>
    internal static class Knapper
    {
        // ***** GPIO pins (ændr hvis nødvendigt) *****
        private const int PLUS_PIN = 25; // + knap
        private const int MINUS_PIN = 20; // - knap
        private const int RESET_PIN = 11; // Reset/Wake

        // Debounce og længde på langt tryk
        private static int _debounceMs = 150;
        private static int _longPressMs = 10000;

        // Temperatur-step pr. tryk og gældende setpoint
        public static double DesiredTemperature { get; private set; } = 21.0;
        private static double _stepDegrees = 0.5;

        // Aktuel temperatur (hentes via delegate, så vi ikke binder til sensor-modul)
        private static Func<double>? _getCurrentTemperature;

        private static GpioController _gpio = null!;
        private static bool _isInitialized = false;

        // Timestamps til debounce og long-press
        private static long _lastPlusEventMs = 0;
        private static long _lastMinusEventMs = 0;
        private static long _resetDownMs = 0;
        private static long _lastResetEdgeMs = 0;

        // Events til resten af systemet
        public static event Action<double>? DesiredTemperatureChanged;   // sender ny desired
        public static event Action? WakeRequested;                       // kort tryk
        public static event Action? StartApSetupRequested;               // langt tryk

        // Initialiser knapper.
        public static void Setup(
            Func<double> getCurrentTemperature,
            double initialDesired = 21.0,
            double stepDegrees = 0.5,
            int longPressMs = 10000,
            int debounceMs = 150)
        {
            if (_isInitialized)
            {
                // Tillad runtime-justeringer
                _getCurrentTemperature = getCurrentTemperature;
                DesiredTemperature = initialDesired;
                _stepDegrees = stepDegrees;
                _longPressMs = longPressMs;
                _debounceMs = debounceMs;
                return;
            }

            _getCurrentTemperature = getCurrentTemperature;
            DesiredTemperature = initialDesired;
            _stepDegrees = stepDegrees;
            _longPressMs = longPressMs;
            _debounceMs = debounceMs;

            _gpio = new GpioController();

            OpenPinInput(PLUS_PIN);
            OpenPinInput(MINUS_PIN);
            OpenPinInput(RESET_PIN);

            // Registrer edge callbacks
            _gpio.RegisterCallbackForPinValueChangedEvent(PLUS_PIN, PinEventTypes.Rising, OnPlusRising);
            _gpio.RegisterCallbackForPinValueChangedEvent(MINUS_PIN, PinEventTypes.Rising, OnMinusRising);
            _gpio.RegisterCallbackForPinValueChangedEvent(RESET_PIN, PinEventTypes.Rising, OnResetRising);
            _gpio.RegisterCallbackForPinValueChangedEvent(RESET_PIN, PinEventTypes.Falling, OnResetFalling);

            _isInitialized = true;
            Debug.WriteLine("[BTN] Setup done");
        }

        private static void OpenPinInput(int pin)
        {
            try
            {
                if (_gpio.IsPinOpen(pin)) _gpio.ClosePin(pin);
            }
            catch { /* nogle drivere mangler IsPinOpen */ }

            // Eksterne pulldown-modstande er i opgaven -> brug Input
            _gpio.OpenPin(pin, PinMode.Input);
        }

        private static long NowMs() => Environment.TickCount64;

        private static bool Debounced(ref long lastEdgeMs, int debounceMs)
        {
            var now = NowMs();
            if (now - lastEdgeMs < debounceMs) return false;
            lastEdgeMs = now;
            return true;
        }

        // + knap: ét trin op
        private static void OnPlusRising(object sender, PinValueChangedEventArgs e)
        {
            if (!Debounced(ref _lastPlusEventMs, _debounceMs)) return;

            DesiredTemperature = Math.Round(DesiredTemperature + _stepDegrees, 2);
            DesiredTemperatureChanged?.Invoke(DesiredTemperature);
            UpdateLeds();
            Debug.WriteLine($"[BTN] + pressed -> Desired={DesiredTemperature:0.00}°C");
        }

        // - knap: ét trin ned
        private static void OnMinusRising(object sender, PinValueChangedEventArgs e)
        {
            if (!Debounced(ref _lastMinusEventMs, _debounceMs)) return;

            DesiredTemperature = Math.Round(DesiredTemperature - _stepDegrees, 2);
            DesiredTemperatureChanged?.Invoke(DesiredTemperature);
            UpdateLeds();
            Debug.WriteLine($"[BTN] - pressed -> Desired={DesiredTemperature:0.00}°C");
        }

        // Reset/Wake: Rising = knap ned (start tid)
        private static void OnResetRising(object sender, PinValueChangedEventArgs e)
        {
            if (!Debounced(ref _lastResetEdgeMs, _debounceMs)) return;
            _resetDownMs = NowMs();
            Debug.WriteLine("[BTN] RESET down");
        }

        // Reset/Wake: Falling = knap op (afgør kort/langt tryk)
        private static void OnResetFalling(object sender, PinValueChangedEventArgs e)
        {
            if (!Debounced(ref _lastResetEdgeMs, _debounceMs)) return;
            var dur = (int)(NowMs() - _resetDownMs);
            _resetDownMs = 0;

            if (dur >= _longPressMs)
            {
                Debug.WriteLine($"[BTN] RESET long ({dur} ms) -> Start AP setup");
                StartApSetupRequested?.Invoke();
            }
            else
            {
                Debug.WriteLine($"[BTN] RESET short ({dur} ms) -> Wake requested");
                WakeRequested?.Invoke();
            }
        }

        /// <summary>
        /// Kaldes når desired ændres, så LED-status følger samme tolerance-logik som Lysdioder.
        /// </summary>
        private static void UpdateLeds()
        {
            if (_getCurrentTemperature == null) return;

            try
            {
                var current = _getCurrentTemperature();
                Lysdioder.UpdateStatus(current, DesiredTemperature);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BTN] UpdateLeds error: {ex.Message}");
            }
        }

        public static void Dispose()
        {
            if (!_isInitialized) return;

            try
            {
                _gpio.UnregisterCallbackForPinValueChangedEvent(PLUS_PIN, OnPlusRising);
                _gpio.UnregisterCallbackForPinValueChangedEvent(MINUS_PIN, OnMinusRising);
                _gpio.UnregisterCallbackForPinValueChangedEvent(RESET_PIN, OnResetRising);
                _gpio.UnregisterCallbackForPinValueChangedEvent(RESET_PIN, OnResetFalling);

                _gpio.ClosePin(PLUS_PIN);
                _gpio.ClosePin(MINUS_PIN);
                _gpio.ClosePin(RESET_PIN);
                _gpio.Dispose();
            }
            finally
            {
                _isInitialized = false;
            }
        }
    }
}
