using Leap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeapMotionDataSender
{
    class ControllerListener
    {
        public SPSender spSender;
        private List<Frame> data = new List<Frame>();
        private long CountId = 0;
        public Hexapode hexapode;
        private int count = 0;
        private int restartingCount = 0;

        public void OnInit(object sender, ConnectionEventArgs args)
        {
            //Console.WriteLine("Service Connected");
        }

        public void OnConnect(object sender, DeviceEventArgs args)
        {
            //Console.WriteLine("Connected");
        }

        public void OnFrame(object sender, FrameEventArgs args)
        {
            char[] characters = System.Text.Encoding.ASCII.GetChars(new byte[] { 13 });
            char c = characters[0];

            Frame frame = args.frame;
            string msg = $"No Hand Detected{c}";

            if (frame.Hands.Count > 0 && count % 5 == 0)
            {
                data.Add(frame);
                if (data.Count > 12)
                {
                    data.RemoveAt(0);
                }

                if (data.Count == 12)
                {
                    float handsCount = 0;
                    int nbHand = 0;
                    float x = 0, y = 0, z = 0;
                    float pitch = 0, roll = 0, yaw = 0;

                    for (int i = data.Count - 10; i < data.Count; i++)
                    {
                        handsCount += data[i].Hands.Count;

                        x += data[i].Hands[0].PalmPosition.x;
                        y += data[i].Hands[0].PalmPosition.y;
                        z += data[i].Hands[0].PalmPosition.z;

                        pitch += data[i].Hands[0].Direction.Pitch * 180.0f / (float)Math.PI;
                        roll += data[i].Hands[0].PalmNormal.Roll * 180.0f / (float)Math.PI;
                        yaw += data[i].Hands[0].Direction.Yaw * 180.0f / (float)Math.PI;
                    }
                    nbHand = (int)Math.Floor(handsCount / 10);

                    x = x / 10;
                    y = y / 10;
                    z = z / 10;

                    pitch = pitch / 10;
                    roll = roll / 10;
                    yaw = yaw / 10;

                    msg = $"{CountId},{nbHand},{x},{y},{z},{pitch},{roll},{yaw}{c}";
                    CountId++;

                    if (Program.HEXAPODE)
                    {
                        hexapode.Update(roll, pitch, yaw, x, y, z);
                    }
                }
            }
            count++;
            if (Program.MYDEBUG == 1)
                Console.WriteLine(msg);
            if (Program.SP_TRANSMISSION)
                spSender.Send(msg);
        }
    }
}
