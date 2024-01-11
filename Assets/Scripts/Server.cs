using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using AssemblyCSharp.Assets.Scripts;

public class Server : NetworkManager
{
    private TcpListener tcpServer;


    public Server(GameManager manager) : base(manager) { }


    public override void StartNetworkManager()
    {
        StartListener();
    }


    public async void StartListener()
    {
        byte[] buffer = new byte[256];

        try
        {   // pronalazak lokalne IP adrese 
            IPAddress myIpAddress = GetLocalIPAddress(); // IPAddress.Loopback;
            // instanciranje novog tcpServera koji čeka za TCP vezu na predefiniranom protu
            tcpServer = new TcpListener(myIpAddress, Constants.PORT);

            if (myIpAddress != null)
            {
                await Task.Run(() => ListenForBroadcast());
                // pohranjivanje strujanja koje dolazi iz tcpClienta
                NetworkStream networkStream = tcpClient.GetStream();
                int read;

                bool close = false;
                while (!close && (read = await networkStream.ReadAsync(buffer)) != 0)
                {
                    ProtocolData recievedUnit = new() { };
                    //dobijanje koda poruke
                    recievedUnit.messageCode = (ProtocolData.MessageCode)BitConverter.ToInt32(buffer, 0);
                    // dobijanje mjesta koje je odabrano
                    recievedUnit.space = (ProtocolData.MoveSpace)BitConverter.ToInt32(buffer, 4);

                    ProtocolData respHeader = new() { };
                    byte[] responseBuff = new byte[256];

                    switch (recievedUnit.messageCode)
                    {
                        case ProtocolData.MessageCode.SYNC:
                            {
                                respHeader.messageCode = ProtocolData.MessageCode.SYNC;
                                respHeader.space = ProtocolData.MoveSpace.NULL_SPACE;

                                Buffer.BlockCopy(BitConverter.GetBytes((int)respHeader.messageCode), 0, responseBuff, 0, 4);
                                Buffer.BlockCopy(BitConverter.GetBytes((int)respHeader.space), 0, responseBuff, 4, 4);

                                networkStream.Write(responseBuff, 0, 8);
                                break;
                            }

                        case ProtocolData.MessageCode.TURN:
                            // slučaj početka igre
                            {
                                //kreiranje odgovora te punjenje podataka u isti
                                respHeader.messageCode = ProtocolData.MessageCode.TURN;
                                respHeader.space = ProtocolData.MoveSpace.NULL_SPACE;

                                // pretvaranje u bitove
                                Buffer.BlockCopy(BitConverter.GetBytes((int)respHeader.messageCode), 0, responseBuff, 0, 4);
                                Buffer.BlockCopy(BitConverter.GetBytes((int)respHeader.space), 0, responseBuff, 4, 4);

                                // kreirajte random varijablu kako bi odlučili čiji je krug
                                // pohranite ostatak od dijeljenja s dva u novu varijablu result (ako je ostatak 0 igra server, a ako je ostatak 1 igra klijent)
                                int result = new System.Random().Next(2);

                                // pretvaranje u bitove te kopiranje u responseBuffer.
                                Buffer.BlockCopy(BitConverter.GetBytes(result), 0, responseBuff, 8, 4);

                                // započnite igru kroz gameManager
                                gameManager.BeginGame();

                                //ako je rezultat nula započinje igrač koji je server odnosno uključuje se ploča kroz EnableBoard()
                                if (result == 0)
                                {
                                    gameManager.EnableBoard();
                                }

                                // pošaljite responseBuff klijentu
                                networkStream.Write(responseBuff, 0, 12);

                                //prekinite switch
                                break;
                            }

                        case ProtocolData.MessageCode.MOVE:
                            {
                                // izvršite potez putem gameManagera te na odgovarajuće polje
                                gameManager.ExecuteMove((int)recievedUnit.space);
                                if (!gameManager.gameFinished)
                                {
                                    // uključite ploču da ovaj igrač može odigrati
                                    gameManager.EnableBoard();
                                }

                                // prekinite switch
                                break;
                            }

                        case ProtocolData.MessageCode.EXIT:
                            {
                                //zatvorite mrežno strujanje
                                networkStream.Close();

                                // postavite zastavicu da da je igra zatvorena
                                close = true;

                                // prekinite switch
                                break;
                            }
                    }
                }
            }
        }
        catch (SocketException exc)
        {
            Debug.Log("SocketException from StartListener: " + exc.Message);
        }
        finally
        {
            if (udpClient != null)
            {
                udpClient.Close();
            }

            if (tcpClient != null)
            {
                tcpClient.Close();
            }

            if (tcpServer != null)
            {
                tcpServer.Stop();
            }

            SceneManager.LoadScene(0);
        }
    }


    IPAddress GetLocalIPAddress()
    {
        // instancirajte novu varijablu tipa IPAdress te je postavite na null
        IPAddress localAddress = null;

        try
        {
            // kreirajte novi socket tipa Dgram te predviđen za IP mrežu
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {

                // spojite socket s Google poslužiteljem 8.8.8.8 na pot 65530
                s.Connect("8.8.8.8", 65530);
                // provjera je li krajnja točka socketa IP krajnja točka
                if (s.LocalEndPoint is IPEndPoint endPoint)
                {
                    //postavite varijablu lokalne adrese na adresu endpointa
                    localAddress = endPoint.Address;
                }
            }
        }
        catch (SocketException exc)
        {
            Debug.Log("SocketException from GetLocalIPAddress: " + exc.Message);
        }

        // vratite kao rezultat adresu
        return localAddress;
    }


    void ListenForBroadcast()
    {
        // definirajte varijablu responseData u koju ćete pohraniti odgovor "TIC" kako bi klijent znao da se radi o poslužitelju
        byte[] responseData = Encoding.ASCII.GetBytes("TIC");

        // definirajte varjablu clientEP koja će biti tipa IPEndpoint te koja će poprimiti vrijednost od nadolazećeg paketa.
        IPEndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);

        // definirajte varijablu udpClient koja će biti tipa udpCient te će čekati na portu predefiniranom u strukturi Constants nadklase NetworkManager
        UdpClient udpClient = new UdpClient(Constants.PORT);

        // inicirajte petlju
        while (true)
        {
            //definirajte varijablue requestData u koju se pohranjuje dolazna informacija iz novog udpCLienta
            byte[] requestData = udpClient.Receive(ref clientEP);

            // provjera je li u dolaznoj informaciji poruka TACTOE
            if (Encoding.ASCII.GetString(requestData) == "TACTOE")
            {
                // pokrenit tcpServer
                tcpServer.Start();

                // odgovorite na poruku putem udpCLeinta te pošaljite responseData ("TIC) na adresu s koje je došla poruka
                udpClient.Send(responseData, responseData.Length, clientEP);

                // postavite tcpClienta da prima poruke
                tcpClient = tcpServer.AcceptTcpClient();

                // prekinite petlju
                break;
            }
        }
        // zatvorite udpClienta
        udpClient.Close();
    }
}
