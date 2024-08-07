//박싱, 언박싱, 업캐스팅, 다운캐스팅
public partial class Program
{
    public static void Main()
    {
        int number = 42;
        Int32 boxed = number; //Boxing
        int unboxed = boxed; //UnBoxing

        object obj = number; //UpCasting, Boxing
        int downed = (int)obj; //강제형변환, DownCasting


    }
}

------------------------------------------------------------------------------
//string과 stringbuilder 비교
using System.Text;

public partial class Program
{
    public static void Main()
    {
        string str1 = "Hello World";
        string str2 = new string("Hello World");
        string str3 = "Hello World";
        string str4 = "Hello World";
        string str5 = new string("Hello World");

        object obj1 = new object();
        object obj2 = new object();

        StringBuilder sb1 = new StringBuilder("Hello World");
        StringBuilder sb2 = new StringBuilder("Hello World");

        Console.WriteLine($"str1 : {str1.GetHashCode()}");
        Console.WriteLine($"str2 : {str2.GetHashCode()}");
        Console.WriteLine($"str3 : {str3.GetHashCode()}");
        Console.WriteLine($"str4 : {str4.GetHashCode()}");
        Console.WriteLine($"str4 : {str5.GetHashCode()}");
        Console.WriteLine();

        Console.WriteLine($"obj1 : {obj1.GetHashCode()}");
        Console.WriteLine($"obj2 : {obj2.GetHashCode()}");
        Console.WriteLine();

        Console.WriteLine($"sb1 : {sb1.GetHashCode()} {sb1.ToString()}");
        Console.WriteLine($"sb2 : {sb2.GetHashCode()} {sb2.ToString()}");
    }
}
------------------------------------------------------------------------------
//깊은 복사 얇은 복사
using System;

namespace DeepCopy
{
    class MyClass
    {
        public int MyField1;
        public int MyField2;

        public MyClass DeepCopy()
        {
            MyClass newCopy = new MyClass();
            newCopy.MyField1 = this.MyField1;
            newCopy.MyField2 = this.MyField2;

            return newCopy;
        }
    }

    class MainApp
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Shallow Copy");

            {
                MyClass source = new MyClass();
                source.MyField1 = 10;
                source.MyField2 = 20;

                MyClass target = source;
                target.MyField2 = 30;

                Console.WriteLine($"{source.MyField1} {source.MyField2}");
                Console.WriteLine($"{target.MyField1} {target.MyField2}");
            }

            Console.WriteLine("Deep Copy");

            {
                MyClass source = new MyClass();
                source.MyField1 = 10;
                source.MyField2 = 20;

                MyClass target = source.DeepCopy();
                target.MyField2 = 30;

                Console.WriteLine($"{source.MyField1} {source.MyField2}");
                Console.WriteLine($"{target.MyField1} {target.MyField2}");
            }
        }
    }
}
------------------------------------------------------------------------------
//
namespace TrapicLight03
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            ChangeSingodoong(0);
        }
        public void ChangeSingodoong(int Color)
        {
            switch (Color)
            {
                case 0:
                    pictureBox1.Image = Image.FromFile(System.Environment.CurrentDirectory + "/신호등(준비중).png");
                    break;
                case 1:
                    pictureBox1.Image = Image.FromFile(System.Environment.CurrentDirectory + "/신호등(빨간색).png");
                    break;
                case 2:
                    pictureBox1.Image = Image.FromFile(System.Environment.CurrentDirectory + "/신호등(노란색).png");
                    break;
                case 3:
                    pictureBox1.Image = Image.FromFile(System.Environment.CurrentDirectory + "/신호등(녹색).png");
                    break;
            }
        }
        int sinhoodoong_Color = 1;
        bool flag = true;
        private void timer1_Tick(object sender, EventArgs e)
        {
            ChangeSingodoong(sinhoodoong_Color);
            if (sinhoodoong_Color == 3)
                flag = false;
            else if (sinhoodoong_Color == 1)
                flag = true;
            if (flag == true)
                sinhoodoong_Color++;
            else if (flag == false)
                sinhoodoong_Color--;
        }
    }
}

