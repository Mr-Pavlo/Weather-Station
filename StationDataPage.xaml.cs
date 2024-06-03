using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Full_App
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StationDataPage : ContentPage
    {
        private HttpClient httpClient = new HttpClient();

        // URL для отримання даних
        private string SensorDataUrl;

        private int failedTemperatureUpdatesCounter = 0;
        private int failedHumidityUpdatesCounter = 0;
        private int failedPressureUpdatesCounter = 0;
        private int failedAltitudeUpdatesCounter = 0;

        public StationDataPage(string sensorDataUrl)
        {
            InitializeComponent();
            SensorDataUrl = sensorDataUrl;
            SetInitialValues();
            StartRealTimeUpdates();
        }

        private void SetInitialValues()
        {
            temperatureLabel.Text = "ТЕМПЕРАТУРА: Analysis";
            humidityLabel.Text = "ВОЛОГІСТЬ: Analysis";
            pressureLabel.Text = "ТИСК: Analysis";
            altitudeLabel.Text = "ВИСОТА: Analysis";
            bmp280_statusLabel.Text = "GY-BMP280-3.3 СТАТУС: Analysis";
            dht22_statusLabel.Text = "DHT22 СТАТУС: Analysis";
            timestampLabel.Text = "ДАТА І ЧАС ОНОВЛЕННЯ: Analysis";
        }

        private void StartRealTimeUpdates()
        {
            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                Task.Run(UpdateSensorDataAsync);
                return true; // Продовжуємо оновлення
            });
        }

        private async Task UpdateSensorDataAsync()
        {
            try
            {
                var response = await httpClient.GetAsync(SensorDataUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var sensorData = JsonConvert.DeserializeObject<SensorData>(content);
                    Device.BeginInvokeOnMainThread(() => UpdateUI(sensorData));
                }
                else
                {
                    UpdateFailureCounters();
                }
            }
            catch
            {
                UpdateFailureCounters();
            }
        }

        private void UpdateUI(SensorData sensorData)
        {
            UpdateLabel(temperatureLabel, $"ТЕМПЕРАТУРА: {sensorData.Temperature}°C", ref failedTemperatureUpdatesCounter, sensorData.Temperature != null);
            UpdateLabel(humidityLabel, $"ВОЛОГІСТЬ: {sensorData.Humidity}%", ref failedHumidityUpdatesCounter, sensorData.Humidity != null);
            UpdateLabel(pressureLabel, $"ТИСК: {sensorData.Pressure} hPa", ref failedPressureUpdatesCounter, sensorData.Pressure != null);
            UpdateLabel(altitudeLabel, $"ВИСОТА: {sensorData.Altitude} m", ref failedAltitudeUpdatesCounter, sensorData.Altitude != null);
            UpdateLabel(timestampLabel, $"ДАТА І ЧАС ОНОВЛЕННЯ: {sensorData.Timestamp}", ref failedAltitudeUpdatesCounter, sensorData.Timestamp != null);
            UpdateDHT22Status(sensorData.Temperature.HasValue);
            UpdateBMP280Status(sensorData.Pressure.HasValue);
        }

        private void UpdateLabel(Label label, string newText, ref int failCounter, bool dataReceived)
        {
            if (dataReceived)
            {
                label.Text = newText;
                label.TextColor = Color.Default;
                failCounter = 0;
            }
            else
            {
                if (++failCounter >= 30) // Налаштування значення на 30 для тиску та висоти
                {
                    label.TextColor = Color.Red;
                }
            }
        }

        private void UpdateFailureCounters()
        {
            UpdateLabel(temperatureLabel, temperatureLabel.Text, ref failedTemperatureUpdatesCounter, false);
            UpdateLabel(humidityLabel, humidityLabel.Text, ref failedHumidityUpdatesCounter, false);
            UpdateLabel(pressureLabel, pressureLabel.Text, ref failedPressureUpdatesCounter, false);
            UpdateLabel(altitudeLabel, altitudeLabel.Text, ref failedAltitudeUpdatesCounter, false);
        }

        private void UpdateDHT22Status(bool hasData)
        {
            if (hasData)
            {
                dht22_statusLabel.FormattedText = CreateStatusText("DHT22 СТАТУС:", " Датчик працездатний ✅", Color.Green);
                failedTemperatureUpdatesCounter = 0;
            }
            else
            {
                if (++failedTemperatureUpdatesCounter >= 45)
                {
                    dht22_statusLabel.FormattedText = CreateStatusText("DHT22 СТАТУС:", " Увага! Ваш датчик DHT22 вийшов з ладу, необхідно замінити ❌", Color.Red);
                }
            }
        }

        private void UpdateBMP280Status(bool hasData)
        {
            if (hasData)
            {
                bmp280_statusLabel.FormattedText = CreateStatusText("GY-BMP280-3.3 СТАТУС:", " Датчик працездатний ✅", Color.Green);
                failedPressureUpdatesCounter = 0;
            }
            else
            {
                if (++failedPressureUpdatesCounter >= 15)
                {
                    bmp280_statusLabel.FormattedText = CreateStatusText("GY-BMP280-3.3 СТАТУС:", " Увага! Ваш датчик GY-BMP280-3.3 вийшов з ладу, необхідно замінити ❌", Color.Red);
                }
            }
        }

        private FormattedString CreateStatusText(string title, string status, Color statusColor)
        {
            return new FormattedString
            {
                Spans =
                {
                    new Span { Text = title, ForegroundColor = Color.Default },
                    new Span { Text = status, ForegroundColor = statusColor }
                }
            };
        }

        // Цей метод викликається, коли користувач натисне на кнопку "Відключитися від метеостанції"
        private async void OnDisconnectButtonClicked(object sender, EventArgs e)
        {
            // Зазначаємо, що користувач більше не підключений
            Application.Current.Properties[MainPage.IsConnectedKey] = false;
            await Application.Current.SavePropertiesAsync();

            // Використання навігації для повернення на головну сторінку
            await Navigation.PopAsync();
        }

        public class SensorData
        {
            public float? Temperature { get; set; }
            public float? Humidity { get; set; }
            public float? Pressure { get; set; }
            public float? Altitude { get; set; }
            public string Timestamp { get; set; }
        }
    }
}