using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeapMotionDataSender
{
    internal class SPListener
    {
        public string SPPort;
        public int SPSpeed;

        SerialPort data_stream = new SerialPort();

        private string receivedstring;

        public string[] data;

        public void Init()
        {
            data_stream = new SerialPort(SPPort, SPSpeed);
        }

        public void Open()
        {
            data_stream.Open();
        }

        public void GetData()
        {
            receivedstring = data_stream.ReadLine();
            data = receivedstring.Split(',');
            StringBuilder sb = new StringBuilder();

            foreach (var item in data)
            {
                sb.Append(item);
                sb.Append(",");
            }
            Debug.WriteLine(sb.ToString());
        }

        public bool isOpen()
        {
            return data_stream.IsOpen;
        }

        public void Close()
        {
            data_stream.Close();
        }
    }
}
