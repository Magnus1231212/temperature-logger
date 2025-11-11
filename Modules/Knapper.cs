using System;
using System.Device.Gpio;
using System.Diagnostics;

namespace temperature_logger.Modules
{
    /// <summary>
    /// Knapper: +, -, Reset/Wake
    /// - Reset: kort tryk = WakeRequested; langt tryk = StartApSetupRequested
    /// - Bruger Display.CurrentTemperature / Display.DesiredTemperature som single source of truth.
    /// - Opdaterer OLED og LED’er ved ændringer.
    /// </summary>
    internal static class Knapper
    {
        // ***** GPIO pins (ændr hvis nødvendigt) *****
        private const int PLUS_PIN = 12;  // + knap
        private const int MINUS_PIN = 13; // - knap
        private const int RESET_PIN = 14; // Reset/Wake

        // Debounce og længde på langt tryk
        private static int _debounceMs = 150;
        private static int _longPressMs = 10000;

        // Trinstørrelse pr. tryk
        private static double _stepDegrees = 0.5;

        // (Valgfri) fallback hvis Display.CurrentTemperature ikke er sat endnu
        private static Func<double>? _getCurrentTemperature;

        private static GpioController _gpio = null!;
        private static bool _isInitialized = false;

        // Timestamps til debounce og long-press
        private static long _lastPlusEventMs = 0;
        private static long _lastMinusEventMs = 0;
        private static long _resetDownMs = 0;
        private static long _lastResetEdgeMs = 0;

        // Events til resten af systemet
        public static event Action<double>? DesiredTemperatureChanged; // sender ny desired (fra Display)
        public static event Action? WakeRequested;                     // kort tryk
        public static event Action? StartApSetupRequested;             // langt tryk

        public static void Setup(
            Func<double> getCurrentTemperature,
            double initialDesired = 22.0,
            double stepDegrees = 0.5,
            int longPressMs = 10000,
            int debounceMs = 150)
        {
            // Synkronisér ønsket temperatur til displayet
            Display.DesiredTemperature = initialDesired;

            if (_isInitialized)
            {
                _getCurrentTemperature = getCurrentTemperature;
                _stepDegrees = stepDegrees;
                _longPressMs = longPressMs;
                _debounceMs = debounceMs;
                return;
            }

            _getCurrentTemperature = getCurrentTemperature;
            _stepDegrees = stepDegrees;
            _longPressMs = longPressMs;
            _debounceMs = debounceMs;

            _gpio = new GpioController();

            OpenPinInput(PLUS_PIN);
            OpenPinInput(MINUS_PIN);
            OpenPinInput(RESET_PIN);

            _gpio.RegisterCallbackForPinValueChangedEvent(PLUS_PIN, PinEventTypes.Rising, OnPlusRising);
            _gpio.RegisterCallbackForPinValueChangedEvent(MINUS_PIN, PinEventTypes.Rising, OnMinusRising);
            _gpio.RegisterCallbackForPinValueChangedEvent(RESET_PIN, PinEventTypes.Rising, OnResetRising);
            _gpio.RegisterCallbackForPinValueChangedEvent(RESET_PIN, PinEventTypes.Falling, OnResetFalling);

            _isInitialized = true;
            Debug.WriteLine("[BTN] Setup done");
        }

        private static void OpenPinInput(int pin)
        {
            try { if (_gpio.IsPinOpen(pin)) _gpio.ClosePin(pin); } catch { /* nogle drivere mangler IsPinOpen */ }
            _gpio.OpenPin(pin, PinMode.Input); // eksterne pulldowns -> ren Input
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

            Display.DesiredTemperature = Math.Round(Display.DesiredTemperature + _stepDegrees, 2);
            DesiredTemperatureChanged?.Invoke(Display.DesiredTemperature);
            UpdateLeds();
            Display.ShowTemperatureDisplay();

            Debug.WriteLine($"[BTN] + pressed -> Desired={Display.DesiredTemperature:0.00}°C");
        }

        // - knap: ét trin ned
        private static void OnMinusRising(object sender, PinValueChangedEventArgs e)
        {
            if (!Debounced(ref _lastMinusEventMs, _debounceMs)) return;

            Display.DesiredTemperature = Math.Round(Display.DesiredTemperature - _stepDegrees, 2);
            DesiredTemperatureChanged?.Invoke(Display.DesiredTemperature);
            UpdateLeds();
            Display.ShowTemperatureDisplay();

            Debug.WriteLine($"[BTN] - pressed -> Desired={Display.DesiredTemperature:0.00}°C");
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
        /// Opdater LED-status så den følger samme tolerance-logik som Lysdioder.
        /// Bruger Display.CurrentTemperature/DesiredTemperature; falder tilbage til delegate hvis nødvendigt.
        /// Skipper opdatering hvis værdier er NaN.
        /// </summary>
        private static void UpdateLeds()
        {
            double current = Display.CurrentTemperature;
            double desired = Display.DesiredTemperature;

            // Fallback til delegate hvis current ikke er gyldig
            if (double.IsNaN(current) && _getCurrentTemperature != null)
            {
                try { current = _getCurrentTemperature(); } catch { /* ignorer enkelt-fejl */ }
            }

            // Guard mod NaN
            if (double.IsNaN(current) || double.IsNaN(desired))
            {
                Debug.WriteLine("[BTN] UpdateLeds skipped (NaN temperature)");
                return;
            }

            try
            {
                Lysdioder.UpdateStatus(current, desired);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BTN] UpdateLeds error: {ex.Message}");
            }
        }
    }
}
