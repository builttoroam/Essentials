﻿using System.Threading.Tasks;
using Xamarin.Essentials;
using Xunit;

namespace Tests
{
    public class Calendar_Tests
    {
        [Fact]
        public async Task Calendar_RequestCalendarReadAccess_Fail_On_NetStandard() => await Assert.ThrowsAsync<NotImplementedInReferenceAssemblyException>(() => Calendar.RequestCalendarReadAccess());

        [Fact]
        public async Task Calendar_RequestCalendarWriteAccess_Fail_On_NetStandard() => await Assert.ThrowsAsync<NotImplementedInReferenceAssemblyException>(() => Calendar.RequestCalendarWriteAccess());

        [Fact]
        public void Calendar_IsSupported_Fail_On_NetStandard() => Assert.Throws<NotImplementedInReferenceAssemblyException>(() => Calendar.IsSupported);

        [Fact]
        public async Task Calendar_Get_Calendar_List_Fail_On_NetStandard() => await Assert.ThrowsAsync<NotImplementedInReferenceAssemblyException>(() => Calendar.GetCalendarsAsync());

        [Fact]
        public async Task Calendar_Get_Event_By_Id_Fail_On_NetStandard() => await Assert.ThrowsAsync<NotImplementedInReferenceAssemblyException>(() => Calendar.GetEventByIdAsync("An ID"));

        [Fact]
        public async Task Calendar_Get_Events_Fail_On_NetStandard() => await Assert.ThrowsAsync<NotImplementedInReferenceAssemblyException>(() => Calendar.GetEventsAsync());
    }
}
