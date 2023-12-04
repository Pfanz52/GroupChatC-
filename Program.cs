using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.IO;


class Program
{
    private static List<Socket> clients = new List<Socket>();
    private static readonly object locker = new object();
    private static DESCryptoServiceProvider des;


    static void Main()
    {
        DESCryptoServiceProvider des = new DESCryptoServiceProvider();
    des.Padding = PaddingMode.Zeros;
        StartServer(des);
    }

    private static void StartServer(DESCryptoServiceProvider des)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 12345);
        server.Start();
        Console.WriteLine("Server started on port 12345");

        while (true)
        {
            TcpClient tcpClient = server.AcceptTcpClient();
            Console.WriteLine("Client connected");

            lock (locker)
            {
                clients.Add(tcpClient.Client);
            }

            Thread clientThread = new Thread(() => HandleClient(tcpClient,des));
            clientThread.Start();
        }
    }

    private static void HandleClient(TcpClient tcpClient,DESCryptoServiceProvider des)
    {
        NetworkStream stream = tcpClient.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Received: " + message);

            Broadcast(message, tcpClient.Client, des);
        }

        Console.WriteLine("Client disconnected");

        lock (locker)
        {
            clients.Remove(tcpClient.Client);
        }
    }

    private static void Broadcast(string message, Socket sender,DESCryptoServiceProvider des)
    {
        // Khởi tạo DESCryptoServiceProvider mỗi khi cần
    // DESCryptoServiceProvider des = new DESCryptoServiceProvider();
        // Mã hóa tin nhắn trước khi lưu vào tệp tin và gửi đến các client
    string encryptedMessage = EncryptMessage(message, des);
    SaveChatToFile(encryptedMessage);
        // Lưu nội dung chat vào tệp tin
    SaveChatToFile(message);
        lock (locker)
        {
            foreach (Socket client in clients)
            {
                if (client != sender)
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    client.Send(data, data.Length, SocketFlags.None);
                }
            }
        }
    }
    private static string EncryptMessage(string message,DESCryptoServiceProvider des)
    {
        if (des == null)
    {
        Console.WriteLine("DESCryptoServiceProvider is null. Please initialize it.");
        return string.Empty;
    }
        byte[] key = des.Key;
        byte[] iv = des.IV;

        try
        {
            using (ICryptoTransform encryptor = des.CreateEncryptor(key, iv))
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                byte[] encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);
                return Convert.ToBase64String(encryptedData);
            }
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"Error encrypting message: {ex.Message}");
            return string.Empty;
        }
    }

    private static string DecryptMessage(string encryptedMessage)
    {
        byte[] key = des.Key;
        byte[] iv = des.IV;

        try
        {
            using (ICryptoTransform decryptor = des.CreateDecryptor(key, iv))
            {
                byte[] encryptedData = Convert.FromBase64String(encryptedMessage);
                byte[] decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                return Encoding.UTF8.GetString(decryptedData);
            }
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"Error decrypting message: {ex.Message}");
            return string.Empty;
        }
    }
    private static void SaveChatToFile(string message)
    {
        string filePath = "chat_log.txt";

        // Ghi nội dung chat vào tệp tin
        using (StreamWriter writer = File.AppendText(filePath))
        {
            writer.WriteLine(message);
        }
    }
}
