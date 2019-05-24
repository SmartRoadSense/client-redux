using System;
using Android;
using Android.App;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Widget;
using static Android.OS.PowerManager;

namespace SmartRoadSense.Redux.Droid {

    [Activity(
        Label = "SmartRoadSense Redux",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
    )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {

        public MainActivity() {
            
        }

        protected override void OnCreate(Bundle savedInstanceState) {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            // Init platform stuff
            App.GetExternalRootPath = () => {
                return this.GetExternalFilesDir(null).AbsolutePath;
            };

            // Init Xamarin.Forms
            global::Xamarin.Forms.Forms.SetFlags("Shell_Experimental", "Visual_Experimental", "CollectionView_Experimental", "FastRenderers_Experimental");
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }

        public const int PermissionRequestLocation = 1212;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
            if(requestCode == PermissionRequestLocation) {
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

        WakeLock _wakeLock;

        protected override void OnStart() {
            base.OnStart();

            if(_wakeLock is null) {
                var context = this.ApplicationContext;
                PowerManager powerManager = (PowerManager)context.GetSystemService(PowerService);
                _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "Sensing");
                _wakeLock.Acquire();
            }

            if(Build.VERSION.SdkInt >= BuildVersionCodes.M) {
                if(ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != global::Android.Content.PM.Permission.Granted) {
                    RequestPermissions(new string[] {
                        Manifest.Permission.AccessFineLocation
                    }, PermissionRequestLocation);

                    return;
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
