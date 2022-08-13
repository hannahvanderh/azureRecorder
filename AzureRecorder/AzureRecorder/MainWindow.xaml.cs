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
    private Process simProcess;

    private Process masterProcess;

    private Process subProcess1;

    private Process subProcess2;

    private bool recording = false;

    private object outputLock = new object();

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
        //this.ExecuteCommand("sim.bat", this.simProcess);
        this.ExecuteCommand("sub1start.bat", this.subProcess1);
        this.ExecuteCommand("sub2start.bat", this.subProcess2);
        this.ExecuteCommand("masterstart.bat", this.masterProcess);
      }
      else
      {
        this.RecordButton.Content = "Record";
        this.recording = false;
        //this.simProcess.Close();
        this.subProcess1.Close();
        this.subProcess2.Close();
        this.masterProcess.Close();
      }
    }

    private void ExecuteCommand(string command, Process process)
    {
      var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
      processInfo.CreateNoWindow = true;
      processInfo.UseShellExecute = false;
      processInfo.RedirectStandardError = true;
      processInfo.RedirectStandardOutput = true;

      process = Process.Start(processInfo);

      if (process == null)
      {
        return;
      }

      process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => OutputConsoleText(e.Data, false);
      process.BeginOutputReadLine();

      process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => OutputConsoleText(e.Data, true);
      process.BeginErrorReadLine();
    }

    private void OutputConsoleText(string text, bool error)
    {
      lock (this.outputLock)
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
}
