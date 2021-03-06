﻿using Frida;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace GTAPilot
{
    // FridaAppConnector represents the script injected to the target process.
    // We receive messages from the target and are notified if the process quits
    // or our target script faults in a fatal way.
    internal class FridaAppConnector : INotifyPropertyChanged
    {
        [DataContract]
        public class JsonMessage
        {
            [DataMember]
            public string type { get; set; }
            [DataMember]
            public string level { get; set; }
            [DataMember]
            public string payload { get; set; }
            [DataMember]
            public string stack { get; set; }
            [DataMember]
            public string description { get; set; }
        }
        
        public event Action<string> MessageReceived;
        public event Action MessageSent;
        public event PropertyChangedEventHandler PropertyChanged;
        
        public bool IsConnected { get; private set; }

        private Dispatcher _fridaDispatcher;
        private Session _session;
        private Script _script;
        private DataContractJsonSerializer _deserializer = new DataContractJsonSerializer(typeof(JsonMessage));

        public void ConnectAsync(uint processId, string scriptContent)
        {
            var t = new Thread(() =>
            {
                WorkerThreadProc(processId, scriptContent);
            });
            t.IsBackground = true;
            t.Start();
        }

        private void WorkerThreadProc(uint processId, string scriptContent)
        {
            try
            {
                _fridaDispatcher = Dispatcher.CurrentDispatcher;
                _session = new DeviceManager(_fridaDispatcher).EnumerateDevices()[0].Attach(processId);
                _session.Detached += Session_Detached;

                _script = _session.CreateScript(scriptContent);
                _script.Message += Script_Message;
                _script.Load();

                TraceLine($"Connected to {_session.Pid}");
                IsConnected = true;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));

                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                TraceLine(ex.ToString());
                OnDisconnected();
            }
        }

        private void Script_Message(object sender, ScriptMessageEventArgs e)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(e.Message)))
            {
                ms.Position = 0;
                var msg = (JsonMessage)_deserializer.ReadObject(ms);

                if (msg.type == "log")
                {
                    TraceLine($"{msg.level}: {msg.payload}");
                }
                else if (msg.type == "error")
                {
                    OnErrorMessage(msg);
                    OnDisconnected();
                }
                else if (msg.type == "send")
                {
                    MessageReceived?.Invoke(msg.payload);
                }
            }
        }

        private void OnErrorMessage(JsonMessage msg)
        {
            var txt = !string.IsNullOrWhiteSpace(msg.stack) ? msg.stack.Replace("\\n", "\r\n") : msg.description;
            TraceLine($"Error: {txt}");
        }

        private void Session_Detached(object sender, SessionDetachedEventArgs e)
        {
            TraceLine($"Session Detached: {e.Reason}");

            OnDisconnected();
        }

        public void SendMessage(string message)
        {
            _fridaDispatcher.BeginInvoke((Action)(() =>
            {
                _script.Post(message);
                MessageSent?.Invoke();
            }));
        }

        private void TraceLine(string msg)
        {
            System.Diagnostics.Trace.WriteLine($"FridaAppConnector: {msg}");
        }

        private void OnDisconnected()
        {
            IsConnected = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));

            Dispatcher.CurrentDispatcher.InvokeShutdown();
        }
    }
}