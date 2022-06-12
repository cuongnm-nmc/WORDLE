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
        //Số lượng người chơi mặc định trong phòng chơi
        const int maxClients = 1;
        IPEndPoint IP;
        Socket server;
        List<Socket> clientList;
        Dictionary<Socket, string> clientDictionary = new Dictionary<Socket, string>();
        Dictionary<string, int> resultDictionary = new Dictionary<string, int>();

        //Phần hỗ trợ random 5 từ trong list từ có sẵn
        int WordIndex = 0;
        string[] words;
        List<string> chosenWords = new List<string>();
        //File chứa list từ
        string[] allWords = File.ReadAllText("Wordlist.txt")
        .Split(
            new[] { ' ', '\n', '\r', ',', ';', ':', '.', '!', '?', '-' },
            StringSplitOptions.RemoveEmptyEntries
        );
        Random rand = new Random();
        //Random 1 từ
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
        //Random 5 từ
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
        //Hàm kiểm tra từ đoán được gửi từ client và trả về code kiểm tra
        private string checkWord(int wordsIndex, string answer)
        {
            string answerCode = "";
            for (int i = 0; i < 5; i++)
            {
                char correctC = words[wordsIndex][i];
                char answerC = answer[i];
                //Nếu đúng ký tự và vị trí -> 2
                if (correctC == answerC)
                {
                    answerCode += '2';
                }
                //Nếu đúng ký tự và sai vị trí -> 1
                else if (words[wordsIndex].Contains(answerC))
                {
                    answerCode += '1';
                }
                //Nếu sai ký tự -> 0
                else
                {
                    answerCode += '0';
                }
            }
            //Trả về dãy code 5 số, tương đương kết quả của 5 ký tự trong từ đoán
            return answerCode;
        }
        //Hàm tổng hợp và tính toán bảng xếp hạng
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
        }
        //Event khi nhấn nút StartServer
        private void btnStart_Click(object sender, EventArgs e)
        {
            //Thiết lập kết nối
            clientList = new List<Socket>();
            IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(IP);
            //Luồng lắng nghe liên tục để kết nối nhiều clients
            Thread Listen = new Thread(() =>
            {
                try
                {
                    //Lắng nghe đến khi có đầy đủ người vào phòng
                    while (clientList.Count <= maxClients)
                    {
                        server.Listen(100);
                        Socket client = server.Accept();
                        AddMessage("A Client connected.\n");
                        //Với mỗi client kết nối thành công, tạo luồng lắng nghe riêng cho client đó
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
        //Event khi nhấn nút EndServer
        private void btnEnd_Click(object sender, EventArgs e)
        {
            server.Close();
            btnEnd.Enabled = false;
            btnStart.Enabled = true;
            AddMessage("Stop listening.\n");
        }
        //Hàm để gửi gói tin cho client chỉ định
        private void Send(Socket client, string msg)
        {
            client.Send(Serialize(msg));
        }
        //Luồng lắng nghe liên tục từ mỗi client
        private void Receive(object obj)
        {
            Socket client = obj as Socket;
            clientList.Add(client);
            try
            {
                while (true)
                {
                    //Tạo mảng byte để nhận dữ liệu
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);
                    //Gom mảnh dữ liệu thành dạng string
                    string message = (string)Deserialize(data);
                    
                    if (message != null)
                    {
                        //Hiển thị gói tin nhận được trên log
                        AddMessage(message + "\n");
                        //Phân tách nội dung của gói tin
                        string[] msg = message.Split('-');
                        //Gói tin tín hiệu sẵn sàng
                        if (msg[0] == "<Ready>") //<Ready>-Tên
                        {
                            clientDictionary.Add(client, msg[1]);
                            if (clientDictionary.Count >= maxClients)
                            {
                                //Khi tất cả người chơi trong phòng sẵn sàng
                                //Gửi tín hiệu bắt đầu bàn đấu
                                //<StartGame>
                                foreach (Socket c in clientList)
                                {
                                    Send(c, "<StartGame>");
                                }
                                AddMessage("---START GAME---\n");
                            }
                        }
                        //Gói tin chứa từ đoán cần kiểm tra
                        else if (msg[0] == "<Check>") //<Check>-Tên-ThứTựTừ-TừĐoán
                        {
                            //Gửi lại code kiểm tra
                            //<CheckCode>-Code
                            string check = "<CheckCode>-" + checkWord(Int32.Parse(msg[2]), msg[3]);
                            Send(client, check);
                            AddMessage("Replied-" + msg[1] + "\n");
                        }
                        //Gói tin tín hiệu kết thúc phần chơi
                        else if (msg[0] == "<EndGame>") //<EndGame>-Tên-Điểm-ThờiGianCònLại
                        {
                            resultDictionary.Add(msg[1] + ":   " + msg[3], Int32.Parse(msg[2]));
                            if (resultDictionary.Count >= maxClients)
                            {
                                //Nếu tất cả người chơi hoàn thành phần chơi
                                //Tính toán và gửi bảng xếp hạng
                                //<Rank>-BXH
                                string result = resultGame();
                                foreach (Socket c in clientList)
                                {
                                    Send(c, "<Rank>-" + result);
                                }
                                AddMessage("---END GAME---\n");
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
        //2 hàm để cập nhật log an toàn khi chương trình có đa luồng
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

        //Gom mảnh cho việc nhận
        private object Deserialize(byte[] data)
        {
            //Tạo stream để lưu trữ mảng byte đầu vào
            MemoryStream stream = new MemoryStream(data);
            //Dùng BinaryFormatter để gom mảnh
            BinaryFormatter bf = new BinaryFormatter();
            //Gom mảnh và trả về dữ liệu
            return bf.Deserialize(stream);
        }
        //Phân mảnh cho việc gửi
        private byte[] Serialize(object obj)
        {
            //Tạo stream để lưu trữ mảng byte đầu ra
            MemoryStream stream = new MemoryStream();
            //Dùng BinaryFormatter để phân mảnh
            BinaryFormatter bf = new BinaryFormatter();
            //Phân mảnh dữ liệu thành 1 mảng byte và lưu vào stream
            bf.Serialize(stream, obj);
            //Trả về 1 mảng byte
            return stream.ToArray();
        }
        //Event khi đóng form
        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (server != null && server.Connected)
            {
                server.Close();
            }
        }
        //Event khi mở form
        private void ServerForm_Load(object sender, EventArgs e)
        {
            words = ChoseNWord();
        }

    }
}
