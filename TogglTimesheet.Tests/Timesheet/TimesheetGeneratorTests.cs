using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CsvHelper;
using Moq;
using TogglTimesheet.Timesheet;

namespace TogglTimesheet.Tests.Timesheet
{
    [ExcludeFromCodeCoverage]
    public class TimesheetGeneratorTests
    {
        [Fact]
        public void GenerateAndSave_ShouldGenerateAndSaveTimesheetCorrectly()
        {
            // Arrange
            var mockTaskGenerator = new Mock<ITaskGenerator>();
            var mockDataProvider = new Mock<IDataProvider>();

            var timeEntries = new List<TimeEntry>
            {
                new TimeEntry { RawStartDate = "2023-10-01", RawDuration = "02:30:00", Project = "ProjectA", Description = "Task1" },
                new TimeEntry { RawStartDate = "2023-10-01", RawDuration = "01:00:00", Project = "ProjectA", Description = "Task2" },
                new TimeEntry { RawStartDate = "2023-10-02", RawDuration = "03:00:00", Project = "ProjectB", Description = "Task1" }
            };

            mockDataProvider.Setup(dp => dp.LoadTimeEntries(It.IsAny<string>())).Returns(timeEntries);
            mockTaskGenerator.Setup(tg => tg.GenerateTask(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((desc, proj) => desc);

            var timesheetGenerator = new TimesheetGenerator(mockTaskGenerator.Object, mockDataProvider.Object);

            // Act
            var inputFile = "path/to/timeentries.csv";
            var outputFile = "path/to/output.csv";
            timesheetGenerator.GenerateAndSave(inputFile, outputFile);

            // Assert
            mockDataProvider.Verify(dp => dp.LoadTimeEntries(It.IsAny<string>()), Times.Once);

            mockDataProvider.Verify(dp => dp.SaveTimesheet(
                It.Is<Dictionary<string, ReportedTimeEntry>>(dict =>
                    dict.Count == 2 &&
                    Math.Abs(dict["Task1"].DayTime[DateTime.Parse("2023-10-01", CultureInfo.InvariantCulture)] - 2.5) < 0.01 &&
                    Math.Abs(dict["Task1"].DayTime[DateTime.Parse("2023-10-02", CultureInfo.InvariantCulture)] - 3) < 0.01 &&
                    Math.Abs(dict["Task2"].DayTime[DateTime.Parse("2023-10-01", CultureInfo.InvariantCulture)] - 1) < 0.01
                ),
                It.Is<List<DateTime>>(dates =>
                    dates.Count == 2 &&
                    dates.Contains(DateTime.Parse("2023-10-01", CultureInfo.InvariantCulture)) &&
                    dates.Contains(DateTime.Parse("2023-10-02", CultureInfo.InvariantCulture))
                ),
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public void GenerateData_ShouldReturnMemoryStreamWithProcessedData()
        {
            // Arrange
            var mockTaskGenerator = new Mock<ITaskGenerator>();
            var mockDataProvider = new Mock<IDataProvider>();
            var entries = new List<TimeEntry>
            {
            new TimeEntry { Description = "Task1", Project = "Project1", RawStartDate = DateTime.Today.ToString("yyyy-MM-dd"), RawDuration = "02:30:00" },
            new TimeEntry { Description = "Task2", Project = "Project2", RawStartDate = DateTime.Today.ToString("yyyy-MM-dd"), RawDuration = "03:00:00" }
            };

            mockDataProvider.Setup(dp => dp.LoadTimeEntriesFromStream(It.IsAny<Stream>())).Returns(entries);
            mockTaskGenerator.Setup(tg => tg.GenerateTask(It.IsAny<string>(), It.IsAny<string>())).Returns((string desc, string proj) => desc);

            var timesheetGenerator = new TimesheetGenerator(mockTaskGenerator.Object, mockDataProvider.Object);
            var inputStream = new MemoryStream();
            using (var writer = new StreamWriter(inputStream, leaveOpen: true))
            {
                var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csvWriter.WriteRecords(entries);
                writer.Flush();
                inputStream.Position = 0;
            }

            // Act
            timesheetGenerator.GenerateData(inputStream);

            // Assert
            mockDataProvider.Verify(dp => dp.SaveTimesheetToStream(
                It.IsAny<StreamWriter>(),
                It.Is<Dictionary<string, ReportedTimeEntry>>(dict =>
                    dict.Count == 2 &&
                    Math.Abs(dict["Task1"].DayTime[DateTime.Today] - 2.5) < 0.01 &&
                    Math.Abs(dict["Task2"].DayTime[DateTime.Today] - 3) < 0.01
                ),
                It.Is<List<DateTime>>(dates =>
                    dates.Count == 1 &&
                    dates.Contains(DateTime.Today)
                )
            ), Times.Once);
        }
    }
}