﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Spatial;
using Xunit;

namespace Simple.OData.Client.Tests
{
#if ODATA_V4
    public class TripPinTestsV4Json : TripPinTests
    {
        public TripPinTestsV4Json() : base(TripPinV4ReadWriteUri, ODataPayloadFormat.Json) { }
    }
#endif

    public abstract class TripPinTests : TripPinTestBase
    {
        protected TripPinTests(string serviceUri, ODataPayloadFormat payloadFormat) : base(serviceUri, payloadFormat) { }

        [Fact]
        public async Task FindAllPeople()
        {
            var annotations = new ODataFeedAnnotations();

            int count = 0;
            var people = await _client
                .For<Person>()
                .FindEntriesAsync(annotations);
            count += people.Count();

            while (annotations.NextPageLink != null)
            {
                people = await _client
                    .For<Person>()
                    .FindEntriesAsync(annotations.NextPageLink, annotations);
                count += people.Count();
            }

            Assert.Equal(count, annotations.Count);
        }

        [Fact]
        public async Task FindPersonExpandTripsAndFriends()
        {
            var person = await _client
                .For<Person>()
                .Key("russellwhyte")
                .Expand(x => new { x.Trips, x.Friends })
                .FindEntryAsync();
            Assert.Equal(3, person.Trips.Count());
            Assert.Equal(4, person.Friends.Count());
        }

        [Fact]
        public async Task FindPersonExpandFriendsWithOrderBy()
        {
            var person = await _client
                .For("People")
                .Key("russellwhyte")
                .Expand("Friends")
                .OrderBy("Friends/LastName")
                .FindEntryAsync();
            //Assert.Equal(3, person.Trips.Count());
            //Assert.Equal(4, person.Friends.Count());
        }

        [Fact]
        public async Task FindPersonPlanItems()
        {
            var flights = await _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo(x => x.Trips)
                .Key(1003)
                .NavigateTo(x => x.PlanItems)
                .FindEntriesAsync();
            Assert.Equal(3, flights.Count());
        }

        [Fact]
        public async Task FindPersonWithAnyTrips()
        {
            var flights = await _client
                .For<Person>()
                .Filter(x => x.Trips
                    .Any(y => y.Budget > 10000d))
                .Expand(x => x.Trips)
                .FindEntriesAsync();
            Assert.True(flights.All(x => x.Trips.Any(y => y.Budget > 10000d)));
            Assert.Equal(2, flights.SelectMany(x => x.Trips).Count());
        }

        [Fact]
        public async Task FindPersonWithAllTrips()
        {
            var flights = await _client
                .For<Person>()
                .Filter(x => x.Trips
                    .All(y => y.Budget > 10000d))
                .Expand(x => x.Trips)
                .FindEntriesAsync();
            Assert.True(flights.All(x => x.Trips == null || x.Trips.All(y => y.Budget > 10000d)));
        }

        [Fact]
        public async Task FindPersonPlanItemsWithAllTripsAnyPlanItems()
        {
            var duration = TimeSpan.FromHours(4);
            var flights = await _client
                .For<Person>()
                .Filter(x => x.Trips
                    .All(y => y.PlanItems
                        .Any(z => z.Duration < duration)))
                .FindEntriesAsync();
            Assert.Equal(8, flights.Count());
        }

        [Fact]
        public async Task FindPersonFlight()
        {
            var flight = await _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo(x => x.Trips)
                .Key(1003)
                .NavigateTo(x => x.PlanItems)
                .Key(21)
                .As<Flight>()
                .FindEntryAsync();
            Assert.Equal("FM1930", flight.FlightNumber);
        }

        [Fact]
        public async Task FindPersonFlightExpandAndSelect()
        {
            var flight = await _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo(x => x.Trips)
                .Key(1003)
                .NavigateTo(x => x.PlanItems)
                .Key(21)
                .As<Flight>()
                .Expand(x => x.Airline)
                .Select(x => new { x.FlightNumber, x.Airline.AirlineCode})
                .FindEntryAsync();
            Assert.Equal("FM", flight.Airline.AirlineCode);
        }

        [Fact]
        public async Task FindPersonFlights()
        {
            var flights = await _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo(x => x.Trips)
                .Key(1003)
                .NavigateTo(x => x.PlanItems)
                .As<Flight>()
                .FindEntriesAsync();
            Assert.Equal(2, flights.Count());
            Assert.True(flights.Any(x => x.FlightNumber == "FM1930"));
        }

