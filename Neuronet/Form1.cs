using System;
using System.Windows.Forms;

namespace Neuronet
{
    public partial class Form1 : Form
    {
        static Random rd; //генератор рандомных чисел
        static double[,] w1; //веса между входом и скрытым слоем
        static double[,] w2; //веса между скрытым слоем и выходом
        static double[] b1; //веса смещения для скрытого слоя
        static double[] b2; //веса смещения для выходного слоя
        static double l_rate; //коэффициент обучения

        static int epoch; //кол-во эпох обучения
        static int train_size; //кол-во тренировочных данных
        static int test_size; //кол-во данных для тестирования

        //структура нейронной сети
        static int inp; //кол-во нейронов на входе
        static int hid; //кол-во нейронов на скрытом слое
        static int outs; //кол-во нейронов на выходе

        //промежуточные значения
        static double[] z_in; //выход на скрытом слое без функции активации
        static double[] z; //с функцией активации
        static double[] y_in; //выход сети без функции активации
        static double[] y; //с ней

        //значения для корректировки весов сети
        static double[] qy;
        static double[] qz;

        public Form1()
        {
            InitializeComponent();

            textBox1.Text = "0.1";
            textBox2.Text = "100";
            textBox3.Text = "500";
            textBox4.Text = "100";
            textBox5.Text = "0.01";
            textBox6.Text = "0.1";
            textBox7.Text = "0.5";
            textBox8.Text = "0.65";

            inp = 3;
            hid = 10;
            outs = 1;

            train_size = 500;
            test_size = 100;

            rd = new Random();

            //инициализация весов сети
            w1 = new double[hid, inp];
            w2 = new double[outs, hid];

            //инициализация весов смещения
            b1 = new double[hid];
            b2 = new double[outs];

            Randomize(); //заполняем их случайными значениями

            //инициализация побочных значений
            z_in = new double[hid];
            z = new double[hid];
            y_in = new double[outs];
            y = new double[outs];

            //инициализация значений для корректировки всех весов сети
            qy = new double[outs];
            qz = new double[hid];
        }

        //функция активации (сигмоида)
        private static double f(double x)
        {
            return 1.0 / (1.0 + Math.Exp(-x));
        }

        //производная функции активации
        private static double df(double x)
        {
            return f(x) * (1.0 - f(x));
        }

        //прямое распространение сигнала
        private static void ForwardPropagate(double[] x)
        {
            //скрытый слой
            for (int i = 0; i < hid; i++)
            {
                double s = 0;
                for (int j = 0; j < inp; j++)
                {
                    s += w1[i, j] * x[j];
                }
                z_in[i] = s + b1[i];
                z[i] = f(z_in[i]);
            }

            //выход
            for (int i = 0; i < outs; i++)
            {
                double s = 0;
                for (int j = 0; j < hid; j++)
                {
                    s += w2[i, j] * z[j];
                }
                y_in[i] = s + b2[i];
                y[i] = f(y_in[i]);
            }
        }

        //обратный ход
        private static void BackwardPropagate(double[] t)
        {
            //считаем ошибку на выходе
            for (int i = 0; i < outs; i++)
            {
                qy[i] = (t[i] - y[i]) * df(y_in[i]);
            }

            //считаем ошибку на скрытом слое
            for (int i = 0; i < hid; i++)
            {
                double s = 0;
                for (int j = 0; j < outs; j++)
                {
                    s += w2[j, i] * qy[j];
                }
                qz[i] = s * df(z_in[i]);
            }
        }

        //обновление всех весов сети
        private static void UpdateWeights(double[] x)
        {
            //веса между скрытым слоем и выходом
            for (int i = 0; i < outs; i++)
            {
                for (int j = 0; j < hid; j++)
                {
                    w2[i, j] += l_rate * qy[i] * z[j];
                }
            }

            //веса между входом и скрытым слоем
            for (int i = 0; i < hid; i++)
            {
                for (int j = 0; j < inp; j++)
                {
                    w1[i, j] += l_rate * qz[i] * x[j];
                }
            }

            //веса смещения на выходе
            for (int i = 0; i < outs; i++)
            {
                b2[i] += l_rate * qy[i];
            }

            //веса смещения на скрытом слое
            for (int i = 0; i < hid; i++)
            {
                b1[i] += l_rate * qz[i];
            }
        }

        //выход из программы
        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        //для получения строки из двумерного массива
        private static double[] row(double[,] x,int idx)
        {
            double[] res = new double[x.GetLength(1)];
            for(int i = 0; i < 3; i++)
            {
                res[i] = x[idx,i];
            }
            return res;
        }

