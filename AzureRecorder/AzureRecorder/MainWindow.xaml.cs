using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AzureRecorder
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private Process process;

    private bool recording = false;

    public MainWindow()
    {
      InitializeComponent();
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
      if (!this.recording)
      {
        this.recording = true;
        this.RecordButton.Content = "Stop Recording";
        this.ExecuteCommand("sim.bat");
      }
      else
      {
        this.RecordButton.Content = "Record";
        this.recording = false;
        this.process.Close();
      }
    }

    private void ExecuteCommand(string command)
    {
      var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
      processInfo.CreateNoWindow = true;
      processInfo.UseShellExecute = false;
      processInfo.RedirectStandardError = true;
      processInfo.RedirectStandardOutput = true;

      this.process = Process.Start(processInfo);

      if (this.process == null)
      {
        return;
      }

      this.process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => OutputConsoleText(e.Data, false);
      this.process.BeginOutputReadLine();

      this.process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => OutputConsoleText(e.Data, true);
      this.process.BeginErrorReadLine();

      //this.process.WaitForExit();

      //Console.WriteLine("ExitCode: {0}", process.ExitCode);
      //this.process.Close();
    }

    private void OutputConsoleText(string text, bool error)
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        return;
      }

      Application.Current.Dispatcher.Invoke(
        () =>
        {
          if (error)
          {
            this.errorOutput.Content = text;
          }
          else
          {
            this.consoleOutput.Content = text;
          }
        });
    }
  }
}
