using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using AssemblyCSharp.Assets.Scripts;
using System.Threading;

public class Client : NetworkManager
{
    public Client(GameManager manager) : base(manager) { }


    public override void StartNetworkManager()
    {
        ConnectClient();
    }


    public async void ConnectClient()
    {
        byte[] buffer = new byte[256];

        try
        {
            IPAddress serverAddress = await Task.Run(() => SendBroadcast());
            if (serverAddress != null)
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(serverAddress, Constants.PORT);

                NetworkStream networkStream = tcpClient.GetStream();
                int read;

                SendSync(networkStream, buffer);

                bool close = false;
                while (!close && (read = await networkStream.ReadAsync(buffer)) != 0)
                {

                    ProtocolData recievedUnit = new() { };
                    recievedUnit.messageCode = (ProtocolData.MessageCode)BitConverter.ToInt32(buffer, 0);
                    recievedUnit.space = (ProtocolData.MoveSpace)BitConverter.ToInt32(buffer, 4);

                    ProtocolData respHeader = new() { };
                    byte[] responseBuff = new byte[256];

                    switch (recievedUnit.messageCode)
                    {
                        case ProtocolData.MessageCode.SYNC:
                            {
                                respHeader.messageCode = ProtocolData.MessageCode.TURN;
                                respHeader.space = ProtocolData.MoveSpace.NULL_SPACE;

                                Buffer.BlockCopy(BitConverter.GetBytes((int)respHeader.messageCode), 0, responseBuff, 0, 4);
                                Buffer.BlockCopy(BitConverter.GetBytes((int)respHeader.space), 0, responseBuff, 4, 4);

                                networkStream.Write(responseBuff, 0, 8);
                                break;
                            }

                        case ProtocolData.MessageCode.TURN:
                            {
                                // dohvaćanje rezultata iz buffera odgovora
                                int result = BitConverter.ToInt32(buffer, 8);

                                //započnite igru kroz gameManagera
                                gameManager.BeginGame();
                                // ako je rezultat jedan igru počinje klijent odnosno uključuje se ploča kroz EnableBoard() 
                                if (result == 1)
                                {
                                    gameManager.EnableBoard();
                                }

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

                        case ProtocolData.MessageCode.RESTART:
                            {
                                //inicijalizirajte ploču kroz gameManager
                                gameManager.InitializeBoard();

                                //započnite igru kroz gameManager
                                gameManager.BeginGame();

                                //pošaljite poruku o sinkronizaciji kroz networkStream
                                SendSync(networkStream, buffer);

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
            Debug.Log("SocketException caught in ConnectClient: " + exc.Message);
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

            SceneManager.LoadScene(0);
        }
    }


    private IPAddress SendBroadcast()
    {
        // definirajte varijablu requestData u koju ćete pohraniti odgovor "TACTOE" kako bi poslužitelj znao da se radi o klijentu
        byte[] requestData = Encoding.ASCII.GetBytes("TACTOE");

        // definirajte varjablu serverEp koja će biti tipa IPEndpoint te koja će poprimiti vrijednost od  paketa odgovora.
        IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, 0); // mozda ova

        // kreirajte byte polje koje će pohraniti povratnu inforaciju te ga inicijalizirajte na null
        byte[] responseData = null;

        // kreirajte varijablu udpClinet tipa UdpClient
        UdpClient udpClient = new UdpClient();

        // u novokreiranom udpClientu uključite opciju boradcast
        udpClient.EnableBroadcast = true;

        // postavite ReceiveTimeout u novom udpClientu na 5000
        udpClient.Client.ReceiveTimeout = 5000;

        // inicirajte petlju
        while (true)
        {
            // provjerite je li udpClient aktivan (ne null)
            if (udpClient != null)
            {
                // pošaljite requestData na broadcast IP adresu te port iz Constant polja
                udpClient.Send(requestData, requestData.Length, new IPEndPoint(IPAddress.Broadcast, Constants.PORT));

                // pokrenite periodički task - ostavite ovaj kod
                Task.Run(() =>
                    {
                        try
                        {
                            // pohranite  u variajblu response data dolazna informacija iz novog udpCLienta
                            responseData = udpClient.Receive(ref serverEp);
                        }
                        // hvatanje iznimke
                        catch (SocketException exc)
                        {
                            Debug.Log("SocketException caught in SendBroadcast, udpClient was closed or timed out: " + exc.Message);
                        }
                    }
                ).Wait(5000);

                // provjera je li postoji odgovor i je li u njemu "TIC"
                if (responseData != null && Encoding.ASCII.GetString(responseData) == "TIC")
                {
                    //zatvorite udpClienta
                    udpClient.Close();

                    // vratite identificiranu adresu poslužitelja
                    return serverEp.Address;
                }
            }
            else
                break;
        }

        return null;
    }


    private void SendSync(NetworkStream networkStream, byte[] buffer)
    {
        ProtocolData reqHeader = new() { };
        reqHeader.messageCode = ProtocolData.MessageCode.SYNC;
        reqHeader.space = ProtocolData.MoveSpace.NULL_SPACE;

        Buffer.BlockCopy(BitConverter.GetBytes((int)reqHeader.messageCode), 0, buffer, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes((int)reqHeader.space), 0, buffer, 4, 4);

        networkStream.Write(buffer, 0, 8);
    }
}
