namespace WpfApp1
{
    using Microsoft.Research.DynamicDataDisplay.DataSources;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Windows;

    public class MainWindowViewModel
    {
        private static readonly object ItemsLock = new object();
        private static int methodCounter = 0;

        volatile private ConcurrentQueue<Point> tempDataQueue = new ConcurrentQueue<Point>();

        private readonly System.Timers.Timer timerControl = new System.Timers.Timer(1000);

        public ObservableDataSource<Point> DataX
        {
            get; set;
        }

        public ObservableDataSource<Point> DataY
        {
            get; set;
        }

        public MainWindowViewModel()
        {
            DataX = new ObservableDataSource<Point>();
            DataY = new ObservableDataSource<Point>();

            timerControl.Elapsed += timerTick;
            timerControl.Start();

            // Dummy data generate
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    GenerateDummyData();

                    Thread.Sleep(1000);
                }
            });
        }

        private void GenerateDummyData()
        {
            double[] my_array = new double[300];
            var random = new Random();
            for (int i = 0; i < my_array.Length; i++)
            {
                double y = (float)(((float)200 * Math.Sin((i + 1) * (
                    i + 1.0) * 48 / my_array.Length)));

                lock (ItemsLock)
                {
                    tempDataQueue.Enqueue(new Point(i + random.Next(1, 10), y));
                }
            }
        }

        private void timerTick(object sender, ElapsedEventArgs e)
        {
            var timer = sender as System.Timers.Timer;
            timer.Stop();

            UpdateGraphData(methodCounter++);

            timer.Start();
        }

        private void UpdateGraphData(int methodCounter)
        {
            DateTime startedOn = DateTime.Now;

            Debug.WriteLine("method start" + methodCounter + "-" + startedOn);
            Point[] xPoints;
            Point[] yPoints;

            lock (ItemsLock)
            {

                xPoints = tempDataQueue.ToArray();
                tempDataQueue = new ConcurrentQueue<Point>();

            }
            Debug.WriteLine("data count " + xPoints.Length + "- old graph cleaning started" + DateTime.Now);


            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send,
                new Action(() =>
                {
                    DataX.Collection.Clear();
                    DataY.Collection.Clear();
                }));

            Debug.WriteLine("new graph rendering started" + DateTime.Now);

            yPoints = new Point[xPoints.Length];
            int counter = 0;

            Array.ForEach(xPoints, element =>
            {
                yPoints[counter++] = new Point(element.X + 2, element.Y + 2);
            });

            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send,
                                               new Action(() =>
                                               {
                                                   DataX.AppendMany(xPoints);
                                                   DataY.AppendMany(yPoints);
                                               }));

            Debug.WriteLine("method end=======================" + methodCounter + "-" + (DateTime.Now - startedOn).TotalMilliseconds);

        }
    }
}
