using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace LeapMotionDataSender
{
    internal class SPSender
    {
        public string SPPort;
        public int SPSpeed;

        SerialPort data_stream = new SerialPort();

        private int count = 0;

        public void Open()
        {
            data_stream = new SerialPort(SPPort, SPSpeed);
            data_stream.Open();
        }

        public string Send(string data)
        {
            if (count % 10 == 0)
            {
                count = 0;
                if (data_stream.IsOpen)
                {
                    try
                    {
                        data_stream.WriteLine(data);
                        return "Data Sent";
                    }
                    catch (Exception e)
                    {
                        return e.ToString();
                    }
                }
                else
                    data_stream.Open();
            }else
                count++;
            return "Data not Sent";
        }

        public void Close()
        {
            data_stream.Close();
        }
    }
}
