# temperature-logger

## Pin Map

| ESP32 Pin   | Function  | Connected Device | Device Pin | Description       |
| ----------- | --------- | ---------------- | ---------- | ----------------- |
| **3.3V**    | Power     | All Modules      | VCC        | 3.3V power supply |
| **GND**     | Ground    | All Modules      | GND        | Common ground     |
| **GPIO 21** | I2C1 SDA  | SSD1306 OLED     | SDA        | I²C data line     |
| **GPIO 22** | I2C1 SCL  | SSD1306 OLED     | SCL        | I²C clock line    |
| **GPIO 33** | Blue LED  | LED              | VCC        |                   |
| **GPIO 32** | Green LED | LED              | VCC        |                   |
| **GPIO 35** | Red LED   | LED              | VCC        |                   |
