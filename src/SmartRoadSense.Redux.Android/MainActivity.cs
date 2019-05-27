using System;
using System.Linq;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Widget;
using Plugin.AudioRecorder;

namespace SmartRoadSense.Redux.Droid {

    [Activity(
        Label = "SmartRoadSense Redux",
        Icon = "@drawable/launcher_redux",
        Theme = "@style/MainTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
    )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {

        public MainActivity() {
            
        }

        private AudioRecorderService _audioRecorder;

        private string _platformDetails = null;

        protected override void OnCreate(Bundle savedInstanceState) {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            _audioRecorder = new AudioRecorderService {
                StopRecordingAfterTimeout = false,
                StopRecordingOnSilence = false
            };

            base.OnCreate(savedInstanceState);

            // Init platform stuff
            App.GetExternalRootPath = () => {
                return this.GetExternalFilesDir(null).AbsolutePath;
            };
            App.StartAudioRecording = async (filepath) => {
                _audioRecorder.FilePath = filepath;
                await _audioRecorder.StartRecording();
            };
            App.StopAudioRecording = async () => {
                if(!_audioRecorder.IsRecording) {
                    return;
                }
                await _audioRecorder.StopRecording();
            };
            App.GeneratePlatformDetails = () => {
                if(_platformDetails == null) {
                    GeneratePlatformDetails();
                }
                return _platformDetails;
            };

            // Init Xamarin.Forms
            global::Xamarin.Forms.Forms.SetFlags("Shell_Experimental", "Visual_Experimental", "CollectionView_Experimental", "FastRenderers_Experimental");
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            Task.Run(() => { GeneratePlatformDetails(); });
        }

        private string GetApplicationVersion() {
            PackageInfo package;
            try {
                package = ApplicationContext.PackageManager.GetPackageInfo(ApplicationContext.PackageName, 0);
                return package.VersionName;
            }
            catch(PackageManager.NameNotFoundException exNotFound) {
                Microsoft.AppCenter.Crashes.Crashes.TrackError(exNotFound);
                return "AppNotFound";
            }
            catch(Exception ex) {
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex);
                return "AppNotInstalled";
            }
        }

        private void GeneratePlatformDetails() {
            var appVersion = GetApplicationVersion();
            var androidRelease = Build.VERSION.Release ?? "Unknown version";
            var androidSdk = Build.VERSION.SdkInt;
            var deviceManufacturer = Build.Manufacturer.ToTitleCase();
            var deviceModel = Build.Model.ToTitleCase();

            _platformDetails = string.Format("SmartRoadSense.Redux v{0} on Android {1} (SDK {2}), running on {3} {4}",
                appVersion, androidRelease, androidSdk, deviceManufacturer, deviceModel
            );

            System.Diagnostics.Debug.WriteLine("Platform: " + _platformDetails);
        }

        public const int PermissionRequestCombo = 1212;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
            if(requestCode == PermissionRequestCombo) {
                var index = Array.IndexOf<string>(permissions, Manifest.Permission.AccessFineLocation);
                if(index >= 0) {
                    if(grantResults[index] == Permission.Granted) {
                        System.Diagnostics.Debug.WriteLine("Location access granted");
                        Toast.MakeText(this, "Location access granted", ToastLength.Short).Show();
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine("Location access not granted");
                    Toast.MakeText(this, "Location access denied, application will not collect GPS data", ToastLength.Long).Show();
                }

                return;
            }

            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private PowerManager.WakeLock _wakeLock;

        private readonly string[] RequiredPermissions = new string[] {
            Manifest.Permission.AccessFineLocation,
            Manifest.Permission.RecordAudio
        };

        protected override void OnStart() {
            base.OnStart();

            if(_wakeLock is null) {
                var context = this.ApplicationContext;
                PowerManager powerManager = (PowerManager)context.GetSystemService(PowerService);
                _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "Sensing");
                _wakeLock.Acquire();
            }

            if(Build.VERSION.SdkInt >= BuildVersionCodes.M) {
                if(RequiredPermissions.Any(p => {
                    return ContextCompat.CheckSelfPermission(this, p) != Permission.Granted;
                })) {
                    RequestPermissions(RequiredPermissions, PermissionRequestCombo);
                }
            }
        }

        protected override void OnStop() {
            base.OnStop();

            if(_wakeLock != null) {
                _wakeLock.Release();
                _wakeLock = null;
            }
        }

    }

}
