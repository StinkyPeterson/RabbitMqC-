using System;
using System.Messaging;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace MSMQ
{
    public partial class frmMain : Form
    {
        private MessageQueue q = null;      // очередь сообщений, в которую будет производиться запись сообщений

        private MessageQueue receiveQueue = null;
        private Thread t = null;
        private bool _continue = true;
        private string Login { get; set; }


        // конструктор формы
        public frmMain()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (MessageQueue.Exists(tbPath.Text))
            {
                // если очередь, путь к которой указан в поле tbPath существует, то открываем ее
                q = new MessageQueue(tbPath.Text);

                if (!string.IsNullOrEmpty(tbLogin.Text))
                {
                    string path = Dns.GetHostName() + $"\\private$\\{tbLogin.Text}";    // путь к очереди сообщений, Dns.GetHostName() - метод, возвращающий имя текущей 
                    if (!MessageQueue.Exists(path))
                    {

                        receiveQueue = MessageQueue.Create(path);
                        receiveQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });

                        Login = tbLogin.Text;
                        btnSend.Enabled = true;
                        btnConnect.Enabled = false;

                        q.Send(Login, "system_connect");
                        // создание потока, отвечающего за работу с очередью сообщений
                        t = new Thread(ReceiveMessage);
                        t.Start();
                    }
                    else
                    {
                        MessageBox.Show("Такой пользователь уже есть в чате");
                    }
                }
            }
            else
                MessageBox.Show("Указан неверный путь к очереди, либо очередь не существует");
        }

        private void ReceiveMessage()
        {
            if (receiveQueue == null)
                return;

            System.Messaging.Message msg = null;

            // входим в бесконечный цикл работы с очередью сообщений
            while (_continue)
            {
                if (receiveQueue.Peek() != null)   // если в очереди есть сообщение, выполняем его чтение, интервал до следующей попытки чтения равен 10 секундам
                    msg = receiveQueue.Receive(TimeSpan.FromSeconds(10.0));

                rtbMessages.Invoke((MethodInvoker)delegate
                {
                    if (msg != null)
                        rtbMessages.Text += "\n >> " + msg.Label + " : " + msg.Body;     // выводим полученное сообщение на форму
                });
                Thread.Sleep(500);          // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            // выполняем отправку сообщения в очередь
            q.Send(tbMessage.Text, Login);
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _continue = false;      // сообщаем, что работа с очередью сообщений завершена

            if (t != null)
            {
                t.Abort();          // завершаем поток
            }

            if (receiveQueue != null)
            {
                MessageQueue.Delete(q.Path);      // в случае необходимости удаляем очередь сообщений
            }
        }
    }
}