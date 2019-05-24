using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace SmartRoadSense.Redux.ViewModels {

    public class SensingViewModel : BaseViewModel {

        private readonly Timer _timer;
        private const int TimerFrequencyHz = 100;
        private const int TimerIntervalMs = (int)(1000.0 / TimerFrequencyHz);

        private StreamWriter _writer;

        public SensingViewModel() {
            Title = "Sensing";

            StartRecording = new Command(async () => await StartRecordingPerform());

            _timer = new Timer(TimerTick, null, Timeout.Infinite, Timeout.Infinite);

            _geolocationRequest = new GeolocationRequest(GeolocationAccuracy.Best);
        }

        private void TimerTick(object v) {
            Debug.WriteLine("Timer ticked");

            _writer.WriteLine("{0},{1:F3},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7:F4},{8:F4}",
                DateTime.UtcNow.Ticks,
                _lastAccelerometerReading.X, _lastAccelerometerReading.Y, _lastAccelerometerReading.Z,
                _lastGyroscopeReading.X, _lastGyroscopeReading.Y, _lastGyroscopeReading.Z,
                _lastLocation.Latitude, _lastLocation.Longitude
            );
        }

        public ICommand StartRecording { get; }

        private async Task StartRecordingPerform() {
            if(IsRecording) {
                return;
            }

            string filename = "srs-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
            string filepath = Path.Combine(App.GetExternalRootPath(), filename);
            _writer = new StreamWriter(new FileStream(filepath, FileMode.CreateNew));

            await _writer.WriteLineAsync(string.Format("# Start time (local): {0:G}", DateTime.Now));
            await _writer.WriteLineAsync(string.Format("# Track name: {0}", TrackName));
            await _writer.WriteLineAsync("Ticks,AccX,AccY,AccZ,GyrX,GyrY,GyrZ,Lat,Lng");
            await _writer.FlushAsync();

            _lastLocation = await Geolocation.GetLastKnownLocationAsync();

            Accelerometer.Start(SensorSpeed.Fastest);
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;

            Gyroscope.Start(SensorSpeed.Fastest);
            Gyroscope.ReadingChanged += Gyroscope_ReadingChanged;

            ScheduleGeolocationRequest();
        }

        private async Task StopRecordingPerform() {
            if(!IsRecording) {
                return;
            }

            Accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
            Accelerometer.Stop();

            Gyroscope.ReadingChanged -= Gyroscope_ReadingChanged;
            Gyroscope.Stop();

            await _writer.FlushAsync();
            _writer.Dispose();
            _writer = null;

            IsRecording = false;
        }

        private readonly GeolocationRequest _geolocationRequest;
        private Location _lastLocation;

        private void ScheduleGeolocationRequest() {
            Task.Run(() => {
                Geolocation.GetLocationAsync(_geolocationRequest).ContinueWith(t => {
                    Debug.WriteLine("Location update: {0:F2},{1:F2} acc {2:F2}", t.Result.Latitude, t.Result.Longitude, t.Result.Accuracy);
                    _lastLocation = t.Result;
                });
                ScheduleGeolocationRequest();
            });
        }

        private Vector3 _lastAccelerometerReading;

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e) {
            _lastAccelerometerReading = e.Reading.Acceleration;
        }

        private Vector3 _lastGyroscopeReading;

        private void Gyroscope_ReadingChanged(object sender, GyroscopeChangedEventArgs e) {
            _lastGyroscopeReading = e.Reading.AngularVelocity;
        }

        bool _isRecording = false;
        public bool IsRecording {
            get {
                return _isRecording;
            }
            private set {
                SetProperty(ref _isRecording, value);
            }
        }

        string _trackName;
        public string TrackName {
            get {
                return _trackName;
            }
            set {
                SetProperty(ref _trackName, value);
            }
        }

    }

}
