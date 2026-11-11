using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vosk;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace aSpeech
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        const int WM_APPCOMMAND = 0x319;
        string commandsGramar = "", exitCommand = "", voskModel = "", csGramar = "";

        List<string> scriptsOsCommandsList = new List<string>();

        private WaveInEvent waveIn;

        public Form1()
        {
            InitializeComponent();
        }

        void printToLog(string text)
        {
            richTextBox1.Text += text + "\n";
        }

        void error(string text, string code)
        {
            printToLog($"Error: {code}!: {text}");
        }

        void init()
        {
            try
            {
                printToLog("Initializing start...");
                string cfgPath = Application.StartupPath + @"cfg.ini";
                printToLog($"Reading cfg.ini[cfgPath='{cfgPath}'] ...");
                string[] cfg = File.ReadAllLines(cfgPath, Encoding.UTF8);
                string[] cga = cfg[0].Replace("commandsGramar=", "").Split(',');
                exitCommand = cfg[1].Replace("exitCommand=", "");
                voskModel = cfg[2].Replace("voskModel=", "");
                string scriptsListPath = Application.StartupPath + @"scriptsList.txt";
                printToLog($"Reading scriptsList.txt[scriptsListPath='{scriptsListPath}'] ...");
                string[] scripts = File.ReadAllLines(scriptsListPath, Encoding.UTF8);
                for (int i = 0; i < scripts.Length; i++) scriptsOsCommandsList.Add(scripts[i]);
                printToLog("Configuration initialization complete!\n");

                for (int i = 0; i < cga.Length; i++)
                {
                    string target = cga[i];
                    if (cga.Length > i + 1) commandsGramar += $"\"{target}\", ";
                    else commandsGramar += $"\"{target}\"";
                }

                string[] strings = scriptsOsCommandsList.ToArray();
                for (int i = 0; i < strings.Length; i++)
                {
                    string name = strings[i].Split("=")[1];
                    string target = Path.GetFileNameWithoutExtension(name);
                    if (strings.Length > i + 1) csGramar += $"\"{target}\", ";
                    else csGramar += $"\"{target}\"";
                }

                printToLog("VOSK gramar(commands): " + commandsGramar);
                printToLog("VOSK model: " + voskModel);

                voskInit();
            }
            catch (Exception ex)
            {
                error(ex.Message, "PRE_INIT_ERROR");
            }
        }

        void voskInit()
        {
            printToLog("\n\nInitializing VOSK ...");

            string finalGramar = $"[\"system call\", {csGramar}, {commandsGramar}, \"{exitCommand}\"]";
            printToLog("Final gramar: " + finalGramar);

            try
            {
                Model model = new Model(voskModel);
                var rec = new VoskRecognizer(model, 16000.0f, finalGramar);
                waveIn = new WaveInEvent
                { WaveFormat = new WaveFormat(16000, 16, 1) };

                printToLog("\nListening...\n");
                waveIn.DataAvailable += (s, a) =>
                {
                    if (rec.AcceptWaveform(a.Buffer, a.BytesRecorded))
                    {
                        string json = rec.FinalResult(); // ������ JSON � ������ ���� "text"
                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            string data = doc.RootElement.GetProperty("text").GetString();
                            if (data.Contains("system call"))
                            {
                                printToLog($"\n\nReceived command![command={data}]");
                                string finalData = data.Replace("system call ", "");

                                if (finalData == exitCommand) Process.Start("taskkill", "/im aSpeech.exe /f");
                                else enterpretating(finalData);
                            }
                        }
                    }
                };

                waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                error(ex.Message, "VOSK_INIT_ERROR");
            }
        }

        string find(string name)
        {
            string result = "";
            string[] strings = scriptsOsCommandsList.ToArray();
            for (int i = 0; i < strings.Length; i++)
            {
                string[] data = strings[i].Split("=");
                if (name == data[1])
                {
                    result = data[0];
                    break;
                }
            }

            return result;
        }

        void start(string name)
        {
            try
            {
                string command = find(name);

                printToLog("Initiate system command(Host: cmd.exe): " + command);
                Process.Start("cmd.exe", $"/c \"{command}\"");
            }
            catch (Exception ex)
            {
                error(ex.Message, "START_HOST_COMMAND_ERROR");
            }
        }

        void deviceControl(string targetCommand)
        {
            printToLog("Initializing device control...");
            printToLog($"Target device control command: '{targetCommand}'");

            try
            {
                if (targetCommand.Contains("press space"))
                {
                    printToLog("Local device::initiate: press space");
                    SendKeys.SendWait(" ");
                }
                else if (targetCommand.Contains("system mute"))
                {
                    printToLog("Local device::initiate: system mute");
                    SendMessage(this.Handle, WM_APPCOMMAND, IntPtr.Zero, (IntPtr)APPCOMMAND_VOLUME_MUTE);
                }
                else if (targetCommand.Contains("press enter"))
                {
                    printToLog("Local device::initiate: press enter");
                    SendKeys.SendWait("{ENTER}");
                }
                else if (targetCommand.Contains("press del"))
                {
                    printToLog("Local device::initiate: press enter");
                    SendKeys.SendWait("{DELETE}");
                }
                else if (targetCommand.Contains("press alt tap") ^ targetCommand.Contains("press alt tab"))
                {
                    printToLog("Local device::initiate: press Alt+TAB");
                    SendKeys.SendWait("%{TAB}");
                }
                else if (targetCommand.Contains("press close window"))
                {
                    printToLog("Local device::initiate: press Alt+F4");
                    SendKeys.SendWait("%{F4}");
                }
                else if (targetCommand.Contains("redact configuration"))
                {
                    printToLog("Local device::initiate: start: redact aSpeech configuration data");
                    sys("notepad.exe", Application.StartupPath + @"cfg.ini");
                }
                else if (targetCommand.Contains("redact commands list"))
                {
                    printToLog("Local device::initiate: start: redact aSpeech host executable commands list");
                    sys("notepad.exe", Application.StartupPath + @"scriptsList.txt");
                }
            }
            catch (Exception ex)
            {
                error(ex.Message, "DEVICE_CONTROL_ERROR");
            }
        }

        void stop(string name)
        {
            try
            {
                string proc = Path.GetFileName(find(name));

                printToLog("Stoping process: " + proc);
                exec("taskkill.exe", $"/im \"{proc}\" /f", false);
            }
            catch (Exception ex)
            {
                error(ex.Message, "STOP_PROCESS_ERROR");
            }
        }

        void enterpretating(string data)
        {
            try
            {
                printToLog($"Attached command: '{data}'");
                printToLog("Enterpretating...");

                if (data.Contains("start")) start(data.Replace("start ", ""));
                if (data.Contains("stop")) stop(data.Replace("stop ", ""));
                if (data.Contains("device control")) deviceControl(data.Replace("device control ", ""));
            }
            catch (Exception ex)
            {
                error(ex.Message, "ENTERPRETATING_TARGET_COMMAND_LINE_ERROR");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            System.Threading.Thread thread = new System.Threading.Thread(() => { init(); });
            thread.Start();
        }

        void exec(string path, string args, bool isWait)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = path;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);
                process.StartInfo.Arguments = args;
                process.Start();

                if (isWait) process.WaitForExit();
            }
            catch (Exception ex)
            {
                error(ex.Message, "START_PROCESS_ERROR");
            }
        }

        void sys(string name, string args)
        {
            exec(name, args, true);
            if (MessageBox.Show("Вы хотите перезапустить программу для применения?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Application.Restart();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            sys("notepad.exe", Application.StartupPath + @"cfg.ini");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            sys("notepad.exe", Application.StartupPath + @"scriptsList.txt");
        }
    }
}