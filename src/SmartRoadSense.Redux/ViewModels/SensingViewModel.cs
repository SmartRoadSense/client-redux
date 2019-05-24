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
            StopRecording = new Command(async () => await StopRecordingPerform());

            _timer = new Timer(TimerTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        private readonly object _writerLock = new object();
        private volatile bool _skipWriting = false;

        private CancellationTokenSource _locationCancellationSource;

        private void TimerTick(object v) {
            // Debug.WriteLine("Timer ticked");

            if(_skipWriting) {
                return;
            }

            lock(_writerLock) {
                _writer.Write(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1:F3},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},",
                    DateTime.UtcNow.Ticks,
                    _lastAccelerometerReading.X, _lastAccelerometerReading.Y, _lastAccelerometerReading.Z,
                    _lastGyroscopeReading.X, _lastGyroscopeReading.Y, _lastGyroscopeReading.Z
                ));

                if(_lastLocationUpdated) {
                    _writer.Write(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0:F4},{1:F4},",
                        _lastLocationLatitude, _lastLocationLongitude
                    ));
                    _lastLocationUpdated = false;
                }

                _writer.WriteLine();
            }
        }

        public ICommand StartRecording { get; }

        public ICommand StopRecording { get; }

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

            Accelerometer.Start(SensorSpeed.Fastest);
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;

            Gyroscope.Start(SensorSpeed.Fastest);
            Gyroscope.ReadingChanged += Gyroscope_ReadingChanged;

            HandleLocation(await Geolocation.GetLastKnownLocationAsync());
            ScheduleGeolocationRequest();

            _timer.Change(TimerIntervalMs, TimerIntervalMs);

            IsRecording = true;
        }

        private async Task StopRecordingPerform() {
            if(!IsRecording) {
                return;
            }

            _skipWriting = true;

            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            Accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
            Accelerometer.Stop();

            Gyroscope.ReadingChanged -= Gyroscope_ReadingChanged;
            Gyroscope.Stop();

            if(_locationCancellationSource != null) {
                _locationCancellationSource.Cancel();
                _locationCancellationSource.Dispose();
                _locationCancellationSource = null;
            }

            // Close up and bundle up the writer
            await _writer.FlushAsync();
            lock(_writerLock) {
                _writer.Dispose();
                _writer = null;
            }

            IsRecording = false;

            _skipWriting = false;
        }

        private double _lastLocationLatitude, _lastLocationLongitude;
        private readonly GeolocationRequest _geolocationRequest = new GeolocationRequest(GeolocationAccuracy.Best);

        private void ScheduleGeolocationRequest() {
            _locationCancellationSource = new CancellationTokenSource();
            var token = _locationCancellationSource.Token;
            Task.Factory.StartNew(async () => {
                while(true) {
                    if(token.IsCancellationRequested) {
                        Debug.WriteLine("Location access canceled, terminating");
                        break;
                    }

                    Debug.WriteLine("Querying location...");
                    HandleLocation(await Geolocation.GetLocationAsync(_geolocationRequest));
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        private volatile bool _lastLocationUpdated = false;

        private void HandleLocation(Location l) {
            Debug.WriteLine("Location update: {0:F2},{1:F2} acc {2:F2}", l.Latitude, l.Longitude, l.Accuracy);
            _lastLocationLatitude = l.Latitude;
            _lastLocationLongitude = l.Longitude;

            _lastLocationUpdated = true;
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
