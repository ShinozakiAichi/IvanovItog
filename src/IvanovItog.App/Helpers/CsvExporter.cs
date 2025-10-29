using System.Globalization;
using System.IO;
using System.Text;
using IvanovItog.Domain.Entities;

namespace IvanovItog.App.Helpers;

public static class CsvExporter
{
    public static string ExportRequests(IEnumerable<Request> requests)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IvanovItog", "Exports");
        Directory.CreateDirectory(directory);
        var fileName = $"requests_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        var path = Path.Combine(directory, fileName);

        var builder = new StringBuilder();
        builder.AppendLine("Id;Title;Description;CategoryId;Priority;StatusId;CreatedById;AssignedToId;CreatedAt;ClosedAt");
        foreach (var request in requests)
        {
            builder.AppendLine(string.Join(';',
                request.Id,
                Escape(request.Title),
                Escape(request.Description),
                request.CategoryId,
                request.Priority,
                request.StatusId,
                request.CreatedById,
                request.AssignedToId?.ToString() ?? string.Empty,
                request.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
                request.ClosedAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty));
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        return path;
    }

    private static string Escape(string value)
    {
        if (value.Contains(';') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
