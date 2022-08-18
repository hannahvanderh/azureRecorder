using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

    private Process kinectInfoProcess;

    private Process audioInfoProess;

    private Process audioProcess;

    private bool recording = false;

    private object outputLock = new object();

    public MainWindow()
    {
      InitializeComponent();
      this.kinectInfoProcess = this.ExecuteCommand($"getKinectDevices.bat", false, false, true);
      this.kinectInfoProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
      {
        Application.Current.Dispatcher.Invoke(
        () =>
        {
          this.deviceInfo.Text += $"{e.Data}\n";
        });
      }; 

      this.kinectInfoProcess.BeginOutputReadLine();
      
      this.audioInfoProess = this.ExecuteCommand($"getAudioDevices.bat", false, false, true);
      this.audioInfoProess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
      {
        Application.Current.Dispatcher.Invoke(
        () =>
        {
          this.captureInfo.Text += $"{e.Data}\n";
        });
      }; 

      this.audioInfoProess.BeginOutputReadLine();
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
      if (!this.recording)
      {
        var path = this.outputFile.Text;
        var baseName = this.outputBaseName.Text;
        
        if(!int.TryParse(this.masterIndex.Text, out var masterIndexNumber))
        {
          this.errorOutput.Text = "Master Index Number Required";
          return;
        }

        if (!int.TryParse(this.sub1Index.Text, out var sub1IndexNumber))
        {
          this.errorOutput.Text = "Sub 1 Index Number Required";
          return;
        }

        if (!int.TryParse(this.sub2Index.Text, out var sub2IndexNumber))
        {
          this.errorOutput.Text = "Sub 2 Index Number Required";
          return;
        }
        
        if (!int.TryParse(this.audioIndex.Text, out var audioIndexNumber))
        {
          this.errorOutput.Text = "Audio Index Number Required";
          return;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
          this.errorOutput.Text = "Output Folder Required";
          return;
        }

        if (string.IsNullOrWhiteSpace(baseName))
        {
          this.errorOutput.Text = "Output instance required";
          return;
        }

        this.recording = true;
        this.RecordButton.Content = "Stop Recording";

        //TODO list devices, user enter master and sub device index
        this.audioProcess = this.ExecuteCommand($"audiostart.bat {path}\\{baseName}-audio.wav {audioIndexNumber}", true, true, false);
        //this.audioProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
        //{
        //  Application.Current.Dispatcher.Invoke(
        //  () =>
        //  {
        //    this.errorOutput.Text += $"{e.Data}\n";
        //  });
        //};

        //this.audioProcess.BeginErrorReadLine();

        this.subProcess1 = this.ExecuteCommand($"sub1start.bat {path}\\{baseName}-sub1.mkv {sub1IndexNumber}", true, false, false);
        this.subProcess2 = this.ExecuteCommand($"sub2start.bat {path}\\{baseName}-sub2.mkv {sub2IndexNumber}", true, false, false);
        this.masterProcess = this.ExecuteCommand($"masterstart.bat {path}\\{baseName}-master.mkv {masterIndexNumber}", true, false, false);
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
        Utils.StopProgram((uint)this.audioProcess.Id);

        this.subProcess1.CloseMainWindow();
        this.subProcess2.CloseMainWindow();
        this.masterProcess.CloseMainWindow();
        this.audioProcess.CloseMainWindow();
      }
    }

    private Process ExecuteCommand(string command, bool createWindow, bool error, bool output)
    {
      var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
      processInfo.CreateNoWindow = !createWindow;
      processInfo.UseShellExecute = false;
      processInfo.RedirectStandardError = error;
      processInfo.RedirectStandardOutput = output;

      var process = Process.Start(processInfo);
      Thread.Sleep(1000);

      if (process == null)
      {
        return null;
      }

      return process;
    }
  }
}
