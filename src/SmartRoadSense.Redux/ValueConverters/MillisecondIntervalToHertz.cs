using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace SmartRoadSense.Redux.ValueConverters {

    public class MillisecondIntervalToHertz : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (1000.0 / (double)value).ToString("F1");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return null;
        }
    }

}
