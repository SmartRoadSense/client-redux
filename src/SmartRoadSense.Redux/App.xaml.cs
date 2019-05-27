using System;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Xamarin.Forms;

namespace SmartRoadSense.Redux {

    public partial class App : Application {

        public App() {
            InitializeComponent();

            // DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();
        }

        protected override void OnStart() {
            // Handle when your app starts

#if !DEBUG
            Microsoft.AppCenter.AppCenter.Start("android=b756ec60-114f-447e-89f9-da1acb6ff6b2;"
                                           // + "uwp={Your UWP App secret here};"
                                           // + "ios={Your iOS App secret here}",
                                                , typeof(Analytics), typeof(Crashes)
            );
#endif
        }

        protected override void OnSleep() {
            // Handle when your app sleeps
        }

        protected override void OnResume() {
            // Handle when your app resumes
        }

        public static Func<string> GetExternalRootPath;

        public static Func<string, Task> StartAudioRecording;
        public static Func<Task> StopAudioRecording;

        public static Func<string> GeneratePlatformDetails;

    }

}
