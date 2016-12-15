using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PdnColorize
{
    class ThreadsafeMessageBox
    {
        public static void Show(string title, string text)
        {
            new Message(title, text).Show();
        }
    }
}
