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

    private Process terminateProcess;

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
        var path = this.outputFile.Text;

        if (string.IsNullOrWhiteSpace(path))
        {
          this.OutputConsoleText("Output path required", true);
          return;
        }

        this.recording = true;
        this.RecordButton.Content = "Stop Recording";
        //this.ExecuteCommand("sim.bat", this.simProcess);
        this.subProcess1 = this.ExecuteCommand($"sub1start.bat {path}\\output-1.mkv");
        this.subProcess2 = this.ExecuteCommand($"sub2start.bat {path}\\output-2.mkv");
        this.masterProcess = this.ExecuteCommand($"masterstart.bat {path}\\output-0.mkv");
      }
      else
      {
        this.RecordButton.Content = "Record";
        this.recording = false;
        //this.simProcess.Close();

        //Process.Start("taskkill /FI \"WindowTitle eq sub1 * \" /T");
        //Process.Start("taskkill /FI \"WindowTitle eq sub2 * \" /T");
        //Process.Start("taskkill /FI \"WindowTitle eq master * \" /T");

        this.terminateProcess = this.ExecuteCommand($"runInterrupt.bat");
        this.terminateProcess.WaitForExit();

        this.subProcess1.Close();
        this.subProcess2.Close();
        this.masterProcess.Close();
        this.terminateProcess.Close();

        //Process.GetProcessById(this.subProcess1.Id).Kill(true);
        //Process.GetProcessById(this.subProcess2.Id).Kill(true);
        //Process.GetProcessById(this.masterProcess.Id).Kill(true);
      }
    }

    private Process ExecuteCommand(string command)
    {
      var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
      processInfo.CreateNoWindow = true;
      processInfo.UseShellExecute = false;
      processInfo.RedirectStandardError = true;
      processInfo.RedirectStandardOutput = true;

      var process = Process.Start(processInfo);

      if (process == null)
      {
        return null;
      }

      process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => OutputConsoleText(e.Data, false);
      process.BeginOutputReadLine();

      process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => OutputConsoleText(e.Data, true);
      process.BeginErrorReadLine();

      return process;
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
              this.errorOutput.Text = text;
            }
            else
            {
              this.consoleOutput.Text = text;
            }
          });
      }
    }
  }
}
