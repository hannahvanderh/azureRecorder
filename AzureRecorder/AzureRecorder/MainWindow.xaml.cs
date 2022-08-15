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
        var baseName = this.outputBaseName.Text;

        if (string.IsNullOrWhiteSpace(path))
        {
          this.OutputConsoleText("Output path required", true);
          return;
        }

        if (string.IsNullOrWhiteSpace(baseName))
        {
          this.OutputConsoleText("Output instance required", true);
          return;
        }

        this.recording = true;
        this.RecordButton.Content = "Stop Recording";

        //TODO list devices, user enter master and sub device index
        this.subProcess1 = this.ExecuteCommand($"sub1start.bat {path}\\{baseName}-sub1.mkv", false);
        this.subProcess2 = this.ExecuteCommand($"sub2start.bat {path}\\{baseName}-sub2.mkv", false);
        this.masterProcess = this.ExecuteCommand($"masterstart.bat {path}\\{baseName}-master.mkv", false);
      }
      else
      {
        this.RecordButton.Content = "Record";
        this.recording = false;

        //this.terminateProcess = this.ExecuteCommand($"runInterrupt.bat");
        //this.terminateProcess.WaitForExit();

        Utils.StopProgram((uint)this.subProcess1.Id);
        Utils.StopProgram((uint)this.subProcess2.Id);
        Utils.StopProgram((uint)this.masterProcess.Id);

        this.subProcess1.CloseMainWindow();
        this.subProcess2.CloseMainWindow();
        this.masterProcess.CloseMainWindow();
      }
    }

    private Process ExecuteCommand(string command, bool output)
    {
      var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
      processInfo.CreateNoWindow = false;
      processInfo.UseShellExecute = false;
      processInfo.RedirectStandardError = false;
      processInfo.RedirectStandardOutput = false;

      var process = Process.Start(processInfo);

      if (process == null)
      {
        return null;
      }

      if (output)
      {
        process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => OutputConsoleText(e.Data, false);
        process.BeginOutputReadLine();

        process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => OutputConsoleText(e.Data, true);
        process.BeginErrorReadLine();
      }


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
              //this.errorOutput.Text = text;
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
