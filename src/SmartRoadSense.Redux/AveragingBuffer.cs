using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartRoadSense.Redux {

    public class AveragingBuffer {

        public const int WindowSize = 300;

        readonly int[] _buffer = new int[WindowSize];
        int _writeIndex = 0;

        public void Add(int v) {
            _buffer[_writeIndex++] = v;

            if(_writeIndex >= WindowSize) {
                LastAverage = _buffer.Average();
                _writeIndex = 0;

                NewCount?.Invoke(this, EventArgs.Empty);
            }
        }

        public double LastAverage { get; private set; } = 0;

        public event EventHandler NewCount;

        public void Reset() {
            _writeIndex = 0;
        }

    }

}