        [Fact]
        public async Task FindPersonFlightsWithFilter()
        {
            var flights = await _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo(x => x.Trips)
                .Key(1003)
                .NavigateTo(x => x.PlanItems)
                .As<Flight>()
                .Filter(x => x.FlightNumber == "FM1930")
                .FindEntriesAsync();
            Assert.Equal(1, flights.Count());
            Assert.True(flights.All(x => x.FlightNumber == "FM1930"));
        }

        [Fact]
        public async Task UpdatePersonLastName()
        {
            var person = await _client
                .For<Person>()
                .Filter(x => x.UserName == "russellwhyte")
                .Set(new { LastName = "White" })
                .UpdateEntryAsync();
            Assert.Equal("White", person.LastName);
        }

        [Fact]
        public async Task UpdatePersonEmail()
        {
            var person = await _client
                .For<Person>()
                .Filter(x => x.UserName == "russellwhyte")
                .Set(new { Emails = new[] { "russell.whyte@gmail.com" } })
                .UpdateEntryAsync();
            Assert.Equal("russell.whyte@gmail.com", person.Emails.First());
        }

        [Fact]
        public async Task UpdatePersonAddress()
        {
            var person = await _client
                .For<Person>()
                .Filter(x => x.UserName == "russellwhyte")
                .Set(new
                {
                    AddressInfo = new[]
                    {
                        new Location()
                        {
                            Address = "187 Suffolk Ln.",
                            City = new Location.LocationCity()
                            {
                                CountryRegion = "United States", 
                                Name = "Boise", 
                                Region = "ID"
                            }
                        }
                    },
                })
                .UpdateEntryAsync();
            Assert.Equal("Boise", person.AddressInfo.First().City.Name);
        }

        [Fact]
        public async Task FindMe()
        {
            var person = await _client
                .For<Person>("Me")
                .FindEntryAsync();
            Assert.Equal("aprilcline", person.UserName);
            Assert.Equal(2, person.Emails.Count());
            Assert.Equal("Lander", person.AddressInfo.Single().City.Name);
            Assert.Equal(PersonGender.Female, person.Gender);
        }

        [Fact]
        public async Task FindMeSelectAddressInfo()
        {
            var person = await _client
                .For<Person>("Me")
                .Select(x => x.AddressInfo)
                .FindEntryAsync();
            Assert.Equal("Lander", person.AddressInfo.Single().City.Name);
            Assert.Null(person.UserName);
            Assert.Null(person.Emails);
        }

        [Fact]
        public async Task UpdateMeGender_PreconditionRequired()
        {
            AssertThrowsAsync<AggregateException>(async () =>
            {
                await _client
                    .For<Person>("Me")
                    .Set(new { Gender = PersonGender.Male })
                    .UpdateEntryAsync();
            });
        }

        //[Fact]
        //public async Task UpdateMe_LastName_PreconditionRequired()
        //{
        //    var person = await _client
        //        .For<Person>("Me")
        //        .Set(new { LastName = "newname" })
        //        .UpdateEntryAsync();
        //    Assert.Equal("newname", person.LastName);
        //}

        [Fact]
        public async Task FindAllAirlines()
        {
            var airlines = await _client
                .For<Airline>()
                .FindEntriesAsync();
            Assert.Equal(8, airlines.Count());
        }

        [Fact]
        public async Task FindAllAirports()
        {
            var airports = await _client
                .For<Airport>()
                .FindEntriesAsync();
            Assert.Equal(8, airports.Count());
        }

        [Fact]
        public async Task FindAirportByCode()
        {
            var airport = await _client
                .For<Airport>()
                .Key("KSFO")
                .FindEntryAsync();
            Assert.Equal("SFO", airport.IataCode);
            Assert.Equal("San Francisco", airport.Location.City.Name);
            Assert.Equal(4326, airport.Location.Loc.CoordinateSystem.EpsgId);
            Assert.Equal(37.6188888888889, airport.Location.Loc.Latitude);
            Assert.Equal(-122.374722222222, airport.Location.Loc.Longitude);
        }

        [Fact]
        public async Task InsertEvent()
        {
            var command = _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo<Trip>()
                .Key(1003)
                .NavigateTo(x => x.PlanItems)
                .As<Event>();

            var tripEvent = await command
                .Set(CreateTestEvent())
                .InsertEntryAsync();

            tripEvent = await command
                .Key(tripEvent.PlanItemId)
                .FindEntryAsync();

            Assert.NotNull(tripEvent);
        }

