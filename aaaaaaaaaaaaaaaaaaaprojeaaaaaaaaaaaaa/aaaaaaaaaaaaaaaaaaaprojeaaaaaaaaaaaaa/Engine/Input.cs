using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace oyun1.Engine
{
    public static class Input
    {
        private static readonly HashSet<Keys> _currentKeys = new HashSet<Keys>();
        private static readonly HashSet<Keys> _previousKeys = new HashSet<Keys>();

        public static void Update()
        {
            _previousKeys.Clear();
            foreach (var key in _currentKeys)
            {
                _previousKeys.Add(key);
            }
        }

        public static void SetKeyState(Keys key, bool isDown)
        {
            if (isDown) _currentKeys.Add(key);
            else _currentKeys.Remove(key);
        }

        public static bool GetKey(Keys key) => _currentKeys.Contains(key);
        public static bool GetKeyDown(Keys key) => _currentKeys.Contains(key) && !_previousKeys.Contains(key);
        public static bool GetKeyUp(Keys key) => !_currentKeys.Contains(key) && _previousKeys.Contains(key);
    }
}
