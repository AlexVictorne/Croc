using System;
using System.Collections;
using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace wsa_game
{
    public partial class MainForm : Form
    {
        struct Client
        {
            public string ip;
            public string socket;
            public int game_status;
            public int game_bill;
        }

        public MainForm()
        {
            InitializeComponent();
            gr = Graphics.FromImage(pictureBox1.Image);
            string myHost = System.Net.Dns.GetHostName();
            MyIP = System.Net.Dns.GetHostByName(myHost).AddressList[0].ToString();
            start_client();
        }

        //editor
        Graphics gr;
        Bitmap btm = new Bitmap(400, 400);
        bool mouse_flag = false;
        SolidBrush brush = new SolidBrush(Color.Black);
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (mySTATUS == 1)
                mouse_flag = true; //флаг если лкм нажата
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            mouse_flag = false; //флаг снять если лкм снято
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if ((mouse_flag) && (e.X > 0) && (e.Y > 0) && (e.X < 400) && (e.Y < 400))
            {
                send(prepare_message(1, e.X.ToString(), e.Y.ToString()));
            }
        }

        //client
        Hashtable clients = new Hashtable();
        int mySTATUS = 0;
        string MyIP;
        IntPtr TCP_socket;
        IntPtr UDP_socket;
        WSA.WSA_Data wsaData = new WSA.WSA_Data();

        void connect_client()
        {
            if (WSA.Connect(textBox1.Text, Convert.ToInt32(textBox3.Text), TCP_socket) != -1)
                log_txt.Text += "\r\nПодключение к серверу установлено";
            else
                log_txt.Text += "\r\nНет подключения к серверу";
            new Thread(delegate() { recvServerTCP(); }).Start();
        }
        void start_client()
        {
            if (WSA.WSA_Startup(wsaData) != 0)
                log_txt.Text = "Все плохо с всастартап";
            else
                log_txt.Text = "Все ок с всастартап";

            UDP_socket = WSA.Socket(WSA.SOCK_STREAM_UDP, WSA.PPROTO_UDP);
            if (UDP_socket != new IntPtr(-1))
                log_txt.Text += "\r\nУДП сокет создан: " + UDP_socket.ToString();
            else
                log_txt.Text += "\r\nУДП сокет неработит";

            TCP_socket = WSA.Socket(WSA.SOCK_STREAM_TCP, WSA.PPROTO_TCP);
            if (TCP_socket != new IntPtr(-1))
                log_txt.Text += "\r\nТСП сокет создан: " + TCP_socket.ToString();
            else
                log_txt.Text += "\r\nТСП сокет не работит";

        }
        void restart_client()
        {
            WSA.Close_Socket(UDP_socket);
            log_txt.Text += "\r\nUDP socket destroy";
            WSA.Close_Socket(TCP_socket);
            log_txt.Text += "\r\nTCP socket destroy";
            ChangeFormTextSafe("Risovalko");
            mySTATUS = 0;
            start_client();
        }

        void recvServerTCP()
        {
            byte[] recvBuff = new byte[512];
            string Source;
            send("IYOUCLIENT");
            while (true)
            {
                int iResult;
                iResult = WSA.Recv(TCP_socket, recvBuff, 512, 0);
                if (iResult > 0)
                {
                    Source = Encoding.Default.GetString(recvBuff);

                    if (regexFunc(@"IAMHERE", Source) == "IAMHERE")
                    {
                        send("IAMHERETOO");
                        int tv = 2000;
                        WSA.setsockopt(TCP_socket, 65535, 4102, ref tv, Marshal.SizeOf(tv));
                        continue;
                    }
                    
                    int CommandType = Convert.ToInt32(regexFunc(@"CommandType=[0-9]+", Source).Substring(12));
                    switch (CommandType)
                    {
                        case 0:
                            string str = regexFunc(@"Text=\W\w+", Source);
                            if (str.Length > 0)
                            {
                                if (str.Substring(5) == "!start")
                                {
                                    lock (this)
                                        Graphics.FromImage(pictureBox1.Image).Clear(Color.White);
                                }
                            }
                            str = Source.Substring(0, 5) + " " + Source.Substring(26);
                            AddTextSafe(str);
                            recvBuff = new byte[512];
                            break;
                        case 1:
                            int x, y;
                            string X, Y;
                            X = regexFunc(@"X=\d+", Source).Substring(2);
                            x = Convert.ToInt32(X);
                            Y = regexFunc(@"Y=\d+", Source).Substring(2);
                            y = Convert.ToInt32(Y);
                            ((Bitmap)pictureBox1.Image).SetPixel(x, y, Color.Black);
                            PBSafeRefresh();
                            recvBuff = new byte[512];
                            break;
                        case 2:
                            clients.Clear();
                            Match match_client_ip = regexFuncMass(@"client=[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+", Source);
                            Match match_client_socket = regexFuncMass(@"socket=[0-9]+", Source);
                            Match match_client_status = regexFuncMass(@"status=[0-9]", Source);
                            Match match_client_results = regexFuncMass(@"result=[0-9]+", Source);
                            while ((match_client_ip.Success) && (match_client_status.Success) && (match_client_results.Success))
                            {
                                Client cclient;
                                cclient.ip = match_client_ip.Value.Substring(7);
                                cclient.game_bill = Convert.ToInt32(match_client_results.Value.Substring(7));
                                cclient.game_status = Convert.ToInt32(match_client_status.Value.Substring(7));
                                cclient.socket = match_client_socket.Value.Substring(7);
                                clients.Add(cclient.socket, cclient);
                                match_client_socket = match_client_socket.NextMatch();
                                match_client_ip = match_client_ip.NextMatch();
                                match_client_status = match_client_status.NextMatch();
                                match_client_results = match_client_results.NextMatch();
                            }
                            Update_list();
                            recvBuff = new byte[512];
                            break;
                        case 3:
                            if (mySTATUS == 1)
                            {
                                mySTATUS = 0;
                                MessageBox.Show("Ваше слово одгадали! +10");
                                ChangeFormTextSafe("Risovalko");
                            }
                            else
                            {
                                mySTATUS = 1;
                                string str2 = Source.Substring(13);
                                ChangeFormTextSafe("Рисовалец. Слово: " + str2);
                            }
                            break;
                    }
                    Source = null;
                }
                else if (iResult == 0 || iResult == -1)
                {
                    AddTextSafe("Сервер отключился!");
                    ChangeFormTextSafe("Risovalko");
                    mySTATUS = 0;
                    break;
                }
            }
            recvBuff = null;
        }

        void send(string str)
        {
            byte[] sendBuff = new byte[512];
            sendBuff = Encoding.Default.GetBytes(str);
            WSA.Send(TCP_socket, sendBuff, 512, 0);
            sendBuff = null;
        }

        string prepare_message(int command_type, string x, string y)
        {
            string message;
            message = "CommandType=" + command_type.ToString() + "\r\n";
            switch (command_type)
            {
                case 0:
                    message += "Text=" + x;
                    break;
                case 1:
                    message += "X=" + x + "\r\n";
                    message += "Y=" + y + "\r\n";
                    break;
            }

            return message;
        }

        void Update_list()
        {
            ClearListBoxSafe();

            PBSafeRefresh();
            lock (clients)
            {
                foreach (string s in clients.Keys)
                {
                    AddListBoxSafe(((Client)clients[s]).socket + " " + ((Client)clients[s]).ip + " Статус:" + ((Client)clients[s]).game_status + " " + ((Client)clients[s]).game_bill);
                }
            }
        }

        Match regexFuncMass(string regex, string source)
        {
            Regex sort = new Regex(regex);
            Match match = sort.Match(source);
            return match;
        }
        string regexFunc(string regex, string source)
        {
            Regex sort = new Regex(regex);
            Match match = sort.Match(source);
            return match.Value;
        }

        WSA.sockaddr SockAddrServer = new WSA.sockaddr();
        void recvServerUDP()
        {
            byte[] recvBuff = new byte[512];
            int SizeAddr = Marshal.SizeOf(SockAddrServer);
            string result;
            string Source;
            while (true)
            {
                int iResult;
                iResult = WSA.recvfrom(UDP_socket, recvBuff, 512, WSA.SocketFlags.None, ref SockAddrServer, ref SizeAddr);
                if (iResult > 0)
                {
                    Source = Encoding.Default.GetString(recvBuff);
                    result = regexFunc(@"Iconnecttheserver", Source);
                    if (result == "Iconnecttheserver")
                    {
                        AddTextSafe("Сервер найден!: " + new IPAddress(BitConverter.GetBytes(SockAddrServer.sin_addr)).ToString());
                        ChangeTextIPSafe(new IPAddress(BitConverter.GetBytes(SockAddrServer.sin_addr)).ToString());
                        SockAddrServer = new WSA.sockaddr();
                        return;
                    }
                }
                else if (iResult == 0 || iResult == -1)
                {
                    AddTextSafe("Сервер не найден");
                    break;
                }
            }
        }
        void SendBroadcast()
        {
            byte[] sendBuff = Encoding.Default.GetBytes("Imyourserverprodigalson");
            string result = regexFunc(@"[0-9]+\.[0-9]+\.[0-9]+\.", MyIP);
            SockAddrServer.sin_addr = WSA.inet_addr(result + "255");
            SockAddrServer.sin_port = WSA.htons((short)Convert.ToInt32(textBox3.Text));
            SockAddrServer.sin_family = 2;
            if (WSA.sendto(UDP_socket, sendBuff, 512, WSA.SocketFlags.None, ref SockAddrServer, Marshal.SizeOf(SockAddrServer)) != -1)
                new Thread(delegate() { recvServerUDP(); }).Start();
            else
                AddTextSafe("Широковещалка не работает");
        }

        //safeee function
        private delegate void Clear();
        void PBSafeRefresh()
        {
            try
            {
                pictureBox1.Invoke(new Clear(() => pictureBox1.Refresh()));
            }
            catch { }
        }
        void PBSafeClear()
        {
            try
            {
                pictureBox1.Invoke(new Clear(() => gr.DrawLine(Pens.Bisque, 12, 12, 12, 19)));
            }
            catch { }
        }
        void ClearListBoxSafe()
        {
            try
            {
                listBox1.Invoke(new Clear(() => listBox1.Items.Clear()));
            }
            catch { }
        }
        void AddListBoxSafe(string newText)
        {
            try
            {
                listBox1.Invoke(new Action<string>((s) => listBox1.Items.Add(s)), newText);
            }
            catch { }
        }
        void AddTextSafe(string newText)
        {
            try
            {
                log_txt.Invoke(new Action<string>((s) => log_txt.Text += s), "\r\n" + newText);
            }
            catch { }
        }
        void ChangeFormTextSafe(string newText)
        {
            try
            {
                this.Invoke(new Action<string>((s) => this.Text = s), newText);
            }
            catch { }
        }
        void ChangeTextIPSafe(string newText)
        {
            try
            {
                textBox1.Invoke(new Action<string>((s) => textBox1.Text = s), newText);
            }
            catch { }
        }

        //events
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            WSA.Close_Socket(UDP_socket);
            WSA.Close_Socket(TCP_socket);
            WSA.WSA_Cleanup();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            connect_client();
            button2.Enabled = false;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox2.Text != "")
                send(prepare_message(0, textBox2.Text, "1"));
            textBox2.Text = "";
        }
        private void button3_Click(object sender, EventArgs e)
        {
            SendBroadcast();
        }
        private void log_txt_TextChanged(object sender, EventArgs e)
        {
            log_txt.SelectionStart = log_txt.Text.Length;
            log_txt.ScrollToCaret();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            lock (this)
                Graphics.FromImage(pictureBox1.Image).Clear(Color.White);

            restart_client();
            button2.Enabled = true;
        }
    }
}
