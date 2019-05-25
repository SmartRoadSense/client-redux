using System;
using System.Linq;
using Android;
using Android.App;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Widget;
using Plugin.AudioRecorder;
using static Android.OS.PowerManager;

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

            // Init Xamarin.Forms
            global::Xamarin.Forms.Forms.SetFlags("Shell_Experimental", "Visual_Experimental", "CollectionView_Experimental", "FastRenderers_Experimental");
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
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
                }

                System.Diagnostics.Debug.WriteLine("Location access not granted");
                Toast.MakeText(this, "Location access denied, application will not collect GPS data", ToastLength.Long).Show();

                return;
            }

            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private WakeLock _wakeLock;

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
