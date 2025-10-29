using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.Domain.Interfaces;
using IvanovItog.Shared.Dtos;
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
    private DateTime _from = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _to = DateTime.Today;

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

            ClearSeries();

            var (fromLocal, toLocal, normalizedFrom, normalizedTo) = NormalizeRange(From, To);
            if (From != normalizedFrom)
            {
                From = normalizedFrom;
            }

            if (To != normalizedTo)
            {
                To = normalizedTo;
            }

            var timelinePoints = (await _analyticsService.GetRequestsTimelineAsync(fromLocal, toLocal)).OrderBy(p => p.Date).ToList();

            if (!timelinePoints.Any())
            {
                var fallbackTimeline = (await _analyticsService.GetRequestsTimelineAsync(DateTime.MinValue, DateTime.MaxValue))
                    .OrderBy(p => p.Date)
                    .ToList();

                if (fallbackTimeline.Any())
                {
                    var fallbackFrom = fallbackTimeline.First().Date.Date;
                    var fallbackTo = fallbackTimeline.Last().Date.Date;

                    if (From != fallbackFrom)
                    {
                        From = fallbackFrom;
                    }

                    if (To != fallbackTo)
                    {
                        To = fallbackTo;
                    }

                    (fromLocal, toLocal, _, _) = NormalizeRange(From, To);
                    timelinePoints = fallbackTimeline;
                }
            }

            if (timelinePoints.Any())
            {
                TimelineSeries.Add(new LineSeries<int>
                {
                    Values = timelinePoints.Select(p => p.Count).ToArray(),
                    GeometrySize = 8,
                    Fill = null
                });

                TimelineXAxis = new Axis[]
                {
                    new Axis
                    {
                        Labels = timelinePoints.Select(p => p.Date.ToString("dd.MM")).ToList(),
                        LabelsRotation = 15
                    }
                };
            }
            else
            {
                TimelineXAxis = Array.Empty<Axis>();
            }

            OnPropertyChanged(nameof(TimelineXAxis));

            Console.WriteLine($"TimelinePoints: {timelinePoints.Count}");

            var statusData = (await _analyticsService.GetRequestsByStatusAsync(fromLocal, toLocal)).ToList();
            Console.WriteLine($"StatusData: {statusData.Count}");
            foreach (var status in statusData)
            {
                StatusSeries.Add(new PieSeries<int>
                {
                    Values = new int[] { status.Count },
                    Name = status.Status,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer
                });
            }

            var loads = (await _analyticsService.GetTechnicianLoadAsync(fromLocal, toLocal)).ToList();
            Console.WriteLine($"Loads: {loads.Count}");
            if (loads.Any())
            {
                LoadSeries.Add(new ColumnSeries<int>
                {
                    Values = loads.Select(l => l.ActiveRequests).ToArray(),
                    Name = "Активные"
                });

                if (loads.Any(l => l.ClosedRequests > 0))
                {
                    LoadSeries.Add(new ColumnSeries<int>
                    {
                        Values = loads.Select(l => l.ClosedRequests).ToArray(),
                        Name = "Завершённые"
                    });
                }

                LoadXAxis = new Axis[]
                {
                    new Axis
                    {
                        Labels = loads.Select(l => l.TechnicianName).ToList(),
                        LabelsRotation = 15
                    }
                };
            }
            else
            {
                LoadXAxis = Array.Empty<Axis>();
            }

            OnPropertyChanged(nameof(LoadXAxis));
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
    }

    private void ClearSeries()
    {
        StatusSeries.Clear();
        TimelineSeries.Clear();
        LoadSeries.Clear();
        TimelineXAxis = Array.Empty<Axis>();
        LoadXAxis = Array.Empty<Axis>();
        OnPropertyChanged(nameof(TimelineXAxis));
        OnPropertyChanged(nameof(LoadXAxis));
    }

    private static (DateTime fromLocal, DateTime toLocal, DateTime normalizedFrom, DateTime normalizedTo) NormalizeRange(DateTime from, DateTime to)
    {
        var normalizedFrom = from.Date;
        var normalizedTo = to.Date;

        if (normalizedFrom > normalizedTo)
        {
            (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);
        }

        var fromLocal = normalizedFrom;
        var toLocal = normalizedTo.AddDays(1).AddTicks(-1);

        return (fromLocal, toLocal, normalizedFrom, normalizedTo);
    }
}