        [Fact]
        public async Task UpdateEvent()
        {
            var command = _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo<Trip>()
                .Key(1003)
                .NavigateTo(x => x.PlanItems)
                .As<Event>();

            var tripEvent = await command
                .Set(CreateTestEvent())
                .InsertEntryAsync();

            tripEvent = await command
                .Key(tripEvent.PlanItemId)
                .Set(new { Description = "This is a new description" })
                .UpdateEntryAsync();

            Assert.Equal("This is a new description", tripEvent.Description);
        }

        [Fact]
        public async Task DeleteEvent()
        {
            var command = _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo<Trip>()
                .Key(1003)
                .NavigateTo(x => x.PlanItems)
                .As<Event>();

            var tripEvent = await command
                .Set(CreateTestEvent())
                .InsertEntryAsync();

            await command
                .Key(tripEvent.PlanItemId)
                .DeleteEntryAsync();

            tripEvent = await command
                .Key(tripEvent.PlanItemId)
                .FindEntryAsync();

            Assert.Null(tripEvent);
        }

        [Fact]
        public async Task FindPersonTrips()
        {
            var trips = await _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo<Trip>()
                .FindEntriesAsync();

            Assert.Equal(3, trips.Count());
        }

        [Fact]
        public async Task FindPersonTripsFilterDescription()
        {
            var trips = await _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo<Trip>()
                .Filter(x => x.Description.Contains("New York"))
                .FindEntriesAsync();

            Assert.Equal(1, trips.Count());
            Assert.Contains("New York", trips.Single().Description);
        }

        [Fact]
        public async Task GetNearestAirport()
        {
            var airport = await _client
                .Unbound<Airport>()
                .Function("GetNearestAirport")
                .Set(new { lat = 100d, lon = 100d })
                .ExecuteAsSingleAsync();

            Assert.Equal("KSEA", airport.IcaoCode);
        }

        [Fact]
        public async Task ResetDataSource()
        {
            var command = _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo<Trip>()
                .Key(1003)
                .NavigateTo(x => x.PlanItems)
                .As<Event>();

            var tripEvent = await command
                .Set(CreateTestEvent())
                .InsertEntryAsync();

            await _client
                .Unbound()
                .Action("ResetDataSource")
                .ExecuteAsync();

            tripEvent = await command
                .Filter(x => x.PlanItemId == tripEvent.PlanItemId)
                .FindEntryAsync();

            Assert.Null(tripEvent);
        }

        [Fact]
        public async Task ShareTrip()
        {
            await _client
                .For<Person>()
                .Key("russellwhyte")
                .Action("ShareTrip")
                .Set(new { userName = "scottketchum", tripId = 1003 })
                .ExecuteAsSingleAsync();
        }

        [Fact]
        public async Task GetInvolvedPeople()
        {
            var people = await _client
                .For<Person>()
                .Key("scottketchum")
                .NavigateTo<Trip>()
                .Key(0)
                .Function("GetInvolvedPeople")
                .ExecuteAsEnumerableAsync();
            Assert.Equal(2, people.Count());
        }

        [Fact]
        public async Task Batch()
        {
            IEnumerable<Airline> airlines1 = null;
            IEnumerable<Airline> airlines2 = null;

            var batch = new ODataBatch(_client);
            batch += async c => airlines1 = await c
               .For<Airline>()
               .FindEntriesAsync();
            batch += c => c
               .For<Airline>()
               .Set(new Airline() { AirlineCode = "TT", Name = "Test Airline"})
               .InsertEntryAsync(false);
            batch += async c => airlines2 = await c
               .For<Airline>()
               .FindEntriesAsync();
            await batch.ExecuteAsync();

            Assert.Equal(8, airlines1.Count());
            Assert.Equal(8, airlines2.Count());
        }

        private Event CreateTestEvent()
        {
            return new Event
            {
                ConfirmationCode = "4372899DD",
                Description = "Client Meeting",
                Duration = TimeSpan.FromHours(3),
                EndsAt = DateTimeOffset.Parse("2014-06-01T23:11:17.5479185-07:00"),
                OccursAt = new EventLocation()
                {
                    Address = "100 Church Street, 8th Floor, Manhattan, 10007",
                    BuildingInfo = "Regus Business Center",
                    City = new Location.LocationCity()
                    {
                        CountryRegion = "United States",
                        Name = "New York City",
                        Region = "New York",
                    }
                },
                PlanItemId = 33,
                StartsAt = DateTimeOffset.Parse("2014-05-25T23:11:17.5459178-07:00"),
            };
        }
    }
}