using EPiServer.Core;
using EPiServer.Core.Internal;
using EPiServer.Shell.Web.Mvc.Html;
using EPiServer.Validation;
using Optimizely.CarbonTracker.Services;
using System.Threading;

namespace Optimizely.CarbonTracker.Models
{
    public class ContentPageValidator(ICarbonReportRepository _repository, ICarbonReportService _reportService) : IValidate<PageData>
    {
        IEnumerable<ValidationError> IValidate<PageData>.Validate(PageData page)
        {
            //// Get previous report for comparison
            //var previousReport = _repository.GetLatestReportAsync(contentGuid, cancellationToken);

            //// Generate new report
            //var report = _reportService.GenerateReportAsync(pageUrl, contentGuid, cancellationToken).Result;
            //_repository.SaveReportAsync(report, cancellationToken);

            //// Log comparison if previous report exists
            //if (previousReport != null)
            //{
            //    var changeDirection = report.EstimatedCO2Grams > previousReport.EstimatedCO2Grams ? "worsened" : "improved";
            //    _logger.LogInformation(
            //        "Carbon score {ChangeDirection} for {PageUrl}: {OldScore} ({OldCO2:F2}g) → {NewScore} ({NewCO2:F2}g)",
            //        changeDirection, pageUrl,
            //        previousReport.Score, previousReport.EstimatedCO2Grams,
            //        report.Score, report.EstimatedCO2Grams);

            //    // Warn if score worsened to D or F
            //    if (report.Score >= GreenScore.D && report.Score > previousReport.Score)
            //    {
            //        _logger.LogWarning(
            //            "⚠️ Carbon score DEGRADED to {Score} for {PageUrl}. Consider reviewing recent changes.",
            //            report.Score, pageUrl);
            //    }
            //}


            // We can do logic on the pages properties here
            return new[]
            {
        new ValidationError()
            {
                ErrorMessage = "This is information",
                PropertyName = page.GetPropertyName(property => property.PageName),
                Severity = ValidationErrorSeverity.Info,
                ValidationType = ValidationErrorType.AttributeMatched
            },
            new ValidationError()
            {
                ErrorMessage = "This is a warning",
                PropertyName = page.GetPropertyName(property => property .PageName),
                Severity = ValidationErrorSeverity.Warning,
                ValidationType = ValidationErrorType.AttributeMatched
            },
            };
        }
    }
}
