using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace wsa_game
{
    public class WSA
    {
        public struct WSA_Data
        {
            public int wVersion;
            public int wHighVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x101)]
            public string szDescription;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x81)]
            public string szSystemStatus;
            public int iMaxSockets;
            public int iMaxUdpDg;
            public long lpVendorInfo;
        }

        public unsafe struct sockaddr
        {
            public short sin_family;
            public short sin_port;
            public int sin_addr;
            public long sin_zero;
        }

        public enum SocketFlags
        {
            Broadcast = 0x400,
            ControlDataTruncated = 0x200,
            DontRoute = 4,
            MaxIOVectorLength = 0x10,
            Multicast = 0x800,
            None = 0,
            OutOfBand = 1,
            Partial = 0x8000,
            Peek = 2,
            Truncated = 0x100
        }

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern Int32 setsockopt(IntPtr socketHandle, Int32 optionLevel, Int32 optionName, ref int optionValue, int optionLength);

        [DllImport("ws2_32.dll")]
        private static extern Int32 WSAStartup(Int16 wVR, ref WSA_Data lpWSAD);

        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Int32 WSAGetLastError();

        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern IntPtr socket(Int32 af, Int32 type, Int32 protocol);

        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern Int32 WSACleanup();

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern Int32 shutdown(IntPtr sock, int how);

        [DllImport("ws2_32.dll")]
        private static extern int connect(IntPtr Sock, ref sockaddr SockAdd, int Size);

        [DllImport("ws2_32.dll")]
        public static extern short htons(int hostshort);

        [DllImport("wsock32.dll")]
        public static extern int inet_addr(string cp);

        [DllImport("ws2_32.dll")]
        private static extern int bind(IntPtr socket, ref sockaddr addr, int namelen);

        [DllImport("ws2_32.dll")]
        public static extern int listen(IntPtr socket, int maxconn);

        [DllImport("ws2_32.dll", SetLastError = true)]
        private static extern IntPtr closesocket(IntPtr socket);

        [DllImport("ws2_32.dll")]
        private static extern int send(IntPtr SocketHandle, byte[] buf, int len, SocketFlags socketFlags);

        [DllImport("ws2_32.dll")]
        private static extern int recv(IntPtr SocketHandle, byte[] buf, int len, SocketFlags socketFlags);

        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr accept(IntPtr socketHandle);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int recvfrom([In] IntPtr socketHandle, [In] byte[] pinnedBuffer, [In] int len, [In] SocketFlags socketFlags, ref sockaddr SockAdd, ref int Size);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int sendto([In] IntPtr socketHandle, [In] byte[] pinnedBuffer, [In] int len, [In] SocketFlags socketFlags, ref sockaddr SockAdd, [In] int Size);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern char inet_ntoa(int sin_addr);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int ntohs(short sin_port);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int getsockname([In] IntPtr socketHandle, ref sockaddr SockAdd, ref int Size);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int getpeername([In] IntPtr socketHandle, ref sockaddr SockAdd, ref int Size);

        public const short WORD_VERSION = 36;

        public const int AF_INET = 2;
        public const int SOCK_STREAM_TCP = 1;
        public const int SOCK_STREAM_UDP = 2;
        public const int PPROTO_TCP = 6;
        public const int PPROTO_UDP = 17;

        //public static int SendTo(IntPtr socketHandle, byte[] pinnedBuffer, int len, SocketFlags socketFlags, ref sockaddr SockAdd, int Size)
        //{
        //    return sendto(socketHandle, pinnedBuffer, len, socketFlags, ref SockAdd, Size);
        //}

        //public static int RecvFrom(IntPtr socketHandle, byte[] pinnedBuffer, int len, SocketFlags socketFlags, sockaddr SockAdd, int Size)
        //{
        //    return recvfrom(socketHandle, pinnedBuffer, len, socketFlags, SockAdd, Size);
        //}

        public static IntPtr Accept(IntPtr socketHandle)
        {
            return accept(socketHandle);
        }

        public static int Send(IntPtr SocketHandle, byte[] buf, int len, SocketFlags socketFlags)
        {
            int result = send(SocketHandle, buf, len, socketFlags);
            GC.Collect();
            return result;
        }

        public static int Recv(IntPtr SocketHandle, byte[] buf, int len, SocketFlags socketFlags)
        {
            int result = recv(SocketHandle, buf, len, socketFlags);
            GC.Collect();
            return result;
        }

        public static int WSA_Startup(WSA_Data lpWSAD)
        {
            return WSAStartup(WORD_VERSION, ref lpWSAD);
        }

        public static IntPtr Socket(int sock_stream, Int32 protocol)
        {
            return socket(AF_INET, sock_stream, protocol);
        }

        public static IntPtr Close_Socket(IntPtr socket)
        {
            return closesocket(socket);
        }

        public static int WSA_Cleanup()
        {
            return WSACleanup();
        }

        public static int AdvBind(string ipAddress, int port, IntPtr socketHandle)
        {
            sockaddr remoteAddress;
            int resultCode = -1;
            int errorCode = 0;

            if (socketHandle != IntPtr.Zero)
            {
                remoteAddress = new sockaddr();
                remoteAddress.sin_family = AF_INET;
                remoteAddress.sin_port = htons((short)port);
                remoteAddress.sin_addr = inet_addr(ipAddress);
                remoteAddress.sin_zero = 0;

                if (remoteAddress.sin_addr != 0)
                {
                    resultCode = bind(socketHandle, ref remoteAddress, Marshal.SizeOf(remoteAddress));
                    errorCode = WSAGetLastError();
                }
            }
            return resultCode;
        }

        public static int Connect(string ipAddress, int port, IntPtr socketHandle)
        {
            sockaddr remoteAddress;
            remoteAddress = new sockaddr();
            remoteAddress.sin_family = AF_INET;
            remoteAddress.sin_port = htons((short)port);
            remoteAddress.sin_addr = inet_addr(ipAddress);
            remoteAddress.sin_zero = 0;
            return connect(socketHandle, ref remoteAddress, Marshal.SizeOf(remoteAddress));
        }
    }
}
