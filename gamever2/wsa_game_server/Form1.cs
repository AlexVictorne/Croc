using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace wsa_game_server
{
    public partial class Form1 : Form
    {
        struct Client
        {
            public string ip;
            public string socket;
            public int game_status;
            public int game_bill;
        }
        void add_new_client(int gamestatus, int gamebill, string ip, string sock, IntPtr usr)
        {
            Client cl = new Client();
            cl.game_status = gamestatus;
            cl.game_bill = gamebill;
            cl.ip = ip;
            cl.socket = sock;
            clients.Remove(usr);
            clients.Add(usr, cl);
        }

        IntPtr currentPUser = new IntPtr(0);
        string currentWord = "";
        List<string> words = new List<string>();
        IntPtr TCP_socket;
        IntPtr UDP_socket;
        WSA.WSA_Data wsaData = new WSA.WSA_Data();
        bool socketFlag = true;
        Hashtable clients = new Hashtable();
        List<string> command_risovalko_log = new List<string>();
        public Form1()
        {
            InitializeComponent();
            string myHost = System.Net.Dns.GetHostName();
            string myIP = System.Net.Dns.GetHostByName(myHost).AddressList[0].ToString();
            ip_txt.Text = myIP;
            words.Add("Утка");
            words.Add("Коза");
            words.Add("Кровать");
            words.Add("Пиксель");
            words.Add("Китай");
            update_list_box();
        }

        void start_server()
        {
            if (WSA.WSA_Startup(wsaData) == 0)
                log_txt.Text = "Все ок с всастартап";
            else
                log_txt.Text = "Все плохо с всастартап";
            UDP_socket = WSA.Socket(WSA.SOCK_STREAM_UDP, WSA.PPROTO_UDP);
            TCP_socket = WSA.Socket(WSA.SOCK_STREAM_TCP, WSA.PPROTO_TCP);
            if (UDP_socket == new IntPtr(-1))
                log_txt.Text += "\r\nУДП сокет неработит";
            if (TCP_socket == new IntPtr(-1))
                log_txt.Text += "\r\nТСП сокет неработит";
            if (WSA.AdvBind(ip_txt.Text, Convert.ToInt32(port_txt.Text), UDP_socket) != -1)
                log_txt.Text += "\r\nУДП сокет :" + UDP_socket.ToString() + " порт:" + port_txt.Text + " IPv4:" + ip_txt.Text;
            else
                log_txt.Text += "\r\nУДП сокет не привязан";
            if (WSA.AdvBind(ip_txt.Text, Convert.ToInt32(port_txt.Text), TCP_socket) != -1)
                log_txt.Text += "\r\nТСП сокет :" + TCP_socket.ToString() + " порт:" + port_txt.Text + " IPv4:" + ip_txt.Text;
            else
                log_txt.Text += "\r\nТСП сокет не привязан";
            new Thread(delegate() { recvClientUDP(); }).Start(); 
            if (WSA.listen(TCP_socket, 9) != -1)
            {
                log_txt.Text += "\r\nСервер начал прослушивание";
                new Thread(delegate() { listen_tcp(); }).Start();
            }
        }
        void stop_server()
        {
            WSA.Close_Socket(UDP_socket);
            log_txt.Text += "\r\nUDP сокет уничтожен";
            WSA.Close_Socket(TCP_socket);
            log_txt.Text += "\r\nTCP сокет уничтожен";
            lock (clients)
            foreach (IntPtr sock in clients.Keys)
                WSA.Close_Socket(sock);
        }
        void listen_tcp()
        {
            IntPtr accSocket;
            WSA.sockaddr client = new WSA.sockaddr();
            while (socketFlag)
            {
                accSocket = WSA.Accept(TCP_socket);
                if (accSocket != new IntPtr(-1))
                {                    
                    int Size = Marshal.SizeOf(client);
                    int result = WSA.getpeername(accSocket, ref client, ref Size);
                    string ip = new IPAddress(BitConverter.GetBytes(client.sin_addr)).ToString();
                    int iResult;
                    byte[] recvBuff = new byte[512];
                    int tv = 2000;
                    int li = WSA.setsockopt(accSocket, 65535, 4102, ref tv, Marshal.SizeOf(tv));
                    iResult = WSA.Recv(accSocket, recvBuff, 512, 0);
                    string Source = Encoding.Default.GetString(recvBuff);
                    if (regexFunc(@"IYOUCLIENT", Source) != "IYOUCLIENT")
                    {
                        WSA.Close_Socket(accSocket);
                        continue;
                    }
                    tv = 2000;
                    li = WSA.setsockopt(accSocket, 65535, 4102, ref tv, Marshal.SizeOf(tv));
                    Client cclient;
                    cclient.ip = ip;
                    cclient.socket = accSocket.ToString();
                    cclient.game_status = 0;
                    cclient.game_bill = 0;
                    clients.Add(accSocket, cclient);
                    Update_list();
                    AddTextSafe("Подключился клиент:"+accSocket.ToString());
                    new Thread(delegate() { recvClientTCP(accSocket); }).Start();
                    if (currentWord != "")
                    {
                        lock (command_risovalko_log)
                            foreach (string str in command_risovalko_log)
                            {
                                sendTCP(accSocket, str);
                            }
                    }
                }
            }
        }

        void recvClientTCP(IntPtr client)
        {
            string Source;
            while (true)
            {
                byte[] recvBuff = new byte[512];
                int iResult;
                iResult = WSA.Recv(client, recvBuff, 512, 0);
                if (iResult > 0)
                {
                    Source = Encoding.Default.GetString(recvBuff);
                    if (regexFunc(@"IAMHERETOO", Source) == "IAMHERETOO")
                    {
                        int tv = 2000;
                        WSA.setsockopt(client, 65535, 4102, ref tv, Marshal.SizeOf(tv));
                        tv = 0;
                        continue;
                    }
                    if ((Convert.ToInt32(regexFunc(@"CommandType=[0-9]", Source).Substring(12)) == 0))
                    {
                        string str = regexFunc(@"Text=\W\w+", Source);
                        if (str.Length > 0)
                        {
                            if (str.Substring(5) == "!start")
                            {
                                lock (command_risovalko_log)
                                    command_risovalko_log.Clear();
                                lock (clients)
                                {
                                    Random r = new Random();
                                    int i = 0;
                                    int needed_i = r.Next(clients.Count);
                                    foreach (IntPtr sock in clients.Keys)
                                    {
                                        if (i == needed_i)
                                        {
                                            currentPUser = sock;
                                            currentWord = words[r.Next(words.Count)];
                                        }
                                        i++;
                                    }
                                    lock (clients)
                                        add_new_client(1, ((Client)clients[currentPUser]).game_bill, ((Client)clients[currentPUser]).ip, ((Client)clients[currentPUser]).socket, currentPUser);
                                    sendTCP(currentPUser, "CommandType=3\r\n" + currentWord);
                                }
                                Update_list();
                            }
                        }
                        else
                        {
                            str = regexFunc(@"Text=\w+", Source);
                            if (str.Substring(5) == currentWord)
                            {
                                try
                                {
                                    add_new_client(0, ((Client)clients[currentPUser]).game_bill + 10, ((Client)clients[currentPUser]).ip, ((Client)clients[currentPUser]).socket, currentPUser);
                                }
                                catch { }
                                try
                                {
                                    add_new_client(0, ((Client)clients[client]).game_bill + 10, ((Client)clients[client]).ip, ((Client)clients[client]).socket, client);
                                    Update_list();
                                    sendTCP(currentPUser, "CommandType=3\r\n");
                                }
                                catch { }
                            }
                        }
                        str = null;

                    }
                    lock (command_risovalko_log)
                        command_risovalko_log.Add(client.ToString() + ": " + Source);
                    lock (clients)
                        foreach (IntPtr sock in clients.Keys)
                        {
                            sendTCP(sock, client.ToString() + ": " + Source);
                        }
                    GC.Collect();
                }
                else break;
            }
            lock (clients)
                if (clients.ContainsKey(client))
                {
                    AddTextSafe("Клиент отключился: " + ((Client)clients[client]).ip);
                    clients.Remove(client);
                    WSA.Close_Socket(client);
                    if ((client == currentPUser) && (clients.Count > 0))
                    {
                        currentWord = "";
                        lock (command_risovalko_log)
                        command_risovalko_log=new List<string>();
                        lock (clients)
                        {
                            Random r = new Random();
                            int i = 0;
                            int needed_i = r.Next(clients.Count);
                            foreach (IntPtr sock in clients.Keys)
                            {
                                if (i == needed_i)
                                {
                                    currentPUser = sock;
                                    currentWord = words[r.Next(words.Count)];
                                }
                                i++;
                            }
                            add_new_client(1, ((Client)clients[currentPUser]).game_bill, ((Client)clients[currentPUser]).ip, ((Client)clients[currentPUser]).socket, currentPUser);
                            sendTCP(currentPUser, "CommandType=3\r\n" + currentWord);
                            foreach (IntPtr sock in clients.Keys)
                            {
                                sendTCP(sock,currentPUser.ToString() + ": CommandType=0\r\nText=!start");
                            }
                        }
                        Update_list();
                    }
                    else if (client == currentPUser)
                    {
                        currentWord = "";
                        command_risovalko_log=new List<string>();
                        GC.Collect();
                    }
                    Update_list();
                }
        }
        void sendTCP(IntPtr sock, string str)
        {
            byte[] sendBuff = new byte[512];
            sendBuff = Encoding.Default.GetBytes(str);
            WSA.Send(sock, sendBuff, 512, 0);
        }

        WSA.sockaddr SockAddrClient = new WSA.sockaddr();
        void SendUDP()
        {
            byte[] sendBuff = Encoding.Default.GetBytes("Iconnecttheserver\r\nYourIP=" + new IPAddress(BitConverter.GetBytes(SockAddrClient.sin_addr)).ToString());
            WSA.sendto(UDP_socket, sendBuff, 512, WSA.SocketFlags.None, ref SockAddrClient, Marshal.SizeOf(SockAddrClient));
        }
        void recvClientUDP()
        {
            byte[] recvBuff = new byte[512];

            int SizeAddr = Marshal.SizeOf(SockAddrClient);
            while (true)
            {
                int iResult;
                iResult = WSA.recvfrom(UDP_socket, recvBuff, 512, WSA.SocketFlags.None, ref SockAddrClient, ref SizeAddr);
                if (iResult > 0)
                {
                    if (Encoding.Default.GetString(recvBuff).Substring(0, 23) == "Imyourserverprodigalson")
                    {
                        AddTextSafe("Нашелся клиент!: " + new IPAddress(BitConverter.GetBytes(SockAddrClient.sin_addr)).ToString());
                        SendUDP();
                    }
                }
                else
                    break;
            }
        }

        void update_list_box()
        {
            listBox2.Items.Clear();
            foreach (string str in words)
                listBox2.Items.Add(str);
        }
        void Update_list()
        {
            ClearListBoxSafe();
            lock (clients)
            {
                foreach (IntPtr s in clients.Keys)
                {
                    AddListBoxSafe(s.ToString() + " " + ((Client)clients[s]).ip);
                    sendTCP(s, prepare_list_to_send());
                }
            }
        }
        string prepare_list_to_send()
        {
            string message;
            message = "CommandType=2\r\n";
            foreach (IntPtr s in clients.Keys)
            {
                message += "client" + "=" + ((Client)clients[s]).ip + "\r\n";
                message += "socket" + "=" + ((Client)clients[s]).socket + "\r\n";
                message += "result" + "=" + ((Client)clients[s]).game_bill + "\r\n";
                message += "status" + "=" + ((Client)clients[s]).game_status + "\r\n";
            }
            return message;
        }
        string regexFunc(string regex, string source)
        {
            Regex sort = new Regex(regex);
            Match match = sort.Match(source);
            return match.Value;
        }

        //safefunction
        private delegate void Clear();
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
 
        //events
        private void button1_Click(object sender, EventArgs e)
        {
            start_server();
            button1.Enabled = false;
            button2.Enabled = true;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            stop_server();
            button1.Enabled = true;
            button2.Enabled = false;
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            WSA.Close_Socket(UDP_socket);
            WSA.Close_Socket(TCP_socket);
            socketFlag = false;
            foreach (IntPtr sock in clients.Keys)
                WSA.Close_Socket(sock);
            WSA.WSA_Cleanup();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            words.Add(textBox1.Text);
            update_list_box();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lock (clients)
            {
                foreach (IntPtr s in clients.Keys)
                {
                    sendTCP(s, "IAMHERE");
                }
            }
        }

        private void log_txt_TextChanged(object sender, EventArgs e)
        {
            log_txt.SelectionStart = log_txt.Text.Length;
            log_txt.ScrollToCaret();
        }
    }
}
