using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Appointments;

namespace Xamarin.Essentials
{
    static class CalendarRequest
    {
        static AppointmentStore uwpAppointmentStore;

        static AppointmentStoreAccessType lastRequestType;

        public static async System.Threading.Tasks.Task<AppointmentStore> GetInstanceAsync(AppointmentStoreAccessType type = AppointmentStoreAccessType.AppCalendarsReadWrite)
        {
            if (uwpAppointmentStore == null || lastRequestType != type)
            {
                uwpAppointmentStore = await AppointmentManager.RequestStoreAsync(type);
                lastRequestType = type;
            }

            return uwpAppointmentStore;
        }
    }
}
