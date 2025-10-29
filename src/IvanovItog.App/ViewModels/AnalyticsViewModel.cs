using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.Domain.Dtos;
using IvanovItog.Domain.Interfaces;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace IvanovItog.App.ViewModels;

public partial class AnalyticsViewModel : ObservableObject
{
    private readonly IAnalyticsService _analyticsService;

    public ObservableCollection<ISeries> StatusSeries { get; } = new();
    public ObservableCollection<ISeries> TimelineSeries { get; } = new();
    public ObservableCollection<ISeries> LoadSeries { get; } = new();

    public Axis[] TimelineXAxis { get; private set; } = Array.Empty<Axis>();
    public Axis[] LoadXAxis { get; private set; } = Array.Empty<Axis>();

    [ObservableProperty]
    private DateTime _from = DateTime.UtcNow.AddDays(-30);

    [ObservableProperty]
    private DateTime _to = DateTime.UtcNow;

    [ObservableProperty]
    private bool _isBusy;

    public IAsyncRelayCommand LoadCommand { get; }

    public AnalyticsViewModel(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
        LoadCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
    }

    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;

            StatusSeries.Clear();
            TimelineSeries.Clear();
            LoadSeries.Clear();

            var statusData = await _analyticsService.GetRequestsByStatusAsync();
            foreach (var kv in statusData.Counts)
            {
                StatusSeries.Add(new PieSeries<int>
                {
                    Values = new[] { kv.Value },
                    Name = kv.Key,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer
                });
            }

            var timelinePoints = (await _analyticsService.GetRequestsTimelineAsync(From, To)).OrderBy(p => p.Date).ToList();
            TimelineSeries.Add(new LineSeries<int>
            {
                Values = timelinePoints.Select(p => p.RequestsCount).ToArray(),
                GeometrySize = 8,
                Fill = null
            });
            TimelineXAxis = new[]
            {
                new Axis
                {
                    Labels = timelinePoints.Select(p => p.Date.ToString("dd.MM")),
                    LabelsRotation = 15
                }
            };
            OnPropertyChanged(nameof(TimelineXAxis));

            var loads = (await _analyticsService.GetTechnicianLoadAsync(From, To)).ToList();
            LoadSeries.Add(new ColumnSeries<int>
            {
                Values = loads.Select(l => l.ActiveRequests).ToArray(),
                Name = "Активные"
            });
            LoadXAxis = new[]
            {
                new Axis
                {
                    Labels = loads.Select(l => l.TechnicianName).ToArray(),
                    LabelsRotation = 15
                }
            };
            OnPropertyChanged(nameof(LoadXAxis));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
