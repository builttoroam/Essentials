using System;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xunit;

namespace DeviceTests
{
    // TEST NOTES:
    //   - a human needs to accept permissions on all systems
    public class Calendar_Tests
    {
        [Fact]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public async Task Get_Calendar_List()
        {
            var calendarList = await Calendar.GetCalendarsAsync();

            Assert.NotNull(calendarList);
        }

        [Fact]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public async Task Get_Events_List()
        {
            var eventList = await Calendar.GetEventsAsync();

            Assert.NotNull(eventList);
        }

        [Fact]
        [Trait(Traits.InteractionType, Traits.InteractionTypes.Human)]
        public async Task Get_Event_By_Bad_Id()
        {
            var evnt = await Calendar.GetEventByIdAsync("ThisIsAFakeId");

            Assert.NotNull(evnt);
        }
    }
}
