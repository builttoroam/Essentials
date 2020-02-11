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
                var calendarList = await Calendars.GetCalendarsAsync();
                Assert.NotNull(calendarList);
            });
        }

        [Fact]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public Task Get_Events_List()
        {
            return Utils.OnMainThread(async () =>
            {
                var eventList = await Calendars.GetEventsAsync();
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
                await Assert.ThrowsAsync<ArgumentException>(() => Calendars.GetEventsAsync(calendarId));
#else
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Calendars.GetEventsAsync(calendarId));
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
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Calendars.GetEventsAsync(calendarId));
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
                await Assert.ThrowsAsync<ArgumentException>(() => Calendars.GetEventByIdAsync(eventId));
            });
        }

        [Theory]
        [InlineData("-1")]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public Task Get_Event_By_Bad_Id(string eventId)
        {
            return Utils.OnMainThread(async () =>
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Calendars.GetEventByIdAsync(eventId));
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
                await Assert.ThrowsAsync<ArgumentException>(() => Calendars.GetEventByIdAsync(eventId));
#else
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Calendars.GetEventByIdAsync(eventId));
#endif
            });
        }

        [Fact]
        public Task Full_Calendar_Edit_Test()
        {
            return Utils.OnMainThread(async () =>
            {
                // Create Calendar
                var calendars = await Calendars.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new Calendar() { Name = "Test_Calendar" };
                    calendarId = await Calendars.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendars.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event");
                var eventId = string.Empty;
                if (newEvent == null)
                {
                    newEvent = new CalendarEvent()
                    {
                        Title = "Test_Event",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10)
                    };
                    eventId = await Calendars.CreateCalendarEvent(newEvent);
                }
                else
                {
                    eventId = newEvent.Id;
                }
                Assert.NotEmpty(eventId);
                var createdEvent = await Calendars.GetEventByIdAsync(eventId);
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

                var updateSuccessful = await Calendars.UpdateCalendarEvent(createdEvent);
                var updatedEvent = await Calendars.GetEventByIdAsync(createdEvent.Id);

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

                var attendeeToAddAndRemove = new CalendarEventAttendee() { Email = "fake@email.com", Name = "Fake Email", Type = AttendeeType.Resource };

                // Added Attendee to event successfully
                var attendeeAddedSuccessfully = await Calendars.AddAttendeeToEvent(attendeeToAddAndRemove, updatedEvent.Id);
                Assert.True(attendeeAddedSuccessfully);

                // Verify Attendee added to event
                updatedEvent = await Calendars.GetEventByIdAsync(createdEvent.Id);
                var expectedAttendeeCount = createdEvent.Attendees != null ? createdEvent.Attendees.Count() + 1 : 1;
                Assert.Equal(updatedEvent.Attendees.Count(), expectedAttendeeCount);

                // Remove Attendee from event
                var removedAttendeeSuccessfully = await Calendars.RemoveAttendeeFromEvent(attendeeToAddAndRemove, updatedEvent.Id);
                Assert.True(removedAttendeeSuccessfully);

                var dateOfSecondOccurence = TimeZoneInfo.ConvertTime(new DateTimeOffset(2020, 4, 9, 0, 0, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var eventInstance = await Calendars.GetEventInstanceByIdAsync(updatedEvent.Id, dateOfSecondOccurence);

                // Retrieve instance of event
                Assert.Equal(eventInstance.Id, updatedEvent.Id);
                Assert.Equal(eventInstance.StartDate.Date, dateOfSecondOccurence.Date);

                // Delete instance of event
                var canDeleteInstance = await Calendars.DeleteCalendarEventInstanceByDate(eventInstance.Id, calendarId, eventInstance.StartDate);
                Assert.True(canDeleteInstance);

                // Get whole event
                var eventStillExists = await Calendars.GetEventByIdAsync(eventInstance.Id);
                Assert.NotNull(eventStillExists);

                // Delete whole event
                var deleteEvent = await Calendars.DeleteCalendarEventById(eventInstance.Id, calendarId);
                Assert.True(deleteEvent);
            });
        }

        [Fact]
        public Task Basic_Calendar_Creation()
        {
            return Utils.OnMainThread(async () =>
            {
                var newCalendar = new Calendar() { Name = "Test_Calendar" };
                var calendarId = await Calendars.CreateCalendar(newCalendar);
                Assert.NotEmpty(calendarId);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Creation()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendars.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new Calendar() { Name = "Test_Calendar" };
                    calendarId = await Calendars.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var newEvent = new CalendarEvent()
                {
                    Title = "Test_Event",
                    CalendarId = calendarId,
                    StartDate = startDate,
                    EndDate = startDate.AddHours(10)
                };
                var eventId = await Calendars.CreateCalendarEvent(newEvent);
                Assert.NotEmpty(eventId);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Attendee_Add()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendars.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new Calendar() { Name = "Test_Calendar" };
                    calendarId = await Calendars.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendars.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event");
                var eventId = string.Empty;
                if (newEvent == null)
                {
                    newEvent = new CalendarEvent()
                    {
                        Title = "Test_Event",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10)
                    };
                    eventId = await Calendars.CreateCalendarEvent(newEvent);
                }
                else
                {
                    eventId = newEvent.Id;
                }
                var attendeeToAdd = new CalendarEventAttendee() { Email = "fake@email.com", Name = "Fake Out", Type = AttendeeType.Required };
                Assert.True(await Calendars.AddAttendeeToEvent(attendeeToAdd, eventId));

                newEvent = await Calendars.GetEventByIdAsync(eventId);
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
                var calendars = await Calendars.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new Calendar() { Name = "Test_Calendar" };
                    calendarId = await Calendars.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendars.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event");
                var eventId = string.Empty;
                if (newEvent == null)
                {
                    newEvent = new CalendarEvent()
                    {
                        Title = "Test_Event",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10)
                    };
                    eventId = await Calendars.CreateCalendarEvent(newEvent);
                }
                else
                {
                    eventId = newEvent.Id;
                }

                var attendeeCount = 0;
                CalendarEventAttendee attendeeToAdd = null;
                CalendarEventAttendee attendee = null;
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
                            attendeeToAdd = new CalendarEventAttendee() { Email = "fake@email.com", Name = "Fake Out", Type = AttendeeType.Required };
                            Assert.True(await Calendars.AddAttendeeToEvent(attendeeToAdd, eventId));
                            newEvent = await Calendars.GetEventByIdAsync(eventId);
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
                    attendeeToAdd = new CalendarEventAttendee() { Email = "fake@email.com", Name = "Fake Out", Type = AttendeeType.Required };
                    Assert.True(await Calendars.AddAttendeeToEvent(attendeeToAdd, eventId));
                    newEvent = await Calendars.GetEventByIdAsync(eventId);
                    attendeeCount = newEvent.Attendees.Count();
                    attendee = newEvent.Attendees.FirstOrDefault(x => x.Email == "fake@email.com");

                    Assert.Equal(attendee.Email, attendeeToAdd.Email);
                    Assert.Equal(attendee.Name, attendeeToAdd.Name);
                    Assert.Equal(attendee.IsOrganizer, attendeeToAdd.IsOrganizer);
                    Assert.Equal(attendee.Type, attendeeToAdd.Type);
                }
                Assert.True(await Calendars.RemoveAttendeeFromEvent(attendee, eventId));
                newEvent = await Calendars.GetEventByIdAsync(eventId);
                var newAttendeeCount = newEvent.Attendees.Count();

                Assert.Equal(attendeeCount - 1, newAttendeeCount);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Update()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendars.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new Calendar() { Name = "Test_Calendar" };
                    calendarId = await Calendars.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendars.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event");
                if (newEvent == null)
                {
                    newEvent = new CalendarEvent()
                    {
                        Title = "Test_Event",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10)
                    };
                    var eventId = await Calendars.CreateCalendarEvent(newEvent);
                    newEvent = await Calendars.GetEventByIdAsync(eventId);
                }
                else
                {
                    newEvent = await Calendars.GetEventByIdAsync(newEvent.Id);
                }

                newEvent.AllDay = true;

                var result = await Calendars.UpdateCalendarEvent(newEvent);
                Assert.True(result);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Recurrence_End_Update()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendars.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new Calendar() { Name = "Test_Calendar" };
                    calendarId = await Calendars.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendars.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event");
                if (newEvent == null)
                {
                    newEvent = new CalendarEvent()
                    {
                        Title = "Test_Event",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10),
                        RecurrancePattern = new RecurrenceRule()
                        {
                            Frequency = RecurrenceFrequency.Daily,
                            Interval = 1
                        }
                    };
                    var eventId = await Calendars.CreateCalendarEvent(newEvent);
                    newEvent = await Calendars.GetEventByIdAsync(eventId);
                }
                else
                {
                    newEvent = await Calendars.GetEventByIdAsync(newEvent.Id);
                }

                newEvent.AllDay = true;

                var result = await Calendars.SetEventRecurrenceEndDate(newEvent.Id, startDate.AddDays(12));
                Assert.True(result);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Deletion()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendars.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new Calendar() { Name = "Test_Calendar" };
                    calendarId = await Calendars.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendars.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event");
                var eventId = string.Empty;
                if (newEvent == null)
                {
                    newEvent = new CalendarEvent()
                    {
                        Title = "Test_Event",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10)
                    };
                    eventId = await Calendars.CreateCalendarEvent(newEvent);
                }
                else
                {
                    eventId = newEvent.Id;
                }
                var result = await Calendars.DeleteCalendarEventById(eventId, calendarId);

                Assert.True(result);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Instance_Creation()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendars.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new Calendar() { Name = "Test_Calendar" };
                    calendarId = await Calendars.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var events = await Calendars.GetEventsAsync(calendarId, startDate, startDate.AddHours(10));
                var newEvent = events.FirstOrDefault(x => x.Title == "Test_Event_Instance");
                CalendarEvent eventInstance;
                if (newEvent == null)
                {
                    newEvent = new CalendarEvent()
                    {
                        Title = "Test_Event_Instance",
                        CalendarId = calendarId,
                        StartDate = startDate,
                        EndDate = startDate.AddHours(10)
                    };
                    var eventId = await Calendars.CreateCalendarEvent(newEvent);
                    eventInstance = await Calendars.GetEventInstanceByIdAsync(eventId, startDate.AddDays(7));
                }
                else
                {
                    eventInstance = await Calendars.GetEventInstanceByIdAsync(newEvent.Id, startDate.AddDays(7));
                }

                Assert.NotEmpty(eventInstance.Id);
                Assert.Equal(eventInstance.StartDate.Date, startDate.AddDays(7).Date);
                Assert.Equal(eventInstance.EndDate.Value.Date, startDate.AddHours(10).AddDays(7).Date);
            });
        }

        [Fact]
        public Task Basic_Calendar_Event_Instance_Deletion()
        {
            return Utils.OnMainThread(async () =>
            {
                var calendars = await Calendars.GetCalendarsAsync();
                var calendar = calendars.FirstOrDefault(x => x.Name == "Test_Calendar");
                var calendarId = string.Empty;
                if (calendar == null)
                {
                    var newCalendar = new Calendar() { Name = "Test_Calendar" };
                    calendarId = await Calendars.CreateCalendar(newCalendar);
                }
                else
                {
                    calendarId = calendar.Id;
                }

                var startDate = TimeZoneInfo.ConvertTime(new DateTimeOffset(2019, 4, 1, 10, 30, 0, TimeZoneInfo.Local.BaseUtcOffset), TimeZoneInfo.Local);
                var newEvent = new CalendarEvent()
                {
                    Title = "Test_Event",
                    CalendarId = calendarId,
                    StartDate = startDate,
                    EndDate = startDate.AddHours(10),
                    RecurrancePattern = new RecurrenceRule() { Frequency = RecurrenceFrequency.Daily, Interval = 1 }
                };
                var eventId = await Calendars.CreateCalendarEvent(newEvent);

                var eventInstance = await Calendars.GetEventInstanceByIdAsync(eventId, startDate.AddDays(7));
                Assert.NotEmpty(eventInstance.Id);
                Assert.Equal(eventInstance.StartDate.Date, startDate.AddDays(7).Date);
                Assert.Equal(eventInstance.EndDate.Value.Date, startDate.AddHours(10).AddDays(7).Date);

                var deletionSuccessful = await Calendars.DeleteCalendarEventInstanceByDate(eventId, eventInstance.CalendarId, startDate.AddDays(7));
                Assert.True(deletionSuccessful);
            });
        }
    }
}
