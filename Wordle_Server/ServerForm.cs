using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

namespace Wordle_Server
{
    public partial class ServerForm : Form
    {
        const int maxClients = 1;
        IPEndPoint IP;
        Socket server;
        List<Socket> clientList;
        Dictionary<Socket, string> clientDictionary = new Dictionary<Socket, string>();
        Dictionary<string, int> resultDictionary = new Dictionary<string, int>();

        //Random words
        int WordIndex = 0;
        string[] words;
        List<string> chosenWords = new List<string>();
        string[] allWords = File.ReadAllText("Wordlist.txt")
        .Split(
            new[] { ' ', '\n', '\r', ',', ';', ':', '.', '!', '?', '-' },
            StringSplitOptions.RemoveEmptyEntries
        );
        Random rand = new Random();
        public string ChoseWord()
        {
            string currentWord;
            WordIndex = rand.Next(0, allWords.Length);
            currentWord = allWords[WordIndex];
            while (chosenWords.Contains(currentWord))
            {
                WordIndex = rand.Next(0, allWords.Length);
                currentWord = allWords[WordIndex];
            }
            chosenWords.Add(currentWord);
            return currentWord;
        }
        public string[] ChoseNWord()
        {
            string[] words = new string[5];
            for (int i = 0; i < 5; i++)
            {
                words[i] = ChoseWord();
            }
            AddMessage("Words:\n" + words[0] + "\n" + words[1] + "\n" + words[2] + "\n" + words[3] + "\n" + words[4] + "\n");
            return words;
        }

        private string checkWord(int wordsIndex, string answer)
        {
            string answerCode = "";
            for (int i = 0; i < 5; i++)
            {
                char correctC = words[wordsIndex][i];
                char answerC = answer[i];

                if (correctC == answerC)
                {
                    answerCode += '2';
                }
                else if (words[wordsIndex].Contains(answerC))
                {
                    answerCode += '1';
                }
                else
                {
                    answerCode += '0';
                }
            }
            return answerCode;
        }

        private string resultGame()
        {
            string result = "";
            int i = 1;
            foreach (KeyValuePair<string, int> rs in resultDictionary.OrderByDescending(point => point.Value))
            {
                result += i.ToString() + ". " + rs.Key + "s, " + rs.Value.ToString() + "points\n";
                i++;
            }
            return result;
        }

        public ServerForm()
        {
            InitializeComponent();
            //CheckForIllegalCrossThreadCalls = false;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            clientList = new List<Socket>();
            IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(IP);
            Thread Listen = new Thread(() =>
            {
                try
                {
                    while (clientList.Count <= maxClients)
                    {
                        server.Listen(100);
                        Socket client = server.Accept();
                        AddMessage("A Client connected.\n");

                        Thread receive = new Thread(Receive);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch
                {
                    IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
            });
            Listen.IsBackground = true;
            Listen.Start();
            btnStart.Enabled = false;
            btnEnd.Enabled = true;
            AddMessage("Start listening...\n");
        }

        private void btnEnd_Click(object sender, EventArgs e)
        {
            server.Close();
            btnEnd.Enabled = false;
            btnStart.Enabled = true;
            AddMessage("Stop listening.\n");
        }

        private void Send(Socket client, string msg)
        {
            client.Send(Serialize(msg));
        }

        

        private void Receive(object obj)
        {
            Socket client = obj as Socket;
            clientList.Add(client);
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);
                    string message = (string)Deserialize(data);
                    
                    //Xu ly thong tin gui tu client
                    if (message != null)
                    {
                        AddMessage(message + "\n");
                        string[] msg = message.Split('-');

                        if (msg[0] == "<Ready>") //<Ready>-name
                        {
                            clientDictionary.Add(client, msg[1]);
                            if (clientDictionary.Count >= maxClients)
                            {
                                //Gui tin hieu bat dau game
                                foreach (Socket c in clientList)
                                {
                                    Send(c, "<StartGame>");
                                }
                                AddMessage("Start game.\n");
                            }
                        }
                        else if (msg[0] == "<Check>") //<Check>-name-wordsindex-answer
                        {
                            string check = "<CheckCode>-" + checkWord(Int32.Parse(msg[2]), msg[3]);
                            Send(client, check);
                            AddMessage("Replied.\n");
                        }
                        else if (msg[0] == "<EndGame>") //<EndGame>-name-point-time
                        {
                            resultDictionary.Add(msg[1] + ":   " + msg[3], Int32.Parse(msg[2]));
                            if (resultDictionary.Count >= maxClients)
                            {
                                string result = resultGame();
                                foreach (Socket c in clientList)
                                {
                                    Send(c, "<Rank>-" + result);
                                }
                                AddMessage("Replied.\n");
                                clientDictionary = new Dictionary<Socket, string>();
                                resultDictionary = new Dictionary<string, int>();
                                words = ChoseNWord();
                            }
                        }
                    }
                }
            }
            catch
            {
                clientList.Remove(client);
                clientDictionary.Remove(client);
                AddMessage("A client disconnected.\n");
                client.Close();
            }
        }

        private delegate void SafeCallDelegate(string text);

        private void AddMessage(string str)
        {
            if (tbxLog.InvokeRequired)
            {
                var d = new SafeCallDelegate(AddMessage);
                tbxLog.Invoke(d, new object[] { str });
            }
            else
            {
                tbxLog.Text += str;
            }

        }

        //Hàm phân mảnh dữ liệu cần gửi từ dạng string sang dạng byte để gửi đi
        private byte[] Serialize(object obj)
        {
            //khởi tạo stream để lưu các byte phân mảnh
            MemoryStream stream = new MemoryStream();
            //khởi tạo đối tượng BinaryFormatter để phân mảnh dữ liệu sang kiểu byte
            BinaryFormatter formatter = new BinaryFormatter();
            //phân mảnh rồi ghi vào stream
            formatter.Serialize(stream, obj);
            //từ stream chuyển các các byte thành dãy rồi cbi gửi đi
            return stream.ToArray();
        }

        //Hàm gom mảnh các byte nhận được rồi chuyển sang kiểu string để hiện thị lên màn hình
        private object Deserialize(byte[] data)
        {
            //khởi tạo stream đọc kết quả của quá trình phân mảnh 
            MemoryStream stream = new MemoryStream(data);
            //khởi tạo đối tượng chuyển đổi
            BinaryFormatter formatter = new BinaryFormatter();
            //chuyển đổi dữ liệu và lưu lại kết quả 
            return formatter.Deserialize(stream);
        }

        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (server != null && server.Connected)
            {
                server.Close();
            }
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {
            words = ChoseNWord();
        }

    }
}
