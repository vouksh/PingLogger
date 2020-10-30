using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using ScottPlot;
using PingLogger.Extensions;

namespace PingLogger.Controls
{
	/// <summary>
	/// Interaction logic for GraphControl.xaml
	/// </summary>
	public partial class GraphControl : UserControl
	{
		public FixedDictionary<DateTime, long> PingTimes { get; set; } = new FixedDictionary<DateTime, long>(100);
		private readonly FixedList<double> xAxis = new FixedList<double>(60);
		private readonly FixedList<double> yAxis = new FixedList<double>(60);
		public GraphControl()
		{
			InitializeComponent();
			PingTimes.EnsureCapacity(100);
		}

		public void UpdatePieChart(int warningValue, int timeoutValue)
		{
			xAxis.Clear();
			yAxis.Clear();
			if (PingTimes.Count > 0)
			{
				pingPlot.plt.Clear();
				var successCount = (double)PingTimes.Values.Where(v => v > 0 && v < warningValue).Count();
				var timeoutCount = (double)PingTimes.Values.Where(v => v == 0 || v >= timeoutValue).Count();
				var warningCount = (double)PingTimes.Values.Where(v => v >= warningValue && v < timeoutValue).Count();
				List<double> values = new List<double>
				{
					successCount
				};
				if (timeoutCount > 0)
				{
					values.Add(timeoutCount);
				}
				if(warningCount > 0)
				{
					values.Add(warningCount);
				}
				string[] labels = { "Success", "Timeout", "Warning" };
				System.Drawing.Color[] colors = { System.Drawing.Color.Green, System.Drawing.Color.Red, System.Drawing.Color.Orange };
				pingPlot.plt.PlotPie(values.ToArray(), labels, colors, showLabels: false, showPercentages: true);
				pingPlot.Render();
			}
		}

		public void StylePlot(bool isPieChart = false)
		{
			if (Util.IsLightTheme())
			{
				pingPlot.plt.Frame(left: true, right: false, bottom: true, top: false, frameColor: System.Drawing.Color.Black);
				pingPlot.plt.Style(figBg: System.Drawing.Color.Transparent, dataBg: System.Drawing.Color.Transparent, label: System.Drawing.Color.Black, grid: System.Drawing.Color.Black, title: System.Drawing.Color.Black);
			}
			else
			{
				pingPlot.plt.Frame(left: true, right: false, bottom: true, top: false, frameColor: System.Drawing.Color.White);
				pingPlot.plt.Style(figBg: System.Drawing.Color.Transparent, dataBg: System.Drawing.Color.Transparent, label: System.Drawing.Color.DarkGray, grid: System.Drawing.Color.DarkGray, title: System.Drawing.Color.DarkGray);
			}
			if (!isPieChart)
			{
				pingPlot.plt.XLabel("Time");
				pingPlot.plt.YLabel("Ping");
				pingPlot.plt.Layout(xScaleHeight: 5);
				pingPlot.plt.Ticks(dateTimeX: true, dateTimeFormatStringX: "hh:mm:ss", xTickRotation: 45);
				//pingPlot.plt.Grid(xSpacing: 1);
				//pingPlot.plt.PlotScatter(xAxis.ToArray(), yAxis.ToArray(), lineWidth: 1.5);
			} else
			{
				double[] values = { 0, 0, 0 };
				string[] labels = { "Success", "Timeout", "Warning" };
				System.Drawing.Color[] colors = { System.Drawing.Color.Green, System.Drawing.Color.Red, System.Drawing.Color.Orange };
				pingPlot.plt.PlotPie(values, labels, colors, showLabels: false, showPercentages: true);
				pingPlot.plt.Grid(false);
				pingPlot.plt.Frame(false);
				pingPlot.plt.Ticks(false, false);
			}
			pingPlot.plt.Legend();
		}

		public void AddData(DateTime time, long ping)
		{
			PingTimes.Add(time, ping);
			xAxis.Add(time.ToOADate());
			if (ping > 0)
			{
				yAxis.Add(ping);
			} else
			{
				yAxis.Add(999);
			}
			//pingPlot.Render();
		}

		public void UpdatePlot()
		{
			PingTimes.Clear();
			if (xAxis.Count > 0)
			{
				pingPlot.plt.Clear();
				pingPlot.plt.PlotScatter(xAxis.ToArray(), yAxis.ToArray(), lineWidth: 1.5);
				pingPlot.Render();
			}
		}

	}
}
