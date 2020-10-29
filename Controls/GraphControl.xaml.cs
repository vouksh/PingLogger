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

namespace PingLogger.Controls
{
	/// <summary>
	/// Interaction logic for GraphControl.xaml
	/// </summary>
	public partial class GraphControl : UserControl
	{
		public Dictionary<DateTime, long> PingTimes { get; set; } = new Dictionary<DateTime, long>();
		public GraphControl()
		{
			InitializeComponent();
			StylePlot();
		}

		public void UpdatePieChart(int warningValue)
		{
			if (PingTimes.Count > 100)
			{
				PingTimes = PingTimes.OrderByDescending(x => x.Key).Take(100).ToDictionary(x => x.Key, y => y.Value);
			}
			if (PingTimes.Count > 0)
			{
				pingPlot.plt.Clear();
				var successCount = (double)PingTimes.Values.Where(v => v > 0 && v < warningValue).Count();
				var timeoutCount = (double)PingTimes.Values.Where(v => v == 0).Count();
				var warningCount = (double)PingTimes.Values.Where(v => v > warningValue).Count();
				double[] values = { successCount, timeoutCount, warningCount };
				string[] labels = { "Success", "Timeout", "Warning" };
				System.Drawing.Color[] colors = { System.Drawing.Color.Green, System.Drawing.Color.Red, System.Drawing.Color.Orange };
				pingPlot.plt.PlotPie(values, labels, colors, showLabels: false, showPercentages: true);
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
			} else
			{
				double[] values = { 0, 0, 0 };
				string[] labels = { "Success", "Timeout", "Warning" };
				System.Drawing.Color[] colors = { System.Drawing.Color.Green, System.Drawing.Color.Red, System.Drawing.Color.Orange };
				pingPlot.plt.PlotPie(values, labels, colors, showLabels: false, showPercentages: true);
				pingPlot.plt.Grid(false);
				pingPlot.plt.Frame(false);
				pingPlot.plt.Ticks(false, false);
				pingPlot.plt.XLabel(null);
				pingPlot.plt.YLabel(null);
			}
			pingPlot.plt.Legend();
		}

		public void UpdatePlot()
		{
			if (PingTimes.Count > 100)
			{
				PingTimes = PingTimes.OrderByDescending(x => x.Key).Take(100).ToDictionary(x => x.Key, y => y.Value);
			}
			if (PingTimes.Count > 0)
			{
				pingPlot.plt.Clear();
				var pingTimes = PingTimes.OrderByDescending(x => x.Key).Take(20).ToDictionary(x => x.Key, y => y.Value);
				int pingTimeCount = pingTimes.Keys.Count > 20 ? 20 : pingTimes.Keys.Count;
				double[] xAxis = new double[pingTimeCount];
				int xIndex = 0;
				foreach (var time in pingTimes.Keys.Take(20))
				{
					xAxis[xIndex] = time.ToOADate();
					xIndex++;
				}
				double[] yAxis = new double[pingTimeCount];
				int yIndex = 0;
				foreach (var ping in pingTimes.Values)
				{
					yAxis[yIndex] = ping;
					yIndex++;
				}

				pingPlot.plt.Ticks(dateTimeX: true, displayTicksX: true, dateTimeFormatStringX: "hh:MM:ss");
				pingPlot.plt.PlotScatter(xAxis, yAxis, lineWidth: 1.5);
				pingPlot.Render();
			}
		}

	}
}
