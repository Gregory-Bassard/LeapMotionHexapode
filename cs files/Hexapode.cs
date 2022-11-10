using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.IO.Ports;
using System.Diagnostics;

namespace LeapMotionDataSender
{
    internal class Hexapode
    {
        private int minAngle;
        private int maxAngle;
        private int portSpeed;
        private int[] servoPos = new int[6];
        private string port;
        private SerialPort portSerie;
        private bool isConnect = false;
        private double height;

        //Plaque de base (0,0) au centre de la plaque
        public double rayVerBase; //Rayon du cercle des vérins
        public double alphaBase; //distance angulaire entre 2 vérins voisins
        public double betaBase;
        //Plateforme (0,0) au centre de la plaque
        public double rayVerPlat; //Rayon du cercle des vérins
        public double alphaPlat; //distance angulaire entre 2 vérins voisins
        public double betaPlat;

        Point3D centreRotation;

        //Degrees of freedom
        public double pitch = 0; //avant-arriere rotation (pitch, axe Y)
        public double roll = 0;  //gauche-droite rotation (roll, axe X)
        public double yaw = 0;   //tourner sur nous-même (yaw, axe Z)
        public double X = 0;     //avant-arriere déplacement (axe X, en avant positif)
        public double Y = 0;     //gauche-droite déplacement (axe Y, gauche positif)
        public double Z = 0;     //haut-bas déplacement (axe Z , haut positif)

        //liste de position des verrins
        Point3D[] posVerBase = new Point3D[6]; //TODO: ne pas oublier de modifier le point de bas
        Point3D[] posVerPlat = new Point3D[6];
        Matrix3D modificationMatrix; //matrice de modification final
        public double[] lengthVer = new double[6];  //longeur verrin
        //Calcul des angles des servos
        public double longBrasLev = 22;//longueur du bras de levier du servo
        public double longTringle = 174; //longueur des tringles entre la platform et la base 
        public double[] AngleServo = new double[6];

        //trMethode1 speed = 2 | trMethode2 speed = 0.1
        private double speed = 0.1; //vitesse de déplacement du offset

        public int dz = 15;

