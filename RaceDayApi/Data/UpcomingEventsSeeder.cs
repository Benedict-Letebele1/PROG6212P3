using Microsoft.EntityFrameworkCore;
using RaceDayApi.Domain;

namespace RaceDayApi.Data;

/// <summary>
/// Adds three upcoming 2027 events to the demo data. Every event in the Part 1 script has already taken place,
/// so without these the MVC home page would have nothing to enter. The Part 1 rows are left exactly as they are.
/// Each event is added only if no event with its name exists, so running this twice changes nothing.
/// </summary>
public static class UpcomingEventsSeeder
{
    public static async Task SeedAsync(RaceDayDbContext db)
    {
        var joburg = await db.Organisers.SingleOrDefaultAsync(o => o.OrganisationName == "Johannesburg Running Events");
        var capeTown = await db.Organisers.SingleOrDefaultAsync(o => o.OrganisationName == "Cape Town Cycling Tours");
        if (joburg is null || capeTown is null)
        {
            return;
        }

        var existing = await db.Events.Select(e => e.Name).ToListAsync();

        if (!existing.Contains("Joburg Spring Half Marathon"))
        {
            var half = NewEvent(joburg, "Joburg Spring Half Marathon",
                "A fast 21.1 km loop through Sandton and Rosebank, finishing at Zoo Lake. Water tables every 3 km.",
                new DateTime(2027, 3, 14, 6, 0, 0), "Johannesburg, Gauteng", 21.10m, EventTypes.Run);
            var open = NewCategory(half, "Open", "Ages 18 to 39", 18, 39);
            NewCategory(half, "Veterans", "Ages 40 and over", 40, null);
            db.Events.Add(half);

            // Thomas (born 1992) gets a confirmed entry, so his My enrolments page shows an upcoming event.
            var thomas = await db.Participants.SingleOrDefaultAsync(p => p.User.Email == "thomas.n@email.com");
            if (thomas is not null)
            {
                db.Enrolments.Add(new Enrolment
                {
                    Participant = thomas,
                    Event = half,
                    Category = open,
                    Status = EnrolmentStatuses.Confirmed,
                    RegistrationDate = DateTime.Now
                });
            }
        }

        if (!existing.Contains("Durban Beachfront Fun Walk"))
        {
            var walk = NewEvent(joburg, "Durban Beachfront Fun Walk",
                "A relaxed 5 km walk along the Golden Mile promenade. Families, prams and dogs on leads are welcome.",
                new DateTime(2027, 2, 20, 7, 30, 0), "Durban, KwaZulu-Natal", 5.00m, EventTypes.Walk);
            NewCategory(walk, "Family", "All ages", null, null);
            NewCategory(walk, "Junior", "Under 18", null, 17);
            db.Events.Add(walk);
        }

        if (!existing.Contains("Chapman's Peak Sunrise Ride"))
        {
            var ride = NewEvent(capeTown, "Chapman's Peak Sunrise Ride",
                "A 60 km coastal ride from Hout Bay over Chapman's Peak and back along the Atlantic seaboard.",
                new DateTime(2027, 4, 10, 6, 30, 0), "Cape Town, Western Cape", 60.00m, EventTypes.Cycle);
            NewCategory(ride, "Under 20", "Ages 16 to 19", 16, 19);
            var senior = NewCategory(ride, "Senior", "Ages 20 to 49", 20, 49);
            NewCategory(ride, "Masters", "Ages 50 and over", 50, null);
            db.Events.Add(ride);

            // Priya (born 1988) has a pending entry, so the organiser can confirm it in the demo.
            var priya = await db.Participants.SingleOrDefaultAsync(p => p.User.Email == "priya.s@email.com");
            if (priya is not null)
            {
                db.Enrolments.Add(new Enrolment
                {
                    Participant = priya,
                    Event = ride,
                    Category = senior,
                    Status = EnrolmentStatuses.Pending,
                    RegistrationDate = DateTime.Now
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static RaceEvent NewEvent(OrganiserProfile organiser, string name, string description, DateTime date, string location, decimal distance, string type) =>
        new()
        {
            Organiser = organiser,
            Name = name,
            Description = description,
            EventDate = date,
            Location = location,
            Distance = distance,
            EventType = type
        };

    private static RaceCategory NewCategory(RaceEvent raceEvent, string name, string description, int? minAge, int? maxAge)
    {
        var category = new RaceCategory { Event = raceEvent, Name = name, Description = description, MinAge = minAge, MaxAge = maxAge };
        raceEvent.Categories.Add(category);
        return category;
    }
}
