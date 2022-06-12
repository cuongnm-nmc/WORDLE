using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Wordle
{
    public partial class GameUI : Form
    {
        IPEndPoint IP;
        Socket client;

        string name;
        string answerCode = null;
        string result = null;
        int points = 0;
        int CountDown = 150;
        bool gameProcess = false;

        List<TextBox[]> rows = new List<TextBox[]>();
        TextBox[] row1 = new TextBox[5];
        TextBox[] row2 = new TextBox[5];
        TextBox[] row3 = new TextBox[5];
        TextBox[] row4 = new TextBox[5];
        TextBox[] row5 = new TextBox[5];
        TextBox[] row6 = new TextBox[5];
        int correctLetters = 0;
        int rowIndex = 0, letterIndex = 0, wordIndex = 0;
        
        public GameUI()
        {
            InitializeComponent();
        }

        private void TextEvent(object sender, EventArgs e)
        {
            //Nothing
        }
        //Event để tính thời gian
        private void TimerClient_Tick(object sender, EventArgs e)
        {
            if (gameProcess)
            {
                if (CountDown > 0)
                {
                    CountDown--;
                    lblTimer.Text = CountDown.ToString();
                    lblPoints.Text = points.ToString();
                }
                else if (CountDown <= 0)
                {
                    ResetAll();
                    GameOver();
                }
                else { }
            }
        }
        //Hàm kết thúc một bàn chơi
        private void GameOver()
        {
            gameProcess = false;
            //Gữi tín hiệu kết thúc phần chơi của mình cho server
            //<EndGame>-Tên-Điểm-ThờiGianCònLại
            Send("<EndGame>-" + name + "-" + points + "-" + CountDown);

            lblTimer.Text = "0";
            lblPoints.Text = "0";
            
            answerCode = null;
            wordIndex = 0;
            points = 0;
            CountDown = 150;
            //Chờ đến khi nhận được bảng xếp hạng
            while (true)
            {
                if (result != null)
                {
                    break;
                }
            }
            //Hiện bảng xếp hạng, sau đó là giao diện chơi lại hoặc thoát
            MessageBox.Show(result, "RANKING", MessageBoxButtons.OK);
            result = null;

            btnReady.Enabled = true;
            btnGameUI.Enabled = true;
            groupJoin.Visible = true;
            groupJoin.Enabled = true;

        }
        //Hàm thiết lập lại các ô chữ
        private void ResetAll()
        {
            correctLetters = 0;
            rowIndex = 0;
            letterIndex = 0;
            //Word1
            word1_letter1.Clear();
            word1_letter2.Clear();
            word1_letter3.Clear();
            word1_letter4.Clear();
            word1_letter5.Clear();

            word1_letter1.BackColor = Color.Black;
            word1_letter2.BackColor = Color.Black;
            word1_letter3.BackColor = Color.Black;
            word1_letter4.BackColor = Color.Black;
            word1_letter5.BackColor = Color.Black;
            //Word2
            word2_letter1.Clear();
            word2_letter2.Clear();
            word2_letter3.Clear();
            word2_letter4.Clear();
            word2_letter5.Clear();

            word2_letter1.BackColor = Color.Black;
            word2_letter2.BackColor = Color.Black;
            word2_letter3.BackColor = Color.Black;
            word2_letter4.BackColor = Color.Black;
            word2_letter5.BackColor = Color.Black;
            //Word3
            word3_letter1.Clear();
            word3_letter2.Clear();
            word3_letter3.Clear();
            word3_letter4.Clear();
            word3_letter5.Clear();

            word3_letter1.BackColor = Color.Black;
            word3_letter2.BackColor = Color.Black;
            word3_letter3.BackColor = Color.Black;
            word3_letter4.BackColor = Color.Black;
            word3_letter5.BackColor = Color.Black;
            //Word4
            word4_letter1.Clear();
            word4_letter2.Clear();
            word4_letter3.Clear();
            word4_letter4.Clear();
            word4_letter5.Clear();

            word4_letter1.BackColor = Color.Black;
            word4_letter2.BackColor = Color.Black;
            word4_letter3.BackColor = Color.Black;
            word4_letter4.BackColor = Color.Black;
            word4_letter5.BackColor = Color.Black;
            //Word5
            word5_letter1.Clear();
            word5_letter2.Clear();
            word5_letter3.Clear();
            word5_letter4.Clear();
            word5_letter5.Clear();

            word5_letter1.BackColor = Color.Black;
            word5_letter2.BackColor = Color.Black;
            word5_letter3.BackColor = Color.Black;
            word5_letter4.BackColor = Color.Black;
            word5_letter5.BackColor = Color.Black;
            //Word6
            word6_letter1.Clear();
            word6_letter2.Clear();
            word6_letter3.Clear();
            word6_letter4.Clear();
            word6_letter5.Clear();

            word6_letter1.BackColor = Color.Black;
            word6_letter2.BackColor = Color.Black;
            word6_letter3.BackColor = Color.Black;
            word6_letter4.BackColor = Color.Black;
            word6_letter5.BackColor = Color.Black;

            if (wordIndex > 4)
            {
                GameOver();
            }  
        }
        //Event được gọi khi có bất kỳ phím nào được gõ từ bàn phím trong lúc trò chơi tiến hành
        private void KeyIsUp(object sender, KeyEventArgs e)
        {
            //Điều kiện để giới hạn Index của ký tự trên 1 từ
            if (letterIndex > 4)
            {
                letterIndex = 4;
            }
            else if (letterIndex < 0)
            {
                letterIndex = 0;
            }
            //Nếu phím gõ vào là ký tự chữ
            if ((e.KeyValue >= 65 && e.KeyValue <= 90))
            {
                if (letterIndex + 1 == 5 && rows[rowIndex][letterIndex].Text != "") ;
                else
                {
                    rows[rowIndex][letterIndex].Text = e.KeyCode.ToString();
                    letterIndex++;
                }
            }
            //Nếu phím gõ vào là phím Enter
            else if (e.KeyCode == Keys.Enter && letterIndex == 4 && rows[rowIndex][4].Text != "")
            {
                string answer = "";
                for (int i = 0; i < 5; i++)
                {
                    char answerC = Convert.ToChar(rows[rowIndex][i].Text[0]);
                    answer += answerC;
                }
                //Gửi từ vừa đoán đến server
                //<Check>-Tên-ThứTựTừ-TừĐoán
                Send("<Check>-" + name + "-" + wordIndex.ToString() + "-" + answer);
                //Chờ đến khi nhận được phản hồi
                while (true)
                {
                    if (answerCode != null)
                    {
                        break;
                    }
                }
                //Biểu thị màu bằng dãy code nhận từ server
                for (int i = 0; i < 5; i++)
                {
                    if (answerCode[i] == '2')
                    {
                        rows[rowIndex][i].BackColor = ColorTranslator.FromHtml("#019A01");
                        rows[rowIndex][i].ForeColor = Color.White;
                        correctLetters++;
                    }
                    else if (answerCode[i] == '1')
                    {
                        rows[rowIndex][i].BackColor = ColorTranslator.FromHtml("#FFC425");
                        rows[rowIndex][i].ForeColor = Color.White;
                    }
                    else if (answerCode[i] == '0')
                    {
                        rows[rowIndex][i].BackColor = ColorTranslator.FromHtml("#444444");
                        rows[rowIndex][i].ForeColor = Color.White;
                    }
                }
                answerCode = null;
                //Kiểm tra kết quả đoán
                if (correctLetters == 5)
                {
                    wordIndex++;
                    if (wordIndex < 5)
                    {
                        CountDown += 20;
                    }
                    points++;
                    ResetAll();
                }
                else if (correctLetters != 5 && rowIndex == 5)
                {
                    wordIndex++;
                    if (wordIndex < 5)
                        CountDown -= 20;
                    ResetAll();
                }
                else
                {
                    rowIndex++;
                    letterIndex = 0;
                    correctLetters = 0;
                }
            }
            //Nếu phím gõ vào là phím Backspace
            else if (e.KeyCode == Keys.Back)
            {
                if (letterIndex <= 4 && letterIndex >= 1)
                {
                    if (rows[rowIndex][4].Text != "")
                    {
                        rows[rowIndex][4].Text = "";
                    }
                    else if (letterIndex - 1 < 0) ;
                    else
                    {
                        rows[rowIndex][letterIndex - 1].Text = "";
                        letterIndex--;
                    }
                }
            }
        }
        //Event đóng form
        private void GameUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            TimerClient.Stop();
            if (client != null && client.Connected)
            {
                client.Close();
            }
        }
        //Event khi nhấn nút Connect
        private void btnConnect_Click(object sender, EventArgs e)
        {
            //Kiểm tra đã nhập tên người chơi hay chưa
            if (textBoxName.Text == null || textBoxName.Text == "")
            {
                MessageBox.Show("Please type your name!", "Warning", MessageBoxButtons.OK);
                textBoxName.Focus();
                return;
            }
            //Thiết lập kết nối
            IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            name = textBoxName.Text;
            try
            {
                //Kết nối đến server
                client.Connect(IP);
            }
            catch
            {
                MessageBox.Show("Fail to connect to server!", "Warning", MessageBoxButtons.OK);
                return;
            }
            //Tạo luồng để lắng nghe server liên tục
            Thread listen = new Thread(Receive);
            listen.IsBackground = true;
            listen.Start();
            btnConnect.Enabled = false;
            btnReady.Enabled = true;
            btnGameUI.Enabled = true;
            textBoxName.Enabled = false;
        }
        //Luồng lắng nghe server
        private void Receive()
        {
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
                        //Phân tách nội dung của gói tin
                        string[] msg = message.Split('-');
                        //Gói tin tín hiệu bắt đầu game
                        if (message == "<StartGame>")
                        {
                            MessageBox.Show("Game start! Press JOIN to play.", "", MessageBoxButtons.OK);
                            gameProcess = true;
                        }
                        //Gói tin chứa dãy code để kiểm tra từ đoán
                        else if (msg[0] == "<CheckCode>")
                        {
                            answerCode = msg[1];
                        }
                        //Gói tin chứa bảng xếp hạng
                        else if (msg[0] == "<Rank>")
                        {
                            result = msg[1];
                        }
                    } 
                }
            }
            catch
            {
                MessageBox.Show("You have disconnected!", "Warning", MessageBoxButtons.OK);
            }

        }
        //Hàm gửi gói tin cho server
        private void Send(string msg)
        {
            client.Send(Serialize(msg));
        }
        //Event khi nhấn nút JOIN
        private void btnGameUI_Click(object sender, EventArgs e)
        {
            if (!gameProcess)
            {
                MessageBox.Show("Waiting for other players...", "", MessageBoxButtons.OK);
                return;
            }
            groupJoin.Enabled = false;
            groupJoin.Visible = false;
            btnGameUI.Enabled = false;
        }
        //Event khi chạy form
        private void Form1_Load(object sender, EventArgs e)
        {
            TimerClient.Start();

            row1[0] = word1_letter1;
            row1[1] = word1_letter2;
            row1[2] = word1_letter3;
            row1[3] = word1_letter4;
            row1[4] = word1_letter5;
            rows.Add(row1);

            row2[0] = word2_letter1;
            row2[1] = word2_letter2;
            row2[2] = word2_letter3;
            row2[3] = word2_letter4;
            row2[4] = word2_letter5;
            rows.Add(row2);

            row3[0] = word3_letter1;
            row3[1] = word3_letter2;
            row3[2] = word3_letter3;
            row3[3] = word3_letter4;
            row3[4] = word3_letter5;
            rows.Add(row3);

            row4[0] = word4_letter1;
            row4[1] = word4_letter2;
            row4[2] = word4_letter3;
            row4[3] = word4_letter4;
            row4[4] = word4_letter5;
            rows.Add(row4);

            row5[0] = word5_letter1;
            row5[1] = word5_letter2;
            row5[2] = word5_letter3;
            row5[3] = word5_letter4;
            row5[4] = word5_letter5;
            rows.Add(row5);

            row6[0] = word6_letter1;
            row6[1] = word6_letter2;
            row6[2] = word6_letter3;
            row6[3] = word6_letter4;
            row6[4] = word6_letter5;
            rows.Add(row6);

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    rows[i][j].Text = "";
                    rows[i][j].ForeColor = Color.White;
                    rows[i][j].BackColor = Color.Black;
                    rows[i][j].Enabled = false;
                }
            }
        }
        //Event khi nhấn nút Ready
        private void btnReady_Click(object sender, EventArgs e)
        {
            //Gửi tín hiệu sẵn sàng cho server
            //<Ready>-Tên
            Send("<Ready>-" + name);
            btnReady.Enabled = false;
        }
        //Event khi nhấn nút Exit
        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }
        //Event để hiển thị tên người chơi
        private void textBoxName_TextChanged(object sender, EventArgs e)
        {
            labelName.Text= textBoxName.Text;
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
    }
}
