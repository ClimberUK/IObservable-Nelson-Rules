using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Reflection;

//using WpfApplication1.Helpers;

namespace WpfApplication1
{
    public class ItemEntry : INotifyPropertyChanged
    {
        private bool _selected;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ItemEntry(string name, bool selected)
        {
            this.IsSelected = selected;
            this.Name = name;
        }
        public string Name { get; set; }
        public bool IsSelected
        {
            get
            {
                return _selected;
            }
            set
            {
                _selected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private Dictionary<string, bool> _results;
        private ObservableCollection<ItemEntry> _results;
        public static double stdDev = 0;
        public static double mean = 0;

        public MainWindow()
        {

            IObservable<EventPattern<MouseEventArgs>> move = Observable.FromEventPattern<MouseEventArgs>(this, "MouseMove");
            IObservable<int> data = from evt in move select (int)evt.EventArgs.GetPosition(this).X;

            var stats = data.Buffer(50, 1).Scan(0, (cur, acc) =>
            {
                stdDev = acc.StdDev();
                mean = acc.Average();
                return 0;
            });

            stats.Subscribe();

            Dictionary<string, IObservable<int>> rules = new Dictionary<string, IObservable<int>>();
            rules.Add("Rule1", data.Buffer(1, 1).Scan(0, (cur, acc) => (Math.Abs((acc.Last() - mean)) > Math.Abs(((3 * stdDev) - mean))) ? 1 : 0));
            rules.Add("Rule2", data.Buffer(9, 1).Scan(0, (cur, acc) => (acc.All(x => x < mean) || acc.All(x => x > mean)) ? 1 : 0));
            rules.Add("Rule3", data.Buffer(6, 1).Scan(0, (cur, acc) => (acc.IsDecreasing() || acc.IsIncreasing()) ? 1 : 0));
            rules.Add("Rule4", data.Buffer(14, 1).Scan(0, (cur, acc) => (acc.Where((x, i) => i % 2 == 0).All(p => p > mean) ||
                                                                  acc.Where((x, i) => i % 2 == 0).All(p => p < mean)) ? 1 : 0));

            rules.Add("Rule5", data.Buffer(3, 1).Scan(0, (cur, acc) => (((acc.Where(p => p > mean).Where(p => Math.Abs(p - mean) > (2 * stdDev)).Count() > 1)
                                                               || (acc.Where(p => p < mean).Where(p => Math.Abs(p - mean) > (2 * stdDev)).Count() > 1)))
                                                               ? 1 : 0));

            rules.Add("Rule6", data.Buffer(5, 1).Scan(0, (cur, acc) => (((acc.Where(p => p > mean).Where(p => Math.Abs(p - mean) > stdDev).Count() > 3)
                                                               || (acc.Where(p => p < mean).Where(p => Math.Abs(p - mean) > stdDev).Count() > 3)))
                                                               ? 1 : 0));

            rules.Add("Rule7", data.Buffer(15, 1).Scan(0, (cur, acc) => (((acc.Where(p => p > mean).Where(p => Math.Abs(p - mean) < stdDev).Count() > 14)
                                                               || (acc.Where(p => p < mean).Where(p => Math.Abs(p - mean) < stdDev).Count() > 14)))
                                                               ? 1 : 0));

            rules.Add("Rule8", data.Buffer(8, 1).Scan(0, (cur, acc) => ((acc.Where(p => p >= mean).Count() < 8 && acc.Where(p => p <= mean).Count() < 8)
                                                                        && (acc.Count(p => (Math.Abs(p - mean) < stdDev)) > 7))
                                                                        ? 1 : 0));


            _results = new ObservableCollection<ItemEntry>();
            for (int i = 1; i <= 8; i++)
            {
                _results.Add(new ItemEntry("Rule" + i, false));
            }



            for (int p = 0; p <= 7; p++)
            {
                ItemEntry e = _results[p];
                rules["Rule" + (p + 1)].Subscribe(x => e.IsSelected = (x == 1) ? true : false);
            }

            this.DataContext = this;

        }

        public ObservableCollection<ItemEntry> Items
        {
            get { return _results; }

        }

    }        

    public static class Helper
    {
        public static double StdDev(this IEnumerable<int> col)
        {
            double ret = 0;
            //var col = values.ToEnumerable();
            int count = col.Count();
            if (count > 1)
            {
                //Compute the Average
                double avg = col.Average();

                //Perform the Sum of (value-avg)^2
                double sum = col.Sum(d => (d - avg) * (d - avg));

                //Put it all together
                ret = Math.Sqrt(sum / count);
            }
            return ret;
        }

        public static bool IsIncreasing(this IEnumerable<int> col)
        {
            int prev = int.MinValue;

            foreach (int i in col)
            {
                if (i < prev)
                {
                    return false;
                }
                prev = i;
            }

            return true;
        }

        public static bool IsDecreasing(this IEnumerable<int> col)
        {
            int prev = int.MaxValue;

            foreach (int i in col)
            {
                if (i > prev)
                {
                    return false;
                }
                prev = i;
            }

            return true;
        }
    }
}
