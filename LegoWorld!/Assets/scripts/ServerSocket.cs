using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

public class ServerSocket {

    public int port;

    public ServerSocket() {
        port = 8888;
        Thread serverThread = new Thread(JobProcessor);
        string hostName = Dns.GetHostName();
        foreach (IPAddress test in Dns.GetHostEntry(hostName).AddressList)
        {
            Debug.Log("Deze: " + test.ToString());
        }
        Debug.Log("IP: " + Dns.GetHostEntry(hostName).AddressList[0].ToString());

        serverThread.Start();
    }

    void JobProcessor() {
        Debug.Log("Ik kom in mijn processing thread!");
        TcpListener serverSocket = new TcpListener(port);
        int requestCount = 0;
        TcpClient clientSocket = default(TcpClient);
        serverSocket.Start();
        Debug.Log(" >> Server Started");
        clientSocket = serverSocket.AcceptTcpClient();
        Debug.Log(" >> Accept connection from client");
        requestCount = 0;

        while ((true))
        {
            try
            {
                clientSocket = serverSocket.AcceptTcpClient();
                Debug.Log(" >> Accept connection from client");
                requestCount = requestCount + 1;
                NetworkStream networkStream = clientSocket.GetStream();
                byte[] bytesFrom = new byte[10025];
                networkStream.Read(bytesFrom, 0, (int)clientSocket.ReceiveBufferSize);
                string dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);
                dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
                Debug.Log(" >> Data from client" + dataFromClient);
                string serverResponse = "Last Message from client - " + dataFromClient;
                Byte[] sendBytes = Encoding.ASCII.GetBytes(serverResponse);
                networkStream.Write(sendBytes, 0, sendBytes.Length);
                networkStream.Flush();
                Debug.Log(" >> " + serverResponse);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                break;
            }
        }

        clientSocket.Close();
        serverSocket.Stop();
        Console.Write(" >> exit");
        Console.ReadLine();
    }

}