        public void Update(double roll, double pitch, double yaw, double x, double y, double z, bool debug = true)
        {

            if (Program.HEXAPODE_TRANSLATION_METHODE == 1)
                TranslationMethode1(x, y, z);
            else if (Program.HEXAPODE_TRANSLATION_METHODE == 2)
                TranslationMethode2(x, -z, y);

            //Hexapode Rotation (Quaternion)
            this.roll = -roll;
            this.pitch = pitch;
            this.yaw = -yaw;

            LimitCheck(roll, pitch, yaw, x, y, z);

            if (Program.MYDEBUG == 2 && debug == true)
            {
                //Console.WriteLine($"data (x : {x}, y : {y}, z : {z}, roll : {roll}, pitch : {pitch}, yaw : {yaw})");
                //Console.WriteLine($"hexa (x : {X}, y : {Y}, z : {Z}, roll : {this.roll}, pitch : {this.pitch}, yaw : {this.yaw})");
                Console.WriteLine($"data (x : {x}, y : {y}, z : {z}) ; hexa (x : {X}, y : {Y}, z : {Z})");
            }

            if (isConnect)
                CalculPosHexapode();
            CalculPosVer();
        }
        public void LimitCheck(double roll, double pitch, double yaw, double x, double y, double z)
        {
            if (roll > 15)
                this.roll = -15;
            else if (roll < -15)
                this.roll = 15;

            if (pitch > 15)
                this.pitch = 15;
            else if (pitch < -15)
                this.pitch = -15;

            if (yaw > 40)
                this.yaw = -40;
            else if (yaw < -40)
                this.yaw = 40;

            if (Z > 150)
                Z = 150;
            else if (Z < -10)
                Z = -10;

            if (X > 125)
                X = 125;
            else if (X < -125)
                X = -125;

            if (Y > 125)
                Y = 125;
            else if (Y < -125)
                Y = -125;
        }
        public void TranslationMethode2(double x, double y, double z)
        {

            //X
            if (x < X - dz)
                X -= Math.Abs((X - x) * speed);
            if (x > X + dz)
                X += Math.Abs((X - x) * speed);
            //Y
            if (y < Y - dz)
                Y -= Math.Abs((Y - y) * speed);
            if (y > Y + dz)
                Y += Math.Abs((Y - y) * speed);
            //Z
            if (z - 200 < Z - dz)
                Z -= Math.Abs((Z - z) * speed);
            if (z - 200 > Z + dz)
                Z += Math.Abs((Z - z) * speed);

        }
        public void TranslationMethode1(double x, double y, double z)
        {
            if (x > 75)
                X += speed;
            else if (x < -75)
                X -= speed;

            if (y > 425)
                Z += speed;
            else if (y < 175)
                Z -= speed;

            if (z < -75)
                Y += speed;
            else if (z > 75)
                Y -= speed;
        }
        public void Init()
        {
            string[] lines = System.IO.File.ReadAllLines(@"./Config.ini");
            foreach (string line in lines)
            {
                if (line.Contains("="))
                {
                    string[] result = line.Split('=');
                    if (result[0] == "minAngle")
                        minAngle = Convert.ToInt32(result[1]);
                    else if (result[0] == "maxAngle")
                        maxAngle = Convert.ToInt32(result[1]);
                    else if (result[0] == "portSpeed")
                        portSpeed = Convert.ToInt32(result[1]);
                    else if (result[0] == "port")
                        port = result[1];
                    else if (result[0] == "rayVerBase")
                        rayVerBase = float.Parse(result[1]);
                    else if (result[0] == "angVerBase")
                    {
                        alphaBase = float.Parse(result[1]);
                        betaBase = 2 * Math.PI / 3 - alphaBase;
                    }
                    else if (result[0] == "rayVerPlat")
                        rayVerPlat = float.Parse(result[1]);
                    else if (result[0] == "angVerPlat")
                    {
                        alphaPlat = float.Parse(result[1]);
                        betaPlat = 2 * Math.PI / 3 - alphaPlat;
                    }
                    else if (result[0] == "height")
                    {
                        height = float.Parse(result[1]);
                        centreRotation.Z = height;
                    }
                    else if (result[0] == "longBrasLev")
                        longBrasLev = float.Parse(result[1]);
                    else if (result[0] == "longTringle")
                        longTringle = float.Parse(result[1]);
                }
            }

            connect();
            CalculPosHexapode();
        }
        public void connect()
        {
            if (!isConnect)
            {
                portSerie = new SerialPort(port, portSpeed);//crée le port série
                try
                {
                    portSerie.Open(); // Ouverture du port
                }
                catch (System.UnauthorizedAccessException)//si exception fin de la méthode
                {
                    return;
                }
                isConnect = true;
            }
            else
            {
                if (port != null && portSerie.IsOpen)
                    portSerie.Close(); // fermeture du port
                isConnect = false;
            }
        }
        private void CalculPosHexapode()
        { //Calcule les angle de l'hexapode en fonction des valeur (pitch,yaw,roll,X,Y,Z)
            Point3D[] oldPosVerPlat = posVerPlat;
            Vector3D offset = new Vector3D(X, Y, Z);//déplacement en x,y,z
            modificationMatrix = Calculation.GetModificationMatrix(yaw, pitch, roll, centreRotation, offset); //matrice de modification qui contient les mouvements(déplacment et rotation)

            for (int i = 0; i < 6; i++)
            {
                posVerPlat[i] = Point3D.Multiply(posVerPlat[i], modificationMatrix); //ajout de la matrice de modification

                lengthVer[i] = Math.Sqrt(Math.Pow(posVerPlat[i].X - posVerBase[i].X, 2) + Math.Pow(posVerPlat[i].Y - posVerBase[i].Y, 2) + Math.Pow(posVerPlat[i].Z - posVerBase[i].Z, 2));
            }
            centreRotation.X += X - centreRotation.X;
            centreRotation.Y += Y - centreRotation.Y;
            centreRotation.Z += (Z + height) - centreRotation.Z;
            SendPos();
        }
        private void SendPos() //renvoie les positions des angles sur le port serie
        {
            if (isConnect)
            {
                try
                {
                    for (int i = 0; i < 6; i++)
                    {
                        lengthVer[i] = (lengthVer[i] / 10 - 28.5) / 20 * 3.3;
                        lengthVer[i] = lengthVer[i] > 3.3 ? 3.3 : lengthVer[i];
                        lengthVer[i] = lengthVer[i] < 0 ? 0 : lengthVer[i];
                    }

                    string data = $"{lengthVer[0]:0.000},{lengthVer[1]:0.000},{lengthVer[2]:0.000},{lengthVer[3]:0.000},{lengthVer[4]:0.000},{lengthVer[5]:0.000}";
                    portSerie.WriteLine(data); // envoie des angle
                    //Console.WriteLine($"{data}  {delta.TotalMilliseconds}ms");
                    portSerie.DiscardInBuffer();
                }
                catch (System.InvalidOperationException)
                {
                    isConnect = false;
                }
            }
                
        }
        private void CalculPosVer()
        {
            posVerBase[0] = new Point3D(-(-rayVerBase) * Math.Sin(-(alphaBase / 2 + betaBase)), (-rayVerBase) * Math.Cos(-(alphaBase / 2 + betaBase)), 0);//verrin 1
            posVerBase[1] = new Point3D(-(-rayVerBase) * Math.Sin(-alphaBase / 2), (-rayVerBase) * Math.Cos(-alphaBase / 2), 0);//verrin 2
            posVerBase[2] = new Point3D(-(-rayVerBase) * Math.Sin(alphaBase / 2), (-rayVerBase) * Math.Cos(alphaBase / 2), 0);//verrin 3
            posVerBase[3] = new Point3D(-(-rayVerBase) * Math.Sin(alphaBase / 2 + betaBase), (-rayVerBase) * Math.Cos(alphaBase / 2 + betaBase), 0);//verrin 4
            posVerBase[4] = new Point3D(-(-rayVerBase) * Math.Sin(alphaBase * 1.5 + betaBase), (-rayVerBase) * Math.Cos(alphaBase * 1.5 + betaBase), 0);//verrin 5
            posVerBase[5] = new Point3D(-(-rayVerBase) * Math.Sin(-(alphaBase * 1.5 + betaBase)), (-rayVerBase) * Math.Cos(-(alphaBase * 1.5 + betaBase)), 0);//verrin 6

            posVerPlat[0] = new Point3D(-rayVerPlat * Math.Sin(alphaPlat / 2 + betaPlat), rayVerPlat * Math.Cos(alphaPlat / 2 + betaPlat), height);//verrin 1
            posVerPlat[1] = new Point3D(-rayVerPlat * Math.Sin(alphaPlat * 1.5 + betaPlat), rayVerPlat * Math.Cos(alphaPlat * 1.5 + betaPlat), height);//verrin 2
            posVerPlat[2] = new Point3D(-rayVerPlat * Math.Sin(-(alphaPlat * 1.5 + betaPlat)), rayVerPlat * Math.Cos(-(alphaPlat * 1.5 + betaPlat)), height);//verrin 3
            posVerPlat[3] = new Point3D(-rayVerPlat * Math.Sin(-(alphaPlat / 2 + betaPlat)), rayVerPlat * Math.Cos(-(alphaPlat / 2 + betaPlat)), height);//verrin 4
            posVerPlat[4] = new Point3D(-rayVerPlat * Math.Sin(-alphaPlat / 2), rayVerPlat * Math.Cos(-alphaPlat / 2), height);//verrin 5
            posVerPlat[5] = new Point3D(-rayVerPlat * Math.Sin(alphaPlat / 2), rayVerPlat * Math.Cos(alphaPlat / 2), height);//verrin 6
        }
    }
}
