using System;
using System.Threading.Tasks;
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

    }

}
