using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Client client;

        private void button1_Click(object sender, EventArgs e)
        {
            client = new Client("irc.friend-chat.jp", 6663, "gummo_test", "gummo", "#wurm_jp");
            client.Connect();
            //client.Disconnect();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            client.Say("日本語テスト", "#wurm_jp");
        }

        private void button3_Click(object sender, EventArgs e)
        {
        }
    }











    public class Client
    {
        #region Constructor

        public Client(string adress, int port, string nick, string owner, string channel)
        {
            _adress = adress;
            _port = port;
            _nick = nick;
            _owner = owner;
            _channel = channel;
        }

        #endregion Constructor

        #region The actually important bits

        public void Connect()
        {
            _socket = new TcpClient();
            try
            {
                Console.WriteLine("[CONNECTING] {0} {1}", _adress, _port);
                _socket.Connect(_adress, _port);
                if (!_socket.Connected)
                {
                    Console.WriteLine("Connect Failed");
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Connect Failed " + e);
                return;
            }

            // Initialize input and output
            _input = new StreamReader(_socket.GetStream(), Encoding.GetEncoding("ISO-2022-JP"));
            _output = new StreamWriter(_socket.GetStream(), Encoding.GetEncoding(932))
            {
                AutoFlush = true // immediatly sends every line to the server
            };

            Send("USER " + _nick + " 0 * :" + _owner);
            Send("NICK " + _nick);

            Session();
        }

        private async void Session()
        {
            // Defined outside the loop so i can see what line it chockes on
            try
            {
                while (_socket.Connected)
                {
                    string buffer = await _input.ReadLineAsync();
                    if (string.IsNullOrEmpty(buffer))
                    {
                        continue;
                    }

                    // Moved this in front of logging to prevent it from cluttering up the log
                    if (buffer.StartsWith("PING"))
                    {
                        _output.WriteLine(buffer.Replace("PING", "PONG"));
                        continue;
                    }

                    Console.WriteLine(buffer);

                    string host, sender, command, message;
                    string[] args;

                    ParseLine(buffer, out sender, out host, out command, out args, out message);

                    // Insert command handling here!

                    switch (command)
                    {
                        case "001":
                            {
                                Join(_channel);
                                break;
                            }
                        case "433":
                            {
                                // Nick in use
                                break;
                            }
                        case "JOIN":
                            {
                                // succesfully joined a channel
                                break;
                            }
                        case "KICK":
                            {
                                // Someone got kicked
                                break;
                            }
                        case "474":
                            {
                                // banned from channel
                                break;
                            }
                        case "353":
                            {
                                // channel user list
                                break;
                            }
                        case "NICK":
                            {
                                // Someone used nick
                                break;
                            }
                        case "PART":
                            {
                                // Someone left the channel
                                break;
                            }
                        case "QUIT":
                            {
                                // Someone quit the channel
                                break;
                            }
                        case "PRIVMSG":
                            {
                                // message received
                                break;
                            }
                        case "NOTICE":
                            {
                                // notice received
                                break;
                            }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (_socket.Connected)
                {
                    _socket.Close();
                }
            }
        }

        public void Disconnect()
        {
            // Since the server can disconnect for you use that since it's a clean way to escape the read loop
            Send("QUIT");
        }

        private void ParseLine(string line, out string user, out string host, out string command, out string[] args, out string message)
        {
            user = null;
            message = null;

            string premessage = line;

            int mindex = line.IndexOf(" :", StringComparison.Ordinal);
            if (mindex != -1)
            {
                premessage = line.Substring(0, line.IndexOf(" :", StringComparison.Ordinal));
                message = line.Substring(mindex + 2);
            }

            string fulluser = premessage.Split(' ')[0];
            command = premessage.Split(' ')[1];

            args = premessage.Split(' ').Skip(2).ToArray();

            host = fulluser;

            if (fulluser.Contains("!"))
            {
                user = fulluser.Split('!')[0].Remove(0, 1);
                host = fulluser.Split('!')[1];
            }
        }

        #endregion The actually important bits

        #region Private Members

        private StreamReader _input;
        private StreamWriter _output;

        private TcpClient _socket;

        private readonly string _nick;

        private readonly string _channel;

        private readonly string _adress;

        private readonly int _port;

        private readonly string _owner;

        #endregion Private Members

        #region IRC Commands

        /// <summary>
        ///     Directly send to the server
        /// </summary>
        /// <param name="input">string to send</param>
        public void Send(string input)
        {
            Console.WriteLine(input);
            _output.WriteLine(input);
        }

        public void Say(string input, string channel)
        {
            Send(string.Format("PRIVMSG {0} :{1}", channel, input));
        }

        public void Notice(string user, string message)
        {
            Send(string.Format("NOTICE {0} :{1}", user, message));
        }

        public void SetNick(string nick)
        {
            Send("NICK " + nick);
        }

        public void Join(string channel, string key = null)
        {
            string output = "JOIN " + channel;
            if (key != null)
            {
                output += " " + key;
            }
            Send(output);
        }

        public void Whois(string user)
        {
            Send("WHOIS " + user);
        }

        public void Part(string channel)
        {
            Send("PART " + channel);
        }

        #endregion IRC Commands
    }
}
