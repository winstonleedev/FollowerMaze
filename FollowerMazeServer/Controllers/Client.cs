﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace FollowerMazeServer
{
    class Client: IDisposable
    {
        private int ClientID;
        private List<int> Followers;
        private Queue<Payload> Messages;

        BackgroundWorker Worker;
        TcpClient Connection;

        // Triggered when the client sends its ID
        public event EventHandler<IDEventArgs> OnIDAvailable;

        // Triggered when the client disconnects
        public event EventHandler<IDEventArgs> OnDisconnect;

        public Client(TcpClient Connection)
        {
            Messages = new Queue<Payload>();
            Followers = new List<int>();
            Connection.ReceiveTimeout = -1;
            Connection.SendTimeout = -1;
            this.Connection = Connection;
            this.Worker = new BackgroundWorker();
            this.Worker.WorkerSupportsCancellation = true;
            this.Worker.DoWork += ClientMessageHandling;
            this.Worker.RunWorkerAsync();
        }

        public void AddFollower(int Target)
        {
            lock (Followers)
            {
                Followers.Add(Target);
            }
        }

        public bool RemoveFollower(int Target)
        {
            bool Result = false;
            lock (Followers)
            {
                Result = Followers.Remove(Target);
            }
            return Result;
        }

        public void QueueMessage(Payload Message)
        {
            lock (Messages)
            {
                Messages.Enqueue(Message);
            }
        }

        public List<int> GetCurrentFollowers()
        {
            // Returns a copy
            return Followers.ToList();
        }

        // Buffer to read client ID, shared between ProcessClientID and ClientMessageHandling
        byte[] Incoming = new byte[Constants.BufferSize];

        private void ProcessClientID(IAsyncResult AR)
        {
            NetworkStream networkStream = (NetworkStream)AR.AsyncState;
            int ReadBytes = networkStream.EndRead(AR);

            // Read client ID            
            string ID = System.Text.Encoding.UTF8.GetString(Incoming, 0, ReadBytes);

            // Invalid client ID? Close this connection
            if (!int.TryParse(ID, out ClientID))
            {
                Shutdown();
            }
            Utils.Log($"Received ID from client ID={ClientID}");
            OnIDAvailable?.Invoke(this, new IDEventArgs(ClientID));
        }

        private void ClientMessageHandling(object sender, DoWorkEventArgs e)
        {
            NetworkStream networkStream;
            
            try
            {
                networkStream = Connection.GetStream();
                networkStream.BeginRead(
                    Incoming,
                    0,
                    Constants.BufferSize,
                    this.ProcessClientID,
                    networkStream);

                while (!Worker.CancellationPending)
                {
                    while (Messages.Count > 0)
                    {
                        Payload Next = Messages.Dequeue();
                        Utils.Log($"Sending message=${Next} from ClientID={ClientID}");
                        byte[] ToSend = System.Text.Encoding.UTF8.GetBytes(Next.ToString());
                        networkStream.Write(ToSend, 0, ToSend.Length);
                    }
                    Thread.Sleep(Constants.WorkerDelay);
                }
            } catch {
                Utils.Log("Client shutdown!");                
            }
            Shutdown();
        }

        public void Shutdown()
        {
            Worker.CancelAsync();
            // Event handler not set in this class, better check if it is properly assigned
            OnDisconnect?.Invoke(this, new IDEventArgs(ClientID));
        }

        void IDisposable.Dispose()
        {
            Worker.Dispose();
        }
    }
}
