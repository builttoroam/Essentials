using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xunit;

namespace DeviceTests
{
    // TEST NOTES:
    //   - a human needs to accept permissions on all systems
    //  If no calendars are set up none will be returned at this stage
    //  Same goes for events
    public class Calendar_Tests
    {
        [Fact]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public Task Get_Calendar_List()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendarList = await Calendar.GetCalendarsAsync();
                Assert.NotNull(calendarList);
            });
        }

        [Fact]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public Task Get_Events_List()
        {
            return Utils.OnMainThread(async () =>
            {
                var eventList = await Calendar.GetEventsAsync();
                Assert.NotNull(eventList);
            });
        }

        [Theory]
        [InlineData("ThisIsAFakeId")]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public Task Get_Events_By_Bad_Calendar_Text_Id(string calendarId)
        {
            return Utils.OnMainThread(async () =>
            {
#if __ANDROID__
                await Assert.ThrowsAsync<ArgumentException>(() => Calendar.GetEventsAsync(calendarId));
#else
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Calendar.GetEventsAsync(calendarId));
#endif
            });
        }

        [Theory]
        [InlineData("-1")]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public Task Get_Events_By_Bad_Calendar_Id(string calendarId)
        {
            return Utils.OnMainThread(async () =>
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Calendar.GetEventsAsync(calendarId));
            });
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public Task Get_Event_By_Blank_Id(string eventId)
        {
            return Utils.OnMainThread(async () =>
            {
                await Assert.ThrowsAsync<ArgumentException>(() => Calendar.GetEventByIdAsync(eventId));
            });
        }

        [Theory]
        [InlineData("-1")]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public Task Get_Event_By_Bad_Id(string eventId)
        {
            return Utils.OnMainThread(async () =>
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Calendar.GetEventByIdAsync(eventId));
            });
        }

        [Theory]
        [InlineData("ThisIsAFakeId")]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public Task Get_Event_By_Bad_Text_Id(string eventId)
        {
            return Utils.OnMainThread(async () =>
            {
#if __ANDROID__
                await Assert.ThrowsAsync<ArgumentException>(() => Calendar.GetEventByIdAsync(eventId));
#else
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Calendar.GetEventByIdAsync(eventId));
#endif
            });
        }

        [Fact]
        public Task Full_Calendar_Edit_Test()
        {
            return Utils.OnMainThread(async () =>
            {
                // Create Calendar
                var calendars = await Calendar.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new DeviceCalendar() { Name = "Test_Calendar" };
                    calendarId = await Calendar.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendar.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event");
                var eventId = string.Empty;
                if (newEvent == null)
                {
                    newEvent = new DeviceEvent()
                    {
                        Title = "Test_Event",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10)
                    };
                    eventId = await Calendar.CreateCalendarEvent(newEvent);
                }
                else
                {
                    eventId = newEvent.Id;
                }
                Assert.NotEmpty(eventId);
                var createdEvent = await Calendar.GetEventByIdAsync(eventId);
                newEvent.Id = createdEvent.Id;
                newEvent.Attendees = createdEvent.Attendees;

                Assert.Equal(newEvent.Id, createdEvent.Id);
                Assert.Equal(newEvent.CalendarId, createdEvent.CalendarId);
                Assert.Equal(newEvent.Title, createdEvent.Title);
                Assert.Equal(string.Empty, createdEvent.Description);
                Assert.Equal(string.Empty, createdEvent.Location);
                Assert.Equal(string.Empty, createdEvent.Url);
                Assert.Equal(newEvent.AllDay, createdEvent.AllDay);
                Assert.Equal(newEvent.StartDate, createdEvent.StartDate);
                Assert.Equal(newEvent.Duration, createdEvent.Duration);
                Assert.Equal(newEvent.EndDate, createdEvent.EndDate);
                Assert.Equal(newEvent.Attendees, createdEvent.Attendees);
                Assert.Equal(newEvent.Reminders, createdEvent.Reminders);
                Assert.Equal(newEvent.RecurrancePattern, createdEvent.RecurrancePattern);

                createdEvent.RecurrancePattern = new RecurrenceRule()
                {
                    Frequency = RecurrenceFrequency.YearlyOnDay,
                    Interval = 1,
                    WeekOfMonth = IterationOffset.Second,
                    DaysOfTheWeek = new List<DayOfTheWeek>() { DayOfTheWeek.Thursday },
                    MonthOfTheYear = MonthOfYear.April,
                    TotalOccurrences = 4
                };
                createdEvent.AllDay = true;

                var updateSuccessful = await Calendar.UpdateCalendarEvent(createdEvent);
                var updatedEvent = await Calendar.GetEventByIdAsync(createdEvent.Id);

                // Updated Successfuly
                Assert.True(updateSuccessful);
                Assert.Equal(createdEvent.Id, updatedEvent.Id);
                Assert.Equal(createdEvent.CalendarId, updatedEvent.CalendarId);
                Assert.Equal(createdEvent.Title, updatedEvent.Title);
                Assert.Equal(createdEvent.Description, updatedEvent.Description);
                Assert.Equal(createdEvent.Location, updatedEvent.Location);
                Assert.Equal(createdEvent.Url, updatedEvent.Url);
                Assert.Equal(createdEvent.AllDay, updatedEvent.AllDay);
                Assert.NotEqual(createdEvent.StartDate, updatedEvent.StartDate);
                Assert.Equal(createdEvent.Attendees, updatedEvent.Attendees);
                Assert.Equal(createdEvent.Reminders, updatedEvent.Reminders);

                var attendeeToAddAndRemove = new DeviceEventAttendee() { Email = "fake@email.com", Name = "Fake Email", Type = AttendeeType.Resource };

                // Added Attendee to event successfully
                var attendeeAddedSuccessfully = await Calendar.AddAttendeeToEvent(attendeeToAddAndRemove, updatedEvent.Id);
                Assert.True(attendeeAddedSuccessfully);

                // Verify Attendee added to event
                updatedEvent = await Calendar.GetEventByIdAsync(createdEvent.Id);
                var expectedAttendeeCount = createdEvent.Attendees != null ? createdEvent.Attendees.Count() + 1 : 1;
                Assert.Equal(updatedEvent.Attendees.Count(), expectedAttendeeCount);

                // Remove Attendee from event
                var removedAttendeeSuccessfully = await Calendar.RemoveAttendeeFromEvent(attendeeToAddAndRemove, updatedEvent.Id);
                Assert.True(removedAttendeeSuccessfully);

                var dateOfSecondOccurence = TimeZoneInfo.ConvertTime(new DateTimeOffset(2020, 4, 9, 0, 0, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var eventInstance = await Calendar.GetEventInstanceByIdAsync(updatedEvent.Id, dateOfSecondOccurence);

                // Retrieve instance of event
                Assert.Equal(eventInstance.Id, updatedEvent.Id);
                Assert.Equal(eventInstance.StartDate.Date, dateOfSecondOccurence.Date);

                // Delete instance of event
                var canDeleteInstance = await Calendar.DeleteCalendarEventInstanceByDate(eventInstance.Id, calendarId, eventInstance.StartDate);
                Assert.True(canDeleteInstance);

                // Get whole event
                var eventStillExists = await Calendar.GetEventByIdAsync(eventInstance.Id);
                Assert.NotNull(eventStillExists);

                // Delete whole event
                var deleteEvent = await Calendar.DeleteCalendarEventById(eventInstance.Id, calendarId);
                Assert.True(deleteEvent);
            });
        }

        [Fact]
        public Task Basic_Calendar_Creation()
        {
            return Utils.OnMainThread(async () =>
            {
                var newCalendar = new DeviceCalendar() { Name = "Test_Calendar" };
                var calendarId = await Calendar.CreateCalendar(newCalendar);
                Assert.NotEmpty(calendarId);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Creation()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendar.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new DeviceCalendar() { Name = "Test_Calendar" };
                    calendarId = await Calendar.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var newEvent = new DeviceEvent()
                {
                    Title = "Test_Event",
                    CalendarId = calendarId,
                    StartDate = startDate,
                    EndDate = startDate.AddHours(10)
                };
                var eventId = await Calendar.CreateCalendarEvent(newEvent);
                Assert.NotEmpty(eventId);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Attendee_Add()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendar.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new DeviceCalendar() { Name = "Test_Calendar" };
                    calendarId = await Calendar.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendar.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event");
                var eventId = string.Empty;
                if (newEvent == null)
                {
                    newEvent = new DeviceEvent()
                    {
                        Title = "Test_Event",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10)
                    };
                    eventId = await Calendar.CreateCalendarEvent(newEvent);
                }
                else
                {
                    eventId = newEvent.Id;
                }
                var attendeeToAdd = new DeviceEventAttendee() { Email = "fake@email.com", Name = "Fake Out", Type = AttendeeType.Required };
                Assert.True(await Calendar.AddAttendeeToEvent(attendeeToAdd, eventId));

                newEvent = await Calendar.GetEventByIdAsync(eventId);
                var attendee = newEvent.Attendees.FirstOrDefault(x => x.Email == "fake@email.com");

                Assert.Equal(attendee.Email, attendeeToAdd.Email);
                Assert.Equal(attendee.Name, attendeeToAdd.Name);
                Assert.Equal(attendee.IsOrganizer, attendeeToAdd.IsOrganizer);
                Assert.Equal(attendee.Type, attendeeToAdd.Type);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Attendee_Remove()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendar.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new DeviceCalendar() { Name = "Test_Calendar" };
                    calendarId = await Calendar.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendar.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event");
                var eventId = string.Empty;
                if (newEvent == null)
                {
                    newEvent = new DeviceEvent()
                    {
                        Title = "Test_Event",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10)
                    };
                    eventId = await Calendar.CreateCalendarEvent(newEvent);
                }
                else
                {
                    eventId = newEvent.Id;
                }

                var attendeeCount = 0;
                DeviceEventAttendee attendeeToAdd = null;
                DeviceEventAttendee attendee = null;
                if (newEvent.Attendees != null)
                {
                    if (newEvent.Attendees.Count() > 0)
                    {
                        attendeeToAdd = newEvent.Attendees.FirstOrDefault(x => x.Email == "fake@email.com");
                        if (attendeeToAdd != null)
                        {
                            attendeeCount = newEvent.Attendees.Count();
                            attendee = attendeeToAdd;
                        }
                        else
                        {
                            attendeeToAdd = new DeviceEventAttendee() { Email = "fake@email.com", Name = "Fake Out", Type = AttendeeType.Required };
                            Assert.True(await Calendar.AddAttendeeToEvent(attendeeToAdd, eventId));
                            newEvent = await Calendar.GetEventByIdAsync(eventId);
                            attendeeCount = newEvent.Attendees.Count();
                            attendee = newEvent.Attendees.FirstOrDefault(x => x.Email == "fake@email.com");

                            Assert.Equal(attendee.Email, attendeeToAdd.Email);
                            Assert.Equal(attendee.Name, attendeeToAdd.Name);
                            Assert.Equal(attendee.IsOrganizer, attendeeToAdd.IsOrganizer);
                            Assert.Equal(attendee.Type, attendeeToAdd.Type);
                        }
                    }
                }
                else
                {
                    attendeeToAdd = new DeviceEventAttendee() { Email = "fake@email.com", Name = "Fake Out", Type = AttendeeType.Required };
                    Assert.True(await Calendar.AddAttendeeToEvent(attendeeToAdd, eventId));
                    newEvent = await Calendar.GetEventByIdAsync(eventId);
                    attendeeCount = newEvent.Attendees.Count();
                    attendee = newEvent.Attendees.FirstOrDefault(x => x.Email == "fake@email.com");

                    Assert.Equal(attendee.Email, attendeeToAdd.Email);
                    Assert.Equal(attendee.Name, attendeeToAdd.Name);
                    Assert.Equal(attendee.IsOrganizer, attendeeToAdd.IsOrganizer);
                    Assert.Equal(attendee.Type, attendeeToAdd.Type);
                }
                Assert.True(await Calendar.RemoveAttendeeFromEvent(attendee, eventId));
                newEvent = await Calendar.GetEventByIdAsync(eventId);
                var newAttendeeCount = newEvent.Attendees.Count();

                Assert.Equal(attendeeCount - 1, newAttendeeCount);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Update()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendar.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new DeviceCalendar() { Name = "Test_Calendar" };
                    calendarId = await Calendar.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendar.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event");
                if (newEvent == null)
                {
                    newEvent = new DeviceEvent()
                    {
                        Title = "Test_Event",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10)
                    };
                    var eventId = await Calendar.CreateCalendarEvent(newEvent);
                    newEvent = await Calendar.GetEventByIdAsync(eventId);
                }
                else
                {
                    newEvent = await Calendar.GetEventByIdAsync(newEvent.Id);
                }

                newEvent.AllDay = true;

                var result = await Calendar.UpdateCalendarEvent(newEvent);
                Assert.True(result);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Deletion()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendar.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new DeviceCalendar() { Name = "Test_Calendar" };
                    calendarId = await Calendar.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendar.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event");
                var eventId = string.Empty;
                if (newEvent == null)
                {
                    newEvent = new DeviceEvent()
                    {
                        Title = "Test_Event",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10)
                    };
                    eventId = await Calendar.CreateCalendarEvent(newEvent);
                }
                else
                {
                    eventId = newEvent.Id;
                }
                var result = await Calendar.DeleteCalendarEventById(eventId, calendarId);

                Assert.True(result);
            });
        }

        // [Fact]
        // public Task Basic_Calendar_Event_Instance_Deletion()
        // {
        //     return Utils.OnMainThread(async () =>
        //     {
        //         var newCalendar = new DeviceCalendar() { Name = "TestBasicEventUpdate" };
        //         var calendarId = await Calendar.CreateCalendar(newCalendar);
        //
        //         var dateTodelete = new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset);
        //
        //         var newEvent = new DeviceEvent()
        //         {
        //             Title = "TestBasicEventName",
        //             CalendarId = calendarId,
        //             StartDate = dateTodelete,
        //             EndDate = dateTodelete.AddHours(10)
        //         };
        //         var eventId = await Calendar.CreateCalendarEvent(newEvent);
        //         var result = await Calendar.DeleteCalendarEventInstanceByDate(eventId, calendarId, dateTodelete);
        //
        //         Assert.True(result);
        //     });
        // }
    }
}
