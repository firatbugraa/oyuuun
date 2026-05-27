using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace oyun1.Engine
{
    public static class Time
    {
        private static Stopwatch _stopwatch = new Stopwatch();
        private static double _lastTime = 0;

        public static float DeltaTime { get; private set; }
        public static float TotalTime => (float)_stopwatch.Elapsed.TotalSeconds;

        public static void Initialize()
        {
            _stopwatch.Start();
            _lastTime = _stopwatch.Elapsed.TotalSeconds;
        }

        public static void Update()
        {
            double currentTime = _stopwatch.Elapsed.TotalSeconds;
            DeltaTime = (float)(currentTime - _lastTime);
            _lastTime = currentTime;

            if (DeltaTime > 0.1f)
            {
                DeltaTime = 0.1f;
            }
        }
    }
}