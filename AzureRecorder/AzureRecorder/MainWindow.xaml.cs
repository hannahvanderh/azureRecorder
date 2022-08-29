using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Application = System.Windows.Application;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

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

    private bool captureRecieved = false;

    private bool allowRecording = true;

    private ObservableCollection<string> kinectIndices = new ObservableCollection<string>();

    private Regex kinectIndexRegex;

    public MainWindow()
    {
      InitializeComponent();

      //verify the tools are installed before checking devices
      if (!Directory.Exists("C:\\Program Files\\Azure Kinect SDK v1.4.1\\tools"))
      {
        this.errorOutput.Text = "Azure Kinect SDK V1.4.1 not found, please install and restart the application";
        this.allowRecording = false;
        return;
      }

      if (!Directory.Exists("C:\\Program Files\\fmedia"))
      {
        this.errorOutput.Text = "Fmedia not found, please install and restart the application";
        this.allowRecording = false;
        return;
      }

      this.kinectIndexRegex = new Regex("Index:([0-9]*)");
      this.kinectInfoProcess = this.ExecuteCommand($"getKinectDevices.bat", false, false, true);
      this.kinectInfoProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
      {
        Application.Current.Dispatcher.Invoke(
        () =>
        {
          if (e.Data != null && e.Data.Contains("Index:"))
          {
            var match = this.kinectIndexRegex.Match(e.Data);

            if (match.Success)
            {
              this.kinectIndices.Add(match.Value.Split(":")[1]);
            }
          }

          this.deviceInfo.Text += $"{e.Data}\n";
        });
      };

      this.kinectInfoProcess.BeginOutputReadLine();
      this.masterIndex.ItemsSource = this.kinectIndices;
      this.sub1Index.ItemsSource = this.kinectIndices;
      this.sub2Index.ItemsSource = this.kinectIndices;

      this.audioInfoProess = this.ExecuteCommand($"getAudioDevices.bat", false, false, true);
      this.audioInfoProess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
      {
        Application.Current.Dispatcher.Invoke(
        () =>
        {
          if (e.Data != null && e.Data.Contains("Capture"))
          {
            this.captureRecieved = true;
          }

          if (this.captureRecieved)
          {
            this.captureInfo.Text += $"{e.Data}\n";
          }
        });
      };

      this.audioInfoProess.BeginOutputReadLine();
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
      if (!this.allowRecording)
      {
        this.errorOutput.Text = "Error starting recording, please verify all required tools are installed and restart the application.";
        return;
      }

      if (!this.recording)
      {
        this.StartRecording();
      }
      else
      {

        this.EndRecordingAsync();
      }
    }

    private void StartRecording()
    {
      var path = this.outputFile.Text;
      var baseName = this.outputBaseName.Text;

      if (!int.TryParse(this.masterIndex.Text, out var masterIndexNumber))
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
      this.subProcess1 = this.ExecuteCommand($"sub1start.bat {path}\\{baseName}-sub1.mkv {sub1IndexNumber}", true, false, false);
      this.subProcess2 = this.ExecuteCommand($"sub2start.bat {path}\\{baseName}-sub2.mkv {sub2IndexNumber}", true, false, false);
      this.masterProcess = this.ExecuteCommand($"masterstart.bat {path}\\{baseName}-master.mkv {masterIndexNumber}", true, false, false);
      this.audioProcess = this.ExecuteCommand($"audiostart.bat {path}\\{baseName}-audio.wav {audioIndexNumber}", true, false, false);
    }

    private async void EndRecordingAsync()
    {
      this.errorOutput.Text = "Closing recording processes please wait.";
      this.recording = false;
      this.mainWindow.IsEnabled = false;

      await Task.Run(() =>
      {
        Utils.StopProgram((uint)this.subProcess1.Id);
        Utils.StopProgram((uint)this.subProcess2.Id);
        Utils.StopProgram((uint)this.masterProcess.Id);
        Utils.StopProgram((uint)this.audioProcess.Id);

        this.subProcess1.CloseMainWindow();
        this.subProcess2.CloseMainWindow();
        this.masterProcess.CloseMainWindow();
        this.audioProcess.CloseMainWindow();
      });

      this.errorOutput.Text = string.Empty;
      this.RecordButton.Content = "Record";
      this.mainWindow.IsEnabled = true;
    }

    private async void OnCloseRecodingAsync()
    {
      this.errorOutput.Text = "Closing recording processes please wait.";
      this.mainWindow.IsEnabled = false;

      await Task.Run(() =>
      {
        Utils.StopProgram((uint)this.subProcess1.Id);
        Utils.StopProgram((uint)this.subProcess2.Id);
        Utils.StopProgram((uint)this.masterProcess.Id);
        Utils.StopProgram((uint)this.audioProcess.Id);

        this.subProcess1.CloseMainWindow();
        this.subProcess2.CloseMainWindow();
        this.masterProcess.CloseMainWindow();
        this.audioProcess.CloseMainWindow();
      });

      this.recording = false;
      this.Close();
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

    private void outputSelect_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new FolderBrowserDialog();
      dialog.ShowDialog();
      this.outputFile.Text = dialog.SelectedPath;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      if (this.recording)
      {
        e.Cancel = true;
        this.OnCloseRecodingAsync();
      }
      else
      {
        base.OnClosing(e);
      }
    }

    private void closeButton_Click(object sender, RoutedEventArgs e)
    {

    }
  }
}
