using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Tests.Infrastructure;

namespace RaceDayApi.Tests;

/// <summary>The extra 2027 events give the MVC home page something to enter. They sit on top of the Part 1 data.</summary>
public class UpcomingEventsSeederTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    private Task SeedTwiceAsync() => factory.WithDbAsync(async db =>
    {
        await DemoDataSeeder.SeedAsync(db);
        await UpcomingEventsSeeder.SeedAsync(db);
        await UpcomingEventsSeeder.SeedAsync(db);
        return 0;
    });

    [Fact]
    public async Task UpcomingEvents_AreAddedOnce_AndAreAllInTheFuture()
    {
        await SeedTwiceAsync();

        var events = await factory.WithDbAsync(db => db.Events.Include(e => e.Categories).ToListAsync());

        // 4 events from the Part 1 script plus the 3 upcoming ones, even though the seeder ran twice.
        Assert.Equal(7, events.Count);
        var upcoming = events.Where(e => e.EventDate.Year == 2027).ToList();
        Assert.Equal(3, upcoming.Count);
        Assert.All(upcoming, e => Assert.True(e.EventDate > DateTime.Now));
        Assert.All(upcoming, e => Assert.NotEmpty(e.Categories));
    }

    [Fact]
    public async Task UpcomingEnrolments_FitTheirCategoryAgeRange()
    {
        await SeedTwiceAsync();

        var enrolments = await factory.WithDbAsync(db => db.Enrolments
            .Include(en => en.Category)
            .Include(en => en.Event)
            .Include(en => en.Participant)
            .Where(en => en.Event.EventDate.Year == 2027)
            .ToListAsync());

        Assert.Equal(2, enrolments.Count);
        foreach (var enrolment in enrolments)
        {
            var onDay = DateOnly.FromDateTime(enrolment.Event.EventDate);
            var age = onDay.Year - enrolment.Participant.DateOfBirth.Year;
            if (onDay < enrolment.Participant.DateOfBirth.AddYears(age))
            {
                age--;
            }

            Assert.True(enrolment.Category.MinAge is null || age >= enrolment.Category.MinAge);
            Assert.True(enrolment.Category.MaxAge is null || age <= enrolment.Category.MaxAge);
        }
    }
}
