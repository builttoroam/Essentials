using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.ViewModel
{
    public class CalendarEventAddAttendeeViewModel : BaseViewModel
    {
        public CalendarEventAddAttendeeViewModel(string eventId, string eventName)
        {
            EventId = eventId;
            EventName = eventName;
            CreateCalendarEventAttendee = new Command(CreateCalendarEventAttendeeCommand);
        }

        public string EventName { get; set; }

        public string EventId { get; set; }

        string name;

        public string Name
        {
            get => name;
            set
            {
                if (SetProperty(ref name, value))
                {
                    OnPropertyChanged(nameof(CanCreateAttendee));
                }
            }
        }

        string emailAddress;

        public string EmailAddress
        {
            get => emailAddress;
            set
            {
                if (SetProperty(ref emailAddress, value))
                {
                    OnPropertyChanged(nameof(CanCreateAttendee));
                }
            }
        }

        bool required;

        public bool Required
        {
            get => required;
            set => SetProperty(ref required, value);
        }

        bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public bool CanCreateAttendee => IsValidEmail(EmailAddress) && !string.IsNullOrWhiteSpace(Name);

        public ICommand CreateCalendarEventAttendee { get; }

        async void CreateCalendarEventAttendeeCommand()
        {
            var newAttendee = new DeviceEventAttendee()
            {
                Name = Name,
                Email = EmailAddress
            };

            var result = await Calendar.AddAttendeeToEvent(newAttendee, EventId);

            await DisplayAlertAsync("Added event attendee: " + newAttendee.Name);
        }
    }
}