        //среднеквадратическая ошибка
        private static double MSE(double[] y1, double[] y2)
        {
            double error = 0;
            for(int i = 0; i < y1.Length; i++)
            {
                error += Math.Pow(y1[i] - y2[i], 2);
            }
            return error / y1.Length;
        }

        //обучение нейронной сети посредством обратного распространения
        private void button2_Click(object sender, EventArgs e)
        {
            //сброс весов сети
            Randomize();

            //получаем все необходимые значения с формы
            l_rate = GetContolText(textBox1);
            epoch = int.Parse(textBox2.Text);
            train_size = int.Parse(textBox3.Text);
            test_size = int.Parse(textBox4.Text);

            chart1.Series["Error"].Points.Clear();
            chart1.ChartAreas[0].AxisX.Title = "Epoch";
            chart1.ChartAreas[0].AxisY.Title = "Error";

            //создаем случайным образом 3 числа из диапазона [0..1]
            double[,] train_x = new double[train_size,3];
            double[] train_y = new double[train_size];
            double[,] test_x = new double[test_size,3];
            double[] test_y = new double[test_size];
            double[] y_pred = new double[train_size];

            //для тренировки
            for (int i = 0; i < train_size; i++)
            {
                double a = rd.NextDouble();
                double b = rd.NextDouble();
                double c = rd.NextDouble();
                train_x[i,0] = a;
                train_x[i, 1] = b;
                train_x[i, 2] = c;
                train_y[i] = a * b * c;
            }

            //для теста
            for (int i = 0; i < test_size; i++)
            {
                double a = rd.NextDouble();
                double b = rd.NextDouble();
                double c = rd.NextDouble();
                test_x[i, 0] = a;
                test_x[i, 1] = b;
                test_x[i, 2] = c;
                test_y[i] = a * b * c;
            }

            //цикл по эпохам
            for(int i = 0; i < epoch; i++)
            {
                //цикл по каждому элементу тренировочной выборки
                for(int j = 0; j < train_size; j++)
                {
                    ForwardPropagate(row(train_x,j));
                    y_pred[j] = y[0];
                    BackwardPropagate(new double[] { train_y[j] });
                    UpdateWeights(row(train_x, j));
                }

                chart1.Series["Error"].Points.AddXY(i, MSE(train_y, y_pred));
            }

            //тестируем обученную сеть
            double eps = GetContolText(textBox5); //точность найденного решения
            int tr = 0; //кол-во верных значений
            int fl = 0; //кол-во неверных
            y_pred = new double[test_size];
            for (int i = 0; i < test_size; i++)
            {
                ForwardPropagate(row(test_x, i));
                y_pred[i] = y[0];
                if(Math.Abs(y_pred[i]-test_y[i]) <= eps)
                {
                    tr++;
                }
                else
                {
                    fl++;
                }
            }

            label4.Text = "Результаты тестирования: " + "MSE: " + MSE(test_y, y_pred) + "; Error: " + eps + "\n" +
                "Правильных значений: " + tr + ";  Неправильных значений: " + fl + ";"; 
            
        }

        //сброс весов сети для повторного их обучения
        private static void Randomize()
        {
            for (int i = 0; i < hid; i++)
            {
                for (int j = 0; j < inp; j++)
                {
                    w1[i, j] = rd.NextDouble();
                }
            }

            for (int i = 0; i < outs; i++)
            {
                for (int j = 0; j < hid; j++)
                {
                    w2[i, j] = rd.NextDouble();
                }
            }

            //инициализация весов смещения
            for (int i = 0; i < hid; i++)
            {
                b1[i] = rd.NextDouble();
            }

            for (int i = 0; i < outs; i++)
            {
                b2[i] = rd.NextDouble();
            }
        }

        //получаем значения из формы заменяя при этом точку на запятую
        public double GetContolText(TextBox textBox)
        {
            if (textBox.Text != "")
            {
                return Convert.ToDouble(textBox.Text.Replace(".", ","));
            }
            else
            {
                return 0;
            }
        }

        //для отдельного тестирования сети на трех заданных числах
        private void button3_Click(object sender, EventArgs e)
        {
            //получаем три значения с формы
            double a = GetContolText(textBox6);
            double b = GetContolText(textBox7);
            double c = GetContolText(textBox8);

            //просчет сетью
            ForwardPropagate(new double[] { a, b, c });
            label10.Text = "Результат: y = a*b*c = " + Math.Round((a * b * c),4) + "; y^ = " + Math.Round(y[0],4) + "; ";
        }
    }
}