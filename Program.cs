using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace marching_2d
{
  static class Program
  {
    // thanks: https://www.csharp411.com/console-output-from-winforms-application/
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);
    private const int ATTACH_PARENT_PROCESS = -1;

    [STAThread]
    private static void Main()
    {
      // redirect console output to parent process;
      // must be before any calls to Console.WriteLine()
      AttachConsole(ATTACH_PARENT_PROCESS);
      
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new MainWindow());
    }
  }
}