using System;
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

        [Fact]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public Task Get_Event_By_Bad_Id()
        {
            return Utils.OnMainThread(async () =>
            {
                await Assert.ThrowsAsync<NullReferenceException>(() => Calendar.GetEventByIdAsync("ThisIsAFakeId"));
            });
        }
    }
}
