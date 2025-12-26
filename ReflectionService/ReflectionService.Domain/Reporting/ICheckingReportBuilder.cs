namespace ReflectionService.Domain.Reporting;

public interface ICheckingReportBuilder
{
    string Build(CheckingContext context);
}
