﻿using System;
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

    private Process audioInfoProcess;

    private Process audioProcess1;

    private Process audioProcess2;

    private Process audioProcess3;

    private bool recording = false;

    private object outputLock = new object();

    private bool captureRecieved = false;

    private bool allowRecording = true;

    private ObservableCollection<string> kinectMasterIndices = new ObservableCollection<string>();

    private ObservableCollection<string> kinectSubIndices = new ObservableCollection<string>() { "N/A" };

    private ObservableCollection<string> audioIndices = new ObservableCollection<string>() { "N/A" };

    private Regex kinectIndexRegex;

    private Regex audioIndexRegex;

    private bool useAudio1 = true;

    private bool useAudio2 = true;

    private bool useAudio3 = true;

    private bool useSub1 = true;

    private bool useSub2 = true;

    public MainWindow()
    {
      InitializeComponent();

      //verify the tools are installed before checking devices
      if (!Directory.Exists("C:\\Program Files\\Azure Kinect SDK v1.4.2\\tools"))
      {
        this.errorOutput.Text = "Azure Kinect SDK v1.4.2 not found, please install and restart the application";
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
          if (e.Data != null)
          {
            var match = this.kinectIndexRegex.Match(e.Data);
            if (match.Success)
            {
              this.kinectMasterIndices.Add(match.Value.Split(":")[1]);
              this.kinectSubIndices.Add(match.Value.Split(":")[1]);
            }

            this.deviceInfo.Text += $"{e.Data}\n";
          }
        });
      };

      this.kinectInfoProcess.BeginOutputReadLine();
      this.masterIndex.ItemsSource = this.kinectMasterIndices;

      this.sub1Index.ItemsSource = this.kinectSubIndices;
      this.sub2Index.ItemsSource = this.kinectSubIndices;

      this.audioIndexRegex = new Regex("device #([0-9]*)");
      this.audioInfoProcess = this.ExecuteCommand($"getAudioDevices.bat", false, false, true);
      this.audioInfoProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
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
            if (e.Data != null)
            {
              var match = this.audioIndexRegex.Match(e.Data);
              if (match.Success)
              {
                this.audioIndices.Add(match.Value.Split("#")[1]);
              }

              this.captureInfo.Text += $"{e.Data}\n";
            }
          }
        });
      };

      this.audioInfoProcess.BeginOutputReadLine();
      this.audioIndex1.ItemsSource = this.audioIndices;
      this.audioIndex2.ItemsSource = this.audioIndices;
      this.audioIndex3.ItemsSource = this.audioIndices;
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

      var sub1IndexNumber = 0;
      if (string.IsNullOrWhiteSpace(this.sub1Index.Text))
      {
        this.errorOutput.Text = "Sub 1 Selection Required";
        return;
      }
      else
      {
        if (int.TryParse(this.sub1Index.Text, out sub1IndexNumber))
        {
          this.useSub1 = true;
        }
        else
        {
          sub1IndexNumber = -1;
          this.useSub1 = false;
        }
      }

      var sub2IndexNumber = 0;
      if (string.IsNullOrWhiteSpace(this.sub2Index.Text))
      {
        this.errorOutput.Text = "Sub 2 Selection Required";
        return;
      }
      else
      {
        if (int.TryParse(this.sub2Index.Text, out sub2IndexNumber))
        {
          this.useSub2 = true;
        }
        else
        {
          sub2IndexNumber = -1;
          this.useSub2 = false;
        }
      }

      //Don't allow duplicates
      if (sub1IndexNumber == sub2IndexNumber || masterIndexNumber == sub1IndexNumber || masterIndexNumber == sub2IndexNumber)
      {
        if (this.useSub1 || this.useSub2)
        {
          this.errorOutput.Text = "Duplicate Kinect Index selections are not allowed";
          return;
        }
      }

      var audio1IndexNumber = 0;
      if (string.IsNullOrWhiteSpace(this.audioIndex1.Text))
      {
        this.errorOutput.Text = "Audio 1 Selection Required";
        return;
      }
      else
      {
        if (int.TryParse(this.audioIndex1.Text, out audio1IndexNumber))
        {
          this.useAudio1 = true;
        }
        else
        {
          this.useAudio1 = false;
        }
      }

      var audio2IndexNumber = 0;
      if (string.IsNullOrWhiteSpace(this.audioIndex2.Text))
      {
        this.errorOutput.Text = "Audio 2 Selection Required";
        return;
      }
      else
      {
        if (int.TryParse(this.audioIndex2.Text, out audio2IndexNumber))
        {
          this.useAudio2 = true;
        }
        else
        {
          this.useAudio2 = false;
        }
      }

      var audio3IndexNumber = 0;
      if (string.IsNullOrWhiteSpace(this.audioIndex3.Text))
      {
        this.errorOutput.Text = "Audio 3 Selection Required";
        return;
      }
      else
      {
        if (int.TryParse(this.audioIndex3.Text, out audio3IndexNumber))
        {
          this.useAudio3 = true;
        }
        else
        {
          this.useAudio3 = false;
        }
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
      if (this.useSub1)
      {
        this.subProcess1 = this.ExecuteCommand($"sub1start.bat {path}\\{baseName}-sub1.mkv {sub1IndexNumber}", true, false, false);
      }

      if (this.useSub2)
      {
        this.subProcess2 = this.ExecuteCommand($"sub2start.bat {path}\\{baseName}-sub2.mkv {sub2IndexNumber}", true, false, false);
      }

      this.masterProcess = this.ExecuteCommand($"masterstart.bat {path}\\{baseName}-master.mkv {masterIndexNumber}", true, false, false);

      if (this.useAudio1)
      {
        this.audioProcess1 = this.ExecuteCommand($"audiostart.bat {path}\\{baseName}-audio.wav {audio1IndexNumber}", true, false, false);
      }

      if (this.useAudio2)
      {
        this.audioProcess2 = this.ExecuteCommand($"audiostart.bat {path}\\{baseName}-audio.wav {audio2IndexNumber}", true, false, false);
      }

      if (this.useAudio3)
      {
        this.audioProcess3 = this.ExecuteCommand($"audiostart.bat {path}\\{baseName}-audio.wav {audio3IndexNumber}", true, false, false);
      }
    }

    private async void EndRecordingAsync()
    {
      this.errorOutput.Text = "Closing recording processes please wait.";
      this.recording = false;
      this.mainWindow.IsEnabled = false;

      await Task.Run(() =>
      {
        if (this.useSub1)
        {
          Utils.StopProgram((uint)this.subProcess1.Id);
        }

        if (this.useSub2)
        {
          Utils.StopProgram((uint)this.subProcess2.Id);
        }

        Utils.StopProgram((uint)this.masterProcess.Id);

        if (this.useAudio1)
        {
          Utils.StopProgram((uint)this.audioProcess1.Id);
        }

        if (this.useAudio2)
        {
          Utils.StopProgram((uint)this.audioProcess2.Id);
        }

        if (this.useAudio3)
        {
          Utils.StopProgram((uint)this.audioProcess3.Id);
        }

        if (this.useSub1)
        {
          this.subProcess1.CloseMainWindow();
        }

        if (this.useSub2)
        {
          this.subProcess2.CloseMainWindow();
        }

        this.masterProcess.CloseMainWindow();

        if (this.useAudio1)
        {
          this.audioProcess1.CloseMainWindow();
        }

        if (this.useAudio2)
        {
          this.audioProcess2.CloseMainWindow();
        }

        if (this.useAudio3)
        {
          this.audioProcess3.CloseMainWindow();
        }
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
        Utils.StopProgram((uint)this.audioProcess1.Id);
        Utils.StopProgram((uint)this.audioProcess2.Id);
        Utils.StopProgram((uint)this.audioProcess3.Id);

        this.subProcess1.CloseMainWindow();
        this.subProcess2.CloseMainWindow();
        this.masterProcess.CloseMainWindow();
        this.audioProcess1.CloseMainWindow();
        this.audioProcess2.CloseMainWindow();
        this.audioProcess3.CloseMainWindow();
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
