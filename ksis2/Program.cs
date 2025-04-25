using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Traceroute
{
    private const int MaxHops = 30;
    private const int Timeout = 3000;
    private const int PacketsPerHop = 3;
    private const int IcmpHeaderSize = 8;
    private static ushort sequenceNumber = 1;

    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Traceroute <IP or Hostname>");
            return;
        }

        string target = args[0];
        if (!IPAddress.TryParse(target, out IPAddress ipAddress))
        {
            try
            {
                ipAddress = Dns.GetHostAddresses(target)[0];
            }
            catch
            {
                Console.WriteLine("Invalid host");
                return;
            }
        }

        Console.WriteLine($"Трассировка маршрута к {ipAddress} с максимальным числом прыжков {MaxHops}:");
        PerformTraceroute(ipAddress);
    }

    private static void PerformTraceroute(IPAddress destination)
    {
        using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
        socket.ReceiveTimeout = Timeout;

        for (int ttl = 1; ttl <= MaxHops; ttl++)
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, ttl);
            Console.Write($"{ttl}\t");
            IPAddress lastAddress = null;

            for (int i = 0; i < PacketsPerHop; i++)
            {
                byte[] packet = CreateIcmpPacket();
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Stopwatch stopwatch = Stopwatch.StartNew();

                try
                {
                    socket.SendTo(packet, new IPEndPoint(destination, 0));
                    byte[] buffer = new byte[512];
                    int bytesReceived = socket.ReceiveFrom(buffer, ref remoteEndPoint);
                    stopwatch.Stop();

                    if (bytesReceived > 0)
                    {
                        lastAddress = ((IPEndPoint)remoteEndPoint).Address;
                        Console.Write($"{stopwatch.ElapsedMilliseconds} ms\t");
                    }
                    else
                    {
                        Console.Write("* \t");
                    }
                }
                catch
                {
                    Console.Write("* \t");
                }
            }
            Console.WriteLine(lastAddress != null ? lastAddress.ToString() : "*");

            if (lastAddress != null && lastAddress.Equals(destination))
            {
                break;
            }
        }
    }

    private static byte[] CreateIcmpPacket()
    {
        byte[] packet = new byte[IcmpHeaderSize];
        packet[0] = 8;
        packet[1] = 0;
        Array.Copy(BitConverter.GetBytes(sequenceNumber), 0, packet, 6, 2);
        sequenceNumber++;
        ushort checksum = ComputeChecksum(packet);
        Array.Copy(BitConverter.GetBytes(checksum), 0, packet, 2, 2);
        return packet;
    }

    private static ushort ComputeChecksum(byte[] buffer)
    {
        int sum = 0;
        for (int i = 0; i < buffer.Length; i += 2)
        {
            sum += BitConverter.ToUInt16(buffer, i);
        }
        while ((sum >> 16) > 0)
        {
            sum = (sum & 0xFFFF) + (sum >> 16);
        }
        return (ushort)~sum;
    }
}