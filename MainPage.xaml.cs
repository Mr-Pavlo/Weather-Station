using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Net.Http;
using Zeroconf;
using System.Diagnostics;

namespace Full_App
{
    public partial class MainPage : ContentPage
    {
        private HttpClient httpClient = new HttpClient();
        public static readonly string IsConnectedKey = "IsConnectedToStation";
        public static readonly string ServiceType = "_weatherstation._tcp.local.";

        public MainPage()
        {
            InitializeComponent();
            CheckPreviousConnection().ConfigureAwait(false);
        }

        // Обробник для кнопки переходу на сторінку інструкції
        private async void instructionClick(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new InstructionForUser());
        }

        private async void OnSearchButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var results = await ZeroconfResolver.ResolveAsync(ServiceType);
                var service = results.FirstOrDefault();

                if (service != null)
                {
                    var address = service.IPAddresses.FirstOrDefault();
                    var port = service.Services.FirstOrDefault().Value.Port;
                    var sensorDataUrl = $"http://{address}:{port}/sensors";

                    Application.Current.Properties[IsConnectedKey] = true;
                    Application.Current.Properties["SensorDataUrl"] = sensorDataUrl;
                    await Application.Current.SavePropertiesAsync();

                    // Оновлення DisplayAlert і перехід на StationDataPage з передачею URL
                    bool answer = await DisplayAlert("Успішно знайдено!", $"Метеостанція знайдена за адресою: {sensorDataUrl}. Бажаєте з'єднатися?", "З'єднатися", "Відхилити");
                    if (answer)
                    {
                        await Navigation.PushAsync(new StationDataPage(sensorDataUrl));
                    }
                }
                else
                {
                    await DisplayAlert("Помилка", "Не вдалося знайти метеостанцію.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Помилка: {ex.Message}");
                await DisplayAlert("Помилка", "Виникла проблема з підключенням до мережі.", "OK");
            }
        }

        private async Task CheckPreviousConnection()
        {
            bool wasConnected = Application.Current.Properties.ContainsKey(IsConnectedKey) &&
                                (bool)Application.Current.Properties[IsConnectedKey];

            if (wasConnected && Application.Current.Properties.TryGetValue("SensorDataUrl", out var sensorDataUrl))
            {
                bool answer = await DisplayAlert("Підключення", "Ви вже підключені до метеостанції. Продовжити сеанс?", "Так", "Ні");
                if (answer)
                {
                    await Navigation.PushAsync(new StationDataPage(sensorDataUrl as string));
                }
                else
                {
                    Application.Current.Properties[IsConnectedKey] = false;
                    Application.Current.Properties["SensorDataUrl"] = null;
                    await Application.Current.SavePropertiesAsync();
                }
            }
        }
    }
}