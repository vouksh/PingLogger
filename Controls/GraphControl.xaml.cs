using PingLogger.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace PingLogger.Controls
{
	/// <summary>
	/// Interaction logic for GraphControl.xaml
	/// </summary>
	public partial class GraphControl : UserControl
	{
		public FixedDictionary<DateTime, long> PingTimes { get; set; } = new(100);
		private readonly FixedList<double> _xAxis = new(60);
		private readonly FixedList<double> _yAxis = new(60);
		public GraphControl()
		{
			InitializeComponent();
			PingTimes.EnsureCapacity(100);
		}

		public void UpdatePieChart(int warningValue, int timeoutValue)
		{
			_xAxis.Clear();
			_yAxis.Clear();
			if (PingTimes.Count > 0)
			{
				PingPlot.plt.Clear();
				var successCount = (double)PingTimes.Values.Count(v => v > 0 && v < warningValue);
				var timeoutCount = (double)PingTimes.Values.Count(v => v == 0 || v >= timeoutValue);
				var warningCount = (double)PingTimes.Values.Count(v => v >= warningValue && v < timeoutValue);
				var values = new List<double>
				{
					successCount
				};
				if (timeoutCount > 0)
				{
					values.Add(timeoutCount);
				}
				if (warningCount > 0)
				{
					values.Add(warningCount);
				}
				string[] labels = { "Success", "Timeout", "Warning" };
				System.Drawing.Color[] colors = { System.Drawing.Color.Green, System.Drawing.Color.Red, System.Drawing.Color.Orange };
				PingPlot.plt.PlotPie(values.ToArray(), labels, colors, showLabels: false, showPercentages: true);
				PingPlot.Render();
			}
		}

		public void StylePlot(bool isPieChart = false)
		{
			if (Util.IsLightTheme)
			{
				PingPlot.plt.Frame(left: true, right: false, bottom: true, top: false, frameColor: System.Drawing.Color.Black);
				PingPlot.plt.Style(figBg: System.Drawing.Color.Transparent, dataBg: System.Drawing.Color.Transparent, label: System.Drawing.Color.Black, grid: System.Drawing.Color.Black, title: System.Drawing.Color.Black);
			}
			else
			{
				PingPlot.plt.Frame(left: true, right: false, bottom: true, top: false, frameColor: System.Drawing.Color.White);
				PingPlot.plt.Style(figBg: System.Drawing.Color.Transparent, dataBg: System.Drawing.Color.Transparent, label: System.Drawing.Color.DarkGray, grid: System.Drawing.Color.DarkGray, title: System.Drawing.Color.DarkGray);
			}
			if (!isPieChart)
			{
				PingTimes.MaxSize = 1;
				PingPlot.plt.XLabel("Time");
				PingPlot.plt.YLabel("Ping");
				PingPlot.plt.Layout(xScaleHeight: 5, yScaleWidth: 1);
				PingPlot.plt.Ticks(dateTimeX: true, dateTimeFormatStringX: "hh:mm:ss", xTickRotation: 45);
				//pingPlot.plt.Grid(xSpacing: 1);
				//pingPlot.plt.PlotScatter(xAxis.ToArray(), yAxis.ToArray(), lineWidth: 1.5);
			}
			else
			{
				_xAxis.MaxSize = 1;
				_yAxis.MaxSize = 1;
				double[] values = { 0, 0, 0 };
				string[] labels = { "Success", "Timeout", "Warning" };
				System.Drawing.Color[] colors = { System.Drawing.Color.Green, System.Drawing.Color.Red, System.Drawing.Color.Orange };
				PingPlot.plt.PlotPie(values, labels, colors, showLabels: false, showPercentages: true);
				PingPlot.plt.Grid(false);
				PingPlot.plt.Frame(false);
				PingPlot.plt.Ticks(false, false);
			}
			PingPlot.plt.Legend();
		}

		public void AddData(DateTime time, long ping)
		{
			PingTimes.Add(time, ping);
			_xAxis.Add(time.ToOADate());
			_yAxis.Add(ping > 0 ? ping : 999);
			//pingPlot.Render();
		}

		public void UpdatePlot()
		{
			PingTimes.Clear();
			if (_xAxis.Count > 0)
			{
				PingPlot.plt.Clear();
				PingPlot.plt.PlotScatter(_xAxis.ToArray(), _yAxis.ToArray(), lineWidth: 1.5);
				PingPlot.Render();
			}
		}

		public void ClearData()
		{
			PingTimes.Clear();
			_xAxis.Clear();
			_yAxis.Clear();
		}

	}
}
