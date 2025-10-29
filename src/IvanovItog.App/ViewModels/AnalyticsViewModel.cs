using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
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
    private static readonly SemaphoreSlim LogSemaphore = new(1, 1);
    private readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "analytics.log");

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
            await LogAsync("Пропуск загрузки: операция уже выполняется.");
            return;
        }

        IsBusy = true;
        await LogAsync($"Начало загрузки аналитики. Файл логов: {_logFilePath}");

        try
        {
            ClearSeries();
            await LogAsync("Очищены предыдущие данные графиков.");

            var (fromLocal, toLocal, normalizedFrom, normalizedTo) = NormalizeRange(From, To);
            if (From != normalizedFrom)
            {
                From = normalizedFrom;
            }

            if (To != normalizedTo)
            {
                To = normalizedTo;
            }

            await LogAsync($"Используемый диапазон: {normalizedFrom:yyyy-MM-dd} — {normalizedTo:yyyy-MM-dd}.");

            var timelinePoints = (await _analyticsService.GetRequestsTimelineAsync(fromLocal, toLocal))
                .OrderBy(p => p.Date)
                .ToList();

            if (!timelinePoints.Any())
            {
                await LogAsync("Основной запрос хронологии не вернул данных. Выполняем запрос по полному диапазону.");

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

                    await LogAsync($"Использован запасной диапазон: {fallbackFrom:yyyy-MM-dd} — {fallbackTo:yyyy-MM-dd}.");
                }

                if (!timelinePoints.Any())
                {
                    await LogAsync("Данные хронологии отсутствуют даже после запасного запроса.");
                }
            }

            await LogAsync($"Получено точек хронологии: {timelinePoints.Count}.");

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

            var statusData = (await _analyticsService.GetRequestsByStatusAsync(fromLocal, toLocal)).ToList();
            await LogAsync($"Получено статусов: {statusData.Count}.");

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
            await LogAsync($"Получено данных по нагрузке: {loads.Count}.");

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
        catch (Exception ex)
        {
            await LogErrorAsync("Ошибка во время загрузки аналитики", ex);
            throw;
        }
        finally
        {
            IsBusy = false;
            await LogAsync("Загрузка аналитики завершена.");
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

    private Task LogAsync(string message) => WriteLogEntryAsync(message);

    private Task LogErrorAsync(string message, Exception exception)
    {
        var details = $"{message}: {exception.GetType().FullName}: {exception.Message}{Environment.NewLine}{exception.StackTrace}";
        return WriteLogEntryAsync(details);
    }

    private async Task WriteLogEntryAsync(string message)
    {
        var line = $"[{DateTime.Now:O}] {message}{Environment.NewLine}";

        await LogSemaphore.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_logFilePath, line);
        }
        finally
        {
            LogSemaphore.Release();
        }
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
