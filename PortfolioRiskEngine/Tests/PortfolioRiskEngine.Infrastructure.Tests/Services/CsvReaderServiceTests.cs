using PortfolioRiskEngine.Infrastructure.Services;
using Shouldly;

namespace PortfolioRiskEngine.Infrastructure.Tests.Services;

public class CsvReaderServiceTests
{
    [Fact]
    public async Task ReadLoansAsync_ShouldParseLoansAndSkipEmptyLines()
    {
        var fixture = CreateFixture();
        fixture.WriteCsv(
            "loans.csv",
            "LoanId,PortId,OriginalLoanAmount,OutstandingAmount,CollateralValue,CreditRating",
            "1, 10, 1000, 900, 1200, A",
            "",
            "2,11,2000,1800,2500, BBB");

        var result = await fixture.Sut.ReadLoansAsync();

        result.Count.ShouldBe(2);
        result[0].LoanId.ShouldBe(1);
        result[0].PortId.ShouldBe(10);
        result[0].OriginalLoanAmount.ShouldBe(1000m);
        result[0].OutstandingAmount.ShouldBe(900m);
        result[0].CollateralValue.ShouldBe(1200m);
        result[0].CreditRating.ShouldBe("A");

        result[1].LoanId.ShouldBe(2);
        result[1].PortId.ShouldBe(11);
        result[1].CreditRating.ShouldBe("BBB");
    }

    [Fact]
    public async Task ReadPortfoliosAsync_ShouldParsePortfolioRows()
    {
        var fixture = CreateFixture();
        fixture.WriteCsv(
            "portfolios.csv",
            "PortId,PortName,PortCountry,PortCcy",
            "1, US Consumer, US, USD",
            "2, UK Corporate, UK, GBP");

        var result = await fixture.Sut.ReadPortfoliosAsync();

        result.Count.ShouldBe(2);
        result[0].PortId.ShouldBe(1);
        result[0].PortName.ShouldBe("US Consumer");
        result[0].PortCountry.ShouldBe("US");
        result[0].PortCcy.ShouldBe("USD");
    }

    [Fact]
    public async Task ReadRatingsAsync_ShouldParseRatingsRows()
    {
        var fixture = CreateFixture();
        fixture.WriteCsv(
            "ratings.csv",
            "RatingCode,ProbabilityOfDefault",
            "AAA,0.001",
            "BB,0.07");

        var result = await fixture.Sut.ReadRatingsAsync();

        result.Count.ShouldBe(2);
        result[0].RatingCode.ShouldBe("AAA");
        result[0].ProbabilityOfDefault.ShouldBe(0.001m);
        result[1].RatingCode.ShouldBe("BB");
        result[1].ProbabilityOfDefault.ShouldBe(0.07m);
    }

    [Fact]
    public async Task ReadLoansAsync_ShouldThrowFileNotFoundException_WhenFileMissing()
    {
        var fixture = CreateFixture();

        await Should.ThrowAsync<FileNotFoundException>(() => fixture.Sut.ReadLoansAsync());
    }

    [Fact]
    public async Task ReadPortfoliosAsync_ShouldThrowFormatException_WhenNumericFieldIsInvalid()
    {
        var fixture = CreateFixture();
        fixture.WriteCsv(
            "portfolios.csv",
            "PortId,PortName,PortCountry,PortCcy",
            "not-an-int, US Consumer, US, USD");

        await Should.ThrowAsync<FormatException>(() => fixture.Sut.ReadPortfoliosAsync());
    }

    [Fact]
    public async Task ReadRatingsAsync_ShouldThrowIndexOutOfRangeException_WhenRowIsMalformed()
    {
        var fixture = CreateFixture();
        fixture.WriteCsv(
            "ratings.csv",
            "RatingCode,ProbabilityOfDefault",
            "AAA");

        await Should.ThrowAsync<IndexOutOfRangeException>(() => fixture.Sut.ReadRatingsAsync());
    }

    private static CsvReaderFixture CreateFixture()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "portfolio-risk-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        var sut = new CsvReaderService(directoryPath);
        return new CsvReaderFixture(sut, directoryPath);
    }

    private sealed record CsvReaderFixture(CsvReaderService Sut, string DirectoryPath) : IDisposable
    {
        public void WriteCsv(string fileName, params string[] lines)
        {
            var path = Path.Combine(DirectoryPath, fileName);
            File.WriteAllLines(path, lines);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
        }
    }
}