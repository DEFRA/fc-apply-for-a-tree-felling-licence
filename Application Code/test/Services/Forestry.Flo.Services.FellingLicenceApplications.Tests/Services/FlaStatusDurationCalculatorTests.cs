using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using NodaTime;
using Xunit;
using AutoFixture;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class FlaStatusDurationCalculatorTests
{
    private static readonly Fixture FixtureInstance = new();
    private readonly Mock<IClock> _clock = new(); 
    private static DateTime _defaultNow;
    public record HistoryRecordSetup(FellingLicenceStatus Status, DateTime Created);
    public record HistoryResultRecordAssertion(FellingLicenceStatus Status, int Days);

    public FlaStatusDurationCalculatorTests()
    {
        FixtureInstance.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => FixtureInstance.Behaviors.Remove(b));
        FixtureInstance.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Theory]
    [MemberData(nameof(CalculatorTestData))]
    public void Tests(
        List<HistoryRecordSetup> statusHistories, 
        List<HistoryResultRecordAssertion> expectedHistories)
    {
        var fellingLicenceApplication = FixtureInstance.Create<FellingLicenceApplication>();
        fellingLicenceApplication.StatusHistories.Clear();

        foreach (var history in statusHistories)
        {
            AppendStatusHistory(fellingLicenceApplication, history.Status, history.Created.ToLocalTime());
        }
        
        var sut = CreateSut();

        var result = sut.CalculateStatusDurations(fellingLicenceApplication);

        Assert.Equal(expectedHistories.Count, result.Count);

        var i = 0;
        foreach (var expectedHistory in expectedHistories.OrderBy(x=>x.Status))
        {
            var actual = result.OrderBy(x => x.Status).ToArray()[i];

            Assert.Equal(expectedHistory.Status, actual.Status);
            Assert.Equal(expectedHistory.Days, actual.Duration.Days);
            i++;
        }
    }

    private FlaStatusDurationCalculator CreateSut(DateTime? now = null)
    {
        now ??= _defaultNow;

        _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.SpecifyKind(now.Value, DateTimeKind.Utc)));

        return new FlaStatusDurationCalculator(_clock.Object);
    }

    private static void AppendStatusHistory(
        FellingLicenceApplication fellingLicenceApplication,
        FellingLicenceStatus status, 
        DateTime createdDateTime)
    {
        fellingLicenceApplication.StatusHistories.Add(new StatusHistory
        {
            Created = createdDateTime,
            FellingLicenceApplication = fellingLicenceApplication,
            Status = status
        });
    }

    public static IEnumerable<object[]> CalculatorTestData()
    {
        _defaultNow = new DateTime(2023, 03, 01, 12, 00, 00);

        var allData = new List<object[]>
        {
            //Just a draft created some days in the past:
            new object[] { 
                new List<HistoryRecordSetup> {
                    new(FellingLicenceStatus.Draft, new DateTime(2023,02,01))
                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft,  (_defaultNow - new DateTime(2023,02,01)).Days)
                }
            },
            //Just a draft created on previous day, should have duration of 1:
            new object[] {
                new List<HistoryRecordSetup> {
                    new(FellingLicenceStatus.Draft, _defaultNow.AddDays(-1))
                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft,  TimeSpan.FromDays(1).Days)
                }
            },
            //Just a draft created an hour before current date and time, should have zero duration
            new object[] {
                new List<HistoryRecordSetup> {
                new(FellingLicenceStatus.Draft, _defaultNow.AddHours(-1))
                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft,  0)
                }
            },
            //Draft > submitted should have correct durations per status
            new object[] {
                new List<HistoryRecordSetup> {
                    new(FellingLicenceStatus.Draft, _defaultNow.AddDays(-25)),
                    new(FellingLicenceStatus.Submitted, _defaultNow.AddDays(-15))
                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft,  10),
                    new (FellingLicenceStatus.Submitted, (_defaultNow - _defaultNow.AddDays(-15)).Days)
                }
            },
            //Full typical happy days workflow Submitted should have correct durations per status
            new object[] {
                new List<HistoryRecordSetup> {
                    new(FellingLicenceStatus.Draft, _defaultNow.AddDays(-25)),
                    new(FellingLicenceStatus.Submitted, _defaultNow.AddDays(-15)),
                    new(FellingLicenceStatus.Received, _defaultNow.AddDays(-13)),
                    new(FellingLicenceStatus.WoodlandOfficerReview, _defaultNow.AddDays(-9)),
                    new(FellingLicenceStatus.SentForApproval, _defaultNow.AddDays(-8)),
                    new(FellingLicenceStatus.Approved, _defaultNow.AddDays(-4))

                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft, 10),
                    new (FellingLicenceStatus.Submitted, 2),
                    new (FellingLicenceStatus.Received, 4),
                    new (FellingLicenceStatus.WoodlandOfficerReview, 1),
                    new (FellingLicenceStatus.SentForApproval, 4),
                    new (FellingLicenceStatus.Approved, 4)
                }
            },
            //2 occurrences of same state should be added, should have correct durations per status
            new object[] {
                new List<HistoryRecordSetup> {
                    new(FellingLicenceStatus.Draft, _defaultNow.AddDays(-25)),
                    new(FellingLicenceStatus.Submitted, _defaultNow.AddDays(-15)),
                    new(FellingLicenceStatus.Received, _defaultNow.AddDays(-13)),
                    new(FellingLicenceStatus.WithApplicant, _defaultNow.AddDays(-10)),
                    new(FellingLicenceStatus.Submitted, _defaultNow.AddDays(-8))

                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft, 10),
                    new (FellingLicenceStatus.Submitted, 10),//2 days between submitted and Received, plus 8 days up until '_default now'
                    new (FellingLicenceStatus.Received, 3),
                    new (FellingLicenceStatus.WithApplicant, 2)
                }
            },
            //lots of back and forth shenanigans should result in the correct durations per status
            new object[] {
                new List<HistoryRecordSetup> {
                    new(FellingLicenceStatus.Draft, _defaultNow.AddDays(-25)),
                    new(FellingLicenceStatus.Submitted, _defaultNow.AddDays(-15)),
                    new(FellingLicenceStatus.Received, _defaultNow.AddDays(-14)),
                    new(FellingLicenceStatus.WithApplicant, _defaultNow.AddDays(-10)),
                    new(FellingLicenceStatus.Submitted, _defaultNow.AddDays(-8)),
                    new(FellingLicenceStatus.Received, _defaultNow.AddDays(-8)), //on same day as submitted
                    new(FellingLicenceStatus.WithApplicant, _defaultNow.AddDays(-6)),
                    new(FellingLicenceStatus.Submitted, _defaultNow.AddDays(-4)),
                    new(FellingLicenceStatus.Received, _defaultNow.AddDays(-3)),
                    new(FellingLicenceStatus.WithApplicant, _defaultNow.AddDays(-2)),
                    new(FellingLicenceStatus.Withdrawn, _defaultNow.AddDays(-1))
                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft, 10),
                    new (FellingLicenceStatus.Submitted, 2),
                    new (FellingLicenceStatus.Received, 7),
                    new (FellingLicenceStatus.WithApplicant, 5),
                    new (FellingLicenceStatus.Withdrawn, 1)
                }
            },
            //when just a draft created earlier today, should be just 0 days
            new object[] {
                new List<HistoryRecordSetup> {
                    new(FellingLicenceStatus.Draft, _defaultNow.AddHours(-4))
                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft, 0)
                }
            },
            //when just a draft created exactly 24hrs since today, should be just 1 days
            new object[] {
                new List<HistoryRecordSetup> {
                    new(FellingLicenceStatus.Draft, _defaultNow.AddHours(-24))
                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft, 1)
                }
            },
            //when just a draft created exactly 24hrs minus a MS since today, should be just 1 days
            new object[] {
                new List<HistoryRecordSetup> {
                    new(FellingLicenceStatus.Draft, _defaultNow.AddHours(-24).AddMilliseconds(-1))
                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft, 1)
                }
            },
            //when just a draft created exactly 24hrs plus a MS since today, should still be 0 days
            new object[] {
                new List<HistoryRecordSetup> {
                    new(FellingLicenceStatus.Draft, _defaultNow.AddHours(-24).AddMilliseconds(1))
                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft, 0)
                }
            },
            //when status changes all in same day the correct durations per status should all be 0 days
            new object[] {
                new List<HistoryRecordSetup> {
                    new(FellingLicenceStatus.Draft, _defaultNow.AddHours(-4)),
                    new(FellingLicenceStatus.Submitted, _defaultNow.AddHours(-3)),
                    new(FellingLicenceStatus.Received, _defaultNow.AddHours(-2)),
                    new(FellingLicenceStatus.Withdrawn, _defaultNow.AddHours(-1))
                },
                new List<HistoryResultRecordAssertion>
                {
                    new (FellingLicenceStatus.Draft, 0),
                    new (FellingLicenceStatus.Submitted, 0),
                    new (FellingLicenceStatus.Received, 0),
                    new (FellingLicenceStatus.Withdrawn, 0)
                }
            }
        };
        return allData;
    }
}